using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Buran.Player.Controls;

public class SpectrogramControl : Control {
    private const double LeftPad   = 48;
    private const double RightPad  = 50;
    private const double TopPad    = 14;
    private const double BottomPad = 18;
    private const double MinHz     = 20;

    private static readonly IBrush BackgroundBrush = new SolidColorBrush(Color.Parse("#FF070C16"));
    private static readonly IBrush PlotEmptyBrush  = new SolidColorBrush(Color.Parse("#22070C16"));
    private static readonly IBrush AxisBrush       = new SolidColorBrush(Color.Parse("#9AA8BC"));
    private static readonly IBrush PlotBorderBrush = new SolidColorBrush(Color.Parse("#3A4F70"));
    private static readonly Pen    AxisPen         = new(AxisBrush, 1);
    private static readonly Pen    PlayheadPen     = new(new SolidColorBrush(Color.Parse("#E8F4FF")), 1.5);

    public static readonly StyledProperty<Bitmap?> SourceProperty =
        AvaloniaProperty.Register<SpectrogramControl, Bitmap?>(nameof(Source));

    public static readonly StyledProperty<double> PositionProperty =
        AvaloniaProperty.Register<SpectrogramControl, double>(nameof(Position));

    public static readonly StyledProperty<double> DurationSecondsProperty =
        AvaloniaProperty.Register<SpectrogramControl, double>(nameof(DurationSeconds));

    public static readonly StyledProperty<double> NyquistHzProperty =
        AvaloniaProperty.Register<SpectrogramControl, double>(nameof(NyquistHz), 22050);

    static SpectrogramControl() {
        AffectsRender<SpectrogramControl>(
            SourceProperty, PositionProperty, DurationSecondsProperty, NyquistHzProperty);
    }

    public event EventHandler<double>? SeekRequested;

    public Bitmap? Source {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public double Position {
        get => GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    public double DurationSeconds {
        get => GetValue(DurationSecondsProperty);
        set => SetValue(DurationSecondsProperty, value);
    }

    public double NyquistHz {
        get => GetValue(NyquistHzProperty);
        set => SetValue(NyquistHzProperty, value);
    }

    public override void Render(DrawingContext context) {
        var bounds = Bounds;
        if (bounds.Width <= 1 || bounds.Height <= 1)
            return;

        context.FillRectangle(BackgroundBrush, bounds);

        var plot = PlotRect(bounds);
        if (plot.Width <= 1 || plot.Height <= 1)
            return;

        if (Source is { } image) {
            using (context.PushClip(plot))
                context.DrawImage(image, plot);
        }
        else {
            context.FillRectangle(PlotEmptyBrush, plot);
        }

        context.DrawRectangle(null, new Pen(PlotBorderBrush, 1), plot);

        if (plot.Width >= 24 && plot.Height >= 24) {
            DrawFrequencyAxis(context, plot);
            DrawTimeAxis(context, plot);
            DrawDbLegend(context, plot);
        }

        var x = plot.X + Math.Clamp(Position, 0, 1) * plot.Width;
        context.DrawLine(PlayheadPen, new Point(x, plot.Y), new Point(x, plot.Y + plot.Height));
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e) {
        base.OnPointerPressed(e);
        if (ReportSeek(e.GetPosition(this).X)) {
            e.Pointer.Capture(this);
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e) {
        base.OnPointerMoved(e);
        if (e.Pointer.Captured == this)
            ReportSeek(e.GetPosition(this).X);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e) {
        base.OnPointerReleased(e);
        if (e.Pointer.Captured == this)
            e.Pointer.Capture(null);
    }

    private static Rect PlotRect(Rect bounds) => new(
        LeftPad,
        TopPad,
        Math.Max(1, bounds.Width  - LeftPad - RightPad),
        Math.Max(1, bounds.Height - TopPad - BottomPad));

    private bool ReportSeek(double x) {
        var plot = PlotRect(Bounds);
        if (plot.Width <= 0)
            return false;
        var t = Math.Clamp((x - plot.X) / plot.Width, 0, 1);
        SeekRequested?.Invoke(this, t);
        return true;
    }

    private void DrawFrequencyAxis(DrawingContext context, Rect plot) {
        var maxHz = Math.Max(MinHz * 2, NyquistHz);
        var lastY = double.NaN;
        var ticks = FrequencyTicks(maxHz);

        for (var i = ticks.Count - 1; i >= 0; i--) {
            var hz = ticks[i];
            var y = plot.Y + (1 - LogPos(hz, maxHz)) * plot.Height;
            y = Math.Clamp(y, plot.Y, plot.Y + plot.Height);
            if (!double.IsNaN(lastY) && Math.Abs(y - lastY) < 12)
                continue;
            lastY = y;

            context.DrawLine(AxisPen, new Point(plot.X - 4, y), new Point(plot.X, y));
            var text = Format(FormatHz(hz), 10, AxisBrush);
            var ty = Fit(y - text.Height / 2, plot.Y - 2, plot.Y + plot.Height - text.Height);
            context.DrawText(text, new Point(plot.X - 8 - text.Width, ty));
        }

        var unit = Format("Hz", 10, AxisBrush);
        context.DrawText(unit, new Point(6, 1));
    }

    private void DrawTimeAxis(DrawingContext context, Rect plot) {
        var duration = Math.Max(0, DurationSeconds);
        var ticks = plot.Width >= 640 ? 8 : plot.Width >= 360 ? 5 : 3;
        var lastX = double.NaN;

        for (var i = 0; i <= ticks; i++) {
            var t = i / (double)ticks;
            var x = plot.X + t * plot.Width;
            context.DrawLine(AxisPen, new Point(x, plot.Y + plot.Height), new Point(x, plot.Y + plot.Height + 4));
            var text = Format(FormatClock(duration * t), 10, AxisBrush);
            var tx = Fit(x - text.Width / 2, plot.X, plot.X + plot.Width - text.Width);
            if (!double.IsNaN(lastX) && tx < lastX + 4)
                continue;
            lastX = tx + text.Width;
            context.DrawText(text, new Point(tx, plot.Y + plot.Height + 5));
        }
    }

    private static void DrawDbLegend(DrawingContext context, Rect plot) {
        var bar = new Rect(plot.X + plot.Width + 8, plot.Y, 10, plot.Height);
        var gradient = new LinearGradientBrush {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint   = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops = [
                new GradientStop(Color.Parse("#FFFF5050"), 0),
                new GradientStop(Color.Parse("#FFFFFF64"), 0.18),
                new GradientStop(Color.Parse("#FF50FF50"), 0.38),
                new GradientStop(Color.Parse("#FF50FFFF"), 0.55),
                new GradientStop(Color.Parse("#FF5050FF"), 0.75),
                new GradientStop(Color.Parse("#FF101028"), 1)
            ]
        };
        context.FillRectangle(gradient, bar);
        context.DrawRectangle(null, new Pen(PlotBorderBrush, 1), bar);

        int[] dbs = [0, -20, -40, -60, -80, -100, -120];
        var lastY = double.NaN;
        foreach (var db in dbs) {
            var t = db / -120.0;
            var y = plot.Y + t * plot.Height;
            if (!double.IsNaN(lastY) && Math.Abs(y - lastY) < 12)
                continue;
            lastY = y;
            var text = Format(db.ToString(CultureInfo.InvariantCulture), 10, AxisBrush);
            var ty = Fit(y - text.Height / 2, plot.Y, plot.Y + plot.Height - text.Height);
            context.DrawText(text, new Point(bar.X + bar.Width + 4, ty));
        }

        var unit = Format("dB", 10, AxisBrush);
        context.DrawText(unit, new Point(bar.X + bar.Width + 4, 1));
    }

    private static List<double> FrequencyTicks(double maxHz) {
        var ticks = new List<double>();
        for (var exp = 1; exp <= 6; exp++) {
            var mag = Math.Pow(10, exp);
            foreach (var m in new[] { 1.0, 2.0, 5.0 }) {
                var hz = m * mag;
                if (hz >= 50 && hz < maxHz)
                    ticks.Add(hz);
            }
        }

        ticks.Add(maxHz);
        return ticks;
    }

    private static string FormatHz(double hz) {
        if (hz < 1000)
            return hz.ToString("0", CultureInfo.InvariantCulture);

        var k = hz / 1000.0;
        return Math.Abs(k - Math.Round(k)) <= 0.05
            ? $"{Math.Round(k):0}k"
            : $"{k:0.#}k";
    }

    private static double Fit(double value, double min, double max) {
        if (double.IsNaN(value) || double.IsNaN(min) || double.IsNaN(max))
            return min;
        if (max < min)
            return min;
        return Math.Clamp(value, min, max);
    }

    private static double LogPos(double hz, double maxHz) {
        var min = Math.Log10(MinHz);
        var max = Math.Log10(maxHz);
        return Math.Clamp((Math.Log10(hz) - min) / (max - min), 0, 1);
    }

    private static FormattedText Format(string text, double size, IBrush brush) =>
        new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface("Inter, ui-sans-serif, sans-serif"), size, brush);

    private static string FormatClock(double seconds) {
        if (seconds <= 0 || double.IsNaN(seconds))
            return "0:00";
        var t = TimeSpan.FromSeconds(seconds);
        return t.ToString(t.TotalHours >= 1 ? @"h\:mm\:ss" : @"m\:ss");
    }
}
