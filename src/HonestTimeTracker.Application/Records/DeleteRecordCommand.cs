using FluentValidation;
using HonestTimeTracker.Application.Tasks;

namespace HonestTimeTracker.Application.Records;

public record DeleteRecordCommand(int Id) : ICommand<Unit>;

public class DeleteRecordCommandValidator : AbstractValidator<DeleteRecordCommand>
{
    public DeleteRecordCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

public class DeleteRecordCommandHandler(IRecordRepository recordRepository, ITaskRepository taskRepository)
    : ICommandHandler<DeleteRecordCommand, Unit>
{
    public async Task<Unit> HandleAsync(DeleteRecordCommand command, CancellationToken ct = default)
    {
        var record = await recordRepository.GetByIdAsync(command.Id, ct)
            ?? throw new InvalidOperationException($"Record with ID {command.Id} does not exist.");

        var task = await taskRepository.GetByIdAsync(record.TaskId, ct);
        if (task is not null)
            task.SpentMinutes = Math.Max(0, task.SpentMinutes - record.MinutesSpent);

        record.IsDeleted = true;

        await recordRepository.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
