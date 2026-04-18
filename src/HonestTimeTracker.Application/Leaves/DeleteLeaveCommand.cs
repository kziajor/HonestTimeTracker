using FluentValidation;

namespace HonestTimeTracker.Application.Leaves;

public record DeleteLeaveCommand(int Id) : ICommand<Unit>;

public class DeleteLeaveCommandValidator : AbstractValidator<DeleteLeaveCommand>
{
    public DeleteLeaveCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
