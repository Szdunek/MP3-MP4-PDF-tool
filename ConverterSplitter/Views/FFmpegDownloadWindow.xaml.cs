using System.Windows;
using ConverterSplitter.Services;

namespace ConverterSplitter.Views;

public partial class FFmpegDownloadWindow : Window
{
    private CancellationTokenSource? _cts;

    public bool WasDownloaded { get; private set; }

    public FFmpegDownloadWindow()
    {
        InitializeComponent();
    }

    private async void OnDownloadClick(object sender, RoutedEventArgs e)
    {
        DownloadButton.IsEnabled = false;
        SkipButton.IsEnabled = false;
        ProgressBar.Visibility = Visibility.Visible;

        _cts = new CancellationTokenSource();
        var progress = new Progress<(string status, double percent)>(report =>
        {
            StatusText.Text = report.status;
            ProgressBar.Value = report.percent;
            ProgressText.Text = $"{report.percent:F0}%";
        });

        try
        {
            await FFmpegDownloader.DownloadAsync(progress, _cts.Token);
            WasDownloaded = true;
            StatusText.Text = Loc.I["ffmpeg_installed"];
            ProgressText.Text = "";
            await Task.Delay(1200);
            DialogResult = true;
            Close();
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = Loc.I["ffmpeg_cancelled"];
            ResetButtons();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"{Loc.I["ffmpeg_failed"]} {ex.Message}";
            ProgressText.Text = Loc.I["ffmpeg_retry"];
            ResetButtons();
        }
    }

    private void OnSkipClick(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        DialogResult = false;
        Close();
    }

    private void ResetButtons()
    {
        DownloadButton.IsEnabled = true;
        SkipButton.IsEnabled = true;
        ProgressBar.Visibility = Visibility.Collapsed;
    }

    protected override void OnClosed(EventArgs e)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        base.OnClosed(e);
    }
}
