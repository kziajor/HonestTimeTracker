namespace HonestTimeTracker.Application.Tfs;

/// Work item state categories defined by Azure DevOps Server.
public static class TfsStateCategories
{
    public const string Proposed = "Proposed";
    public const string InProgress = "InProgress";
    public const string Resolved = "Resolved";
    public const string Completed = "Completed";
    public const string Removed = "Removed";

    private static readonly HashSet<string> Known =
        new([Proposed, InProgress, Resolved, Completed, Removed], StringComparer.OrdinalIgnoreCase);

    public static bool IsKnown(string? category) => category is not null && Known.Contains(category);

    public static bool IsFinished(string? category) =>
        string.Equals(category, Completed, StringComparison.OrdinalIgnoreCase)
        || string.Equals(category, Removed, StringComparison.OrdinalIgnoreCase);
}
