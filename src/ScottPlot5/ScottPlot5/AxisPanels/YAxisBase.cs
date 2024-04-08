﻿namespace ScottPlot.AxisPanels;

public abstract class YAxisBase : AxisBase, IAxis
{
    public double Height => Range.Span;

    public float GetPixel(double position, PixelRect dataArea)
    {
        double pxPerUnit = dataArea.Height / Height;
        double unitsFromMinValue = position - Min;
        float pxFromEdge = (float)(unitsFromMinValue * pxPerUnit);
        return dataArea.Bottom - pxFromEdge;
    }

    public double GetCoordinate(float pixel, PixelRect dataArea)
    {
        double pxPerUnit = dataArea.Height / Height;
        float pxFromMinValue = pixel - dataArea.Bottom;
        double unitsFromMinValue = pxFromMinValue / pxPerUnit;
        return Min - unitsFromMinValue;
    }

    public float Measure()
    {
        if (!IsVisible)
            return 0;

        if (!Range.HasBeenSet)
            return SizeWhenNoData;

        float largestTickSize = MeasureTicks();
        float largestTickLabelSize = Label.Measure().Height;
        float spaceBetweenTicksAndAxisLabel = 15;
        return largestTickSize + largestTickLabelSize + spaceBetweenTicksAndAxisLabel;
    }

    private float MeasureTicks()
    {
        using SKPaint paint = new();
        TickLabelStyle.ApplyToPaint(paint);

        float largestTickWidth = 0;

        foreach (Tick tick in TickGenerator.Ticks)
        {
            PixelSize tickLabelSize = Drawing.MeasureString(tick.Label, paint);
            largestTickWidth = Math.Max(largestTickWidth, tickLabelSize.Width + 10);
        }

        return largestTickWidth;
    }

    private PixelRect GetPanelRectangleLeft(PixelRect dataRect, float size, float offset)
    {
        return new PixelRect(
            left: dataRect.Left - offset - size,
            right: dataRect.Left - offset,
            bottom: dataRect.Bottom,
            top: dataRect.Top);
    }

    private PixelRect GetPanelRectangleRight(PixelRect dataRect, float size, float offset)
    {
        return new PixelRect(
            left: dataRect.Right + offset,
            right: dataRect.Right + offset + size,
            bottom: dataRect.Bottom,
            top: dataRect.Top);
    }

    public PixelRect GetPanelRect(PixelRect dataRect, float size, float offset)
    {
        return Edge == Edge.Left
            ? GetPanelRectangleLeft(dataRect, size, offset)
            : GetPanelRectangleRight(dataRect, size, offset);
    }

    public void Render(RenderPack rp, float size, float offset)
    {
        if (!IsVisible)
            return;

        PixelRect panelRect = GetPanelRect(rp.DataRect, size, offset);

        float textDistanceFromEdge = 10;
        float labelX = Edge == Edge.Left
            ? panelRect.Left + textDistanceFromEdge
            : panelRect.Right - textDistanceFromEdge;

        Pixel labelPoint = new(labelX, rp.DataRect.VerticalCenter);

        if (ShowDebugInformation)
        {
            Drawing.DrawDebugRectangle(rp.Canvas, panelRect, labelPoint, Label.ForeColor);
        }

        Label.Alignment = Alignment.UpperCenter;
        Label.Render(rp.Canvas, labelPoint);

        DrawTicks(rp, TickLabelStyle, panelRect, TickGenerator.Ticks, this, MajorTickStyle, MinorTickStyle);
        DrawFrame(rp, panelRect, Edge, FrameLineStyle);
    }

    public double GetPixelDistance(double distance, PixelRect dataArea)
    {
        return distance * dataArea.Height / Height;
    }

    public double GetCoordinateDistance(float distance, PixelRect dataArea)
    {
        return distance / (dataArea.Height / Height);
    }

    public void RegenerateTicks(PixelLength size)
    {
        using SKPaint paint = new();
        TickLabelStyle.ApplyToPaint(paint);
        TickGenerator.Regenerate(Range.ToCoordinateRange, Edge, size, paint);
    }
}
