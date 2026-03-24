using System.Windows;
using ConverterSplitter.Services;
using ConverterSplitter.Views;

namespace ConverterSplitter;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();

        if (!FFmpegDownloader.IsFFmpegAvailable())
        {
            var dlg = new FFmpegDownloadWindow { Owner = mainWindow };
            dlg.ShowDialog();
        }
    }
}
