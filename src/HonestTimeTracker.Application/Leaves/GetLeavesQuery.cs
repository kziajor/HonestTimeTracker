using HonestTimeTracker.Domain;

namespace HonestTimeTracker.Application.Leaves;

public interface ILeaveRepository
{
    Task<List<Leave>> GetAllAsync(CancellationToken ct);
    Task<Leave?> GetByIdAsync(int id, CancellationToken ct);
    Task AddAsync(Leave leave, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public record GetLeavesQuery : IQuery<List<LeaveDto>>;

public class GetLeavesQueryHandler(ILeaveRepository repository)
    : IQueryHandler<GetLeavesQuery, List<LeaveDto>>
{
    public async Task<List<LeaveDto>> HandleAsync(GetLeavesQuery query, CancellationToken ct = default)
    {
        var leaves = await repository.GetAllAsync(ct);
        return leaves
            .Select(l => new LeaveDto(
                l.Id,
                l.StartDate,
                l.EndDate,
                l.Description,
                l.EndDate.DayNumber - l.StartDate.DayNumber + 1))
            .ToList();
    }
}
