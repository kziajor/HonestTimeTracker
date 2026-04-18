using FluentValidation;

namespace HonestTimeTracker.Application.Leaves;

public record UpdateLeaveCommand(int Id, DateOnly StartDate, DateOnly EndDate, string? Description) : ICommand<Unit>;

public class UpdateLeaveCommandValidator : AbstractValidator<UpdateLeaveCommand>
{
    public UpdateLeaveCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("End date must be on or after start date.");
    }
}
