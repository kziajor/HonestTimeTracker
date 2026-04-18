using System.Windows;
using System.Windows.Input;

namespace HonestTimeTracker.Desktop.Features.Projects;

public partial class ProjectDialog : Window
{
    public string ProjectName => NameBox.Text.Trim();

    public ProjectDialog(string? existingName = null)
    {
        InitializeComponent();

        if (existingName is not null)
        {
            TitleText.Text = "Edit project";
            NameBox.Text = existingName;
            OkButton.Content = "Save changes";
        }

        Loaded += (_, _) =>
        {
            NameBox.Focus();
            NameBox.SelectAll();
        };
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ProjectName))
        {
            MessageBox.Show("Project name cannot be empty.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            NameBox.Focus();
            return;
        }

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void NameBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) Ok_Click(sender, e);
        if (e.Key == Key.Escape) Cancel_Click(sender, e);
    }
}
