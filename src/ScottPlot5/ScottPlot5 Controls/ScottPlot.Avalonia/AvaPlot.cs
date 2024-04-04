﻿using Avalonia;
using Avalonia.Skia;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Threading;

using ScottPlot.Control;

using SkiaSharp;

using Controls = Avalonia.Controls;

namespace ScottPlot.Avalonia;

public class AvaPlot : Controls.Control, IPlotControl
{
    public Plot Plot { get; } = new();

    public IPlotInteraction Interaction { get; set; }
    public IPlotMenu Menu { get; set; }

    public GRContext? GRContext => null;

    public float DisplayScale { get; set; }

    public AvaPlot()
    {
        Focusable = true;
        ClipToBounds = true;
        DisplayScale = DetectDisplayScale();
        Interaction = new Interaction(this);
        Menu = new AvaPlotMenu(this);
        Refresh();
    }

    private class CustomDrawOp : ICustomDrawOperation
    {
        private readonly Plot _plot;

        public Rect Bounds { get; }
        public bool HitTest(Point p) => true;
        public bool Equals(ICustomDrawOperation? other) => false;

        public CustomDrawOp(Rect bounds, Plot plot)
        {
            _plot = plot;
            Bounds = bounds;
        }

        public void Dispose()
        {
            // No-op
        }

        public void Render(ImmediateDrawingContext context)
        {
            if (!context.TryGetFeature<ISkiaSharpApiLeaseFeature>(out var leaseFeature)) return;
            using var lease = leaseFeature.Lease();
            _plot.Render(lease.SkCanvas, new(0, (float)Bounds.Width, (float)Bounds.Height, 0));
        }
    }

    public override void Render(DrawingContext context)
    {
        context.Custom(new CustomDrawOp(new(0, 0, Bounds.Width, Bounds.Height), Plot));
    }

    public void Refresh()
    {
        Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Background);
    }

    public void ShowContextMenu(Pixel position)
    {
        Menu.ShowContextMenu(position);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        Interaction.MouseDown(
            position: e.ToPixel(this),
            button: e.GetCurrentPoint(this).Properties.PointerUpdateKind.ToButton());

        e.Pointer.Capture(this);

        if (e.ClickCount == 2)
        {
            Interaction.DoubleClick();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        Interaction.MouseUp(
            position: e.ToPixel(this),
            button: e.GetCurrentPoint(this).Properties.PointerUpdateKind.ToButton());

        e.Pointer.Capture(null);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        Interaction.OnMouseMove(e.ToPixel(this));
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        float delta = (float)e.Delta.Y; // This is now the correct behavior even if shift is held, see https://github.com/AvaloniaUI/Avalonia/pull/8628

        if (delta != 0)
        {
            Interaction.MouseWheelVertical(e.ToPixel(this), delta);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        Interaction.KeyDown(e.ToKey());
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        Interaction.KeyUp(e.ToKey());
    }

    public float DetectDisplayScale()
    {
        // TODO: improve support for DPI scale detection
        // https://github.com/ScottPlot/ScottPlot/issues/2760
        return 1.0f;
    }
}
