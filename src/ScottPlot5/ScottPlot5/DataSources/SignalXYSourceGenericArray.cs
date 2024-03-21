﻿namespace ScottPlot.DataSources;

public class SignalXYSourceGenericArray<TX, TY> : ISignalXYSource
{
    public TX[] Xs { get; set; }
    public TY[] Ys { get; set; }
    public bool Rotated
    {
        get => false;
        set => throw new NotImplementedException("rotation is not yet supported for generic SignalXY plots (try using doubles)");
    }

    public double XOffset { get; set; } = 0;
    public double YOffset { get; set; } = 0;
    public int MinimumIndex { get; set; } = 0;
    public int MaximumIndex { get; set; }

    public SignalXYSourceGenericArray(TX[] xs, TY[] ys)
    {
        if (xs.Length != ys.Length)
        {
            throw new ArgumentException($"{nameof(xs)} and {nameof(ys)} must have equal length");
        }

        Xs = xs;
        Ys = ys;
        MaximumIndex = xs.Length - 1;
    }

    public AxisLimits GetAxisLimits()
    {
        double xMin = NumericConversion.GenericToDouble(Xs, MinimumIndex) + XOffset;
        double xMax = NumericConversion.GenericToDouble(Xs, MaximumIndex) + XOffset;
        CoordinateRange xRange = new(xMin, xMax);
        CoordinateRange yRange = GetRangeY(MinimumIndex, MaximumIndex);
        return Rotated
            ? new AxisLimits(yRange, xRange)
            : new AxisLimits(xRange, yRange);
    }

    public Pixel[] GetPixelsToDraw(RenderPack rp, IAxes axes)
    {
        // determine the range of data in view
        (Pixel[] PointBefore, int dataIndexFirst) = GetFirstPoint(axes);
        (Pixel[] PointAfter, int dataIndexLast) = GetLastPoint(axes);
        IndexRange columnIndexRange = new(dataIndexFirst, dataIndexLast);

        // get all points in view
        IEnumerable<Pixel> VisiblePoints = Enumerable.Range(0, (int)Math.Ceiling(rp.DataRect.Width))
            .Select(pixelColumnIndex => GetColumnPixels(pixelColumnIndex, columnIndexRange, rp, axes))
            .SelectMany(x => x);

        Pixel[] leftOutsidePoint = PointBefore, rightOutsidePoint = PointAfter;
        if (axes.XAxis.Range.Span < 0)
        {
            leftOutsidePoint = PointAfter;
            rightOutsidePoint = PointBefore;
        }

        // combine with one extra point before and after
        Pixel[] points = [.. leftOutsidePoint, .. VisiblePoints, .. rightOutsidePoint];

        // use interpolation at the edges to prevent points from going way off the screen
        if (leftOutsidePoint.Length > 0)
            SignalInterpolation.InterpolateBeforeX(rp, points);

        if (rightOutsidePoint.Length > 0)
            SignalInterpolation.InterpolateAfterX(rp, points);

        return points;
    }

    /// <summary>
    /// Return the vertical range covered by data between the given indices (inclusive)
    /// </summary>
    public CoordinateRange GetRangeY(int index1, int index2)
    {
        double min = NumericConversion.GenericToDouble(Ys, index1);
        double max = NumericConversion.GenericToDouble(Ys, index1);

        var minindex = Math.Min(index1, index2);
        var maxindex = Math.Max(index1, index2);

        for (int i = minindex; i <= maxindex; i++)
        {
            double value = NumericConversion.GenericToDouble(Ys, i);
            min = Math.Min(value, min);
            max = Math.Max(value, max);
        }

        return new CoordinateRange(min + YOffset, max + YOffset);
    }

    /// <summary>
    /// Get the index associated with the given X position
    /// </summary>
    public int GetIndexX(double x)
    {
        IndexRange range = new(MinimumIndex, MaximumIndex);
        return GetIndex(x, range);
    }

    /// <summary>
    /// Get the index associated with the given X position limited to the given range
    /// </summary>
    public int GetIndex(double x, IndexRange indexRange)
    {
        var (_, index) = SearchIndex(x, indexRange);
        return index;
    }

    /// <summary>
    /// Given a pixel column, return the pixels to render its line.
    /// If the column contains no data, no pixels are returned.
    /// If the column contains one point, return that one pixel.
    /// If the column contains multiple points, return 4 pixels: enter, min, max, and exit
    /// </summary>
    public IEnumerable<Pixel> GetColumnPixels(int pixelColumnIndex, IndexRange rng, RenderPack rp, IAxes axes)
    {
        float xPixel = pixelColumnIndex + rp.DataRect.Left;
        double unitsPerPixelX = axes.XAxis.Width / rp.DataRect.Width;
        double start = axes.XAxis.Min + unitsPerPixelX * pixelColumnIndex;
        double end = start + unitsPerPixelX;
        var (startPosition, startIndex) = SearchIndex(start, rng);
        var (endPosition, endIndex) = SearchIndex(end, rng);
        int pointsInRange = Math.Abs(endPosition - startPosition);

        if (pointsInRange == 0)
        {
            yield break;
        }

        double yStart = NumericConversion.GenericToDouble(Ys, startIndex);
        yield return new Pixel(xPixel, axes.GetPixelY(yStart + YOffset)); // enter

        if (pointsInRange > 1)
        {
            int lastIndex = startIndex < endIndex ? endIndex - 1 : endIndex + 1;
            double yEnd = NumericConversion.GenericToDouble(Ys, lastIndex);
            CoordinateRange yRange = GetRangeY(startIndex, lastIndex); //YOffset is added in GetRangeY
            yield return new Pixel(xPixel, axes.GetPixelY(yRange.Min)); // min
            yield return new Pixel(xPixel, axes.GetPixelY(yRange.Max)); // max
            yield return new Pixel(xPixel, axes.GetPixelY(yEnd) + YOffset); // exit
        }
    }

    /// <summary>
    /// If data is off to the screen to the left, 
    /// returns information about the closest point off the screen
    /// </summary>
    private (Pixel[] pointsBefore, int firstIndex) GetFirstPoint(IAxes axes)
    {
        var (firstPointPosition, firstPointIndex) = SearchIndex(axes.XAxis.Range.Span > 0 ? axes.XAxis.Min : axes.XAxis.Max); // if axis is reversed first index will on the right limit of the plot

        if (firstPointPosition > MinimumIndex)
        {
            double x = NumericConversion.GenericToDouble(Xs, firstPointIndex - 1) + XOffset;
            double y = NumericConversion.GenericToDouble(Ys, firstPointIndex - 1) + YOffset;
            float beforeX = axes.GetPixelX(x);
            float beforeY = axes.GetPixelY(y);
            Pixel beforePoint = new(beforeX, beforeY);
            return ([beforePoint], firstPointIndex);
        }
        else
        {
            return ([], MinimumIndex);
        }
    }

    /// <summary>
    /// If data is off to the screen to the right, 
    /// returns information about the closest point off the screen
    /// </summary>
    private (Pixel[] pointsAfter, int lastIndex) GetLastPoint(IAxes axes)
    {
        var (lastPointPosition, lastPointIndex) = SearchIndex(axes.XAxis.Range.Span > 0 ? axes.XAxis.Max : axes.XAxis.Min); // if axis is reversed last index will on the left limit of the plot

        if (lastPointPosition <= MaximumIndex)
        {
            double x = NumericConversion.GenericToDouble(Xs, lastPointIndex) + XOffset;
            double y = NumericConversion.GenericToDouble(Ys, lastPointIndex) + YOffset;
            float afterX = axes.GetPixelX(x);
            float afterY = axes.GetPixelY(y);
            Pixel afterPoint = new(afterX, afterY);
            return ([afterPoint], lastPointIndex);
        }
        else
        {
            return ([], MaximumIndex);
        }
    }

    /// <summary>
    /// Search the index associated with the given X position
    /// </summary>
    private (int SearchedPosition, int LimitedIndex) SearchIndex(double x)
    {
        IndexRange range = new(MinimumIndex, MaximumIndex);
        return SearchIndex(x, range);
    }

    /// <summary>
    /// Search the index associated with the given X position limited to the given range
    /// </summary>
    private (int SearchedPosition, int LimitedIndex) SearchIndex(double x, IndexRange indexRange)
    {
        NumericConversion.DoubleToGeneric(x - XOffset, out TX x2);
        int index = Array.BinarySearch(Xs, indexRange.Min, indexRange.Length, x2);

        // If x is not exactly matched to any value in Xs, BinarySearch returns a negative number. We can bitwise negation to obtain the position where x would be inserted (i.e., the next highest index).
        // If x is below the min Xs, BinarySearch returns -1. Here, bitwise negation returns 0 (i.e., x would be inserted at the first index of the array).
        // If x is above the max Xs, BinarySearch returns -maxIndex. Bitwise negation of this value returns maxIndex + 1 (i.e., the position after the last index). However, this index is beyond the array bounds, so we return the final index instead.
        if (index < 0)
        {
            index = ~index; // read BinarySearch() docs
        }

        return (SearchedPosition: index, LimitedIndex: index > indexRange.Max ? indexRange.Max : index);
    }
}
