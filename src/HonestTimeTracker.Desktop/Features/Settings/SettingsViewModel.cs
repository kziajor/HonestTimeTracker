using HonestTimeTracker.Application;
using HonestTimeTracker.Application.Settings;
using HonestTimeTracker.Desktop.Common;
using HonestTimeTracker.Desktop.Features.FloatingTimer;
using Microsoft.Extensions.DependencyInjection;
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

    public ICommand SaveCommand { get; }

    public SettingsViewModel(IServiceScopeFactory scopeFactory, FloatingTimerViewModel floatingTimerViewModel)
    {
        _scopeFactory = scopeFactory;
        _floatingTimerViewModel = floatingTimerViewModel;
        SaveCommand = new AsyncRelayCommand(_ => SaveAsync());
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
            await handler.HandleAsync(new UpdateSettingsCommand(dailyHours, defaultTaskHours, ShowFloatingTimer));
            _floatingTimerViewModel.ShowFloatingTimer = ShowFloatingTimer;
            MessageBox.Show("Settings saved.", "Settings",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
