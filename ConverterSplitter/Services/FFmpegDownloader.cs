using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;

namespace ConverterSplitter.Services;

public static class FFmpegDownloader
{
    private const string FFmpegUrl = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip";

    public static string LocalFFmpegDir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg");
    public static string LocalFFmpegExe => Path.Combine(LocalFFmpegDir, "bin", "ffmpeg.exe");

    public static bool IsFFmpegAvailable()
    {
        // Check local bundled copy first
        if (File.Exists(LocalFFmpegExe))
            return true;

        // Check PATH
        return FFmpegService.FindFFmpeg() != null;
    }

    public static string? GetFFmpegPath()
    {
        if (File.Exists(LocalFFmpegExe))
            return LocalFFmpegExe;
        return FFmpegService.FindFFmpeg();
    }

    public static async Task DownloadAsync(IProgress<(string status, double percent)>? progress = null, CancellationToken ct = default)
    {
        var tempZip = Path.Combine(Path.GetTempPath(), "ffmpeg_download.zip");
        var tempExtract = Path.Combine(Path.GetTempPath(), "ffmpeg_extract_" + Guid.NewGuid().ToString("N")[..8]);

        try
        {
            // Download
            progress?.Report(("Downloading FFmpeg...", 0));

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(10);

            using var response = await httpClient.GetAsync(FFmpegUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1;
            long downloaded = 0;

            using (var contentStream = await response.Content.ReadAsStreamAsync(ct))
            using (var fileStream = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
            {
                var buffer = new byte[81920];
                int bytesRead;
                var lastReport = DateTime.UtcNow;
                while ((bytesRead = await contentStream.ReadAsync(buffer, ct)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                    downloaded += bytesRead;
                    var now = DateTime.UtcNow;
                    if (totalBytes > 0 && (now - lastReport).TotalMilliseconds >= 100)
                    {
                        lastReport = now;
                        var pct = (double)downloaded / totalBytes * 80;
                        var mb = downloaded / (1024.0 * 1024);
                        var totalMb = totalBytes / (1024.0 * 1024);
                        progress?.Report(($"Downloading FFmpeg... {mb:F0}/{totalMb:F0} MB", pct));
                    }
                }
            }

            // Extract
            progress?.Report(("Extracting FFmpeg...", 85));

            if (Directory.Exists(tempExtract))
                Directory.Delete(tempExtract, true);

            ZipFile.ExtractToDirectory(tempZip, tempExtract);

            // Find the extracted directory (usually named ffmpeg-master-latest-win64-gpl)
            var extractedDirs = Directory.GetDirectories(tempExtract);
            var ffmpegExtracted = extractedDirs.Length > 0 ? extractedDirs[0] : tempExtract;

            // Copy to local directory
            progress?.Report(("Installing FFmpeg...", 92));

            if (Directory.Exists(LocalFFmpegDir))
                Directory.Delete(LocalFFmpegDir, true);

            CopyDirectory(ffmpegExtracted, LocalFFmpegDir);

            // Verify
            progress?.Report(("Verifying FFmpeg...", 97));

            if (!File.Exists(LocalFFmpegExe))
            {
                // Try to find ffmpeg.exe recursively
                var found = Directory.GetFiles(LocalFFmpegDir, "ffmpeg.exe", SearchOption.AllDirectories);
                if (found.Length == 0)
                    throw new FileNotFoundException("FFmpeg binary not found after extraction.");

                // Restructure so bin/ffmpeg.exe exists
                var binDir = Path.Combine(LocalFFmpegDir, "bin");
                if (!Directory.Exists(binDir))
                    Directory.CreateDirectory(binDir);

                foreach (var exe in Directory.GetFiles(Path.GetDirectoryName(found[0])!, "*.exe"))
                {
                    var dest = Path.Combine(binDir, Path.GetFileName(exe));
                    if (!File.Exists(dest))
                        File.Copy(exe, dest);
                }
            }

            progress?.Report(("FFmpeg installed successfully!", 100));
        }
        finally
        {
            // Cleanup temp files
            try { if (File.Exists(tempZip)) File.Delete(tempZip); } catch { }
            try { if (Directory.Exists(tempExtract)) Directory.Delete(tempExtract, true); } catch { }
        }
    }

    private static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);

        foreach (var file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), true);
        }

        foreach (var dir in Directory.GetDirectories(source))
        {
            CopyDirectory(dir, Path.Combine(dest, Path.GetFileName(dir)));
        }
    }
}
