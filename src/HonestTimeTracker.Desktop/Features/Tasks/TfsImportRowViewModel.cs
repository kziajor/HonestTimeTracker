using HonestTimeTracker.Application.Projects;
using HonestTimeTracker.Application.Tfs;
using HonestTimeTracker.Desktop.Common;
using System.Globalization;

namespace HonestTimeTracker.Desktop.Features.Tasks;

public class TfsImportRowViewModel : ViewModelBase
{
    private bool _isSelected = true;
    private string _title;
    private string _plannedHoursText;
    private ProjectDto? _selectedProject;
    private bool _isOnTodayList;

    public TfsImportRowViewModel(TfsImportCandidate candidate, ProjectDto? preselectedProject, double? defaultPlannedHours)
    {
        Candidate = candidate;
        _title = candidate.ProposedTitle;
        _selectedProject = preselectedProject;
        _plannedHoursText = (defaultPlannedHours ?? 0).ToString(CultureInfo.CurrentCulture);
    }

    public TfsImportCandidate Candidate { get; }
    public string CollectionName => Candidate.CollectionName;
    public int WorkItemId => Candidate.WorkItemId;
    public string WorkItemType => Candidate.WorkItemType;
    public string TeamProject => Candidate.TeamProject;

    public bool IsSelected
    {
        get => _isSelected;
        set { if (Set(ref _isSelected, value)) NotifyValidation(); }
    }

    public string Title
    {
        get => _title;
        set { if (Set(ref _title, value)) NotifyValidation(); }
    }

    public string PlannedHoursText
    {
        get => _plannedHoursText;
        set { if (Set(ref _plannedHoursText, value)) NotifyValidation(); }
    }

    public ProjectDto? SelectedProject
    {
        get => _selectedProject;
        set { if (Set(ref _selectedProject, value)) NotifyValidation(); }
    }

    public bool IsOnTodayList
    {
        get => _isOnTodayList;
        set => Set(ref _isOnTodayList, value);
    }

    public bool HasTitleError => IsSelected
        && (string.IsNullOrWhiteSpace(Title) || Title.Trim().Length > TfsTaskTitleComposer.MaxTitleLength);

    public bool HasProjectError => IsSelected && SelectedProject is null;

    public bool HasPlannedError => IsSelected && TryGetPlannedMinutes() is null;

    public bool IsValid => !HasTitleError && !HasProjectError && !HasPlannedError;

    /// Empty text means 0. Returns null when the value is not a non-negative number.
    public int? TryGetPlannedMinutes()
    {
        var text = PlannedHoursText.Trim().Replace(',', '.');
        if (text.Length == 0) return 0;
        return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var hours) && hours >= 0
            ? (int)Math.Round(hours * 60)
            : null;
    }

    public ImportTfsTaskItem ToImportItem() => new(
        Candidate.CollectionId,
        Candidate.WorkItemId,
        Candidate.TeamProject,
        Title.Trim(),
        TryGetPlannedMinutes() ?? 0,
        SelectedProject!.Id,
        IsOnTodayList);

    private void NotifyValidation()
    {
        OnPropertyChanged(nameof(HasTitleError));
        OnPropertyChanged(nameof(HasProjectError));
        OnPropertyChanged(nameof(HasPlannedError));
        OnPropertyChanged(nameof(IsValid));
    }
}
