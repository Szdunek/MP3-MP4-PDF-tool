using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConverterSplitter.Services;
using ConverterSplitter.Views;

namespace ConverterSplitter.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private bool _isVideoSelected = true;
    [ObservableProperty] private bool _isAudioCutterSelected;
    [ObservableProperty] private bool _isAudioConverterSelected;
    [ObservableProperty] private bool _isPdfMergeSelected;
    [ObservableProperty] private bool _isPdfSplitSelected;
    [ObservableProperty] private bool _isImageConverterSelected;

    [ObservableProperty] private bool _isUpdateAvailable;
    [ObservableProperty] private string _updateVersionText = "";
    [ObservableProperty] private string _currentVersionText = "";

    private UpdateInfo? _latestUpdateInfo;

    public MainViewModel()
    {
        var ver = UpdateService.GetCurrentVersion();
        CurrentVersionText = $"v{ver.ToString(3)}";
        _ = CheckForUpdateAsync();
    }

    private async Task CheckForUpdateAsync()
    {
        try
        {
            var info = await UpdateService.CheckForUpdateAsync();
            if (info is { IsUpdateAvailable: true })
            {
                _latestUpdateInfo = info;
                IsUpdateAvailable = true;
                UpdateVersionText = $"v{info.LatestVersion.ToString(3)} available";
            }
        }
        catch { /* silently ignore update check failures */ }
    }

    [RelayCommand]
    private void ShowUpdate()
    {
        if (_latestUpdateInfo == null) return;

        var window = new UpdateWindow(_latestUpdateInfo);
        window.Owner = System.Windows.Application.Current.MainWindow;
        window.ShowDialog();
    }
}
