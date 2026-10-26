using FluentValidation;
using HonestTimeTracker.Domain;

namespace HonestTimeTracker.Application.Tfs;

public record CreateTfsCollectionCommand(string Name, string Url, string Pat, bool Enabled) : ICommand<int>;

public class CreateTfsCollectionCommandValidator : AbstractValidator<CreateTfsCollectionCommand>
{
    public CreateTfsCollectionCommandValidator()
    {
        RuleFor(x => x.Name).ValidCollectionName();
        RuleFor(x => x.Url).ValidCollectionUrl();
        RuleFor(x => x.Pat).NotEmpty().WithMessage("Personal access token is required.");
    }
}

public class CreateTfsCollectionCommandHandler(ITfsCollectionRepository repository, ISecretProtector secretProtector)
    : ICommandHandler<CreateTfsCollectionCommand, int>
{
    public async Task<int> HandleAsync(CreateTfsCollectionCommand command, CancellationToken ct = default)
    {
        var name = command.Name.Trim();
        var url = TfsCollectionRules.NormalizeUrl(command.Url);

        if (await repository.NameExistsAsync(name, null, ct))
            throw new InvalidOperationException($"A collection named \"{name}\" already exists.");
        if (await repository.UrlExistsAsync(url, null, ct))
            throw new InvalidOperationException($"A collection with URL \"{url}\" already exists.");

        var collection = new TfsCollection
        {
            Name = name,
            Url = url,
            EncryptedPat = secretProtector.Protect(command.Pat.Trim()),
            Enabled = command.Enabled
        };
        await repository.AddAsync(collection, ct);
        await repository.SaveChangesAsync(ct);
        return collection.Id;
    }
}
