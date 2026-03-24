using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;

namespace ConverterSplitter.Services;

public record GitHubRelease(
    [property: JsonPropertyName("tag_name")] string TagName,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("html_url")] string HtmlUrl,
    [property: JsonPropertyName("body")] string? Body,
    [property: JsonPropertyName("published_at")] DateTime PublishedAt,
    [property: JsonPropertyName("assets")] GitHubAsset[] Assets
);

public record GitHubAsset(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("browser_download_url")] string BrowserDownloadUrl,
    [property: JsonPropertyName("size")] long Size
);

public record UpdateInfo(
    Version CurrentVersion,
    Version LatestVersion,
    string ReleaseName,
    string ReleaseUrl,
    string? ReleaseNotes,
    string? DownloadUrl,
    long DownloadSize,
    DateTime PublishedAt
)
{
    public bool IsUpdateAvailable => Normalize(LatestVersion) > Normalize(CurrentVersion);

    private static Version Normalize(Version v) => new(v.Major, v.Minor, Math.Max(v.Build, 0));
}

public static class UpdateService
{
    private const string Owner = "Szdunek";
    private const string Repo = "MP3-MP4-PDF-tool";
    private const string ApiUrl = $"https://api.github.com/repos/{Owner}/{Repo}/releases/latest";

    public static Version GetCurrentVersion()
    {
        var asm = Assembly.GetExecutingAssembly();
        var ver = asm.GetName().Version;
        if (ver == null) return new Version(1, 0, 0);
        // Normalize to 3 segments (Major.Minor.Build) to match tag format X.X.X
        return new Version(ver.Major, ver.Minor, Math.Max(ver.Build, 0));
    }

    public static async Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken ct = default)
    {
        try
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Add("User-Agent", "ConverterSplitter-UpdateCheck");
            http.Timeout = TimeSpan.FromSeconds(15);

            var release = await http.GetFromJsonAsync<GitHubRelease>(ApiUrl, ct);
            if (release == null) return null;

            var tagVersion = ParseVersion(release.TagName);
            if (tagVersion == null) return null;

            var currentVersion = GetCurrentVersion();

            // Find the self-contained zip for win-x64
            var asset = release.Assets
                .FirstOrDefault(a => a.Name.Contains("win-x64-self-contained", StringComparison.OrdinalIgnoreCase))
                ?? release.Assets
                    .FirstOrDefault(a => a.Name.Contains("win-x64", StringComparison.OrdinalIgnoreCase))
                ?? release.Assets.FirstOrDefault(a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));

            return new UpdateInfo(
                CurrentVersion: currentVersion,
                LatestVersion: tagVersion,
                ReleaseName: release.Name,
                ReleaseUrl: release.HtmlUrl,
                ReleaseNotes: release.Body,
                DownloadUrl: asset?.BrowserDownloadUrl,
                DownloadSize: asset?.Size ?? 0,
                PublishedAt: release.PublishedAt
            );
        }
        catch
        {
            return null;
        }
    }

    public static async Task DownloadAndApplyUpdateAsync(
        string downloadUrl,
        IProgress<(string status, double percent)>? progress = null,
        CancellationToken ct = default)
    {
        var tempZip = Path.Combine(Path.GetTempPath(), $"ConverterSplitter_update_{Guid.NewGuid():N}.zip");
        var tempExtract = Path.Combine(Path.GetTempPath(), $"ConverterSplitter_update_extract_{Guid.NewGuid():N}");
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var updaterBat = Path.Combine(Path.GetTempPath(), $"ConverterSplitter_updater_{Guid.NewGuid():N}.bat");

        try
        {
            // Download
            progress?.Report(("Downloading update...", 0));

            using var http = new HttpClient();
            http.DefaultRequestHeaders.Add("User-Agent", "ConverterSplitter-Updater");
            http.Timeout = TimeSpan.FromMinutes(10);

            using var response = await http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1;
            long downloaded = 0;

            using (var contentStream = await response.Content.ReadAsStreamAsync(ct))
            using (var fileStream = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
            {
                var buffer = new byte[81920];
                int bytesRead;
                while ((bytesRead = await contentStream.ReadAsync(buffer, ct)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                    downloaded += bytesRead;
                    if (totalBytes > 0)
                    {
                        var pct = (double)downloaded / totalBytes * 70;
                        var mb = downloaded / (1024.0 * 1024);
                        var totalMb = totalBytes / (1024.0 * 1024);
                        progress?.Report(($"Downloading... {mb:F1}/{totalMb:F1} MB", pct));
                    }
                }
            }

            // Extract
            progress?.Report(("Extracting update...", 75));
            if (Directory.Exists(tempExtract))
                Directory.Delete(tempExtract, true);
            ZipFile.ExtractToDirectory(tempZip, tempExtract);

            // Create updater batch script that waits for app to close, copies files, restarts
            progress?.Report(("Preparing update...", 90));

            var exePath = Environment.ProcessPath ?? Path.Combine(appDir, "ConverterSplitter.exe");

            var batContent = $"""
                @echo off
                echo Waiting for application to close...
                timeout /t 2 /nobreak >nul
                :waitloop
                tasklist /fi "PID eq %1" 2>nul | find /i "ConverterSplitter" >nul
                if not errorlevel 1 (
                    timeout /t 1 /nobreak >nul
                    goto waitloop
                )
                echo Copying update files...
                xcopy /s /y /q "{tempExtract}\*" "{appDir}"
                echo Update complete. Restarting...
                start "" "{exePath}"
                del "{tempZip}" 2>nul
                rmdir /s /q "{tempExtract}" 2>nul
                del "%~f0" 2>nul
                exit
                """;

            await File.WriteAllTextAsync(updaterBat, batContent, ct);

            progress?.Report(("Restarting to apply update...", 100));

            // Launch updater script and close application
            var pid = Environment.ProcessId;
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{updaterBat}\" {pid}",
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            });

            // Shutdown app
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                System.Windows.Application.Current.Shutdown();
            });
        }
        catch
        {
            // Cleanup on failure
            try { if (File.Exists(tempZip)) File.Delete(tempZip); } catch { }
            try { if (Directory.Exists(tempExtract)) Directory.Delete(tempExtract, true); } catch { }
            try { if (File.Exists(updaterBat)) File.Delete(updaterBat); } catch { }
            throw;
        }
    }

    private static Version? ParseVersion(string tag)
    {
        var cleaned = tag.TrimStart('v', 'V');
        if (!Version.TryParse(cleaned, out var v)) return null;
        // Normalize to 3 segments to match assembly version format
        return new Version(v.Major, v.Minor, Math.Max(v.Build, 0));
    }
}
