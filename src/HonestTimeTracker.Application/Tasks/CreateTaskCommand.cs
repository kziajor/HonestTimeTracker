using FluentValidation;

namespace HonestTimeTracker.Application.Tasks;

public record CreateTaskCommand(string Title, int PlannedMinutes, int ProjectId) : ICommand<int>;

public class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Task title is required.")
            .MaximumLength(500).WithMessage("Task title cannot exceed 500 characters.");
        RuleFor(x => x.PlannedMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Planned time cannot be negative.");
    }
}
