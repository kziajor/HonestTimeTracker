using HonestTimeTracker.Application.Projects;
using HonestTimeTracker.Domain;
using HonestTimeTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HonestTimeTracker.Infrastructure.Repositories;

public class ProjectRepository(AppDbContext db) : IProjectRepository
{
    public Task<List<Project>> GetAllAsync(bool showClosed, CancellationToken ct)
    {
        var query = db.Projects.AsQueryable();
        query = showClosed
            ? query.Where(p => p.Closed)
            : query.Where(p => !p.Closed);
        return query.OrderBy(p => p.Name).ToListAsync(ct);
    }

    public Task<Project?> GetByIdAsync(int id, CancellationToken ct) =>
        db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task AddAsync(Project project, CancellationToken ct) =>
        await db.Projects.AddAsync(project, ct);

    public Task SaveChangesAsync(CancellationToken ct) =>
        db.SaveChangesAsync(ct);
}
