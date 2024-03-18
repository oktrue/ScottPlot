namespace ScottPlot.Rendering.RenderActions;

public class RenderPlottables : IRenderAction
{
    public void Render(RenderPack rp)
    {
        rp.ClipToDataArea();

        foreach (IPlottable plottable in rp.Plot.PlottableList)
        {
            if (!plottable.IsVisible)
                continue;

            plottable.Axes.DataRect = rp.DataRect;

            if (plottable is IPlottableGL plottableGL)
            {
                plottableGL.Render(rp);
            }
            else
            {
                plottable.Render(rp);
            }
        }
    }
}
