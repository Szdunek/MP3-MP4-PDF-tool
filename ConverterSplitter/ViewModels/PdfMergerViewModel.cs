using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConverterSplitter.Services;
using Microsoft.Win32;

namespace ConverterSplitter.ViewModels;

public partial class PdfMergerViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<PdfFileItem> _files = [];
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private bool _isMerging;
    [ObservableProperty] private bool _showOpenButtons;
    [ObservableProperty] private string? _lastOutputPath;
    [ObservableProperty] private string? _lastOutputDir;

    public PdfMergerViewModel() { StatusText = Loc.I["merge_status_ready"]; }

    [RelayCommand]
    private void BrowseFiles()
    {
        var dlg = new OpenFileDialog { Multiselect = true, Filter = "PDF Files|*.pdf" };
        if (dlg.ShowDialog() == true) foreach (var f in dlg.FileNames) AddFile(f);
    }

    public void AddFile(string path)
    {
        if (!path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) || Files.Any(f => f.FilePath == path)) return;
        int pages; try { pages = PdfService.GetPageCount(path); } catch { pages = 0; }
        Files.Add(new PdfFileItem { FileName = Path.GetFileName(path), FilePath = path, FileSize = new FileInfo(path).Length, PageCount = pages });
        UpdateStatus(); ShowOpenButtons = false;
    }

    [RelayCommand] private void RemoveFile(PdfFileItem item) { Files.Remove(item); UpdateStatus(); ShowOpenButtons = false; }
    [RelayCommand] private void ClearFiles() { Files.Clear(); StatusText = Loc.I["merge_status_ready"]; ShowOpenButtons = false; }
    [RelayCommand] private void MoveUp(PdfFileItem item) { var i = Files.IndexOf(item); if (i > 0) Files.Move(i, i - 1); }
    [RelayCommand] private void MoveDown(PdfFileItem item) { var i = Files.IndexOf(item); if (i < Files.Count - 1) Files.Move(i, i + 1); }

    [RelayCommand]
    private async Task MergeAsync()
    {
        if (Files.Count < 2) { StatusText = Loc.I["merge_need_two"]; return; }
        var dlg = new SaveFileDialog { Filter = "PDF|*.pdf", FileName = "merged.pdf" };
        if (dlg.ShowDialog() != true) return;

        IsMerging = true; ShowOpenButtons = false;
        StatusText = Loc.I["converting"];
        try
        {
            await Task.Run(() => PdfService.MergePdfs(Files.Select(f => f.FilePath), dlg.FileName));
            StatusText = string.Format(Loc.I["merge_status_done"], Files.Count, Path.GetFileName(dlg.FileName));
            LastOutputPath = dlg.FileName; LastOutputDir = Path.GetDirectoryName(dlg.FileName);
            ShowOpenButtons = true;
        }
        catch (Exception ex) { StatusText = $"{Loc.I["error"]}: {ex.Message}"; }
        finally { IsMerging = false; }
    }

    [RelayCommand] private void OpenFile() { if (LastOutputPath != null) Process.Start(new ProcessStartInfo(LastOutputPath) { UseShellExecute = true }); }
    [RelayCommand] private void OpenFolder() { if (LastOutputDir != null) Process.Start(new ProcessStartInfo(LastOutputDir) { UseShellExecute = true }); }

    private void UpdateStatus()
    {
        StatusText = Files.Count > 0
            ? string.Format(Loc.I["merge_pages_total"], Files.Count, Files.Sum(f => f.PageCount))
            : Loc.I["merge_status_ready"];
    }

    public void HandleDrop(DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
            foreach (var f in (string[])e.Data.GetData(DataFormats.FileDrop)!) AddFile(f);
    }
}

public partial class PdfFileItem : ObservableObject
{
    [ObservableProperty] private string _fileName = "";
    [ObservableProperty] private string _filePath = "";
    [ObservableProperty] private long _fileSize;
    [ObservableProperty] private int _pageCount;
}
