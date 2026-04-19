using HonestTimeTracker.Application.Records;
using HonestTimeTracker.Application.Tasks;
using System.Windows;
using System.Windows.Input;

namespace HonestTimeTracker.Desktop.Features.Records;

public partial class RecordDialog : Window
{
    private readonly bool _allowRunning;
    private readonly Func<DateTime, DateTime?, Task<bool>>? _overlapChecker;

    public int SelectedTaskId => ((TaskDto)TaskComboBox.SelectedItem).Id;
    public string? Comment => string.IsNullOrWhiteSpace(CommentBox.Text) ? null : CommentBox.Text.Trim();

    public DateTime StartedAt => CombineDateTime(DatePicker.SelectedDate!.Value, StartTimeBox.Text);
    public DateTime? FinishedAt => string.IsNullOrWhiteSpace(EndTimeBox.Text)
        ? null
        : CombineDateTime(DatePicker.SelectedDate!.Value, EndTimeBox.Text);

    public RecordDialog(IEnumerable<TaskDto> tasks, RecordDto? existing = null, DateTime? defaultDate = null, bool allowRunning = false, Func<DateTime, DateTime?, Task<bool>>? overlapChecker = null)
    {
        InitializeComponent();
        _allowRunning = allowRunning;
        _overlapChecker = overlapChecker;

        var taskList = tasks.ToList();
        TaskComboBox.ItemsSource = taskList;

        if (allowRunning)
            EndTimeHint.Visibility = Visibility.Visible;

        if (existing is not null)
        {
            TitleText.Text = "Edit record";
            OkButton.Content = "Save changes";
            TaskComboBox.SelectedItem = taskList.FirstOrDefault(t => t.Id == existing.TaskId);
            DatePicker.SelectedDate = existing.StartedAt.Date;
            StartTimeBox.Text = existing.StartedAt.ToString("HH:mm");
            EndTimeBox.Text = existing.FinishedAt?.ToString("HH:mm") ?? string.Empty;
            CommentBox.Text = existing.Comment ?? string.Empty;
        }
        else
        {
            var now = DateTime.Now;
            DatePicker.SelectedDate = (defaultDate ?? now).Date;
            StartTimeBox.Text = now.ToString("HH:mm");
        }

        Loaded += (_, _) => TaskComboBox.Focus();
    }

    private async void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (TaskComboBox.SelectedItem is null)
        {
            MessageBox.Show("Please select a task.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            TaskComboBox.Focus();
            return;
        }

        if (DatePicker.SelectedDate is null)
        {
            MessageBox.Show("Please select a date.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            DatePicker.Focus();
            return;
        }

        if (!TryParseTime(StartTimeBox.Text, out _))
        {
            MessageBox.Show("Start time must be in HH:mm format (e.g. 09:30).", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            StartTimeBox.Focus();
            StartTimeBox.SelectAll();
            return;
        }

        var endEmpty = string.IsNullOrWhiteSpace(EndTimeBox.Text);

        if (endEmpty && !_allowRunning)
        {
            MessageBox.Show("End time is required. Another record is already running.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            EndTimeBox.Focus();
            return;
        }

        if (!endEmpty)
        {
            if (!TryParseTime(EndTimeBox.Text, out _))
            {
                MessageBox.Show("End time must be in HH:mm format (e.g. 17:00).", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                EndTimeBox.Focus();
                EndTimeBox.SelectAll();
                return;
            }

            if (FinishedAt <= StartedAt)
            {
                MessageBox.Show("End time must be after start time.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                EndTimeBox.Focus();
                EndTimeBox.SelectAll();
                return;
            }
        }

        if (_overlapChecker is not null && await _overlapChecker(StartedAt, FinishedAt))
        {
            MessageBox.Show("The time range overlaps with an existing record.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            StartTimeBox.Focus();
            StartTimeBox.SelectAll();
            return;
        }

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private static bool TryParseTime(string input, out TimeSpan time)
    {
        time = default;
        if (string.IsNullOrWhiteSpace(input)) return false;

        var parts = input.Trim().Split(':');
        if (parts.Length != 2) return false;
        if (!int.TryParse(parts[0], out var h) || !int.TryParse(parts[1], out var m)) return false;
        if (h < 0 || h > 23 || m < 0 || m > 59) return false;

        time = new TimeSpan(h, m, 0);
        return true;
    }

    private static DateTime CombineDateTime(DateTime date, string timeText)
    {
        TryParseTime(timeText, out var time);
        return date.Date + time;
    }
}
