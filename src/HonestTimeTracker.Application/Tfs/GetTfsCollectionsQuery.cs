namespace HonestTimeTracker.Application.Tfs;

public record GetTfsCollectionsQuery : IQuery<List<TfsCollectionDto>>;

public class GetTfsCollectionsQueryHandler(ITfsCollectionRepository repository)
    : IQueryHandler<GetTfsCollectionsQuery, List<TfsCollectionDto>>
{
    public async Task<List<TfsCollectionDto>> HandleAsync(GetTfsCollectionsQuery query, CancellationToken ct = default)
    {
        var collections = await repository.GetAllAsync(ct);
        return collections
            .Select(c => new TfsCollectionDto(c.Id, c.Name, c.Url, !string.IsNullOrEmpty(c.EncryptedPat), c.Enabled))
            .ToList();
    }
}
