using HonestTimeTracker.Application.Tfs;
using HonestTimeTracker.Infrastructure.Tfs;
using HonestTimeTracker.IntegrationTests.Support;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;

namespace HonestTimeTracker.IntegrationTests.Tfs;

public sealed class TfsClientTests : IDisposable
{
    private const string Pat = "s3cr3t-pat-value";
    private static readonly TfsConnection Connection = new("https://tfs.test/tfs/DefaultCollection", Pat);

    private readonly CapturingLoggerProvider _logs = new();
    private readonly ILoggerFactory _loggerFactory;

    public TfsClientTests()
    {
        _loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Trace).AddProvider(_logs));
    }

    public void Dispose() => _loggerFactory.Dispose();

    private TfsClient CreateClient(StubHttpMessageHandler stub)
    {
        var logging = new TfsLoggingHandler(_loggerFactory.CreateLogger<TfsLoggingHandler>()) { InnerHandler = stub };
        return new TfsClient(new HttpClient(logging), _loggerFactory.CreateLogger<TfsClient>());
    }

    private const string WorkItemsJson = """
        {
          "count": 3,
          "value": [
            {
              "id": 101,
              "fields": {
                "System.Title": "Implement API",
                "System.WorkItemType": "Task",
                "System.State": "Active",
                "System.TeamProject": "Alpha",
                "System.AssignedTo": { "displayName": "Test User" }
              },
              "relations": [
                { "rel": "System.LinkTypes.Related", "url": "https://tfs.test/tfs/DefaultCollection/_apis/wit/workItems/7" },
                { "rel": "System.LinkTypes.Hierarchy-Reverse", "url": "https://tfs.test/tfs/DefaultCollection/_apis/wit/workItems/50" }
              ]
            },
            null,
            { "id": 102, "fields": { "System.Title": "No parent", "System.WorkItemType": "Bug", "System.State": "New", "System.TeamProject": "Alpha" } }
          ]
        }
        """;

    [Fact]
    public async Task GetWorkItems_maps_fields_parent_and_skips_omitted_items()
    {
        var stub = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json(WorkItemsJson));

        var items = await CreateClient(stub).GetWorkItemsAsync(Connection, [101, 999, 102], includeRelations: true, CancellationToken.None);

        Assert.Equal(2, items.Count);
        Assert.Equal(new TfsWorkItem(101, "Implement API", "Task", "Active", "Alpha", 50), items[0]);
        Assert.Equal(new TfsWorkItem(102, "No parent", "Bug", "New", "Alpha", null), items[1]);

        var request = Assert.Single(stub.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        var query = Uri.UnescapeDataString(request.Uri.Query);
        Assert.Contains("ids=101,999,102", query);
        Assert.Contains("errorPolicy=omit", query);
        Assert.Contains("$expand=relations", query);
        Assert.Contains("api-version=7.1", query);
    }

    [Fact]
    public async Task Sends_basic_auth_header_with_pat()
    {
        var stub = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json("""{ "value": [] }"""));

        await CreateClient(stub).GetWorkItemsAsync(Connection, [1], includeRelations: false, CancellationToken.None);

        var expected = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(":" + Pat));
        Assert.Equal(expected, Assert.Single(stub.Requests).Authorization);
    }

    [Fact]
    public async Task GetWorkItems_splits_requests_into_batches_of_200()
    {
        var stub = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json("""{ "value": [] }"""));

        await CreateClient(stub).GetWorkItemsAsync(Connection, Enumerable.Range(1, 450).ToList(), includeRelations: false, CancellationToken.None);

        Assert.Equal(3, stub.Requests.Count);
        Assert.DoesNotContain("$expand", stub.Requests[0].Uri.Query);
    }

    [Fact]
    public async Task QueryWorkItemIds_posts_wiql()
    {
        var stub = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json("""{ "workItems": [ { "id": 5, "url": "x" }, { "id": 8, "url": "y" } ] }"""));

        var ids = await CreateClient(stub).QueryWorkItemIdsAsync(Connection, "SELECT [System.Id] FROM WorkItems", CancellationToken.None);

        Assert.Equal([5, 8], ids);
        var request = Assert.Single(stub.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/tfs/DefaultCollection/_apis/wit/wiql", request.Uri.AbsolutePath);
        Assert.Equal("""{"query":"SELECT [System.Id] FROM WorkItems"}""", request.Body);
    }

    [Fact]
    public async Task GetWorkItemTypeStates_escapes_project_and_type()
    {
        var stub = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json(
            """{ "value": [ { "name": "Active", "color": "007acc", "category": "InProgress" }, { "name": "Closed", "category": "Completed" } ] }"""));

        var states = await CreateClient(stub).GetWorkItemTypeStatesAsync(Connection, "My Project", "User Story", CancellationToken.None);

        Assert.Equal([new TfsWorkItemTypeState("Active", "InProgress"), new TfsWorkItemTypeState("Closed", "Completed")], states);
        Assert.Contains("/My%20Project/_apis/wit/workitemtypes/User%20Story/states", Assert.Single(stub.Requests).Uri.AbsoluteUri);
    }

    [Fact]
    public async Task GetAuthenticatedUser_returns_display_name()
    {
        var stub = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json(
            """{ "authenticatedUser": { "id": "1", "providerDisplayName": "Jane Doe" }, "instanceId": "x" }"""));

        Assert.Equal("Jane Doe", await CreateClient(stub).GetAuthenticatedUserAsync(Connection, CancellationToken.None));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task Maps_auth_failures_to_unauthorized(HttpStatusCode status)
    {
        var stub = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Html("denied", status));

        var ex = await Assert.ThrowsAsync<TfsException>(() => CreateClient(stub).GetAuthenticatedUserAsync(Connection, CancellationToken.None));

        Assert.Equal(TfsErrorKind.Unauthorized, ex.Kind);
    }

    [Fact]
    public async Task Maps_html_sign_in_page_to_unauthorized()
    {
        var stub = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Html("<html>Sign in</html>", HttpStatusCode.NonAuthoritativeInformation));

        var ex = await Assert.ThrowsAsync<TfsException>(() => CreateClient(stub).GetAuthenticatedUserAsync(Connection, CancellationToken.None));

        Assert.Equal(TfsErrorKind.Unauthorized, ex.Kind);
    }

    [Fact]
    public async Task Maps_server_error_and_includes_tfs_message()
    {
        var stub = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json(
            """{ "$id": "1", "message": "TF51005: The query references a field that does not exist.", "typeKey": "x" }""", HttpStatusCode.BadRequest));

        var ex = await Assert.ThrowsAsync<TfsException>(() => CreateClient(stub).QueryWorkItemIdsAsync(Connection, "bad", CancellationToken.None));

        Assert.Equal(TfsErrorKind.Other, ex.Kind);
        Assert.Contains("400", ex.Message);
        Assert.Contains("TF51005", ex.Message);
        Assert.Contains(_logs.Entries, e => e.StartsWith("Warning") && e.Contains("TF51005"));
    }

    [Fact]
    public async Task Maps_network_failure_to_unreachable()
    {
        var stub = new StubHttpMessageHandler(_ => throw new HttpRequestException("No such host is known."));

        var ex = await Assert.ThrowsAsync<TfsException>(() => CreateClient(stub).GetAuthenticatedUserAsync(Connection, CancellationToken.None));

        Assert.Equal(TfsErrorKind.Unreachable, ex.Kind);
        Assert.Contains("No such host", ex.Message);
    }

    [Fact]
    public async Task Maps_unparsable_json_to_invalid_response_and_logs_body()
    {
        var stub = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json("""{ "value": "not-a-list" }"""));

        var ex = await Assert.ThrowsAsync<TfsException>(() => CreateClient(stub).GetWorkItemsAsync(Connection, [1], false, CancellationToken.None));

        Assert.Equal(TfsErrorKind.InvalidResponse, ex.Kind);
        Assert.Contains(_logs.Entries, e => e.StartsWith("Error") && e.Contains("not-a-list"));
    }

    [Fact]
    public async Task Never_writes_pat_to_log()
    {
        var responses = new Queue<HttpResponseMessage>([
            StubHttpMessageHandler.Json("""{ "workItems": [] }"""),
            StubHttpMessageHandler.Html("denied", HttpStatusCode.Unauthorized),
            StubHttpMessageHandler.Html("<html/>", HttpStatusCode.NonAuthoritativeInformation)
        ]);
        var stub = new StubHttpMessageHandler(_ => responses.Dequeue());
        var client = CreateClient(stub);

        await client.QueryWorkItemIdsAsync(Connection, "SELECT 1", CancellationToken.None);
        await Assert.ThrowsAsync<TfsException>(() => client.GetAuthenticatedUserAsync(Connection, CancellationToken.None));
        await Assert.ThrowsAsync<TfsException>(() => client.GetAuthenticatedUserAsync(Connection, CancellationToken.None));

        var encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(":" + Pat));
        Assert.NotEmpty(_logs.Entries);
        Assert.DoesNotContain(_logs.Entries, e => e.Contains(Pat) || e.Contains(encoded));
        Assert.Contains(_logs.Entries, e => e.Contains("SELECT 1"));
    }
}
