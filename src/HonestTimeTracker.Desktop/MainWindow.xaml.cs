using HonestTimeTracker.Desktop.Features.Projects;
using HonestTimeTracker.Desktop.Features.Records;
using HonestTimeTracker.Desktop.Features.Settings;
using HonestTimeTracker.Desktop.Features.Tasks;
using HonestTimeTracker.Desktop.Features.Today;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace HonestTimeTracker.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await NavigateToAsync("Projects");
    }

    private async void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
            await NavigateToAsync(btn.Tag?.ToString() ?? string.Empty);
    }

    private async Task NavigateToAsync(string tag)
    {
        switch (tag)
        {
            case "Projects":
                var page = App.Services.GetRequiredService<ProjectsPage>();
                MainContent.Content = page;
                await page.InitializeAsync();
                break;

            case "Tasks":
                var tasksPage = App.Services.GetRequiredService<TasksPage>();
                MainContent.Content = tasksPage;
                await tasksPage.InitializeAsync();
                break;

            case "Records":
                var recordsPage = App.Services.GetRequiredService<RecordsPage>();
                MainContent.Content = recordsPage;
                await recordsPage.InitializeAsync();
                break;

            case "Today":
                var todayPage = App.Services.GetRequiredService<TodayPage>();
                MainContent.Content = todayPage;
                await todayPage.InitializeAsync();
                break;

            case "Settings":
                var settingsPage = App.Services.GetRequiredService<SettingsPage>();
                MainContent.Content = settingsPage;
                await settingsPage.InitializeAsync();
                break;

            default:
                MainContent.Content = new TextBlock
                {
                    Text = "Section under construction.",
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontSize = 16,
                    Foreground = System.Windows.Media.Brushes.Gray
                };
                break;
        }
    }
}
