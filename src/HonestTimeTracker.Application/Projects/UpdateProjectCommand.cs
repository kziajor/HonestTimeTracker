using FluentValidation;

namespace HonestTimeTracker.Application.Projects;

public record UpdateProjectCommand(int Id, string Name) : ICommand<Unit>;

public class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Project name is required.")
            .MaximumLength(200).WithMessage("Project name cannot exceed 200 characters.");
    }
}
