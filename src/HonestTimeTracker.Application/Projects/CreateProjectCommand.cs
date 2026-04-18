using FluentValidation;

namespace HonestTimeTracker.Application.Projects;

public record CreateProjectCommand(string Name) : ICommand<int>;

public class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Project name is required.")
            .MaximumLength(200).WithMessage("Project name cannot exceed 200 characters.");
    }
}
