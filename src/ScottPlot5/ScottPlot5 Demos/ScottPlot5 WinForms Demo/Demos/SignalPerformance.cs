﻿using ScottPlot;

namespace WinForms_Demo.Demos;

public partial class SignalPerformance : Form, IDemoWindow
{
    public string Title => "Scatter Plot vs. Signal Plot";

    public string Description => "Demonstrates how Signal plots and " +
        "OpenGL-accelerated Scatter plots can display " +
        "millions of points interactively at high framerates";

    public SignalPerformance()
    {
        InitializeComponent();
        Replot();

        rbSignal.CheckedChanged += (s, e) => Replot();
        rbScatter.CheckedChanged += (s, e) => Replot();
        rbFastSignal.CheckedChanged += (s, e) => Replot();
        cachePeriodApplyBtn.Click += (s, e) => Replot();
    }

    private void Replot()
    {
        formsPlot1.Plot.Clear();
        label1.Text = "Generating random data...";
        Application.DoEvents();

        int pointCount = 1_000_000;
        double[] xs = ScottPlot.Generate.Consecutive(pointCount);
        double[] ys = ScottPlot.Generate.Sin(pointCount);
        Generate.AddNoiseInPlace(ys);

        if (rbSignal.Checked)
        {
            nUDCachePeriod.Visible = false;
            cachePeriodApplyBtn.Visible = false;
            labelCachePeriod.Visible = false;
            formsPlot1.Plot.Add.Signal(ys);
            formsPlot1.Plot.Axes.Title.Label.Text = $"Signal Plot with {ys.Length:N0} Points";
            label1.Text = "Signal plots are very performant for large datasets";
        }
        else if (rbScatter.Checked)
        {
            nUDCachePeriod.Visible = false;
            cachePeriodApplyBtn.Visible = false;
            labelCachePeriod.Visible = false;
            var sp = formsPlot1.Plot.Add.ScatterLine(xs, ys);
            formsPlot1.Plot.Axes.Title.Label.Text = $"Scatter Plot with {ys.Length:N0} Points";
            label1.Text = "Traditional Scatter plots are not performant for large datasets";
        }
        else if (rbFastSignal.Checked)
        {
            nUDCachePeriod.Visible = true;
            cachePeriodApplyBtn.Visible = true;
            labelCachePeriod.Visible = true;
            formsPlot1.Plot.Add.Signal(new ScottPlot.DataSources.FastSignalSourceDouble(ys, 1, (int)nUDCachePeriod.Value));
            formsPlot1.Plot.Axes.Title.Label.Text = $"FastSignal Plot with {ys.Length:N0} Points";
            label1.Text = "Signal plots are very performant for large datasets + Cached!";
        }

        formsPlot1.Plot.Axes.AutoScale();
        formsPlot1.Refresh();
    }
}
