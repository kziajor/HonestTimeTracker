using FluentValidation;
using HonestTimeTracker.Application.Tasks;
using HonestTimeTracker.Domain;

namespace HonestTimeTracker.Application.Records;

public record CreateRecordCommand(
    int TaskId,
    DateTime StartedAt,
    DateTime? FinishedAt,
    string? Comment) : ICommand<int>;

public class CreateRecordCommandValidator : AbstractValidator<CreateRecordCommand>
{
    public CreateRecordCommandValidator()
    {
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

public class CreateRecordCommandHandler(IRecordRepository recordRepository, ITaskRepository taskRepository)
    : ICommandHandler<CreateRecordCommand, int>
{
    public async Task<int> HandleAsync(CreateRecordCommand command, CancellationToken ct = default)
    {
        if (!command.FinishedAt.HasValue)
        {
            var active = await recordRepository.GetActiveAsync(ct);
            if (active is not null)
                throw new InvalidOperationException("Another record is already running. Stop it before starting a new one.");
        }

        var minutesSpent = command.FinishedAt.HasValue
            ? (int)(command.FinishedAt.Value - command.StartedAt).TotalMinutes
            : 0;

        var record = new WorkRecord
        {
            TaskId = command.TaskId,
            StartedAt = command.StartedAt,
            FinishedAt = command.FinishedAt,
            MinutesSpent = minutesSpent,
            Comment = command.Comment?.Trim()
        };

        await recordRepository.AddAsync(record, ct);

        if (command.FinishedAt.HasValue)
        {
            var task = await taskRepository.GetByIdAsync(command.TaskId, ct);
            if (task is not null)
                task.SpentMinutes += minutesSpent;
        }

        await recordRepository.SaveChangesAsync(ct);
        return record.Id;
    }
}
