using System.Collections.ObjectModel;
using System.Diagnostics;
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
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private bool _isSplitting;
    [ObservableProperty] private int _splitMode; // 0 = all pages, 1 = ranges, 2 = pick pages
    [ObservableProperty] private string _rangeText = "";
    [ObservableProperty] private ObservableCollection<PageItem> _pages = [];
    [ObservableProperty] private bool _showOpenButtons;
    [ObservableProperty] private string? _lastOutputPath;
    [ObservableProperty] private string? _lastOutputDir;

    public PdfSplitterViewModel() { StatusText = Loc.I["split_status_load"]; }

    [RelayCommand]
    private void BrowseFile()
    {
        var dlg = new OpenFileDialog { Filter = "PDF Files|*.pdf" };
        if (dlg.ShowDialog() == true) LoadFile(dlg.FileName);
    }

    public void LoadFile(string path)
    {
        try
        {
            PageCount = PdfService.GetPageCount(path);
            FilePath = path;
            FileName = Path.GetFileName(path);
            IsFileLoaded = true;
            ShowOpenButtons = false;
            StatusText = string.Format(Loc.I["split_status_loaded"], FileName, PageCount);
            RangeText = $"1-{PageCount}";
            SplitMode = 0;

            Pages.Clear();
            for (int i = 1; i <= PageCount; i++)
            {
                var page = new PageItem { Number = i, IsSelected = true };
                page.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(PageItem.IsSelected))
                        UpdateRangeFromSelection();
                };
                Pages.Add(page);
            }
        }
        catch (Exception ex) { StatusText = $"{Loc.I["error"]}: {ex.Message}"; }
    }

    partial void OnSplitModeChanged(int value)
    {
        if (value == 0) // all pages
        {
            RangeText = $"1-{PageCount}";
            foreach (var p in Pages) p.IsSelected = true;
        }
        else if (value == 2) // pick pages
        {
            UpdateRangeFromSelection();
        }
    }

    private bool _updatingRange;

    private void UpdateRangeFromSelection()
    {
        if (_updatingRange || SplitMode != 2) return;
        _updatingRange = true;
        var selected = Pages.Where(p => p.IsSelected).Select(p => p.Number).OrderBy(n => n).ToList();
        RangeText = FormatPageList(selected);
        _updatingRange = false;
    }

    [RelayCommand]
    private void TogglePage(PageItem page)
    {
        if (SplitMode != 2) return;
        page.IsSelected = !page.IsSelected;
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var p in Pages) p.IsSelected = true;
    }

    [RelayCommand]
    private void SelectNone()
    {
        foreach (var p in Pages) p.IsSelected = false;
    }

    [RelayCommand]
    private async Task SplitAsync()
    {
        if (string.IsNullOrEmpty(FilePath)) return;
        var dlg = new OpenFolderDialog();
        if (dlg.ShowDialog() != true) return;

        IsSplitting = true;
        ShowOpenButtons = false;
        StatusText = Loc.I["converting"];
        try
        {
            if (SplitMode == 0) // all pages individually
            {
                await Task.Run(() => PdfService.SplitPdf(FilePath, dlg.FolderName));
                StatusText = string.Format(Loc.I["split_status_split_pages"], PageCount);
            }
            else if (SplitMode == 1) // custom ranges
            {
                var ranges = ParseRanges(RangeText);
                if (ranges.Count == 0) { StatusText = "Invalid range format"; IsSplitting = false; return; }
                await Task.Run(() => PdfService.SplitPdf(FilePath, dlg.FolderName, ranges));
                StatusText = string.Format(Loc.I["split_status_split_parts"], ranges.Count);
            }
            else // pick pages
            {
                var selected = Pages.Where(p => p.IsSelected).Select(p => p.Number).ToList();
                if (selected.Count == 0) { StatusText = "No pages selected"; IsSplitting = false; return; }
                // Split selected pages as individual files
                foreach (var pageNum in selected)
                {
                    var outPath = Path.Combine(dlg.FolderName,
                        $"{Path.GetFileNameWithoutExtension(FilePath)}_page_{pageNum}.pdf");
                    await Task.Run(() => PdfService.ExtractPages(FilePath, outPath, [pageNum]));
                }
                StatusText = $"Split {selected.Count} pages";
            }
            LastOutputDir = dlg.FolderName;
            ShowOpenButtons = true;
        }
        catch (Exception ex) { StatusText = $"{Loc.I["error"]}: {ex.Message}"; }
        finally { IsSplitting = false; }
    }

    [RelayCommand]
    private async Task ExtractPagesAsync()
    {
        if (string.IsNullOrEmpty(FilePath)) return;

        List<int> pageNums;
        if (SplitMode == 2)
            pageNums = Pages.Where(p => p.IsSelected).Select(p => p.Number).ToList();
        else
            pageNums = ParsePageNumbers(RangeText);

        if (pageNums.Count == 0) { StatusText = "No pages selected"; return; }

        var dlg = new SaveFileDialog
        {
            Filter = "PDF|*.pdf",
            FileName = $"{Path.GetFileNameWithoutExtension(FilePath)}_extracted.pdf"
        };
        if (dlg.ShowDialog() != true) return;

        IsSplitting = true;
        ShowOpenButtons = false;
        try
        {
            await Task.Run(() => PdfService.ExtractPages(FilePath, dlg.FileName, pageNums));
            StatusText = $"Extracted {pageNums.Count} pages";
            LastOutputPath = dlg.FileName;
            LastOutputDir = Path.GetDirectoryName(dlg.FileName);
            ShowOpenButtons = true;
        }
        catch (Exception ex) { StatusText = $"{Loc.I["error"]}: {ex.Message}"; }
        finally { IsSplitting = false; }
    }

    [RelayCommand]
    private void OpenFile()
    {
        if (LastOutputPath != null) Process.Start(new ProcessStartInfo(LastOutputPath) { UseShellExecute = true });
    }

    [RelayCommand]
    private void OpenFolder()
    {
        if (LastOutputDir != null) Process.Start(new ProcessStartInfo(LastOutputDir) { UseShellExecute = true });
    }

    private static string FormatPageList(List<int> pages)
    {
        if (pages.Count == 0) return "";
        var parts = new List<string>();
        int start = pages[0], end = pages[0];
        for (int i = 1; i < pages.Count; i++)
        {
            if (pages[i] == end + 1) { end = pages[i]; }
            else { parts.Add(start == end ? $"{start}" : $"{start}-{end}"); start = end = pages[i]; }
        }
        parts.Add(start == end ? $"{start}" : $"{start}-{end}");
        return string.Join(", ", parts);
    }

    private static List<(int start, int end)> ParseRanges(string text)
    {
        var ranges = new List<(int, int)>();
        foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var d = part.Trim().Split('-');
            if (d.Length == 2 && int.TryParse(d[0].Trim(), out var s) && int.TryParse(d[1].Trim(), out var e))
                ranges.Add((s, e));
        }
        return ranges;
    }

    private static List<int> ParsePageNumbers(string text)
    {
        var pages = new List<int>();
        foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var t = part.Trim();
            if (t.Contains('-'))
            {
                var d = t.Split('-');
                if (int.TryParse(d[0].Trim(), out var s) && int.TryParse(d[1].Trim(), out var e))
                    for (int i = s; i <= e; i++) pages.Add(i);
            }
            else if (int.TryParse(t, out var p)) pages.Add(p);
        }
        return pages;
    }

    public void HandleDrop(DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
        if (files.Length > 0 && files[0].EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            LoadFile(files[0]);
    }
}

public partial class PageItem : ObservableObject
{
    [ObservableProperty] private int _number;
    [ObservableProperty] private bool _isSelected = true;
}
