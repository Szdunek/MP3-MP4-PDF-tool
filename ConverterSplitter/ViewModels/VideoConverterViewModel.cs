using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConverterSplitter.Services;
using Microsoft.Win32;

namespace ConverterSplitter.ViewModels;

public partial class VideoConverterViewModel : ObservableObject
{
    public static readonly string[] SupportedFormats =
        [".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpeg", ".mpg", ".3gp", ".ts", ".mts"];
    public static readonly int[] Bitrates = [128, 192, 256, 320];

    [ObservableProperty] private ObservableCollection<VideoFileItem> _files = [];
    [ObservableProperty] private int _selectedBitrate = 192;
    [ObservableProperty] private bool _isConverting;
    [ObservableProperty] private double _overallProgress;
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private string? _outputFolder;
    [ObservableProperty] private bool _ffmpegAvailable;
    [ObservableProperty] private bool _showOpenButtons;
    [ObservableProperty] private string? _lastOutputPath;
    [ObservableProperty] private string? _lastOutputDir;

    public VideoConverterViewModel()
    {
        FfmpegAvailable = FFmpegDownloader.IsFFmpegAvailable();
        StatusText = Loc.I["video_status_ready"];
    }

    [RelayCommand]
    private void BrowseFiles()
    {
        var dlg = new OpenFileDialog
        {
            Multiselect = true,
            Filter = "Video Files|*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv;*.webm;*.m4v;*.mpeg;*.mpg;*.3gp;*.ts;*.mts|All Files|*.*"
        };
        if (dlg.ShowDialog() == true)
            foreach (var file in dlg.FileNames) AddFile(file);
    }

    [RelayCommand]
    private void BrowseOutputFolder()
    {
        var dlg = new OpenFolderDialog();
        if (dlg.ShowDialog() == true) OutputFolder = dlg.FolderName;
    }

    public void AddFile(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (!SupportedFormats.Contains(ext) || Files.Any(f => f.FilePath == path)) return;
        Files.Add(new VideoFileItem
        {
            FileName = Path.GetFileName(path), FilePath = path,
            FileSize = new FileInfo(path).Length
        });
        StatusText = string.Format(Loc.I["video_status_ready"], Files.Count);
        ShowOpenButtons = false;
    }

    [RelayCommand] private void RemoveFile(VideoFileItem item) { Files.Remove(item); ShowOpenButtons = false; }
    [RelayCommand] private void ClearFiles() { Files.Clear(); ShowOpenButtons = false; StatusText = Loc.I["video_status_ready"]; }

    [RelayCommand]
    private async Task ConvertAllAsync(CancellationToken ct)
    {
        if (Files.Count == 0) return;
        var outDir = OutputFolder ?? Path.GetDirectoryName(Files[0].FilePath)!;
        IsConverting = true; OverallProgress = 0; ShowOpenButtons = false;
        int completed = 0;

        try
        {
            foreach (var file in Files)
            {
                ct.ThrowIfCancellationRequested();
                file.Status = Loc.I["converting"]; file.Progress = 0;
                var outputPath = Path.Combine(outDir, Path.GetFileNameWithoutExtension(file.FilePath) + ".mp3");
                int c = 1;
                while (File.Exists(outputPath))
                    outputPath = Path.Combine(outDir, $"{Path.GetFileNameWithoutExtension(file.FilePath)} ({c++}).mp3");

                var progress = new Progress<double>(p => { file.Progress = p; OverallProgress = (completed * 100.0 + p) / Files.Count; });
                try
                {
                    await FFmpegService.ConvertVideoToMp3Async(file.FilePath, outputPath, SelectedBitrate, progress, ct);
                    file.Status = Loc.I["done"]; file.Progress = 100; completed++;
                    LastOutputPath = outputPath; LastOutputDir = outDir;
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { file.Status = $"{Loc.I["error"]}: {ex.Message}"; }
                OverallProgress = completed * 100.0 / Files.Count;
            }
            StatusText = string.Format(Loc.I["video_status_done"], completed, Files.Count);
            ShowOpenButtons = completed > 0;
        }
        catch (OperationCanceledException) { StatusText = "Cancelled."; }
        finally { IsConverting = false; }
    }

    [RelayCommand] private void OpenFile() { if (LastOutputPath != null) Process.Start(new ProcessStartInfo(LastOutputPath) { UseShellExecute = true }); }
    [RelayCommand] private void OpenFolder() { if (LastOutputDir != null) Process.Start(new ProcessStartInfo(LastOutputDir) { UseShellExecute = true }); }

    public void HandleDrop(DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
            foreach (var file in (string[])e.Data.GetData(DataFormats.FileDrop)!) AddFile(file);
    }
}

public partial class VideoFileItem : ObservableObject
{
    [ObservableProperty] private string _fileName = "";
    [ObservableProperty] private string _filePath = "";
    [ObservableProperty] private long _fileSize;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string _status = "Ready";
}
