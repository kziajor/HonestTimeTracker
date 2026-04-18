using FluentValidation;

namespace HonestTimeTracker.Application.Projects;

public record UpdateProjectCommand(int Id, string Name) : ICommand<Unit>;

public class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nazwa projektu jest wymagana.")
            .MaximumLength(200).WithMessage("Nazwa projektu nie może przekraczać 200 znaków.");
    }
}
