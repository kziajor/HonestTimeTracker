using FluentValidation;
using HonestTimeTracker.Domain;
using Microsoft.Extensions.Logging;

namespace HonestTimeTracker.Application.Tfs;

public record ImportTfsTaskItem(
    int TfsCollectionId,
    int TfsWorkItemId,
    string TfsProjectName,
    string Title,
    int PlannedMinutes,
    int ProjectId,
    bool IsOnTodayList);

/// Creates tasks for the selected work items and remembers the chosen project per TFS project. Returns the number of created tasks.
public record ImportTfsTasksCommand(IReadOnlyList<ImportTfsTaskItem> Items) : ICommand<int>;

public class ImportTfsTasksCommandValidator : AbstractValidator<ImportTfsTasksCommand>
{
    public ImportTfsTasksCommandValidator()
    {
        RuleFor(x => x.Items).NotEmpty().WithMessage("Select at least one work item to import.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.TfsCollectionId).GreaterThan(0);
            item.RuleFor(i => i.TfsWorkItemId).GreaterThan(0);
            item.RuleFor(i => i.Title)
                .NotEmpty().WithMessage("Task title is required.")
                .MaximumLength(TfsTaskTitleComposer.MaxTitleLength).WithMessage("Task title cannot exceed 500 characters.");
            item.RuleFor(i => i.PlannedMinutes)
                .GreaterThanOrEqualTo(0).WithMessage("Planned time cannot be negative.");
            item.RuleFor(i => i.ProjectId)
                .GreaterThan(0).WithMessage("Project is required.");
        });
    }
}

public class ImportTfsTasksCommandHandler(ITfsSyncRepository repository, ILogger<ImportTfsTasksCommandHandler> logger)
    : ICommandHandler<ImportTfsTasksCommand, int>
{
    public async Task<int> HandleAsync(ImportTfsTasksCommand command, CancellationToken ct = default)
    {
        var links = await repository.GetAllTaskLinksAsync(ct);
        var openProjectIds = (await repository.GetOpenProjectsAsync(ct)).Select(p => p.Id).ToHashSet();
        var preferences = await repository.GetImportPreferencesAsync(ct);

        var created = 0;
        foreach (var item in command.Items)
        {
            if (links.Exists(l => l.TfsWorkItemId == item.TfsWorkItemId
                    && (l.TfsCollectionId is null || l.TfsCollectionId == item.TfsCollectionId)))
            {
                logger.LogWarning("Work item {WorkItemId} from collection {CollectionId} is already linked to a task — skipped",
                    item.TfsWorkItemId, item.TfsCollectionId);
                continue;
            }

            if (!openProjectIds.Contains(item.ProjectId))
                throw new InvalidOperationException($"Project selected for work item {item.TfsWorkItemId} is closed or no longer exists.");

            await repository.AddTaskAsync(new WorkTask
            {
                Title = item.Title.Trim(),
                PlannedMinutes = item.PlannedMinutes,
                ProjectId = item.ProjectId,
                TfsWorkItemId = item.TfsWorkItemId,
                TfsCollectionId = item.TfsCollectionId,
                IsOnTodayList = item.IsOnTodayList
            }, ct);
            links.Add(new TfsTaskLink(item.TfsCollectionId, item.TfsWorkItemId));
            created++;

            await RememberProjectAsync(preferences, item, ct);
        }

        // A single SaveChanges call keeps the whole import in one transaction.
        await repository.SaveChangesAsync(ct);
        logger.LogInformation("Imported {Count} task(s) from TFS", created);
        return created;
    }

    private async Task RememberProjectAsync(List<TfsImportPreference> preferences, ImportTfsTaskItem item, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(item.TfsProjectName)) return;

        var preference = preferences.Find(p => p.TfsCollectionId == item.TfsCollectionId
            && string.Equals(p.TfsProjectName, item.TfsProjectName, StringComparison.OrdinalIgnoreCase));
        if (preference is not null)
        {
            preference.LastProjectId = item.ProjectId;
            return;
        }

        preference = new TfsImportPreference
        {
            TfsCollectionId = item.TfsCollectionId,
            TfsProjectName = item.TfsProjectName,
            LastProjectId = item.ProjectId
        };
        preferences.Add(preference);
        await repository.AddImportPreferenceAsync(preference, ct);
    }
}
