namespace HonestTimeTracker.Application.Tfs;

public static class TfsTaskTitleComposer
{
    public const int MaxTitleLength = 500;
    private const string Separator = " - ";
    private const string Ellipsis = "…";

    /// Builds "{parent} - {title}". The parent title is shortened when the result would exceed the limit.
    public static string Compose(string? parentTitle, string title, int maxLength = MaxTitleLength)
    {
        title = title.Trim();
        parentTitle = parentTitle?.Trim();

        if (title.Length > maxLength)
            return title[..(maxLength - Ellipsis.Length)] + Ellipsis;

        if (string.IsNullOrEmpty(parentTitle))
            return title;

        var composed = parentTitle + Separator + title;
        if (composed.Length <= maxLength)
            return composed;

        var parentBudget = maxLength - Separator.Length - title.Length - Ellipsis.Length;
        if (parentBudget <= 0)
            return title;

        return parentTitle[..parentBudget].TrimEnd() + Ellipsis + Separator + title;
    }
}
