using System.Diagnostics;
using System.IO;

namespace ConverterSplitter.Services;

public static class FFmpegService
{
    private static string? _ffmpegPath;

    public static string? FindFFmpeg()
    {
        if (_ffmpegPath != null) return _ffmpegPath;

        // Check local bundled copy first, then common locations
        var candidates = new[]
        {
            FFmpegDownloader.LocalFFmpegExe,
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe"),
            "ffmpeg",
            "ffmpeg.exe",
            @"C:\ffmpeg\bin\ffmpeg.exe",
            @"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
            @"C:\ProgramData\chocolatey\bin\ffmpeg.exe",
        };

        foreach (var path in candidates)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = "-version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit(3000);
                if (proc?.ExitCode == 0)
                {
                    _ffmpegPath = path;
                    return _ffmpegPath;
                }
            }
            catch { }
        }

        return null;
    }

    public static async Task ConvertVideoToMp3Async(
        string inputPath,
        string outputPath,
        int bitrate,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        var ffmpeg = FindFFmpeg() ?? throw new InvalidOperationException(
            "FFmpeg not found. Please install FFmpeg and make sure it's in your PATH.");

        var duration = await GetDurationAsync(ffmpeg, inputPath, ct);

        var args = $"-i \"{inputPath}\" -vn -acodec libmp3lame -ab {bitrate}k -y \"{outputPath}\"";

        var psi = new ProcessStartInfo
        {
            FileName = ffmpeg,
            Arguments = args,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start FFmpeg process.");

        var errorReader = process.StandardError;
        while (!errorReader.EndOfStream)
        {
            ct.ThrowIfCancellationRequested();
            var line = await errorReader.ReadLineAsync(ct);
            if (line != null && duration.TotalSeconds > 0)
            {
                var time = ParseTime(line);
                if (time.HasValue)
                    progress?.Report(time.Value.TotalSeconds / duration.TotalSeconds * 100);
            }
        }

        await process.WaitForExitAsync(ct);
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"FFmpeg exited with code {process.ExitCode}");

        progress?.Report(100);
    }

    public static async Task ConvertAudioAsync(
        string inputPath,
        string outputPath,
        int? bitrate = null,
        int? sampleRate = null,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        var ffmpeg = FindFFmpeg() ?? throw new InvalidOperationException(
            "FFmpeg not found. Please install FFmpeg and make sure it's in your PATH.");

        var duration = await GetDurationAsync(ffmpeg, inputPath, ct);

        var ext = Path.GetExtension(outputPath).ToLowerInvariant();
        var codec = ext switch
        {
            ".mp3" => "libmp3lame",
            ".ogg" => "libvorbis",
            ".flac" => "flac",
            ".wav" => "pcm_s16le",
            ".aac" or ".m4a" => "aac",
            ".wma" => "wmav2",
            _ => "copy"
        };

        var bitrateArg = bitrate.HasValue ? $"-ab {bitrate}k" : "";
        var sampleRateArg = sampleRate.HasValue ? $"-ar {sampleRate}" : "";

        var args = $"-i \"{inputPath}\" -acodec {codec} {bitrateArg} {sampleRateArg} -y \"{outputPath}\"";

        var psi = new ProcessStartInfo
        {
            FileName = ffmpeg,
            Arguments = args,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start FFmpeg process.");

        var errorReader = process.StandardError;
        while (!errorReader.EndOfStream)
        {
            ct.ThrowIfCancellationRequested();
            var line = await errorReader.ReadLineAsync(ct);
            if (line != null && duration.TotalSeconds > 0)
            {
                var time = ParseTime(line);
                if (time.HasValue)
                    progress?.Report(time.Value.TotalSeconds / duration.TotalSeconds * 100);
            }
        }

        await process.WaitForExitAsync(ct);
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"FFmpeg exited with code {process.ExitCode}");

        progress?.Report(100);
    }

    public static async Task CutAudioAsync(
        string inputPath,
        string outputPath,
        TimeSpan start,
        TimeSpan end,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        var ffmpeg = FindFFmpeg() ?? throw new InvalidOperationException(
            "FFmpeg not found. Please install FFmpeg and make sure it's in your PATH.");

        var duration = end - start;
        var args = $"-i \"{inputPath}\" -ss {start:hh\\:mm\\:ss\\.fff} -to {end:hh\\:mm\\:ss\\.fff} -acodec copy -y \"{outputPath}\"";

        var psi = new ProcessStartInfo
        {
            FileName = ffmpeg,
            Arguments = args,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start FFmpeg process.");

        var errorReader = process.StandardError;
        while (!errorReader.EndOfStream)
        {
            ct.ThrowIfCancellationRequested();
            var line = await errorReader.ReadLineAsync(ct);
            if (line != null && duration.TotalSeconds > 0)
            {
                var time = ParseTime(line);
                if (time.HasValue)
                    progress?.Report(time.Value.TotalSeconds / duration.TotalSeconds * 100);
            }
        }

        await process.WaitForExitAsync(ct);
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"FFmpeg exited with code {process.ExitCode}");

        progress?.Report(100);
    }

    private static async Task<TimeSpan> GetDurationAsync(string ffmpeg, string inputPath, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = ffmpeg,
            Arguments = $"-i \"{inputPath}\"",
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process == null) return TimeSpan.Zero;

        var output = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        var match = System.Text.RegularExpressions.Regex.Match(output, @"Duration:\s+(\d{2}):(\d{2}):(\d{2})\.(\d{2})");
        if (match.Success)
        {
            return new TimeSpan(0,
                int.Parse(match.Groups[1].Value),
                int.Parse(match.Groups[2].Value),
                int.Parse(match.Groups[3].Value),
                int.Parse(match.Groups[4].Value) * 10);
        }

        return TimeSpan.Zero;
    }

    private static TimeSpan? ParseTime(string line)
    {
        var match = System.Text.RegularExpressions.Regex.Match(line, @"time=(\d{2}):(\d{2}):(\d{2})\.(\d{2})");
        if (match.Success)
        {
            return new TimeSpan(0,
                int.Parse(match.Groups[1].Value),
                int.Parse(match.Groups[2].Value),
                int.Parse(match.Groups[3].Value),
                int.Parse(match.Groups[4].Value) * 10);
        }
        return null;
    }
}
