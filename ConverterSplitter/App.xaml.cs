using System.IO;
using System.Windows;
using System.Windows.Threading;
using ConverterSplitter.Services;
using ConverterSplitter.Views;

namespace ConverterSplitter;

public partial class App : Application
{
    private static readonly string LogDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
    private static readonly string LogFile = Path.Combine(LogDir, $"app_{DateTime.Now:yyyyMMdd_HHmmss}.log");

    public static void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(LogDir);
            var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}";
            File.AppendAllText(LogFile, line);
        }
        catch { }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        Log("=== Application starting ===");

        // Global exception handlers
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        try
        {
            Log("Calling base.OnStartup");
            base.OnStartup(e);

            Log("Creating MainWindow");
            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            Log("MainWindow created, showing...");
            mainWindow.Show();
            Log("MainWindow shown");

            Log("Checking FFmpeg availability...");
            bool ffmpegAvailable = FFmpegDownloader.IsFFmpegAvailable();
            Log($"FFmpeg available: {ffmpegAvailable}");

            if (!ffmpegAvailable)
            {
                Log("Showing FFmpeg download dialog");
                var dlg = new FFmpegDownloadWindow { Owner = mainWindow };
                dlg.ShowDialog();
                Log("FFmpeg dialog closed");
            }

            Log("Startup complete");
        }
        catch (Exception ex)
        {
            Log($"FATAL in OnStartup: {ex}");
            MessageBox.Show($"Startup error:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log($"DISPATCHER EXCEPTION: {e.Exception}");
        e.Handled = true;
        MessageBox.Show($"Error: {e.Exception.Message}\n\nCheck logs: {LogDir}", "Error",
            MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private static void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Log($"DOMAIN EXCEPTION (terminating={e.IsTerminating}): {e.ExceptionObject}");
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Log($"UNOBSERVED TASK EXCEPTION: {e.Exception}");
        e.SetObserved();
    }
}
