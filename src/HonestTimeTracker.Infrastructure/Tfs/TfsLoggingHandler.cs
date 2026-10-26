using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace HonestTimeTracker.Infrastructure.Tfs;

/// Logs TFS HTTP traffic. Headers are never logged, so the PAT in the Authorization header cannot leak.
public class TfsLoggingHandler(ILogger<TfsLoggingHandler> logger) : DelegatingHandler
{
    internal const int MaxLoggedBodyLength = 4096;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (logger.IsEnabled(LogLevel.Debug) && request.Content is not null)
        {
            var requestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            logger.LogDebug("TFS request {Method} {Url} body: {Body}", request.Method, request.RequestUri, Truncate(requestBody));
        }

        var stopwatch = Stopwatch.StartNew();
        var response = await base.SendAsync(request, cancellationToken);
        stopwatch.Stop();

        var verbose = logger.IsEnabled(LogLevel.Debug);
        if (response.IsSuccessStatusCode && !verbose)
            return response;

        await response.Content.LoadIntoBufferAsync(cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            logger.LogDebug("TFS {Method} {Url} -> {StatusCode} in {Elapsed} ms, body: {Body}",
                request.Method, request.RequestUri, (int)response.StatusCode, stopwatch.ElapsedMilliseconds, Truncate(body));
        }
        else
        {
            logger.LogWarning("TFS {Method} {Url} -> {StatusCode} {Reason} in {Elapsed} ms, body: {Body}",
                request.Method, request.RequestUri, (int)response.StatusCode, response.ReasonPhrase, stopwatch.ElapsedMilliseconds, Truncate(body));
        }

        return response;
    }

    internal static string Truncate(string text) =>
        text.Length <= MaxLoggedBodyLength ? text : text[..MaxLoggedBodyLength] + $"… [truncated, {text.Length} chars]";
}
