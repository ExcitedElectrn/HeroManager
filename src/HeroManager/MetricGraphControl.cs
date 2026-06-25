using System.Drawing.Drawing2D;

namespace HeroManager;

internal sealed class MetricGraphControl : Control
{
    private const int MaximumSamples = 90;
    private readonly Queue<double> _cpuSamples = new();
    private readonly Queue<double> _memorySamples = new();

    public MetricGraphControl()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Color.FromArgb(20, 24, 31);
    }

    public void AddSample(double cpuPercent, double memoryMegabytes)
    {
        EnqueueSample(_cpuSamples, Math.Clamp(cpuPercent, 0, 100));
        EnqueueSample(_memorySamples, Math.Max(0, memoryMegabytes));
        Invalidate();
    }

    public void ClearSamples()
    {
        _cpuSamples.Clear();
        _memorySamples.Clear();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(BackColor);

        var bounds = ClientRectangle;
        if (bounds.Width < 4 || bounds.Height < 4)
        {
            return;
        }

        DrawGrid(e.Graphics, bounds);
        DrawSeries(e.Graphics, bounds, _cpuSamples.ToArray(), 100, Color.FromArgb(74, 163, 255), "CPU %");

        var memorySamples = _memorySamples.ToArray();
        var memoryScale = Math.Max(100, memorySamples.DefaultIfEmpty(0).Max());
        DrawSeries(e.Graphics, bounds, memorySamples, memoryScale, Color.FromArgb(116, 213, 129), "RAM MB");
        DrawLegend(e.Graphics, bounds);
    }

    private static void EnqueueSample(Queue<double> samples, double value)
    {
        samples.Enqueue(value);
        while (samples.Count > MaximumSamples)
        {
            samples.Dequeue();
        }
    }

    private static void DrawGrid(Graphics graphics, Rectangle bounds)
    {
        using var gridPen = new Pen(Color.FromArgb(45, 51, 62));
        for (var i = 1; i < 4; i++)
        {
            var y = bounds.Top + bounds.Height * i / 4;
            graphics.DrawLine(gridPen, bounds.Left, y, bounds.Right, y);
        }
    }

    private static void DrawSeries(Graphics graphics, Rectangle bounds, double[] samples, double scale, Color color, string label)
    {
        if (samples.Length < 2 || scale <= 0)
        {
            return;
        }

        using var pen = new Pen(color, 2f);
        var points = samples.Select((sample, index) => new PointF(
            bounds.Left + index * bounds.Width / (float)(MaximumSamples - 1),
            bounds.Bottom - (float)(Math.Clamp(sample / scale, 0, 1) * bounds.Height))).ToArray();

        graphics.DrawLines(pen, points);
        using var brush = new SolidBrush(color);
        graphics.DrawString(label, SystemFonts.CaptionFont, brush, points[^1].X - 60, points[^1].Y - 18);
    }

    private static void DrawLegend(Graphics graphics, Rectangle bounds)
    {
        using var cpuBrush = new SolidBrush(Color.FromArgb(74, 163, 255));
        using var memoryBrush = new SolidBrush(Color.FromArgb(116, 213, 129));
        graphics.DrawString("Selected process history", SystemFonts.CaptionFont, Brushes.WhiteSmoke, bounds.Left + 8, bounds.Top + 8);
        graphics.FillRectangle(cpuBrush, bounds.Left + 8, bounds.Bottom - 22, 12, 8);
        graphics.DrawString("CPU %", SystemFonts.CaptionFont, Brushes.WhiteSmoke, bounds.Left + 26, bounds.Bottom - 27);
        graphics.FillRectangle(memoryBrush, bounds.Left + 90, bounds.Bottom - 22, 12, 8);
        graphics.DrawString("RAM MB", SystemFonts.CaptionFont, Brushes.WhiteSmoke, bounds.Left + 108, bounds.Bottom - 27);
    }
}
