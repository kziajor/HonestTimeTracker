using FluentValidation;

namespace HonestTimeTracker.Application.Tasks;

public record SetTodayListCommand(int Id, bool IsOnTodayList) : ICommand<Unit>;

public class SetTodayListCommandValidator : AbstractValidator<SetTodayListCommand>
{
    public SetTodayListCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

public class SetTodayListCommandHandler(ITaskRepository repository)
    : ICommandHandler<SetTodayListCommand, Unit>
{
    public async Task<Unit> HandleAsync(SetTodayListCommand command, CancellationToken ct = default)
    {
        var task = await repository.GetByIdAsync(command.Id, ct)
            ?? throw new InvalidOperationException($"Task with ID {command.Id} does not exist.");

        task.IsOnTodayList = command.IsOnTodayList;
        await repository.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
