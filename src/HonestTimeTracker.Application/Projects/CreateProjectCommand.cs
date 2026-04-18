using FluentValidation;

namespace HonestTimeTracker.Application.Projects;

public record CreateProjectCommand(string Name) : ICommand<int>;

public class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nazwa projektu jest wymagana.")
            .MaximumLength(200).WithMessage("Nazwa projektu nie może przekraczać 200 znaków.");
    }
}
