using HonestTimeTracker.Application.Leaves;
using System.Windows;
using System.Windows.Controls;

namespace HonestTimeTracker.Desktop.Features.Leaves;

public partial class LeaveDialog : Window
{
    public DateOnly StartDate => DateOnly.FromDateTime(StartDatePicker.SelectedDate!.Value);
    public DateOnly EndDate => DateOnly.FromDateTime(EndDatePicker.SelectedDate!.Value);
    public string? Description => string.IsNullOrWhiteSpace(DescriptionBox.Text) ? null : DescriptionBox.Text.Trim();

    public LeaveDialog(LeaveDto? existing = null)
    {
        InitializeComponent();

        if (existing is not null)
        {
            TitleText.Text = "Edit leave";
            OkButton.Content = "Save changes";
            StartDatePicker.SelectedDate = existing.StartDate.ToDateTime(TimeOnly.MinValue);
            EndDatePicker.SelectedDate = existing.EndDate.ToDateTime(TimeOnly.MinValue);
            DescriptionBox.Text = existing.Description ?? string.Empty;
        }
        else
        {
            StartDatePicker.SelectedDate = DateTime.Today;
            EndDatePicker.SelectedDate = DateTime.Today;
        }

        UpdateDaysLabel();

        Loaded += (_, _) => StartDatePicker.Focus();
    }

    private void DatePicker_SelectedDateChanged(object? sender, SelectionChangedEventArgs e) =>
        UpdateDaysLabel();

    private void UpdateDaysLabel()
    {
        if (StartDatePicker?.SelectedDate is null || EndDatePicker?.SelectedDate is null)
        {
            DaysLabel.Text = string.Empty;
            return;
        }

        var start = DateOnly.FromDateTime(StartDatePicker.SelectedDate.Value);
        var end = DateOnly.FromDateTime(EndDatePicker.SelectedDate.Value);
        var days = end.DayNumber - start.DayNumber + 1;

        DaysLabel.Text = days > 0
            ? $"{days} day{(days == 1 ? "" : "s")}"
            : string.Empty;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (StartDatePicker.SelectedDate is null)
        {
            MessageBox.Show("Please select a start date.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            StartDatePicker.Focus();
            return;
        }

        if (EndDatePicker.SelectedDate is null)
        {
            MessageBox.Show("Please select an end date.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            EndDatePicker.Focus();
            return;
        }

        var start = DateOnly.FromDateTime(StartDatePicker.SelectedDate.Value);
        var end = DateOnly.FromDateTime(EndDatePicker.SelectedDate.Value);

        if (end < start)
        {
            MessageBox.Show("End date must be on or after start date.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            EndDatePicker.Focus();
            return;
        }

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
