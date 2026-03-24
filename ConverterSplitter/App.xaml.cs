using System.Windows;
using ConverterSplitter.Services;
using ConverterSplitter.Views;

namespace ConverterSplitter;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (!FFmpegDownloader.IsFFmpegAvailable())
        {
            var dlgWindow = new FFmpegDownloadWindow();
            dlgWindow.ShowDialog();
        }
    }
}
