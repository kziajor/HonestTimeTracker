using HonestTimeTracker.Application.Records;
using HonestTimeTracker.Domain;
using HonestTimeTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HonestTimeTracker.Infrastructure.Repositories;

public class RecordRepository(AppDbContext db) : IRecordRepository
{
    public Task<List<WorkRecord>> GetAllAsync(int? taskId, DateOnly? date, CancellationToken ct)
    {
        var query = db.Records
            .Include(r => r.Task)
            .ThenInclude(t => t.Project)
            .AsQueryable();

        if (taskId.HasValue)
            query = query.Where(r => r.TaskId == taskId.Value);

        if (date.HasValue)
        {
            var day = date.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(r => r.StartedAt.Date == day.Date);
        }

        return query.OrderByDescending(r => r.StartedAt).ToListAsync(ct);
    }

    public Task<WorkRecord?> GetByIdAsync(int id, CancellationToken ct) =>
        db.Records.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<WorkRecord?> GetActiveAsync(CancellationToken ct) =>
        db.Records
            .Include(r => r.Task)
            .FirstOrDefaultAsync(r => r.FinishedAt == null, ct);

    public async Task AddAsync(WorkRecord record, CancellationToken ct) =>
        await db.Records.AddAsync(record, ct);

    public Task SaveChangesAsync(CancellationToken ct) =>
        db.SaveChangesAsync(ct);
}
