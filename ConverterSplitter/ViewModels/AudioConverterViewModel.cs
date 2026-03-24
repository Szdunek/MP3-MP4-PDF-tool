using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConverterSplitter.Services;
using Microsoft.Win32;

namespace ConverterSplitter.ViewModels;

public partial class AudioConverterViewModel : ObservableObject
{
    public static readonly string[] InputFormats = [".mp3", ".wav", ".flac", ".ogg", ".aac", ".wma", ".m4a", ".opus"];
    public static readonly string[] OutputFormats = ["MP3", "WAV", "FLAC", "OGG", "AAC"];
    public static readonly int[] Bitrates = [96, 128, 192, 256, 320];
    public static readonly int[] SampleRates = [22050, 44100, 48000, 96000];

    [ObservableProperty] private ObservableCollection<AudioFileItem> _files = [];
    [ObservableProperty] private string _selectedFormat = "MP3";
    [ObservableProperty] private int _selectedBitrate = 192;
    [ObservableProperty] private int _selectedSampleRate = 44100;
    [ObservableProperty] private bool _isConverting;
    [ObservableProperty] private double _overallProgress;
    [ObservableProperty] private string _statusText = "Drag & drop audio files or click Browse";
    [ObservableProperty] private string? _outputFolder;

    [RelayCommand]
    private void BrowseFiles()
    {
        var dlg = new OpenFileDialog
        {
            Multiselect = true,
            Filter = "Audio Files|*.mp3;*.wav;*.flac;*.ogg;*.aac;*.wma;*.m4a;*.opus|All Files|*.*"
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
        if (!InputFormats.Contains(ext)) return;
        if (Files.Any(f => f.FilePath == path)) return;

        Files.Add(new AudioFileItem
        {
            FileName = Path.GetFileName(path),
            FilePath = path,
            FileSize = new FileInfo(path).Length
        });
        StatusText = $"{Files.Count} file(s) ready for conversion";
    }

    [RelayCommand]
    private void RemoveFile(AudioFileItem item)
    {
        Files.Remove(item);
        StatusText = Files.Count > 0 ? $"{Files.Count} file(s) ready" : "Drag & drop audio files or click Browse";
    }

    [RelayCommand]
    private void ClearFiles()
    {
        Files.Clear();
        StatusText = "Drag & drop audio files or click Browse";
    }

    [RelayCommand]
    private async Task ConvertAllAsync(CancellationToken ct)
    {
        if (Files.Count == 0) return;

        var outDir = OutputFolder;
        if (string.IsNullOrEmpty(outDir))
            outDir = Path.GetDirectoryName(Files[0].FilePath)!;

        IsConverting = true;
        OverallProgress = 0;
        int completed = 0;

        var outExt = SelectedFormat.ToLowerInvariant() switch
        {
            "aac" => ".m4a",
            var f => $".{f}"
        };

        try
        {
            foreach (var file in Files)
            {
                ct.ThrowIfCancellationRequested();
                file.Status = "Converting...";
                file.Progress = 0;

                var outputPath = Path.Combine(outDir, Path.GetFileNameWithoutExtension(file.FilePath) + outExt);
                int counter = 1;
                while (File.Exists(outputPath))
                {
                    outputPath = Path.Combine(outDir, $"{Path.GetFileNameWithoutExtension(file.FilePath)} ({counter++}){outExt}");
                }

                var progress = new Progress<double>(p =>
                {
                    file.Progress = p;
                    OverallProgress = (completed * 100.0 + p) / Files.Count;
                });

                try
                {
                    await FFmpegService.ConvertAudioAsync(
                        file.FilePath, outputPath, SelectedBitrate, SelectedSampleRate, progress, ct);
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

            StatusText = $"Conversion complete! {completed}/{Files.Count} files.";
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

public partial class AudioFileItem : ObservableObject
{
    [ObservableProperty] private string _fileName = "";
    [ObservableProperty] private string _filePath = "";
    [ObservableProperty] private long _fileSize;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string _status = "Ready";
}
