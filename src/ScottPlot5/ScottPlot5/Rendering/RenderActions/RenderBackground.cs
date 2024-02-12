namespace ScottPlot.Rendering.RenderActions;

public class RenderBackground : IRenderAction
{
    public void Render(RenderPack rp)
    {
        rp.Canvas.DrawColor(rp.Plot.DataBackground.ToSKColor());
    }
}
