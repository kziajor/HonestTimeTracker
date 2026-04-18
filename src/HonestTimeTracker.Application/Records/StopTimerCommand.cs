using FluentValidation;
using HonestTimeTracker.Application.Tasks;

namespace HonestTimeTracker.Application.Records;

public record StopTimerCommand : ICommand<Unit>;

public class StopTimerCommandValidator : AbstractValidator<StopTimerCommand> { }

public class StopTimerCommandHandler(IRecordRepository recordRepository, ITaskRepository taskRepository)
    : ICommandHandler<StopTimerCommand, Unit>
{
    public async Task<Unit> HandleAsync(StopTimerCommand command, CancellationToken ct = default)
    {
        var record = await recordRepository.GetActiveAsync(ct);
        if (record is null)
            return Unit.Value;

        record.FinishedAt = DateTime.Now;
        record.MinutesSpent = (int)(record.FinishedAt.Value - record.StartedAt).TotalMinutes;

        var task = await taskRepository.GetByIdAsync(record.TaskId, ct);
        if (task is not null)
            task.SpentMinutes += record.MinutesSpent;

        await recordRepository.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
