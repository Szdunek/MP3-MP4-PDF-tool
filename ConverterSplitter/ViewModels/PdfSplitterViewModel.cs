using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConverterSplitter.Services;
using Microsoft.Win32;

namespace ConverterSplitter.ViewModels;

public partial class PdfSplitterViewModel : ObservableObject
{
    [ObservableProperty] private string? _filePath;
    [ObservableProperty] private string? _fileName;
    [ObservableProperty] private int _pageCount;
    [ObservableProperty] private bool _isFileLoaded;
    [ObservableProperty] private string _statusText = "Load a PDF file to split";
    [ObservableProperty] private bool _isSplitting;
    [ObservableProperty] private bool _splitIntoPages = true;
    [ObservableProperty] private string _rangeText = "";

    [RelayCommand]
    private void BrowseFile()
    {
        var dlg = new OpenFileDialog { Filter = "PDF Files|*.pdf" };
        if (dlg.ShowDialog() == true)
            LoadFile(dlg.FileName);
    }

    public void LoadFile(string path)
    {
        try
        {
            PageCount = PdfService.GetPageCount(path);
            FilePath = path;
            FileName = Path.GetFileName(path);
            IsFileLoaded = true;
            StatusText = $"Loaded: {FileName} ({PageCount} pages)";
            RangeText = $"1-{PageCount}";
        }
        catch (Exception ex)
        {
            StatusText = $"Error loading PDF: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SplitAsync()
    {
        if (string.IsNullOrEmpty(FilePath)) return;

        var dlg = new OpenFolderDialog();
        if (dlg.ShowDialog() != true) return;

        IsSplitting = true;
        StatusText = "Splitting PDF...";

        try
        {
            if (SplitIntoPages)
            {
                await Task.Run(() => PdfService.SplitPdf(FilePath, dlg.FolderName));
                StatusText = $"Split into {PageCount} individual pages";
            }
            else
            {
                var ranges = ParseRanges(RangeText);
                if (ranges.Count == 0)
                {
                    StatusText = "Invalid range format. Use: 1-3, 4-6, 7-10";
                    return;
                }
                await Task.Run(() => PdfService.SplitPdf(FilePath, dlg.FolderName, ranges));
                StatusText = $"Split into {ranges.Count} parts";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsSplitting = false;
        }
    }

    [RelayCommand]
    private async Task ExtractPagesAsync()
    {
        if (string.IsNullOrEmpty(FilePath) || string.IsNullOrEmpty(RangeText)) return;

        var pages = ParsePageNumbers(RangeText);
        if (pages.Count == 0)
        {
            StatusText = "Invalid page numbers";
            return;
        }

        var dlg = new SaveFileDialog
        {
            Filter = "PDF File|*.pdf",
            FileName = $"{Path.GetFileNameWithoutExtension(FilePath)}_extracted.pdf"
        };
        if (dlg.ShowDialog() != true) return;

        IsSplitting = true;
        StatusText = "Extracting pages...";

        try
        {
            await Task.Run(() => PdfService.ExtractPages(FilePath, dlg.FileName, pages));
            StatusText = $"Extracted {pages.Count} pages to {Path.GetFileName(dlg.FileName)}";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsSplitting = false;
        }
    }

    private static List<(int start, int end)> ParseRanges(string text)
    {
        var ranges = new List<(int, int)>();
        foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = part.Trim();
            var dash = trimmed.Split('-');
            if (dash.Length == 2 && int.TryParse(dash[0].Trim(), out var start) && int.TryParse(dash[1].Trim(), out var end))
                ranges.Add((start, end));
        }
        return ranges;
    }

    private static List<int> ParsePageNumbers(string text)
    {
        var pages = new List<int>();
        foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = part.Trim();
            if (trimmed.Contains('-'))
            {
                var dash = trimmed.Split('-');
                if (int.TryParse(dash[0].Trim(), out var start) && int.TryParse(dash[1].Trim(), out var end))
                    for (int i = start; i <= end; i++) pages.Add(i);
            }
            else if (int.TryParse(trimmed, out var page))
            {
                pages.Add(page);
            }
        }
        return pages;
    }

    public void HandleDrop(DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
            if (files.Length > 0 && files[0].EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                LoadFile(files[0]);
        }
    }
}
