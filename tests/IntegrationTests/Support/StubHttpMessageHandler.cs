using System.Net;
using System.Text;

namespace HonestTimeTracker.IntegrationTests.Support;

public sealed record RecordedRequest(HttpMethod Method, Uri Uri, string? Authorization, string? Body);

/// Returns canned responses and records every request (body included, read eagerly).
public sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    public List<RecordedRequest> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add(new RecordedRequest(request.Method, request.RequestUri!, request.Headers.Authorization?.ToString(), body));
        return respond(request);
    }

    public static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    public static HttpResponseMessage Html(string html, HttpStatusCode status) =>
        new(status) { Content = new StringContent(html, Encoding.UTF8, "text/html") };
}
