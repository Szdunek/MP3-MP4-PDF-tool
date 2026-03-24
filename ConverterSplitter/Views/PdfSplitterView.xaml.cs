using System.Windows;
using System.Windows.Controls;
using ConverterSplitter.ViewModels;

namespace ConverterSplitter.Views;

public partial class PdfSplitterView : UserControl
{
    public PdfSplitterView()
    {
        InitializeComponent();
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (DataContext is PdfSplitterViewModel vm)
            vm.HandleDrop(e);
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnSplitModeChecked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string tag && DataContext is PdfSplitterViewModel vm)
        {
            if (int.TryParse(tag, out var mode))
                vm.SplitMode = mode;
        }
    }
}
