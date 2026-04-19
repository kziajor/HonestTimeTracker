using FluentValidation;
using HonestTimeTracker.Application.Tasks;

namespace HonestTimeTracker.Application.Records;

public record UpdateRecordCommand(
    int Id,
    int TaskId,
    DateTime StartedAt,
    DateTime? FinishedAt,
    string? Comment) : ICommand<Unit>;

public class UpdateRecordCommandValidator : AbstractValidator<UpdateRecordCommand>
{
    public UpdateRecordCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.TaskId).GreaterThan(0);
        RuleFor(x => x.StartedAt).NotEmpty();
        RuleFor(x => x.Comment).MaximumLength(1000).When(x => x.Comment is not null);

        When(x => x.FinishedAt.HasValue, () =>
        {
            RuleFor(x => x).Must(x => x.FinishedAt!.Value > x.StartedAt)
                .WithMessage("End time must be after start time.");
            RuleFor(x => x).Must(x => x.StartedAt.Date == x.FinishedAt!.Value.Date)
                .WithMessage("Record must start and end on the same day.");
        });
    }
}

public class UpdateRecordCommandHandler(IRecordRepository recordRepository, ITaskRepository taskRepository)
    : ICommandHandler<UpdateRecordCommand, Unit>
{
    public async Task<Unit> HandleAsync(UpdateRecordCommand command, CancellationToken ct = default)
    {
        var record = await recordRepository.GetByIdAsync(command.Id, ct)
            ?? throw new InvalidOperationException($"Record with ID {command.Id} does not exist.");

        if (!command.FinishedAt.HasValue)
        {
            var active = await recordRepository.GetActiveAsync(ct);
            if (active is not null && active.Id != command.Id)
                throw new InvalidOperationException("Another record is already running. Stop it before marking this one as running.");
        }

        if (await recordRepository.HasOverlapAsync(command.StartedAt, command.FinishedAt, excludeId: command.Id, ct))
            throw new InvalidOperationException("The time range overlaps with an existing record.");

        var oldMinutes = record.MinutesSpent;
        var newMinutes = command.FinishedAt.HasValue
            ? (int)(command.FinishedAt.Value - command.StartedAt).TotalMinutes
            : 0;

        if (record.TaskId != command.TaskId)
        {
            var oldTask = await taskRepository.GetByIdAsync(record.TaskId, ct);
            if (oldTask is not null)
                oldTask.SpentMinutes = Math.Max(0, oldTask.SpentMinutes - oldMinutes);

            if (command.FinishedAt.HasValue)
            {
                var newTask = await taskRepository.GetByIdAsync(command.TaskId, ct);
                if (newTask is not null)
                    newTask.SpentMinutes += newMinutes;
            }
        }
        else
        {
            var task = await taskRepository.GetByIdAsync(record.TaskId, ct);
            if (task is not null)
                task.SpentMinutes = Math.Max(0, task.SpentMinutes - oldMinutes + newMinutes);
        }

        record.TaskId = command.TaskId;
        record.StartedAt = command.StartedAt;
        record.FinishedAt = command.FinishedAt;
        record.MinutesSpent = newMinutes;
        record.Comment = command.Comment?.Trim();

        await recordRepository.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
