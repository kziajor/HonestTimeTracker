using FluentValidation;

namespace HonestTimeTracker.Application.Projects;

public record ToggleProjectClosedCommand(int Id) : ICommand<Unit>;

public class ToggleProjectClosedCommandValidator : AbstractValidator<ToggleProjectClosedCommand>
{
    public ToggleProjectClosedCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
