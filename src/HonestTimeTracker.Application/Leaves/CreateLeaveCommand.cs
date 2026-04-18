using FluentValidation;

namespace HonestTimeTracker.Application.Leaves;

public record CreateLeaveCommand(DateOnly StartDate, DateOnly EndDate, string? Description) : ICommand<int>;

public class CreateLeaveCommandValidator : AbstractValidator<CreateLeaveCommand>
{
    public CreateLeaveCommandValidator()
    {
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("End date must be on or after start date.");
    }
}
