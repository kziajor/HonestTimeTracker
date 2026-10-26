using HonestTimeTracker.Application.Tfs;
using System.Text;

namespace HonestTimeTracker.Desktop.Features.Tasks;

public static class TfsSyncSummaryFormatter
{
    public static string Format(TfsSyncResult result)
    {
        var sb = new StringBuilder();

        if (result.ClosedTasks.Count > 0)
        {
            sb.AppendLine($"Closed tasks ({result.ClosedTasks.Count}):");
            foreach (var t in result.ClosedTasks)
                sb.AppendLine($"  • #{t.WorkItemId} {t.Title} — {t.State} ({t.CollectionName})");
        }

        if (result.SkippedTasks.Count > 0)
        {
            AppendSeparator(sb);
            sb.AppendLine($"Skipped tasks ({result.SkippedTasks.Count}):");
            foreach (var t in result.SkippedTasks)
                sb.AppendLine($"  • #{t.WorkItemId} {t.Title} — {t.Reason}");
        }

        if (result.Errors.Count > 0)
        {
            AppendSeparator(sb);
            sb.AppendLine("Errors:");
            foreach (var e in result.Errors)
                sb.AppendLine($"  • {e.CollectionName}: {e.Message}");
        }

        return sb.ToString().TrimEnd();
    }

    private static void AppendSeparator(StringBuilder sb)
    {
        if (sb.Length > 0) sb.AppendLine();
    }
}
