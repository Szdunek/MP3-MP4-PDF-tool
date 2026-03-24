using System.Diagnostics;
using System.Windows;
using ConverterSplitter.Services;

namespace ConverterSplitter.Views;

public partial class UpdateWindow : Window
{
    private readonly UpdateInfo _updateInfo;
    private CancellationTokenSource? _cts;

    public UpdateWindow(UpdateInfo updateInfo)
    {
        InitializeComponent();
        _updateInfo = updateInfo;

        CurrentVersionText.Text = $"v{updateInfo.CurrentVersion.ToString(3)}";
        LatestVersionText.Text = $"v{updateInfo.LatestVersion.ToString(3)}";
        ReleaseNameText.Text = updateInfo.ReleaseName;
        ReleaseDateText.Text = $"Released: {updateInfo.PublishedAt:yyyy-MM-dd}";

        if (updateInfo.DownloadSize > 0)
        {
            var sizeMb = updateInfo.DownloadSize / (1024.0 * 1024);
            UpdateButtonText.Text = $"Download & Install ({sizeMb:F0} MB)";
        }

        if (string.IsNullOrEmpty(updateInfo.DownloadUrl))
        {
            UpdateButton.IsEnabled = false;
            UpdateButton.ToolTip = "No download available for this platform";
        }
    }

    private async void OnUpdateClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_updateInfo.DownloadUrl)) return;

        UpdateButton.IsEnabled = false;
        GithubButton.IsEnabled = false;
        LaterButton.IsEnabled = false;
        ProgressBar.Visibility = Visibility.Visible;

        _cts = new CancellationTokenSource();
        var progress = new Progress<(string status, double percent)>(report =>
        {
            ProgressText.Text = report.status;
            ProgressBar.Value = report.percent;
        });

        try
        {
            await UpdateService.DownloadAndApplyUpdateAsync(_updateInfo.DownloadUrl, progress, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            ProgressText.Text = "Update cancelled.";
            ResetButtons();
        }
        catch (Exception ex)
        {
            ProgressText.Text = $"Update failed: {ex.Message}";
            ResetButtons();
        }
    }

    private void OnGithubClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = _updateInfo.ReleaseUrl,
            UseShellExecute = true
        });
    }

    private void OnLaterClick(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        Close();
    }

    private void ResetButtons()
    {
        UpdateButton.IsEnabled = true;
        GithubButton.IsEnabled = true;
        LaterButton.IsEnabled = true;
        ProgressBar.Visibility = Visibility.Collapsed;
    }

    protected override void OnClosed(EventArgs e)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        base.OnClosed(e);
    }
}
