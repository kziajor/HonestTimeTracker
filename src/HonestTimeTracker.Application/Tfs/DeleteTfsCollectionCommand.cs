using FluentValidation;

namespace HonestTimeTracker.Application.Tfs;

public record DeleteTfsCollectionCommand(int Id) : ICommand<Unit>;

public class DeleteTfsCollectionCommandValidator : AbstractValidator<DeleteTfsCollectionCommand>
{
    public DeleteTfsCollectionCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

public class DeleteTfsCollectionCommandHandler(ITfsCollectionRepository repository)
    : ICommandHandler<DeleteTfsCollectionCommand, Unit>
{
    public async Task<Unit> HandleAsync(DeleteTfsCollectionCommand command, CancellationToken ct = default)
    {
        var collection = await repository.GetByIdAsync(command.Id, ct)
            ?? throw new InvalidOperationException($"TFS collection with ID {command.Id} does not exist.");

        collection.IsDeleted = true;
        await repository.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
