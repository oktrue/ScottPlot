using System.CodeDom;

namespace ScottPlot.Plottables;

public class FillAboveAndBelowPlot(IScatterSource data) : IPlottable
{
    public string Label { get; set; } = string.Empty;
    public bool IsVisible { get; set; } = true;
    public IAxes Axes { get; set; } = new Axes();
    public LineStyle UpperLineStyle { get; set; } = new();
    public LineStyle LowerLineStyle { get; set; } = new();

    public MarkerStyle MarkerStyle { get; set; } = MarkerStyle.Default;

    public IScatterSource Data { get; } = data;

    public LinePattern LinePattern { get => UpperLineStyle.Pattern; set => UpperLineStyle.Pattern = value; }
    public float LineWidth { get => UpperLineStyle.Width; set => UpperLineStyle.Width = value; }
    public float BelowLineWidth { get => LowerLineStyle.Width; set => LowerLineStyle.Width = value; }
    public float MarkerSize { get => MarkerStyle.Size; set => MarkerStyle.Size = value; }

    /// <summary>
    /// The style of lines to use when connecting points.
    /// </summary>
    public ConnectStyle ConnectStyle = ConnectStyle.Straight;

    /// <summary>
    /// If enabled, points will be connected by smooth lines instead of straight diagnal lines.
    /// <see cref="SmoothTension"/> adjusts the smoothnes of the lines.
    /// </summary>
    public bool Smooth = false;

    public double BaseLine;

    /// <summary>
    /// Tension to use for smoothing when <see cref="Smooth"/> is enabled
    /// </summary>
    public double SmoothTension = 0.5;

    public Color Color
    {
        get => UpperLineStyle.Color;
        set
        {
            UpperLineStyle.Color = value;
            MarkerStyle.Fill.Color = value;
            MarkerStyle.Outline.Color = value;
        }
    }

    public Color BelowColor
    {
        get => LowerLineStyle.Color;
        set
        {
            LowerLineStyle.Color = value;
            MarkerStyle.Fill.Color = value;
            MarkerStyle.Outline.Color = value;
        }
    }

    public AxisLimits GetAxisLimits() => Data.GetLimits();

    public IEnumerable<LegendItem> LegendItems => LegendItem.Single(Label, MarkerStyle, UpperLineStyle);

    public void Render(RenderPack rp)
    {
        // TODO: can this be more effecient by moving this logic into the DataSource to avoid copying?
        Pixel[] markerPixels = Data.GetScatterPoints().Select(Axes.GetPixel).ToArray();

        if (!markerPixels.Any())
            return;

        IEnumerable<Pixel> linePixels = ConnectStyle switch
        {
            ConnectStyle.Straight => markerPixels,
            ConnectStyle.StepHorizontal => GetStepDisplayPixels(markerPixels, true),
            ConnectStyle.StepVertical => GetStepDisplayPixels(markerPixels, false),
            _ => throw new NotImplementedException($"unsupported {nameof(ConnectStyle)}: {ConnectStyle}"),
        };

        using SKPaint paint = new();

        float bottom = Axes.GetPixelY(BaseLine);
        var clipRect = new SKRect(rp.DataRect.Left, rp.DataRect.Top, rp.DataRect.Right, bottom);

        rp.Canvas.Save();
        rp.Canvas.ClipRect(clipRect);
        Drawing.DrawLines(rp.Canvas, paint, linePixels, UpperLineStyle);

        IEnumerable<SKPoint> skPoints = linePixels.Select(x => x.ToSKPoint());
        using SKPath path = new();

        path.MoveTo(skPoints.First().X, bottom);
        path.LineTo(skPoints.First());

        foreach (SKPoint p in skPoints.Skip(1))
        {
            path.LineTo(p);
        }

        path.LineTo(skPoints.Last().X, bottom);

        using var upperGradient = SKShader.CreateLinearGradient(
                                    new SKPoint(0, path.Bounds.Top),
                                    new SKPoint(0, bottom),
                                    [Color.ToSKColor().WithAlpha(99), Color.ToSKColor().WithAlpha(0)],
                                    SKShaderTileMode.Clamp);

        using var upperPaint = new SKPaint
        {
            Color = Color.ToSKColor(),
            Shader = upperGradient,
        };

        rp.Canvas.DrawPath(path, upperPaint);

        rp.Canvas.Restore();

        var belowClipRect = new SKRect(rp.DataRect.Left, bottom, rp.DataRect.Right, rp.DataRect.Bottom);

        rp.Canvas.Save();
        rp.Canvas.ClipRect(belowClipRect);
        Drawing.DrawLines(rp.Canvas, paint, linePixels, LowerLineStyle);

        using var lowerGradient = SKShader.CreateLinearGradient(
                                    new SKPoint(0, bottom),
                                    new SKPoint(0, path.Bounds.Bottom),
                                    [BelowColor.ToSKColor().WithAlpha(0), BelowColor.ToSKColor().WithAlpha(99)],
                                    SKShaderTileMode.Clamp);

        using var lowerPaint = new SKPaint
        {
            Color = BelowColor.ToSKColor(),
            Shader = lowerGradient,
        };

        rp.Canvas.DrawPath(path, lowerPaint);

        rp.Canvas.Restore();

        Drawing.DrawMarkers(rp.Canvas, paint, markerPixels, MarkerStyle);
    }

    /// <summary>
    /// Convert scatter plot points (connected by diagnal lines) to step plot points (connected by right angles)
    /// by inserting an extra point between each of the original data points to result in L-shaped steps.
    /// </summary>
    /// <param name="points">Array of corner positions</param>
    /// <param name="right">Indicates that a line will extend to the right before rising or falling.</param>
    public static IEnumerable<Pixel> GetStepDisplayPixels(Pixel[] pixels, bool right)
    {
        Pixel[] pixelsStep = new Pixel[pixels.Count() * 2 - 1];

        int offsetX = right ? 1 : 0;
        int offsetY = right ? 0 : 1;

        for (int i = 0; i < pixels.Count() - 1; i++)
        {
            pixelsStep[i * 2] = pixels[i];
            pixelsStep[i * 2 + 1] = new Pixel(pixels[i + offsetX].X, pixels[i + offsetY].Y);
        }

        pixelsStep[pixelsStep.Length - 1] = pixels[pixels.Length - 1];

        return pixelsStep;
    }
}
