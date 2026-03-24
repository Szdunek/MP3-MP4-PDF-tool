using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConverterSplitter.Services;
using Microsoft.Win32;

namespace ConverterSplitter.ViewModels;

public partial class ImageConverterViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<ImageFileItem> _files = [];
    [ObservableProperty] private string _selectedFormat = "PNG";
    [ObservableProperty] private int _quality = 90;
    [ObservableProperty] private bool _resizeEnabled;
    [ObservableProperty] private int _targetWidth = 1920;
    [ObservableProperty] private int _targetHeight = 1080;
    [ObservableProperty] private bool _keepAspectRatio = true;
    [ObservableProperty] private bool _isConverting;
    [ObservableProperty] private double _overallProgress;
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private string? _outputFolder;
    [ObservableProperty] private bool _showOpenButtons;
    [ObservableProperty] private string? _lastOutputPath;
    [ObservableProperty] private string? _lastOutputDir;

    public ImageConverterViewModel() { StatusText = Loc.I["img_status_ready"]; }

    [RelayCommand]
    private void BrowseFiles()
    {
        var dlg = new OpenFileDialog { Multiselect = true, Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp;*.tiff;*.tif|All|*.*" };
        if (dlg.ShowDialog() == true) foreach (var f in dlg.FileNames) AddFile(f);
    }

    [RelayCommand] private void BrowseOutputFolder() { var dlg = new OpenFolderDialog(); if (dlg.ShowDialog() == true) OutputFolder = dlg.FolderName; }

    public void AddFile(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (!ImageService.SupportedExtensions.Contains(ext) || Files.Any(f => f.FilePath == path)) return;
        try
        {
            var (w, h) = ImageService.GetImageDimensions(path);
            Files.Add(new ImageFileItem { FileName = Path.GetFileName(path), FilePath = path, FileSize = new FileInfo(path).Length, Width = w, Height = h });
            StatusText = $"{Files.Count} image(s) ready"; ShowOpenButtons = false;
        }
        catch (Exception ex) { StatusText = $"{Loc.I["error"]}: {ex.Message}"; }
    }

    [RelayCommand] private void RemoveFile(ImageFileItem item) { Files.Remove(item); ShowOpenButtons = false; }
    [RelayCommand] private void ClearFiles() { Files.Clear(); ShowOpenButtons = false; StatusText = Loc.I["img_status_ready"]; }

    [RelayCommand]
    private async Task ConvertAllAsync(CancellationToken ct)
    {
        if (Files.Count == 0) return;
        var outDir = OutputFolder ?? Path.GetDirectoryName(Files[0].FilePath)!;
        IsConverting = true; OverallProgress = 0; ShowOpenButtons = false;
        int completed = 0;
        var outExt = SelectedFormat.ToLowerInvariant() switch { "jpeg" => ".jpg", var f => $".{f}" };

        try
        {
            foreach (var file in Files)
            {
                ct.ThrowIfCancellationRequested();
                file.Status = Loc.I["converting"];
                var outputPath = Path.Combine(outDir, Path.GetFileNameWithoutExtension(file.FilePath) + outExt);
                int c = 1;
                while (File.Exists(outputPath) && outputPath != file.FilePath)
                    outputPath = Path.Combine(outDir, $"{Path.GetFileNameWithoutExtension(file.FilePath)} ({c++}){outExt}");
                if (outputPath == file.FilePath) outputPath = Path.Combine(outDir, $"{Path.GetFileNameWithoutExtension(file.FilePath)}_converted{outExt}");

                try
                {
                    await ImageService.ConvertImageAsync(file.FilePath, outputPath, SelectedFormat, Quality,
                        ResizeEnabled ? TargetWidth : null, ResizeEnabled ? TargetHeight : null, KeepAspectRatio, ct);
                    file.Status = Loc.I["done"]; completed++;
                    LastOutputPath = outputPath; LastOutputDir = outDir;
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { file.Status = $"{Loc.I["error"]}: {ex.Message}"; }
                OverallProgress = (double)completed / Files.Count * 100;
            }
            StatusText = string.Format(Loc.I["img_status_done"], completed, Files.Count);
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

public partial class ImageFileItem : ObservableObject
{
    [ObservableProperty] private string _fileName = "";
    [ObservableProperty] private string _filePath = "";
    [ObservableProperty] private long _fileSize;
    [ObservableProperty] private int _width;
    [ObservableProperty] private int _height;
    [ObservableProperty] private string _status = "Ready";
}
