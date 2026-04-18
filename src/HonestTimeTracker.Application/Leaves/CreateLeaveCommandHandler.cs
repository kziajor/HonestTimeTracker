using HonestTimeTracker.Domain;

namespace HonestTimeTracker.Application.Leaves;

public class CreateLeaveCommandHandler(ILeaveRepository repository)
    : ICommandHandler<CreateLeaveCommand, int>
{
    public async Task<int> HandleAsync(CreateLeaveCommand command, CancellationToken ct = default)
    {
        var leave = new Leave
        {
            StartDate = command.StartDate,
            EndDate = command.EndDate,
            Description = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim()
        };
        await repository.AddAsync(leave, ct);
        await repository.SaveChangesAsync(ct);
        return leave.Id;
    }
}
