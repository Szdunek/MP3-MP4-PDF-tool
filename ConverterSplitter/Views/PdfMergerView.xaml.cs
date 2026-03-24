using System.Windows;
using System.Windows.Controls;
using ConverterSplitter.ViewModels;

namespace ConverterSplitter.Views;

public partial class PdfMergerView : UserControl
{
    public PdfMergerView()
    {
        InitializeComponent();
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (DataContext is PdfMergerViewModel vm)
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
