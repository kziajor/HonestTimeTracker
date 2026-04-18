using FluentValidation;

namespace HonestTimeTracker.Application.Tasks;

public record UpdateTaskCommand(int Id, string Title, int PlannedMinutes, int ProjectId) : ICommand<Unit>;

public class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Task title is required.")
            .MaximumLength(500).WithMessage("Task title cannot exceed 500 characters.");
        RuleFor(x => x.PlannedMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Planned time cannot be negative.");
    }
}
