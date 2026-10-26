namespace HonestTimeTracker.Application.Tfs;

public record TfsCollectionDto(int Id, string Name, string Url, bool HasPat, bool Enabled);
