using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConverterSplitter.Services;
using Microsoft.Win32;

namespace ConverterSplitter.ViewModels;

public partial class VideoConverterViewModel : ObservableObject
{
    public static readonly string[] SupportedFormats = [".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpeg", ".mpg", ".3gp"];
    public static readonly int[] Bitrates = [128, 192, 256, 320];

    [ObservableProperty] private ObservableCollection<VideoFileItem> _files = [];
    [ObservableProperty] private int _selectedBitrate = 192;
    [ObservableProperty] private bool _isConverting;
    [ObservableProperty] private double _overallProgress;
    [ObservableProperty] private string _statusText = "Drag & drop video files or click Browse";
    [ObservableProperty] private string? _outputFolder;
    [ObservableProperty] private bool _ffmpegAvailable;

    public VideoConverterViewModel()
    {
        FfmpegAvailable = FFmpegDownloader.IsFFmpegAvailable();
    }

    [RelayCommand]
    private void BrowseFiles()
    {
        var dlg = new OpenFileDialog
        {
            Multiselect = true,
            Filter = "Video Files|*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv;*.webm;*.m4v;*.mpeg;*.mpg;*.3gp|All Files|*.*"
        };
        if (dlg.ShowDialog() == true)
        {
            foreach (var file in dlg.FileNames)
                AddFile(file);
        }
    }

    [RelayCommand]
    private void BrowseOutputFolder()
    {
        var dlg = new OpenFolderDialog();
        if (dlg.ShowDialog() == true)
            OutputFolder = dlg.FolderName;
    }

    public void AddFile(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (!SupportedFormats.Contains(ext)) return;
        if (Files.Any(f => f.FilePath == path)) return;

        Files.Add(new VideoFileItem
        {
            FileName = Path.GetFileName(path),
            FilePath = path,
            FileSize = new FileInfo(path).Length
        });
        StatusText = $"{Files.Count} file(s) ready for conversion";
    }

    [RelayCommand]
    private void RemoveFile(VideoFileItem item)
    {
        Files.Remove(item);
        StatusText = Files.Count > 0 ? $"{Files.Count} file(s) ready" : "Drag & drop video files or click Browse";
    }

    [RelayCommand]
    private void ClearFiles()
    {
        Files.Clear();
        StatusText = "Drag & drop video files or click Browse";
    }

    [RelayCommand]
    private async Task ConvertAllAsync(CancellationToken ct)
    {
        if (Files.Count == 0) return;

        var outDir = OutputFolder;
        if (string.IsNullOrEmpty(outDir))
        {
            outDir = Path.GetDirectoryName(Files[0].FilePath)!;
        }

        IsConverting = true;
        OverallProgress = 0;
        int completed = 0;

        try
        {
            foreach (var file in Files)
            {
                ct.ThrowIfCancellationRequested();
                file.Status = "Converting...";
                file.Progress = 0;

                var outputPath = Path.Combine(outDir, Path.GetFileNameWithoutExtension(file.FilePath) + ".mp3");
                int counter = 1;
                while (File.Exists(outputPath))
                {
                    outputPath = Path.Combine(outDir, $"{Path.GetFileNameWithoutExtension(file.FilePath)} ({counter++}).mp3");
                }

                var progress = new Progress<double>(p =>
                {
                    file.Progress = p;
                    OverallProgress = (completed * 100.0 + p) / Files.Count;
                });

                try
                {
                    await FFmpegService.ConvertVideoToMp3Async(file.FilePath, outputPath, SelectedBitrate, progress, ct);
                    file.Status = "Done";
                    file.Progress = 100;
                    completed++;
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    file.Status = $"Error: {ex.Message}";
                }

                OverallProgress = completed * 100.0 / Files.Count;
            }

            StatusText = $"Conversion complete! {completed}/{Files.Count} files converted.";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Conversion cancelled.";
        }
        finally
        {
            IsConverting = false;
        }
    }

    public void HandleDrop(DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
            foreach (var file in files)
                AddFile(file);
        }
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
