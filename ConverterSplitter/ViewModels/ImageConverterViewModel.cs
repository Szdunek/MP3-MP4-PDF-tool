using System.Collections.ObjectModel;
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
    [ObservableProperty] private string _statusText = "Drag & drop images or click Browse";
    [ObservableProperty] private string? _outputFolder;

    [RelayCommand]
    private void BrowseFiles()
    {
        var dlg = new OpenFileDialog
        {
            Multiselect = true,
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp;*.tiff;*.tif|All Files|*.*"
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
        if (!ImageService.SupportedExtensions.Contains(ext)) return;
        if (Files.Any(f => f.FilePath == path)) return;

        try
        {
            var (w, h) = ImageService.GetImageDimensions(path);
            Files.Add(new ImageFileItem
            {
                FileName = Path.GetFileName(path),
                FilePath = path,
                FileSize = new FileInfo(path).Length,
                Width = w,
                Height = h
            });
            StatusText = $"{Files.Count} image(s) ready for conversion";
        }
        catch (Exception ex)
        {
            StatusText = $"Error loading {Path.GetFileName(path)}: {ex.Message}";
        }
    }

    [RelayCommand]
    private void RemoveFile(ImageFileItem item)
    {
        Files.Remove(item);
        StatusText = Files.Count > 0 ? $"{Files.Count} image(s) ready" : "Drag & drop images or click Browse";
    }

    [RelayCommand]
    private void ClearFiles()
    {
        Files.Clear();
        StatusText = "Drag & drop images or click Browse";
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
            "jpeg" => ".jpg",
            var f => $".{f}"
        };

        try
        {
            foreach (var file in Files)
            {
                ct.ThrowIfCancellationRequested();
                file.Status = "Converting...";

                var outputPath = Path.Combine(outDir, Path.GetFileNameWithoutExtension(file.FilePath) + outExt);
                int counter = 1;
                while (File.Exists(outputPath) && outputPath != file.FilePath)
                {
                    outputPath = Path.Combine(outDir, $"{Path.GetFileNameWithoutExtension(file.FilePath)} ({counter++}){outExt}");
                }
                if (outputPath == file.FilePath)
                    outputPath = Path.Combine(outDir, $"{Path.GetFileNameWithoutExtension(file.FilePath)}_converted{outExt}");

                try
                {
                    await ImageService.ConvertImageAsync(
                        file.FilePath, outputPath, SelectedFormat, Quality,
                        ResizeEnabled ? TargetWidth : null,
                        ResizeEnabled ? TargetHeight : null,
                        KeepAspectRatio, ct);
                    file.Status = "Done";
                    completed++;
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    file.Status = $"Error: {ex.Message}";
                }

                OverallProgress = (double)completed / Files.Count * 100;
            }

            StatusText = $"Converted {completed}/{Files.Count} images.";
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

public partial class ImageFileItem : ObservableObject
{
    [ObservableProperty] private string _fileName = "";
    [ObservableProperty] private string _filePath = "";
    [ObservableProperty] private long _fileSize;
    [ObservableProperty] private int _width;
    [ObservableProperty] private int _height;
    [ObservableProperty] private string _status = "Ready";
}
