using HonestTimeTracker.Application.Tfs;
using HonestTimeTracker.Domain;
using HonestTimeTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HonestTimeTracker.Infrastructure.Repositories;

public class TfsCollectionRepository(AppDbContext db) : ITfsCollectionRepository
{
    public Task<List<TfsCollection>> GetAllAsync(CancellationToken ct) =>
        db.TfsCollections.OrderBy(c => c.Name).ToListAsync(ct);

    public Task<TfsCollection?> GetByIdAsync(int id, CancellationToken ct) =>
        db.TfsCollections.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct) =>
        db.TfsCollections.AnyAsync(c => c.Id != excludeId && c.Name.ToLower() == name.ToLower(), ct);

    public Task<bool> UrlExistsAsync(string url, int? excludeId, CancellationToken ct) =>
        db.TfsCollections.AnyAsync(c => c.Id != excludeId && c.Url.ToLower() == url.ToLower(), ct);

    public async Task AddAsync(TfsCollection collection, CancellationToken ct) =>
        await db.TfsCollections.AddAsync(collection, ct);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
