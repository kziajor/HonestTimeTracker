using HonestTimeTracker.Application.Projects;
using System.Windows;
using System.Windows.Input;

namespace HonestTimeTracker.Desktop.Features.Tasks;

public partial class TaskDialog : Window
{
    public string TaskTitle => TitleBox.Text.Trim();
    public int PlannedMinutes => int.TryParse(PlannedMinutesBox.Text, out var v) ? Math.Max(0, v) : 0;
    public int ProjectId => ((ProjectDto)ProjectComboBox.SelectedItem).Id;

    public TaskDialog(IEnumerable<ProjectDto> projects, string? existingTitle = null, int existingPlannedMinutes = 0, int? existingProjectId = null)
    {
        InitializeComponent();

        var list = projects.ToList();
        ProjectComboBox.ItemsSource = list;
        ProjectComboBox.SelectedItem = existingProjectId.HasValue
            ? list.FirstOrDefault(p => p.Id == existingProjectId.Value)
            : null;

        if (existingTitle is not null)
        {
            TitleText.Text = "Edit task";
            TitleBox.Text = existingTitle;
            PlannedMinutesBox.Text = existingPlannedMinutes.ToString();
            OkButton.Content = "Save changes";
        }

        Loaded += (_, _) =>
        {
            TitleBox.Focus();
            TitleBox.SelectAll();
        };
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TaskTitle))
        {
            MessageBox.Show("Task title cannot be empty.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            TitleBox.Focus();
            return;
        }

        if (!int.TryParse(PlannedMinutesBox.Text, out var minutes) || minutes < 0)
        {
            MessageBox.Show("Planned time must be a non-negative number.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            PlannedMinutesBox.Focus();
            return;
        }

        if (ProjectComboBox.SelectedItem is null)
        {
            MessageBox.Show("Please select a project.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            ProjectComboBox.Focus();
            return;
        }

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void TitleBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) Ok_Click(sender, e);
        if (e.Key == Key.Escape) Cancel_Click(sender, e);
    }
}
