using CommunityToolkit.Mvvm.ComponentModel;

namespace ConverterSplitter.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private bool _isVideoSelected = true;
    [ObservableProperty] private bool _isAudioCutterSelected;
    [ObservableProperty] private bool _isAudioConverterSelected;
    [ObservableProperty] private bool _isPdfMergeSelected;
    [ObservableProperty] private bool _isPdfSplitSelected;
    [ObservableProperty] private bool _isImageConverterSelected;
}
