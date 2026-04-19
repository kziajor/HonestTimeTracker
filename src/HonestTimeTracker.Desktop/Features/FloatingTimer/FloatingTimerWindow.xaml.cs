using HonestTimeTracker.Application;
using HonestTimeTracker.Application.Settings;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace HonestTimeTracker.Desktop.Features.FloatingTimer;

public partial class FloatingTimerWindow : Window
{
    private readonly FloatingTimerViewModel _viewModel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DispatcherTimer _savePositionTimer;
    private bool _initialized;

    public FloatingTimerWindow(FloatingTimerViewModel viewModel, IServiceScopeFactory scopeFactory)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _scopeFactory = scopeFactory;
        DataContext = _viewModel;

        _savePositionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _savePositionTimer.Tick += async (_, _) =>
        {
            _savePositionTimer.Stop();
            await SavePositionAsync();
        };

        _viewModel.PropertyChanged += (_, e) =>
        {
            if (_initialized && e.PropertyName == nameof(FloatingTimerViewModel.IsVisible))
                UpdateVisibility();
        };

        LocationChanged += (_, _) =>
        {
            _savePositionTimer.Stop();
            _savePositionTimer.Start();
        };
    }

    public async Task InitializeAsync()
    {
        await _viewModel.InitializeAsync();

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<GetSettingsQuery, SettingsDto>>();
        var settings = await handler.HandleAsync(new GetSettingsQuery());

        if (settings.FloatingTimerLeft.HasValue && settings.FloatingTimerTop.HasValue)
        {
            var (l, t) = ClampToScreen(settings.FloatingTimerLeft.Value, settings.FloatingTimerTop.Value);
            Left = l;
            Top = t;
        }
        else
        {
            PlaceAtDefaultPosition();
        }

        _initialized = true;
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (_viewModel.IsVisible)
        {
            if (!IsVisible) Show();
        }
        else
        {
            if (IsVisible) Hide();
        }
    }

    private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private async Task SavePositionAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<SaveFloatingTimerPositionCommand, Unit>>();
            await handler.HandleAsync(new SaveFloatingTimerPositionCommand(Left, Top));
        }
        catch { }
    }

    private void PlaceAtDefaultPosition()
    {
        var screen = System.Windows.SystemParameters.WorkArea;
        Left = screen.Right - Width - 20;
        Top = screen.Bottom - 150;
    }

    private (double left, double top) ClampToScreen(double left, double top)
    {
        var virtualLeft = SystemParameters.VirtualScreenLeft;
        var virtualTop = SystemParameters.VirtualScreenTop;
        var virtualWidth = SystemParameters.VirtualScreenWidth;
        var virtualHeight = SystemParameters.VirtualScreenHeight;

        left = Math.Max(virtualLeft, Math.Min(left, virtualLeft + virtualWidth - Width));
        top = Math.Max(virtualTop, Math.Min(top, virtualTop + virtualHeight - 100));
        return (left, top);
    }
}
