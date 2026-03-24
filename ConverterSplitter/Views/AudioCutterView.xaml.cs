using System.Windows;
using System.Windows.Controls;
using ConverterSplitter.ViewModels;

namespace ConverterSplitter.Views;

public partial class AudioCutterView : UserControl
{
    public AudioCutterView()
    {
        InitializeComponent();
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (DataContext is AudioCutterViewModel vm)
            vm.HandleDrop(e);
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }
}
