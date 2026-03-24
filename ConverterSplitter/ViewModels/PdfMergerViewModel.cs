using System.Collections.ObjectModel;
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
    [ObservableProperty] private string _statusText = "Drag & drop PDF files or click Browse";
    [ObservableProperty] private bool _isMerging;

    [RelayCommand]
    private void BrowseFiles()
    {
        var dlg = new OpenFileDialog
        {
            Multiselect = true,
            Filter = "PDF Files|*.pdf"
        };
        if (dlg.ShowDialog() == true)
        {
            foreach (var file in dlg.FileNames)
                AddFile(file);
        }
    }

    public void AddFile(string path)
    {
        if (!path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) return;
        if (Files.Any(f => f.FilePath == path)) return;

        int pages;
        try { pages = PdfService.GetPageCount(path); }
        catch { pages = 0; }

        Files.Add(new PdfFileItem
        {
            FileName = Path.GetFileName(path),
            FilePath = path,
            FileSize = new FileInfo(path).Length,
            PageCount = pages
        });
        StatusText = $"{Files.Count} PDF(s) ready to merge ({Files.Sum(f => f.PageCount)} pages total)";
    }

    [RelayCommand]
    private void RemoveFile(PdfFileItem item)
    {
        Files.Remove(item);
        UpdateStatus();
    }

    [RelayCommand]
    private void ClearFiles()
    {
        Files.Clear();
        StatusText = "Drag & drop PDF files or click Browse";
    }

    [RelayCommand]
    private void MoveUp(PdfFileItem item)
    {
        var idx = Files.IndexOf(item);
        if (idx > 0) Files.Move(idx, idx - 1);
    }

    [RelayCommand]
    private void MoveDown(PdfFileItem item)
    {
        var idx = Files.IndexOf(item);
        if (idx < Files.Count - 1) Files.Move(idx, idx + 1);
    }

    [RelayCommand]
    private async Task MergeAsync()
    {
        if (Files.Count < 2)
        {
            StatusText = "Need at least 2 PDF files to merge";
            return;
        }

        var dlg = new SaveFileDialog
        {
            Filter = "PDF File|*.pdf",
            FileName = "merged.pdf"
        };
        if (dlg.ShowDialog() != true) return;

        IsMerging = true;
        StatusText = "Merging PDFs...";

        try
        {
            await Task.Run(() => PdfService.MergePdfs(Files.Select(f => f.FilePath), dlg.FileName));
            StatusText = $"Merged {Files.Count} PDFs into {Path.GetFileName(dlg.FileName)}";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsMerging = false;
        }
    }

    private void UpdateStatus()
    {
        StatusText = Files.Count > 0
            ? $"{Files.Count} PDF(s) ready to merge ({Files.Sum(f => f.PageCount)} pages total)"
            : "Drag & drop PDF files or click Browse";
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

public partial class PdfFileItem : ObservableObject
{
    [ObservableProperty] private string _fileName = "";
    [ObservableProperty] private string _filePath = "";
    [ObservableProperty] private long _fileSize;
    [ObservableProperty] private int _pageCount;
}
