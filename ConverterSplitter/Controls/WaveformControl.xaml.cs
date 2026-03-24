using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ConverterSplitter.Controls;

public partial class WaveformControl : UserControl
{
    private enum DragMode { None, Start, End, Seek }
    private DragMode _dragMode = DragMode.None;

    public static readonly DependencyProperty WaveformDataProperty =
        DependencyProperty.Register(nameof(WaveformData), typeof(float[]), typeof(WaveformControl),
            new PropertyMetadata(null, OnWaveformDataChanged));

    public static readonly DependencyProperty DurationProperty =
        DependencyProperty.Register(nameof(Duration), typeof(TimeSpan), typeof(WaveformControl),
            new PropertyMetadata(TimeSpan.Zero));

    public static readonly DependencyProperty CurrentPositionProperty =
        DependencyProperty.Register(nameof(CurrentPosition), typeof(double), typeof(WaveformControl),
            new PropertyMetadata(0.0, OnPositionChanged));

    public static readonly DependencyProperty SelectionStartProperty =
        DependencyProperty.Register(nameof(SelectionStart), typeof(double), typeof(WaveformControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectionChanged));

    public static readonly DependencyProperty SelectionEndProperty =
        DependencyProperty.Register(nameof(SelectionEnd), typeof(double), typeof(WaveformControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectionChanged));

    public static readonly DependencyProperty WaveColorProperty =
        DependencyProperty.Register(nameof(WaveColor), typeof(string), typeof(WaveformControl),
            new PropertyMetadata("#B39DDB", OnWaveformDataChanged));

    public static readonly DependencyProperty SelectionColorProperty =
        DependencyProperty.Register(nameof(SelectionColor), typeof(string), typeof(WaveformControl),
            new PropertyMetadata("#4DB39DDB"));

    public static readonly DependencyProperty PositionColorProperty =
        DependencyProperty.Register(nameof(PositionColor), typeof(string), typeof(WaveformControl),
            new PropertyMetadata("#80CBC4"));

    public float[]? WaveformData { get => (float[]?)GetValue(WaveformDataProperty); set => SetValue(WaveformDataProperty, value); }
    public TimeSpan Duration { get => (TimeSpan)GetValue(DurationProperty); set => SetValue(DurationProperty, value); }
    public double CurrentPosition { get => (double)GetValue(CurrentPositionProperty); set => SetValue(CurrentPositionProperty, value); }
    public double SelectionStart { get => (double)GetValue(SelectionStartProperty); set => SetValue(SelectionStartProperty, value); }
    public double SelectionEnd { get => (double)GetValue(SelectionEndProperty); set => SetValue(SelectionEndProperty, value); }
    public string WaveColor { get => (string)GetValue(WaveColorProperty); set => SetValue(WaveColorProperty, value); }
    public string SelectionColor { get => (string)GetValue(SelectionColorProperty); set => SetValue(SelectionColorProperty, value); }
    public string PositionColor { get => (string)GetValue(PositionColorProperty); set => SetValue(PositionColorProperty, value); }

    public WaveformControl()
    {
        InitializeComponent();
        SizeChanged += (_, _) => { RenderWaveform(); UpdateOverlays(); };

        SelectionCanvas.MouseLeftButtonDown += OnCanvasMouseDown;
        SelectionCanvas.MouseMove += OnCanvasMouseMove;
        SelectionCanvas.MouseLeftButtonUp += OnCanvasMouseUp;
    }

    private static void OnWaveformDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WaveformControl ctrl) ctrl.RenderWaveform();
    }

    private static void OnPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WaveformControl ctrl) ctrl.UpdatePositionLine();
    }

    private static void OnSelectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WaveformControl ctrl) ctrl.UpdateOverlays();
    }

    private void RenderWaveform()
    {
        var data = WaveformData;
        var width = (int)ActualWidth;
        var height = (int)ActualHeight;
        if (data == null || data.Length == 0 || width <= 0 || height <= 0)
        {
            WaveformImage.Source = null;
            return;
        }

        var dv = new DrawingVisual();
        var waveColor = (Color)ColorConverter.ConvertFromString(WaveColor);
        var waveBrush = new SolidColorBrush(waveColor);
        var wavePen = new Pen(waveBrush, 1);
        waveBrush.Freeze();
        wavePen.Freeze();

        using (var dc = dv.RenderOpen())
        {
            var centerY = height / 2.0;
            var samplesPerPixel = (double)data.Length / width;

            for (int x = 0; x < width; x++)
            {
                var sampleIdx = (int)(x * samplesPerPixel);
                if (sampleIdx >= data.Length) sampleIdx = data.Length - 1;
                var amplitude = data[sampleIdx] * centerY * 0.9;
                dc.DrawLine(wavePen,
                    new Point(x, centerY - amplitude),
                    new Point(x, centerY + amplitude));
            }

            // Center line
            var centerPen = new Pen(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 1);
            centerPen.Freeze();
            dc.DrawLine(centerPen, new Point(0, centerY), new Point(width, centerY));
        }

        var bmp = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bmp.Render(dv);
        bmp.Freeze();
        WaveformImage.Source = bmp;

        UpdateOverlays();
    }

    private void UpdateOverlays()
    {
        var width = ActualWidth;
        var height = ActualHeight;
        if (width <= 0 || height <= 0 || Duration.TotalSeconds <= 0) return;

        var startX = SelectionStart / Duration.TotalSeconds * width;
        var endX = SelectionEnd / Duration.TotalSeconds * width;

        // Left dimmer
        LeftDimmer.Width = Math.Max(0, startX);
        LeftDimmer.Height = height;

        // Right dimmer
        Canvas.SetLeft(RightDimmer, endX);
        RightDimmer.Width = Math.Max(0, width - endX);
        RightDimmer.Height = height;

        // Selection rect
        Canvas.SetLeft(SelectionRect, startX);
        SelectionRect.Width = Math.Max(0, endX - startX);
        SelectionRect.Height = height;
        SelectionRect.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(SelectionColor));

        // Handles
        Canvas.SetLeft(StartHandle, startX - 2);
        StartHandle.Height = height;
        Canvas.SetLeft(EndHandle, endX - 2);
        EndHandle.Height = height;

        UpdatePositionLine();
    }

    private void UpdatePositionLine()
    {
        var width = ActualWidth;
        var height = ActualHeight;
        if (width <= 0 || height <= 0 || Duration.TotalSeconds <= 0) return;

        var x = CurrentPosition / Duration.TotalSeconds * width;
        PositionLine.X1 = x;
        PositionLine.X2 = x;
        PositionLine.Y2 = height;
        PositionLine.Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString(PositionColor));
    }

    private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
    {
        var x = e.GetPosition(SelectionCanvas).X;
        var width = ActualWidth;
        if (width <= 0 || Duration.TotalSeconds <= 0) return;

        var startX = SelectionStart / Duration.TotalSeconds * width;
        var endX = SelectionEnd / Duration.TotalSeconds * width;

        if (Math.Abs(x - startX) < 8)
            _dragMode = DragMode.Start;
        else if (Math.Abs(x - endX) < 8)
            _dragMode = DragMode.End;
        else
            _dragMode = DragMode.Seek;

        if (_dragMode == DragMode.Seek)
        {
            var seconds = x / width * Duration.TotalSeconds;
            var vm = DataContext as ViewModels.AudioCutterViewModel;
            vm?.SeekTo(seconds);
        }

        SelectionCanvas.CaptureMouse();
        e.Handled = true;
    }

    private void OnCanvasMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragMode == DragMode.None) return;

        var x = e.GetPosition(SelectionCanvas).X;
        var width = ActualWidth;
        if (width <= 0) return;

        var seconds = Math.Clamp(x / width * Duration.TotalSeconds, 0, Duration.TotalSeconds);

        switch (_dragMode)
        {
            case DragMode.Start:
                if (seconds < SelectionEnd - 0.1)
                    SelectionStart = seconds;
                break;
            case DragMode.End:
                if (seconds > SelectionStart + 0.1)
                    SelectionEnd = seconds;
                break;
            case DragMode.Seek:
                var vm = DataContext as ViewModels.AudioCutterViewModel;
                vm?.SeekTo(seconds);
                break;
        }
    }

    private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
    {
        _dragMode = DragMode.None;
        SelectionCanvas.ReleaseMouseCapture();
    }
}
