using FluentValidation;
using HonestTimeTracker.Application.Tasks;
using HonestTimeTracker.Domain;

namespace HonestTimeTracker.Application.Records;

public record StartTimerCommand(int TaskId) : ICommand<int>;

public class StartTimerCommandValidator : AbstractValidator<StartTimerCommand>
{
    public StartTimerCommandValidator()
    {
        RuleFor(x => x.TaskId).GreaterThan(0);
    }
}

public class StartTimerCommandHandler(IRecordRepository recordRepository)
    : ICommandHandler<StartTimerCommand, int>
{
    public async Task<int> HandleAsync(StartTimerCommand command, CancellationToken ct = default)
    {
        var record = new WorkRecord
        {
            TaskId = command.TaskId,
            StartedAt = DateTime.Now,
            FinishedAt = null,
            MinutesSpent = 0
        };

        await recordRepository.AddAsync(record, ct);
        await recordRepository.SaveChangesAsync(ct);
        return record.Id;
    }
}
