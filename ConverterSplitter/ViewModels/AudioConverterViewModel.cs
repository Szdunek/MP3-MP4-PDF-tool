using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConverterSplitter.Services;
using Microsoft.Win32;

namespace ConverterSplitter.ViewModels;

public partial class AudioConverterViewModel : ObservableObject
{
    // iPhone: .caf, .aif, .aiff, .m4a  |  Android: .amr, .3gp, .ogg  |  Standard: rest
    public static readonly string[] InputFormats =
        [".mp3", ".wav", ".flac", ".ogg", ".aac", ".wma", ".m4a", ".opus", ".caf", ".aif", ".aiff", ".amr", ".3gp", ".3gpp"];
    public static readonly string[] OutputFormats = ["MP3", "WAV", "FLAC", "OGG", "AAC"];
    public static readonly int[] Bitrates = [96, 128, 192, 256, 320];
    public static readonly int[] SampleRates = [22050, 44100, 48000, 96000];

    [ObservableProperty] private ObservableCollection<AudioFileItem> _files = [];
    [ObservableProperty] private string _selectedFormat = "MP3";
    [ObservableProperty] private int _selectedBitrate = 192;
    [ObservableProperty] private int _selectedSampleRate = 44100;
    [ObservableProperty] private bool _isConverting;
    [ObservableProperty] private double _overallProgress;
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private string? _outputFolder;
    [ObservableProperty] private bool _showOpenButtons;
    [ObservableProperty] private string? _lastOutputPath;
    [ObservableProperty] private string? _lastOutputDir;

    public AudioConverterViewModel() { StatusText = Loc.I["audio_status_ready"]; }

    [RelayCommand]
    private void BrowseFiles()
    {
        var dlg = new OpenFileDialog
        {
            Multiselect = true,
            Filter = "Audio Files|*.mp3;*.wav;*.flac;*.ogg;*.aac;*.wma;*.m4a;*.opus;*.caf;*.aif;*.aiff;*.amr;*.3gp;*.3gpp|All Files|*.*"
        };
        if (dlg.ShowDialog() == true) foreach (var f in dlg.FileNames) AddFile(f);
    }

    [RelayCommand] private void BrowseOutputFolder() { var dlg = new OpenFolderDialog(); if (dlg.ShowDialog() == true) OutputFolder = dlg.FolderName; }

    public void AddFile(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (!InputFormats.Contains(ext) || Files.Any(f => f.FilePath == path)) return;
        Files.Add(new AudioFileItem { FileName = Path.GetFileName(path), FilePath = path, FileSize = new FileInfo(path).Length });
        StatusText = $"{Files.Count} file(s) ready"; ShowOpenButtons = false;
    }

    [RelayCommand] private void RemoveFile(AudioFileItem item) { Files.Remove(item); ShowOpenButtons = false; }
    [RelayCommand] private void ClearFiles() { Files.Clear(); ShowOpenButtons = false; StatusText = Loc.I["audio_status_ready"]; }

    [RelayCommand]
    private async Task ConvertAllAsync(CancellationToken ct)
    {
        if (Files.Count == 0) return;
        var outDir = OutputFolder ?? Path.GetDirectoryName(Files[0].FilePath)!;
        IsConverting = true; OverallProgress = 0; ShowOpenButtons = false;
        int completed = 0;
        var outExt = SelectedFormat.ToLowerInvariant() switch { "aac" => ".m4a", var f => $".{f}" };

        try
        {
            foreach (var file in Files)
            {
                ct.ThrowIfCancellationRequested();
                file.Status = Loc.I["converting"]; file.Progress = 0;
                var outputPath = Path.Combine(outDir, Path.GetFileNameWithoutExtension(file.FilePath) + outExt);
                int c = 1;
                while (File.Exists(outputPath))
                    outputPath = Path.Combine(outDir, $"{Path.GetFileNameWithoutExtension(file.FilePath)} ({c++}){outExt}");

                var progress = new Progress<double>(p => { file.Progress = p; OverallProgress = (completed * 100.0 + p) / Files.Count; });
                try
                {
                    await FFmpegService.ConvertAudioAsync(file.FilePath, outputPath, SelectedBitrate, SelectedSampleRate, progress, ct);
                    file.Status = Loc.I["done"]; file.Progress = 100; completed++;
                    LastOutputPath = outputPath; LastOutputDir = outDir;
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { file.Status = $"{Loc.I["error"]}: {ex.Message}"; }
                OverallProgress = completed * 100.0 / Files.Count;
            }
            StatusText = string.Format(Loc.I["audio_status_done"], completed, Files.Count);
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
            foreach (var f in (string[])e.Data.GetData(DataFormats.FileDrop)!) AddFile(f);
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
