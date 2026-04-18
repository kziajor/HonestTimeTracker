using HonestTimeTracker.Application.Projects;
using System.Windows;
using System.Windows.Input;

namespace HonestTimeTracker.Desktop.Features.Tasks;

public partial class TaskDialog : Window
{
    public string TaskTitle => TitleBox.Text.Trim();
    public int PlannedMinutes { get; private set; }
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
            PlannedHoursBox.Text = (existingPlannedMinutes / 60.0).ToString("F2", System.Globalization.CultureInfo.CurrentCulture);
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

        var hoursText = PlannedHoursBox.Text.Replace(',', '.');
        if (!double.TryParse(hoursText, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var hours) || hours < 0)
        {
            MessageBox.Show("Planned time must be a non-negative number of hours (e.g. 8 or 8.50).", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            PlannedHoursBox.Focus();
            return;
        }
        PlannedMinutes = (int)Math.Round(hours * 60);

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
