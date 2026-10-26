using HonestTimeTracker.Application.Tfs;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace HonestTimeTracker.Infrastructure.Tfs;

/// Azure DevOps Server REST client authenticated with a personal access token.
public class TfsClient(HttpClient http, ILogger<TfsClient> logger) : ITfsClient
{
    public const string ApiVersion = "7.1";
    internal const int BatchSize = 200;
    private const string ParentRelation = "System.LinkTypes.Hierarchy-Reverse";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<string> GetAuthenticatedUserAsync(TfsConnection connection, CancellationToken ct)
    {
        var data = await SendAsync<ConnectionDataResponse>(connection, HttpMethod.Get, "_apis/connectionData", null, ct);
        var user = data.AuthenticatedUser;
        return user?.ProviderDisplayName ?? user?.CustomDisplayName
            ?? throw InvalidResponse("connectionData response does not contain the authenticated user.");
    }

    public async Task<IReadOnlyList<int>> QueryWorkItemIdsAsync(TfsConnection connection, string wiql, CancellationToken ct)
    {
        var response = await SendAsync<WiqlResponse>(connection, HttpMethod.Post,
            $"_apis/wit/wiql?api-version={ApiVersion}", new { query = wiql }, ct);
        return response.WorkItems?.Select(w => w.Id).ToList() ?? [];
    }

    public async Task<IReadOnlyList<TfsWorkItem>> GetWorkItemsAsync(
        TfsConnection connection, IReadOnlyCollection<int> ids, bool includeRelations, CancellationToken ct)
    {
        var result = new List<TfsWorkItem>(ids.Count);
        foreach (var batch in ids.Distinct().Chunk(BatchSize))
        {
            var url = $"_apis/wit/workitems?ids={string.Join(',', batch)}&errorPolicy=omit&api-version={ApiVersion}";
            if (includeRelations)
                url += "&$expand=relations";

            var response = await SendAsync<WorkItemsResponse>(connection, HttpMethod.Get, url, null, ct);
            // With errorPolicy=omit, missing or inaccessible items come back as null entries.
            result.AddRange(response.Value?.OfType<WorkItemResponse>().Select(Map) ?? []);
        }
        return result;
    }

    public async Task<IReadOnlyList<TfsWorkItemTypeState>> GetWorkItemTypeStatesAsync(
        TfsConnection connection, string teamProject, string workItemType, CancellationToken ct)
    {
        var url = $"{Uri.EscapeDataString(teamProject)}/_apis/wit/workitemtypes/{Uri.EscapeDataString(workItemType)}/states?api-version={ApiVersion}";
        var response = await SendAsync<StatesResponse>(connection, HttpMethod.Get, url, null, ct);
        return response.Value?
            .Where(s => !string.IsNullOrEmpty(s.Name))
            .Select(s => new TfsWorkItemTypeState(s.Name!, s.Category))
            .ToList() ?? [];
    }

    private TfsWorkItem Map(WorkItemResponse item)
    {
        var fields = item.Fields ?? [];
        return new TfsWorkItem(
            item.Id,
            GetString(fields, "System.Title"),
            GetString(fields, "System.WorkItemType"),
            GetString(fields, "System.State"),
            GetString(fields, "System.TeamProject"),
            GetParentId(item));
    }

    private int? GetParentId(WorkItemResponse item)
    {
        var parent = item.Relations?.FirstOrDefault(r => r.Rel == ParentRelation);
        if (parent?.Url is null) return null;

        var idText = parent.Url[(parent.Url.LastIndexOf('/') + 1)..];
        if (int.TryParse(idText, out var id)) return id;

        logger.LogWarning("Cannot parse parent work item ID from relation URL {Url} of work item {WorkItemId}", parent.Url, item.Id);
        return null;
    }

    private static string? GetString(Dictionary<string, JsonElement> fields, string name) =>
        fields.TryGetValue(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private async Task<T> SendAsync<T>(TfsConnection connection, HttpMethod method, string relativeUrl, object? body, CancellationToken ct)
        where T : class
    {
        var url = $"{connection.Url.TrimEnd('/')}/{relativeUrl}";
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.ASCII.GetBytes(":" + connection.Pat)));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (body is not null)
            request.Content = JsonContent.Create(body, options: JsonOptions);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, ct);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "TFS request {Method} {Url} timed out", method, url);
            throw new TfsException(TfsErrorKind.Unreachable, "Request timed out.", ex);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "TFS request {Method} {Url} failed", method, url);
            throw new TfsException(TfsErrorKind.Unreachable, $"Cannot connect to the server: {ex.Message}", ex);
        }

        using (response)
        {
            var content = await response.Content.ReadAsStringAsync(ct);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                throw new TfsException(TfsErrorKind.Unauthorized, $"Access denied ({(int)response.StatusCode}). The PAT may be invalid, expired or missing the Work Items (Read) scope.");

            if (!response.IsSuccessStatusCode)
                throw new TfsException(TfsErrorKind.Other,
                    $"TFS returned {(int)response.StatusCode} {response.ReasonPhrase}: {ExtractErrorMessage(content)}");

            // An invalid PAT may produce a 203 with an HTML sign-in page instead of a 401.
            var mediaType = response.Content.Headers.ContentType?.MediaType;
            if (response.StatusCode == HttpStatusCode.NonAuthoritativeInformation
                || mediaType is null
                || !mediaType.Contains("json", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogError("TFS {Method} {Url} returned non-JSON response ({StatusCode}, {MediaType}): {Body}",
                    method, url, (int)response.StatusCode, mediaType, TfsLoggingHandler.Truncate(content));
                throw new TfsException(TfsErrorKind.Unauthorized, "Unexpected non-JSON response (sign-in page?). The PAT may be invalid.");
            }

            try
            {
                return JsonSerializer.Deserialize<T>(content, JsonOptions)
                    ?? throw InvalidResponse($"Empty response from {relativeUrl}.");
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Cannot parse TFS response of {Method} {Url}: {Body}", method, url, TfsLoggingHandler.Truncate(content));
                throw new TfsException(TfsErrorKind.InvalidResponse, "Unexpected response format — see log.", ex);
            }
        }
    }

    private static string ExtractErrorMessage(string content)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty("message", out var message)
                && message.ValueKind == JsonValueKind.String)
                return message.GetString() ?? string.Empty;
        }
        catch (JsonException)
        {
            // Not a JSON error payload — fall back to raw text.
        }
        return content.Length <= 300 ? content : content[..300] + "…";
    }

    private TfsException InvalidResponse(string message)
    {
        logger.LogError("Invalid TFS response: {Message}", message);
        return new TfsException(TfsErrorKind.InvalidResponse, message);
    }

    private sealed record ConnectionDataResponse(ConnectionDataUser? AuthenticatedUser);
    private sealed record ConnectionDataUser(string? ProviderDisplayName, string? CustomDisplayName);

    private sealed record WiqlResponse(List<WiqlWorkItem>? WorkItems);
    private sealed record WiqlWorkItem(int Id);

    private sealed record WorkItemsResponse(List<WorkItemResponse?>? Value);
    private sealed record WorkItemResponse(int Id, Dictionary<string, JsonElement>? Fields, List<WorkItemRelation>? Relations);
    private sealed record WorkItemRelation(string? Rel, string? Url);

    private sealed record StatesResponse(List<StateResponse>? Value);
    private sealed record StateResponse(string? Name, string? Category);
}
