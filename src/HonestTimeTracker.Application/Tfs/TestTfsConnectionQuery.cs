namespace HonestTimeTracker.Application.Tfs;

/// Tests the connection and returns the display name of the PAT owner.
/// When Pat is empty, the token stored for CollectionId is used.
public record TestTfsConnectionQuery(string Url, string? Pat, int? CollectionId) : IQuery<string>;

public class TestTfsConnectionQueryHandler(
    ITfsClient client,
    ITfsCollectionRepository repository,
    ISecretProtector secretProtector) : IQueryHandler<TestTfsConnectionQuery, string>
{
    public async Task<string> HandleAsync(TestTfsConnectionQuery query, CancellationToken ct = default)
    {
        var pat = query.Pat?.Trim();
        if (string.IsNullOrEmpty(pat))
        {
            var collection = query.CollectionId is { } id ? await repository.GetByIdAsync(id, ct) : null;
            if (collection?.EncryptedPat is null)
                throw new InvalidOperationException("Personal access token is required.");
            pat = secretProtector.Unprotect(collection.EncryptedPat);
        }

        var connection = new TfsConnection(TfsCollectionRules.NormalizeUrl(query.Url), pat);
        return await client.GetAuthenticatedUserAsync(connection, ct);
    }
}
