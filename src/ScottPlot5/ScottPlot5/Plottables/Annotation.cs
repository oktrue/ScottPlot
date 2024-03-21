﻿namespace ScottPlot.Plottables;

public class Annotation : IPlottable
{
    public bool IsVisible { get; set; } = true;
    public IAxes Axes { get; set; } = new Axes();
    public IEnumerable<LegendItem> LegendItems => LegendItem.None;

    public Label Label { get; set; } = new();
    public Alignment Alignment { get; set; } = Alignment.UpperLeft;
    public float OffsetX { get; set; } = 5;
    public float OffsetY { get; set; } = 5;

    public AxisLimits GetAxisLimits() => AxisLimits.NoLimits;

    public void Render(RenderPack rp)
    {
        if (!IsVisible)
            return;

        Pixel px = Label.GetRenderLocation(rp.DataRect, Alignment, OffsetX, OffsetY);

        using SKPaint paint = new();
        Label.Render(rp.Canvas, px, paint);
    }
}
