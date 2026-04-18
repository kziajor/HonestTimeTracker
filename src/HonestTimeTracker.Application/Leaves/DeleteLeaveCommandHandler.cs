namespace HonestTimeTracker.Application.Leaves;

public class DeleteLeaveCommandHandler(ILeaveRepository repository)
    : ICommandHandler<DeleteLeaveCommand, Unit>
{
    public async Task<Unit> HandleAsync(DeleteLeaveCommand command, CancellationToken ct = default)
    {
        var leave = await repository.GetByIdAsync(command.Id, ct)
            ?? throw new InvalidOperationException($"Leave with id {command.Id} not found.");

        leave.IsDeleted = true;
        await repository.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
