using HonestTimeTracker.Application.Projects;
using HonestTimeTracker.Application.Tfs;
using HonestTimeTracker.Desktop.Common;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;

namespace HonestTimeTracker.Desktop.Features.Tasks;

public class TfsImportViewModel : ViewModelBase
{
    private ProjectDto? _bulkProject;

    public TfsImportViewModel(TfsSyncResult result, IReadOnlyList<ProjectDto> projects, double? defaultPlannedHours)
    {
        Projects = projects;
        Summary = TfsSyncSummaryFormatter.Format(result);

        foreach (var candidate in result.Candidates)
        {
            var preselected = candidate.PreselectedProjectId is { } id ? projects.FirstOrDefault(p => p.Id == id) : null;
            var row = new TfsImportRowViewModel(candidate, preselected, defaultPlannedHours);
            row.PropertyChanged += OnRowChanged;
            Rows.Add(row);
        }

        RowsView = CollectionViewSource.GetDefaultView(Rows);
        RowsView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(TfsImportRowViewModel.CollectionName)));

        SelectAllCommand = new RelayCommand(_ => SetSelection(true));
        SelectNoneCommand = new RelayCommand(_ => SetSelection(false));
        ApplyBulkProjectCommand = new RelayCommand(_ => ApplyBulkProject(), _ => BulkProject is not null && Rows.Any(r => r.IsSelected));
    }

    public ObservableCollection<TfsImportRowViewModel> Rows { get; } = [];
    public ICollectionView RowsView { get; }
    public IReadOnlyList<ProjectDto> Projects { get; }
    public string Summary { get; }
    public bool HasSummary => Summary.Length > 0;

    public ProjectDto? BulkProject
    {
        get => _bulkProject;
        set => Set(ref _bulkProject, value);
    }

    public int SelectedCount => Rows.Count(r => r.IsSelected);
    public string SelectionText => $"{SelectedCount} of {Rows.Count} selected";
    public bool CanImport => SelectedCount > 0 && Rows.Where(r => r.IsSelected).All(r => r.IsValid);

    public ICommand SelectAllCommand { get; }
    public ICommand SelectNoneCommand { get; }
    public ICommand ApplyBulkProjectCommand { get; }

    public List<ImportTfsTaskItem> BuildImportItems() =>
        Rows.Where(r => r.IsSelected).Select(r => r.ToImportItem()).ToList();

    private void SetSelection(bool selected)
    {
        foreach (var row in Rows) row.IsSelected = selected;
    }

    private void ApplyBulkProject()
    {
        foreach (var row in Rows.Where(r => r.IsSelected)) row.SelectedProject = BulkProject;
    }

    private void OnRowChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(SelectionText));
        OnPropertyChanged(nameof(CanImport));
    }
}
