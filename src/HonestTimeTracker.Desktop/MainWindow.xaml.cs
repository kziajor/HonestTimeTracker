using System.Windows;
using System.Windows.Controls;

namespace HonestTimeTracker.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
            PageTitle.Text = btn.Content?.ToString() ?? string.Empty;
    }
}
