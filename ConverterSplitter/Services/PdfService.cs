using System.IO;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;

namespace ConverterSplitter.Services;

public static class PdfService
{
    public static void MergePdfs(IEnumerable<string> inputPaths, string outputPath)
    {
        using var outputDocument = new PdfDocument();

        foreach (var path in inputPaths)
        {
            using var inputDocument = PdfReader.Open(path, PdfDocumentOpenMode.Import);
            for (int i = 0; i < inputDocument.PageCount; i++)
            {
                outputDocument.AddPage(inputDocument.Pages[i]);
            }
        }

        outputDocument.Save(outputPath);
    }

    public static int GetPageCount(string path)
    {
        using var doc = PdfReader.Open(path, PdfDocumentOpenMode.Import);
        return doc.PageCount;
    }

    public static void SplitPdf(string inputPath, string outputDirectory, IList<(int start, int end)>? ranges = null)
    {
        using var inputDocument = PdfReader.Open(inputPath, PdfDocumentOpenMode.Import);
        var baseName = Path.GetFileNameWithoutExtension(inputPath);

        if (ranges == null || ranges.Count == 0)
        {
            // Split into individual pages
            for (int i = 0; i < inputDocument.PageCount; i++)
            {
                using var output = new PdfDocument();
                output.AddPage(inputDocument.Pages[i]);
                var outputPath = Path.Combine(outputDirectory, $"{baseName}_page_{i + 1}.pdf");
                output.Save(outputPath);
            }
        }
        else
        {
            int part = 1;
            foreach (var (start, end) in ranges)
            {
                using var output = new PdfDocument();
                for (int i = start - 1; i < end && i < inputDocument.PageCount; i++)
                {
                    output.AddPage(inputDocument.Pages[i]);
                }
                var outputPath = Path.Combine(outputDirectory, $"{baseName}_part_{part}.pdf");
                output.Save(outputPath);
                part++;
            }
        }
    }

    public static void ExtractPages(string inputPath, string outputPath, IEnumerable<int> pageNumbers)
    {
        using var inputDocument = PdfReader.Open(inputPath, PdfDocumentOpenMode.Import);
        using var outputDocument = new PdfDocument();

        foreach (var pageNum in pageNumbers)
        {
            if (pageNum >= 1 && pageNum <= inputDocument.PageCount)
            {
                outputDocument.AddPage(inputDocument.Pages[pageNum - 1]);
            }
        }

        outputDocument.Save(outputPath);
    }
}
