using FluentValidation;

namespace HonestTimeTracker.Application.Tasks;

public record DeleteTaskCommand(int Id) : ICommand<Unit>;

public class DeleteTaskCommandValidator : AbstractValidator<DeleteTaskCommand>
{
    public DeleteTaskCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
