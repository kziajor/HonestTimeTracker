using FluentValidation;

namespace HonestTimeTracker.Application.Tfs;

/// A null or empty Pat keeps the currently stored token.
public record UpdateTfsCollectionCommand(int Id, string Name, string Url, string? Pat, bool Enabled) : ICommand<Unit>;

public class UpdateTfsCollectionCommandValidator : AbstractValidator<UpdateTfsCollectionCommand>
{
    public UpdateTfsCollectionCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).ValidCollectionName();
        RuleFor(x => x.Url).ValidCollectionUrl();
    }
}

public class UpdateTfsCollectionCommandHandler(ITfsCollectionRepository repository, ISecretProtector secretProtector)
    : ICommandHandler<UpdateTfsCollectionCommand, Unit>
{
    public async Task<Unit> HandleAsync(UpdateTfsCollectionCommand command, CancellationToken ct = default)
    {
        var collection = await repository.GetByIdAsync(command.Id, ct)
            ?? throw new InvalidOperationException($"TFS collection with ID {command.Id} does not exist.");

        var name = command.Name.Trim();
        var url = TfsCollectionRules.NormalizeUrl(command.Url);

        if (await repository.NameExistsAsync(name, command.Id, ct))
            throw new InvalidOperationException($"A collection named \"{name}\" already exists.");
        if (await repository.UrlExistsAsync(url, command.Id, ct))
            throw new InvalidOperationException($"A collection with URL \"{url}\" already exists.");

        collection.Name = name;
        collection.Url = url;
        collection.Enabled = command.Enabled;
        if (!string.IsNullOrWhiteSpace(command.Pat))
            collection.EncryptedPat = secretProtector.Protect(command.Pat.Trim());

        await repository.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
