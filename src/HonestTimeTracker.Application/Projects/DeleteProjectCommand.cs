using FluentValidation;

namespace HonestTimeTracker.Application.Projects;

public record DeleteProjectCommand(int Id) : ICommand<Unit>;

public class DeleteProjectCommandValidator : AbstractValidator<DeleteProjectCommand>
{
    public DeleteProjectCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
