using HonestTimeTracker.Application.Tasks;
using HonestTimeTracker.Domain;
using HonestTimeTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HonestTimeTracker.Infrastructure.Repositories;

public class TaskRepository(AppDbContext db) : ITaskRepository
{
    public Task<List<WorkTask>> GetAllAsync(bool showClosed, int? projectId, CancellationToken ct)
    {
        var query = db.Tasks.Include(t => t.Project).AsQueryable();
        query = showClosed
            ? query.Where(t => t.Closed)
            : query.Where(t => !t.Closed);
        if (projectId.HasValue)
            query = query.Where(t => t.ProjectId == projectId);
        return query.OrderBy(t => t.Title).ToListAsync(ct);
    }

    public Task<List<WorkTask>> GetTodayAsync(CancellationToken ct) =>
        db.Tasks.Include(t => t.Project)
            .Where(t => t.IsOnTodayList)
            .OrderBy(t => t.Title)
            .ToListAsync(ct);

    public Task<WorkTask?> GetByIdAsync(int id, CancellationToken ct) =>
        db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task AddAsync(WorkTask task, CancellationToken ct) =>
        await db.Tasks.AddAsync(task, ct);

    public Task SaveChangesAsync(CancellationToken ct) =>
        db.SaveChangesAsync(ct);
}
