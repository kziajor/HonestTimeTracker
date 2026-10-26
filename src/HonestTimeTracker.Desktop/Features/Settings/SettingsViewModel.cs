using HonestTimeTracker.Application;
using HonestTimeTracker.Application.Settings;
using HonestTimeTracker.Application.Tfs;
using HonestTimeTracker.Desktop.Common;
using HonestTimeTracker.Desktop.Features.FloatingTimer;
using HonestTimeTracker.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace HonestTimeTracker.Desktop.Features.Settings;

public class SettingsViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly FloatingTimerViewModel _floatingTimerViewModel;

    private string _dailyWorkHours = "8";
    private string _defaultTaskPlannedHours = string.Empty;
    private bool _showFloatingTimer = true;
    private bool _verboseTfsLogging;

    public string DailyWorkHours
    {
        get => _dailyWorkHours;
        set => Set(ref _dailyWorkHours, value);
    }

    public string DefaultTaskPlannedHours
    {
        get => _defaultTaskPlannedHours;
        set => Set(ref _defaultTaskPlannedHours, value);
    }

    public bool ShowFloatingTimer
    {
        get => _showFloatingTimer;
        set => Set(ref _showFloatingTimer, value);
    }

    public bool VerboseTfsLogging
    {
        get => _verboseTfsLogging;
        set => Set(ref _verboseTfsLogging, value);
    }

    public ObservableCollection<TfsCollectionRowViewModel> TfsCollections { get; } = [];

    public ICommand SaveCommand { get; }
    public ICommand OpenLogFolderCommand { get; }
    public ICommand AddCollectionCommand { get; }
    public ICommand EditCollectionCommand { get; }
    public ICommand DeleteCollectionCommand { get; }
    public ICommand TestCollectionCommand { get; }

    public SettingsViewModel(IServiceScopeFactory scopeFactory, FloatingTimerViewModel floatingTimerViewModel)
    {
        _scopeFactory = scopeFactory;
        _floatingTimerViewModel = floatingTimerViewModel;
        SaveCommand = new AsyncRelayCommand(_ => SaveAsync());
        OpenLogFolderCommand = new RelayCommand(_ => OpenLogFolder());
        AddCollectionCommand = new AsyncRelayCommand(_ => AddCollectionAsync());
        EditCollectionCommand = new AsyncRelayCommand(p => EditCollectionAsync((TfsCollectionRowViewModel)p!), p => p is TfsCollectionRowViewModel);
        DeleteCollectionCommand = new AsyncRelayCommand(p => DeleteCollectionAsync((TfsCollectionRowViewModel)p!), p => p is TfsCollectionRowViewModel);
        TestCollectionCommand = new AsyncRelayCommand(p => TestCollectionAsync((TfsCollectionRowViewModel)p!),
            p => p is TfsCollectionRowViewModel { TestState: not ConnectionTestState.Testing });
    }

    public async Task LoadAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<GetSettingsQuery, SettingsDto>>();
        var dto = await handler.HandleAsync(new GetSettingsQuery());

        DailyWorkHours = dto.DailyWorkHours.ToString(System.Globalization.CultureInfo.CurrentCulture);
        DefaultTaskPlannedHours = dto.DefaultTaskPlannedHours.HasValue
            ? dto.DefaultTaskPlannedHours.Value.ToString(System.Globalization.CultureInfo.CurrentCulture)
            : string.Empty;
        ShowFloatingTimer = dto.ShowFloatingTimer;
        VerboseTfsLogging = dto.VerboseTfsLogging;

        await LoadCollectionsAsync();
    }

    private async Task LoadCollectionsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<GetTfsCollectionsQuery, List<TfsCollectionDto>>>();
        var collections = await handler.HandleAsync(new GetTfsCollectionsQuery());
        TfsCollections.Clear();
        foreach (var c in collections) TfsCollections.Add(new TfsCollectionRowViewModel(c));
    }

    private async Task SaveAsync()
    {
        var dailyText = DailyWorkHours.Replace(',', '.');
        if (!double.TryParse(dailyText, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var dailyHours) || dailyHours <= 0)
        {
            MessageBox.Show("Daily work hours must be a positive number.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        double? defaultTaskHours = null;
        var defaultText = DefaultTaskPlannedHours.Trim().Replace(',', '.');
        if (!string.IsNullOrEmpty(defaultText))
        {
            if (!double.TryParse(defaultText, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var parsed) || parsed <= 0)
            {
                MessageBox.Show("Default task planned hours must be a positive number, or leave empty.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            defaultTaskHours = parsed;
        }

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateSettingsCommand, Unit>>();
        try
        {
            await handler.HandleAsync(new UpdateSettingsCommand(dailyHours, defaultTaskHours, ShowFloatingTimer, VerboseTfsLogging));
            _floatingTimerViewModel.ShowFloatingTimer = ShowFloatingTimer;
            AppLogging.SetVerboseTfsLogging(VerboseTfsLogging);
            MessageBox.Show("Settings saved.", "Settings",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static void OpenLogFolder()
    {
        try
        {
            Directory.CreateDirectory(AppLogging.LogDirectory);
            Process.Start(new ProcessStartInfo { FileName = AppLogging.LogDirectory, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task AddCollectionAsync()
    {
        var dialog = new TfsCollectionDialog(null,
            (url, pat) => TestConnectionAsync(url, pat, null),
            async d =>
            {
                using var scope = _scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateTfsCollectionCommand, int>>();
                await handler.HandleAsync(new CreateTfsCollectionCommand(d.CollectionName, d.Url, d.Pat ?? string.Empty, d.IsCollectionEnabled));
            })
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (dialog.ShowDialog() == true)
            await LoadCollectionsAsync();
    }

    private async Task EditCollectionAsync(TfsCollectionRowViewModel row)
    {
        var dialog = new TfsCollectionDialog(row.Dto,
            (url, pat) => TestConnectionAsync(url, pat, row.Dto.Id),
            async d =>
            {
                using var scope = _scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateTfsCollectionCommand, Unit>>();
                await handler.HandleAsync(new UpdateTfsCollectionCommand(row.Dto.Id, d.CollectionName, d.Url, d.Pat, d.IsCollectionEnabled));
            })
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (dialog.ShowDialog() == true)
            await LoadCollectionsAsync();
    }

    private async Task DeleteCollectionAsync(TfsCollectionRowViewModel row)
    {
        var confirm = MessageBox.Show(
            $"Are you sure you want to delete TFS collection \"{row.Name}\"?\nTasks linked to it will be skipped during TFS sync.",
            "Delete TFS collection",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<DeleteTfsCollectionCommand, Unit>>();
        try
        {
            await handler.HandleAsync(new DeleteTfsCollectionCommand(row.Dto.Id));
            await LoadCollectionsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task TestCollectionAsync(TfsCollectionRowViewModel row)
    {
        row.SetTesting();
        try
        {
            var user = await TestConnectionAsync(row.Url, null, row.Dto.Id);
            row.SetSucceeded(user);
        }
        catch (Exception ex)
        {
            row.SetFailed(ex.Message);
        }
    }

    private async Task<string> TestConnectionAsync(string url, string? pat, int? collectionId)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<TestTfsConnectionQuery, string>>();
        return await handler.HandleAsync(new TestTfsConnectionQuery(url, pat, collectionId));
    }
}
