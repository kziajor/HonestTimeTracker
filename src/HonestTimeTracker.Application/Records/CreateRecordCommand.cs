using FluentValidation;
using HonestTimeTracker.Application.Tasks;
using HonestTimeTracker.Domain;

namespace HonestTimeTracker.Application.Records;

public record CreateRecordCommand(
    int TaskId,
    DateTime StartedAt,
    DateTime FinishedAt,
    string? Comment) : ICommand<int>;

public class CreateRecordCommandValidator : AbstractValidator<CreateRecordCommand>
{
    public CreateRecordCommandValidator()
    {
        RuleFor(x => x.TaskId).GreaterThan(0);
        RuleFor(x => x.StartedAt).NotEmpty();
        RuleFor(x => x.FinishedAt).NotEmpty();
        RuleFor(x => x).Must(x => x.FinishedAt > x.StartedAt)
            .WithMessage("End time must be after start time.");
        RuleFor(x => x).Must(x => x.StartedAt.Date == x.FinishedAt.Date)
            .WithMessage("Record must start and end on the same day.");
        RuleFor(x => x.Comment).MaximumLength(1000).When(x => x.Comment is not null);
    }
}

public class CreateRecordCommandHandler(IRecordRepository recordRepository, ITaskRepository taskRepository)
    : ICommandHandler<CreateRecordCommand, int>
{
    public async Task<int> HandleAsync(CreateRecordCommand command, CancellationToken ct = default)
    {
        var minutesSpent = (int)(command.FinishedAt - command.StartedAt).TotalMinutes;

        var record = new WorkRecord
        {
            TaskId = command.TaskId,
            StartedAt = command.StartedAt,
            FinishedAt = command.FinishedAt,
            MinutesSpent = minutesSpent,
            Comment = command.Comment?.Trim()
        };

        await recordRepository.AddAsync(record, ct);

        var task = await taskRepository.GetByIdAsync(command.TaskId, ct);
        if (task is not null)
            task.SpentMinutes += minutesSpent;

        await recordRepository.SaveChangesAsync(ct);
        return record.Id;
    }
}
