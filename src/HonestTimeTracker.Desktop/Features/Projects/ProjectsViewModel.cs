using HonestTimeTracker.Application;
using HonestTimeTracker.Application.Projects;
using HonestTimeTracker.Desktop.Common;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace HonestTimeTracker.Desktop.Features.Projects;

public class ProjectsViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private bool _showClosed = false;

    public ObservableCollection<ProjectDto> Projects { get; } = [];

    public bool ShowClosed
    {
        get => _showClosed;
        set { if (Set(ref _showClosed, value)) _ = LoadAsync(); }
    }

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ToggleClosedCommand { get; }

    public ProjectsViewModel(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        AddCommand = new AsyncRelayCommand(_ => AddAsync());
        EditCommand = new AsyncRelayCommand(p => EditAsync((ProjectDto)p!), p => p is ProjectDto);
        DeleteCommand = new AsyncRelayCommand(p => DeleteAsync((ProjectDto)p!), p => p is ProjectDto);
        ToggleClosedCommand = new AsyncRelayCommand(p => ToggleClosedAsync((ProjectDto)p!), p => p is ProjectDto);
    }

    public async Task LoadAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<GetProjectsQuery, List<ProjectDto>>>();
        var projects = await handler.HandleAsync(new GetProjectsQuery(ShowClosed));
        Projects.Clear();
        foreach (var p in projects) Projects.Add(p);
    }

    private async Task AddAsync()
    {
        var dialog = new ProjectDialog { Owner = System.Windows.Application.Current.MainWindow };
        if (dialog.ShowDialog() != true) return;

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateProjectCommand, int>>();
        try
        {
            await handler.HandleAsync(new CreateProjectCommand(dialog.ProjectName));
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task EditAsync(ProjectDto project)
    {
        var dialog = new ProjectDialog(project.Name) { Owner = System.Windows.Application.Current.MainWindow };
        if (dialog.ShowDialog() != true) return;

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateProjectCommand, Unit>>();
        try
        {
            await handler.HandleAsync(new UpdateProjectCommand(project.Id, dialog.ProjectName));
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task DeleteAsync(ProjectDto project)
    {
        var confirm = MessageBox.Show(
            $"Are you sure you want to delete project \"{project.Name}\"?\nThis action cannot be undone.",
            "Delete project",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<DeleteProjectCommand, Unit>>();
        try
        {
            await handler.HandleAsync(new DeleteProjectCommand(project.Id));
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task ToggleClosedAsync(ProjectDto project)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ToggleProjectClosedCommand, Unit>>();
        try
        {
            await handler.HandleAsync(new ToggleProjectClosedCommand(project.Id));
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
