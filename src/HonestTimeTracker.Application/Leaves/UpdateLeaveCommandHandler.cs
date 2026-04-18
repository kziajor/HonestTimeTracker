namespace HonestTimeTracker.Application.Leaves;

public class UpdateLeaveCommandHandler(ILeaveRepository repository)
    : ICommandHandler<UpdateLeaveCommand, Unit>
{
    public async Task<Unit> HandleAsync(UpdateLeaveCommand command, CancellationToken ct = default)
    {
        var leave = await repository.GetByIdAsync(command.Id, ct)
            ?? throw new InvalidOperationException($"Leave with id {command.Id} not found.");

        leave.StartDate = command.StartDate;
        leave.EndDate = command.EndDate;
        leave.Description = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim();

        await repository.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
