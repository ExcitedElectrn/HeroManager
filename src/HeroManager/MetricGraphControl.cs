using System.Drawing.Drawing2D;

namespace HeroManager;

internal sealed class MetricGraphControl : Control
{
    private const int MaximumSamples = 180;
    private readonly Queue<double> _samples = new();

    public MetricGraphControl(string title, string unit, Color seriesColor, double fixedScale = 0)
    {
        Title = title;
        Unit = unit;
        SeriesColor = seriesColor;
        FixedScale = fixedScale;
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Color.FromArgb(20, 24, 31);
        ForeColor = Color.WhiteSmoke;
    }

    public string Title { get; }

    public string Unit { get; }

    public Color SeriesColor { get; }

    public double FixedScale { get; }

    public double CurrentValue { get; private set; }

    public void AddSample(double value)
    {
        CurrentValue = Math.Max(0, value);
        _samples.Enqueue(CurrentValue);
        while (_samples.Count > MaximumSamples)
        {
            _samples.Dequeue();
        }

        Invalidate();
    }

    public void ClearSamples()
    {
        CurrentValue = 0;
        _samples.Clear();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(BackColor);

        var bounds = Rectangle.Inflate(ClientRectangle, -8, -8);
        if (bounds.Width < 4 || bounds.Height < 4)
        {
            return;
        }

        DrawHeader(e.Graphics, bounds);
        var chartBounds = new Rectangle(bounds.Left, bounds.Top + 30, bounds.Width, bounds.Height - 36);
        DrawGrid(e.Graphics, chartBounds);
        DrawSeries(e.Graphics, chartBounds, _samples.ToArray(), GetScale());
    }

    private double GetScale()
    {
        if (FixedScale > 0)
        {
            return FixedScale;
        }

        var peak = _samples.DefaultIfEmpty(0).Max();
        return Math.Max(100, Math.Ceiling(peak / 100d) * 100d);
    }

    private void DrawHeader(Graphics graphics, Rectangle bounds)
    {
        using var titleBrush = new SolidBrush(Color.WhiteSmoke);
        using var valueBrush = new SolidBrush(SeriesColor);
        var current = FixedScale == 100 ? $"{CurrentValue:N1}{Unit}" : $"{CurrentValue:N1} {Unit}";
        graphics.DrawString(Title, SystemFonts.CaptionFont, titleBrush, bounds.Left, bounds.Top);
        graphics.DrawString(current, SystemFonts.CaptionFont, valueBrush, bounds.Right - 110, bounds.Top);
    }

    private static void DrawGrid(Graphics graphics, Rectangle bounds)
    {
        using var gridPen = new Pen(Color.FromArgb(45, 51, 62));
        for (var i = 0; i <= 4; i++)
        {
            var y = bounds.Top + bounds.Height * i / 4;
            graphics.DrawLine(gridPen, bounds.Left, y, bounds.Right, y);
        }
    }

    private void DrawSeries(Graphics graphics, Rectangle bounds, double[] samples, double scale)
    {
        if (samples.Length < 2 || scale <= 0)
        {
            return;
        }

        using var pen = new Pen(SeriesColor, 2f);
        var points = samples.Select((sample, index) => new PointF(
            bounds.Left + index * bounds.Width / (float)(MaximumSamples - 1),
            bounds.Bottom - (float)(Math.Clamp(sample / scale, 0, 1) * bounds.Height))).ToArray();

        graphics.DrawLines(pen, points);
    }
}
