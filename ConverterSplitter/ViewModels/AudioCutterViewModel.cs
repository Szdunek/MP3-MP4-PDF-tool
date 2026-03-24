using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConverterSplitter.Services;
using Microsoft.Win32;
using NAudio.Wave;

namespace ConverterSplitter.ViewModels;

public partial class AudioCutterViewModel : ObservableObject, IDisposable
{
    private WaveOutEvent? _waveOut;
    private AudioFileReader? _audioReader;
    private DispatcherTimer? _positionTimer;

    [ObservableProperty] private string? _filePath;
    [ObservableProperty] private string? _fileName;
    [ObservableProperty] private TimeSpan _duration;
    [ObservableProperty] private TimeSpan _currentPosition;
    [ObservableProperty] private double _currentPositionSeconds;
    [ObservableProperty] private double _selectionStartSeconds;
    [ObservableProperty] private double _selectionEndSeconds;
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private bool _isFileLoaded;
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private double _volume = 0.7;
    [ObservableProperty] private float[]? _waveformData;
    [ObservableProperty] private bool _isCutting;
    [ObservableProperty] private double _cutProgress;
    [ObservableProperty] private bool _showOpenButtons;
    [ObservableProperty] private string? _lastOutputPath;
    [ObservableProperty] private string? _lastOutputDir;

    public TimeSpan SelectionStart => TimeSpan.FromSeconds(SelectionStartSeconds);
    public TimeSpan SelectionEnd => TimeSpan.FromSeconds(SelectionEndSeconds);
    public TimeSpan SelectionDuration => SelectionEnd - SelectionStart;

    public AudioCutterViewModel() { StatusText = Loc.I["cutter_status_load"]; }

    partial void OnCurrentPositionSecondsChanged(double value) => CurrentPosition = TimeSpan.FromSeconds(value);
    partial void OnVolumeChanged(double value) { if (_waveOut != null) _waveOut.Volume = (float)value; }
    partial void OnSelectionStartSecondsChanged(double value) { OnPropertyChanged(nameof(SelectionStart)); OnPropertyChanged(nameof(SelectionDuration)); }
    partial void OnSelectionEndSecondsChanged(double value) { OnPropertyChanged(nameof(SelectionEnd)); OnPropertyChanged(nameof(SelectionDuration)); }

    [RelayCommand]
    private void BrowseFile()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Audio Files|*.mp3;*.wav;*.flac;*.ogg;*.aac;*.wma;*.m4a;*.opus;*.caf;*.aif;*.aiff;*.amr;*.3gp|All Files|*.*"
        };
        if (dlg.ShowDialog() == true) LoadFile(dlg.FileName);
    }

    public void LoadFile(string path)
    {
        Stop(); DisposeAudio(); ShowOpenButtons = false;
        try
        {
            _audioReader = new AudioFileReader(path);
            _waveOut = new WaveOutEvent();
            _waveOut.Init(_audioReader);
            _waveOut.Volume = (float)Volume;
            _waveOut.PlaybackStopped += (_, _) => Application.Current?.Dispatcher.Invoke(() => IsPlaying = false);

            FilePath = path; FileName = Path.GetFileName(path);
            Duration = _audioReader.TotalTime;
            SelectionStartSeconds = 0; SelectionEndSeconds = Duration.TotalSeconds;
            CurrentPositionSeconds = 0; IsFileLoaded = true;
            StatusText = string.Format(Loc.I["cutter_status_loaded"], FileName, Duration.ToString(@"mm\:ss"));
            LoadWaveformData(path);

            _positionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            _positionTimer.Tick += (_, _) => { if (_audioReader != null && IsPlaying) CurrentPositionSeconds = _audioReader.CurrentTime.TotalSeconds; };
            _positionTimer.Start();
        }
        catch (Exception ex) { StatusText = $"{Loc.I["error"]}: {ex.Message}"; }
    }

    private void LoadWaveformData(string path)
    {
        try
        {
            using var reader = new AudioFileReader(path);
            var totalSamples = (int)(reader.Length / (reader.WaveFormat.BitsPerSample / 8));
            var samplesPerPixel = Math.Max(1, totalSamples / 2000);
            var peaks = new List<float>();
            var buffer = new float[samplesPerPixel];
            int read;
            while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                float max = 0;
                for (int i = 0; i < read; i++) max = Math.Max(max, Math.Abs(buffer[i]));
                peaks.Add(max);
            }
            WaveformData = peaks.ToArray();
        }
        catch { WaveformData = null; }
    }

    [RelayCommand]
    private void PlayPause()
    {
        if (_waveOut == null || _audioReader == null) return;
        if (IsPlaying) { _waveOut.Pause(); IsPlaying = false; }
        else
        {
            if (_audioReader.CurrentTime >= Duration) _audioReader.CurrentTime = TimeSpan.FromSeconds(SelectionStartSeconds);
            _waveOut.Play(); IsPlaying = true;
        }
    }

    [RelayCommand]
    private void Stop()
    {
        if (_waveOut == null || _audioReader == null) return;
        _waveOut.Stop();
        _audioReader.CurrentTime = TimeSpan.FromSeconds(SelectionStartSeconds);
        CurrentPositionSeconds = SelectionStartSeconds; IsPlaying = false;
    }

    [RelayCommand]
    private void PlaySelection()
    {
        if (_audioReader == null || _waveOut == null) return;
        _audioReader.CurrentTime = TimeSpan.FromSeconds(SelectionStartSeconds);
        CurrentPositionSeconds = SelectionStartSeconds;
        _waveOut.Play(); IsPlaying = true;
    }

    public void SeekTo(double seconds)
    {
        if (_audioReader == null) return;
        seconds = Math.Clamp(seconds, 0, Duration.TotalSeconds);
        _audioReader.CurrentTime = TimeSpan.FromSeconds(seconds);
        CurrentPositionSeconds = seconds;
    }

    [RelayCommand]
    private async Task CutAndSaveAsync(CancellationToken ct)
    {
        if (string.IsNullOrEmpty(FilePath) || SelectionStartSeconds >= SelectionEndSeconds) return;
        var ext = Path.GetExtension(FilePath);
        var dlg = new SaveFileDialog { FileName = $"{Path.GetFileNameWithoutExtension(FilePath)}_cut{ext}", Filter = $"Audio (*{ext})|*{ext}|All|*.*" };
        if (dlg.ShowDialog() != true) return;

        IsCutting = true; CutProgress = 0; ShowOpenButtons = false;
        StatusText = Loc.I["cutter_status_cutting"];
        try
        {
            await FFmpegService.CutAudioAsync(FilePath, dlg.FileName, SelectionStart, SelectionEnd, new Progress<double>(p => CutProgress = p), ct);
            StatusText = string.Format(Loc.I["cutter_status_saved"], Path.GetFileName(dlg.FileName));
            LastOutputPath = dlg.FileName; LastOutputDir = Path.GetDirectoryName(dlg.FileName);
            ShowOpenButtons = true;
        }
        catch (OperationCanceledException) { StatusText = "Cancelled."; }
        catch (Exception ex) { StatusText = $"{Loc.I["error"]}: {ex.Message}"; }
        finally { IsCutting = false; }
    }

    [RelayCommand] private void OpenFile() { if (LastOutputPath != null) Process.Start(new ProcessStartInfo(LastOutputPath) { UseShellExecute = true }); }
    [RelayCommand] private void OpenFolder() { if (LastOutputDir != null) Process.Start(new ProcessStartInfo(LastOutputDir) { UseShellExecute = true }); }

    public void HandleDrop(DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        { var files = (string[])e.Data.GetData(DataFormats.FileDrop)!; if (files.Length > 0) LoadFile(files[0]); }
    }

    private void DisposeAudio() { _positionTimer?.Stop(); _waveOut?.Stop(); _waveOut?.Dispose(); _waveOut = null; _audioReader?.Dispose(); _audioReader = null; }
    public void Dispose() { DisposeAudio(); GC.SuppressFinalize(this); }
}
