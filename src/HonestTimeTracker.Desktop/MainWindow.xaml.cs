using HonestTimeTracker.Desktop.Features.Projects;
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

            default:
                MainContent.Content = new TextBlock
                {
                    Text = "Sekcja w budowie.",
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontSize = 16,
                    Foreground = System.Windows.Media.Brushes.Gray
                };
                break;
        }
    }
}
