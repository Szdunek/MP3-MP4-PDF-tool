using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using ConverterSplitter.Services;
using ConverterSplitter.Views;

namespace ConverterSplitter;

public partial class App : Application
{
    [Conditional("DEBUG")]
    public static void Log(string msg)
    {
        try
        {
            var logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log");
            File.AppendAllText(logFile, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\n");
        }
        catch { }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        Log("=== START ===");

#if DEBUG
        DispatcherUnhandledException += (_, ex) =>
        {
            Log($"DISPATCHER: {ex.Exception}");
            ex.Handled = true;
            MessageBox.Show(ex.Exception.ToString(), "Error");
        };
        AppDomain.CurrentDomain.UnhandledException += (_, ex) => Log($"DOMAIN: {ex.ExceptionObject}");
        TaskScheduler.UnobservedTaskException += (_, ex) => { Log($"TASK: {ex.Exception}"); ex.SetObserved(); };
#endif

        try
        {
            base.OnStartup(e);
            Log("Creating MainWindow");
            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            Log("Showing MainWindow");
            mainWindow.Show();
            Log("MainWindow shown");

            if (!FFmpegDownloader.IsFFmpegAvailable())
            {
                Log("Showing FFmpeg dialog");
                new FFmpegDownloadWindow { Owner = mainWindow }.ShowDialog();
            }
            Log("Startup done");
        }
        catch (Exception ex)
        {
            Log($"FATAL: {ex}");
#if DEBUG
            MessageBox.Show(ex.ToString(), "Startup Error");
#endif
        }
    }
}
