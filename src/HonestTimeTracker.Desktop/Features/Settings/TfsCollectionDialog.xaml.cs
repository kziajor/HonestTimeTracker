using HonestTimeTracker.Application.Tfs;
using System.Windows;
using System.Windows.Media;

namespace HonestTimeTracker.Desktop.Features.Settings;

public partial class TfsCollectionDialog : Window
{
    private readonly TfsCollectionDto? _existing;
    private readonly Func<string, string?, Task<string>> _testConnection;
    private readonly Func<TfsCollectionDialog, Task> _save;

    public string CollectionName => NameBox.Text.Trim();
    public string Url => UrlBox.Text.Trim();
    /// Null when the field is empty (keep the stored token when editing).
    public string? Pat => string.IsNullOrWhiteSpace(PatBox.Password) ? null : PatBox.Password.Trim();
    public bool IsCollectionEnabled => EnabledCheckBox.IsChecked == true;

    /// <param name="testConnection">Tests (url, pat) and returns the authenticated user name; an empty PAT means the stored one.</param>
    /// <param name="save">Persists the dialog values; an exception keeps the dialog open and shows the message.</param>
    public TfsCollectionDialog(
        TfsCollectionDto? existing,
        Func<string, string?, Task<string>> testConnection,
        Func<TfsCollectionDialog, Task> save)
    {
        InitializeComponent();
        _existing = existing;
        _testConnection = testConnection;
        _save = save;

        if (existing is not null)
        {
            TitleText.Text = "Edit TFS collection";
            NameBox.Text = existing.Name;
            UrlBox.Text = existing.Url;
            EnabledCheckBox.IsChecked = existing.Enabled;
            OkButton.Content = "Save changes";
            if (existing.HasPat)
                PatHintText.Text = "Leave empty to keep the stored token. Required scope: Work Items (Read).";
        }

        Loaded += (_, _) => NameBox.Focus();
    }

    private string? Validate()
    {
        if (string.IsNullOrWhiteSpace(CollectionName))
            return "Collection name is required.";
        if (!Uri.TryCreate(Url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return "Collection URL must be an absolute http(s) address.";
        if (Pat is null && _existing?.HasPat != true)
            return "Personal access token is required.";
        return null;
    }

    private async void Test_Click(object sender, RoutedEventArgs e)
    {
        var error = Validate();
        if (error is not null)
        {
            ShowTestResult(error, success: false);
            return;
        }

        TestButton.IsEnabled = false;
        TestResultText.Foreground = Brushes.Gray;
        TestResultText.Text = "Testing…";
        try
        {
            var user = await _testConnection(Url, Pat);
            ShowTestResult($"✔ Connected as {user}", success: true);
        }
        catch (Exception ex)
        {
            ShowTestResult($"✖ {ex.Message}", success: false);
        }
        finally
        {
            TestButton.IsEnabled = true;
        }
    }

    private void ShowTestResult(string text, bool success)
    {
        TestResultText.Text = text;
        TestResultText.Foreground = new SolidColorBrush(success
            ? Color.FromRgb(0x2E, 0x7D, 0x32)
            : Color.FromRgb(0xC6, 0x28, 0x28));
    }

    private async void Ok_Click(object sender, RoutedEventArgs e)
    {
        var error = Validate();
        if (error is not null)
        {
            ShowError(error);
            return;
        }

        OkButton.IsEnabled = false;
        try
        {
            await _save(this);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            OkButton.IsEnabled = true;
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
