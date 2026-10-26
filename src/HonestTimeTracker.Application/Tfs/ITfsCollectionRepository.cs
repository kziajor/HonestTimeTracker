using HonestTimeTracker.Domain;

namespace HonestTimeTracker.Application.Tfs;

public interface ITfsCollectionRepository
{
    Task<List<TfsCollection>> GetAllAsync(CancellationToken ct);
    Task<TfsCollection?> GetByIdAsync(int id, CancellationToken ct);
    Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct);
    Task<bool> UrlExistsAsync(string url, int? excludeId, CancellationToken ct);
    Task AddAsync(TfsCollection collection, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
