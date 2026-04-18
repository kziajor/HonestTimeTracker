using FluentValidation;

namespace HonestTimeTracker.Application.Tasks;

public record ToggleTaskClosedCommand(int Id) : ICommand<Unit>;

public class ToggleTaskClosedCommandValidator : AbstractValidator<ToggleTaskClosedCommand>
{
    public ToggleTaskClosedCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
