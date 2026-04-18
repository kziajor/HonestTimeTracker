using HonestTimeTracker.Application.Leaves;
using HonestTimeTracker.Domain;
using HonestTimeTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HonestTimeTracker.Infrastructure.Repositories;

public class LeaveRepository(AppDbContext db) : ILeaveRepository
{
    public Task<List<Leave>> GetAllAsync(CancellationToken ct) =>
        db.Leaves.OrderByDescending(l => l.StartDate).ToListAsync(ct);

    public Task<Leave?> GetByIdAsync(int id, CancellationToken ct) =>
        db.Leaves.FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task AddAsync(Leave leave, CancellationToken ct) =>
        await db.Leaves.AddAsync(leave, ct);

    public Task SaveChangesAsync(CancellationToken ct) =>
        db.SaveChangesAsync(ct);
}
