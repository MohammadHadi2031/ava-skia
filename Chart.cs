using Avalonia;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvaSkia
{
    /// <summary>
    /// Draws all points from scratch
    /// </summary>
    public class Chart : IDisposable
    {
        private AcceleratedDownSampler acceleratedDownSampler;
        private List<Series> series;
        
        public float SignalPeriod { get; set; }
        public IReadOnlyList<Series> Series => series.AsReadOnly();
        
        public Chart()
        {
            series = new List<Series>();
            acceleratedDownSampler = new AcceleratedDownSampler();
        }

        public void AddSeries(int limit)
        {
            series.Add(new Series(limit, acceleratedDownSampler.Accelerator));
        }

        public void Draw(SKCanvas canvas, Rect bounds)
        {
            var count = series.Count;
            var height = bounds.Height / count;

            for (int i = 0; i < count; i++)
            {
                var y = i * height;
                var b = new Rect(bounds.X, y, bounds.Width, height);
                DrawSeries(canvas, b, series[i]);
            }
        }

        private void DrawSeries(SKCanvas canvas, Rect bounds, Series series)
        {
            SKPoint[] screenPoints = GetDownSampledScreenPoints(bounds, series);

            using (var paint = new SKPaint())
            {
                paint.Color = SKColors.Blue;
                paint.StrokeWidth = 1;
                paint.IsAntialias = true;
                paint.Style = SKPaintStyle.Stroke;
                canvas.DrawPoints(SKPointMode.Lines, screenPoints, paint);
            }
        }

        private SKPoint[] GetDownSampledScreenPoints(Rect bounds, Series series)
        {
            var mergeSize = (int)Math.Floor(series.Limit / bounds.Width);
            //var downSampledPoints = GetDownSampledPoints(series.Points, mergeSize);
            var downSampledPoints = GetDownSampledPointsWithAccelerator(series, mergeSize);

            var maxY = downSampledPoints.Max(x => x.Y);
            var minY = downSampledPoints.Min(x => x.Y);

            var yCoeff = (float)(bounds.Height / (maxY - minY));
            var yBias = -minY * yCoeff + (float)bounds.Y;

            var xCoeff = (float)(bounds.Width / series.Limit);
            var xBias = 0f;

            var screenPoints = GetScreenPoints(downSampledPoints, xCoeff, xBias, yCoeff, yBias);
            return screenPoints;
        }

        private SKPoint[] GetDownSampledPointsWithAccelerator(Series series, int mergeSize)
        {
            var pointsCount = series.Points.Length;
            var partCount = (int)Math.Ceiling(pointsCount / (double)mergeSize);
            var len = 2 * partCount;
            var results = new SKPoint[len];

            acceleratedDownSampler.FillDownSampledPointsArray(partCount, results, series, mergeSize);
            return results;
        }

        private static SKPoint[] GetDownSampledPoints(float[] points, int mergeSize)
        {
            var pointsCount = points.Length;
            var partCount = (int)Math.Ceiling(pointsCount / (double)mergeSize);
            var len = 2 * partCount;
            var results = new SKPoint[len];

            for (int part = 0; part < partCount; part++)
            {
                FillDownSampledPointsArray(part, results, points, mergeSize);
            }

            return results;
        }

        private static void FillDownSampledPointsArray(
            int part,
            SKPoint[] outputs,
            float[] inputs,
            int mergeSize)
        {
            var pointsCount = inputs.Length;

            var i = part * mergeSize;
            var maxY = inputs[i];
            var minY = inputs[i];
            var maxX = i;
            var minX = i;

            for (int j = i + 1; j < i + mergeSize && j < pointsCount; j++)
            {
                var y = inputs[j];

                if (maxY < y)
                {
                    maxY = y;
                    maxX = j;
                }
                else if (minY > y)
                {
                    minY = y;
                    minX = j;
                }
            }

            outputs[2 * part] = new SKPoint { Y = minY, X = minX };
            outputs[2 * part + 1] = new SKPoint { Y = maxY, X = maxX };
        }

        private static SKPoint[] GetScreenPoints(
            SKPoint[] samplePoints,
            float xCoeff,
            float xBias,
            float yCoeff,
            float yBias)
        {
            var screenPoints = new SKPoint[samplePoints.Length];

            for (int i = 0; i < screenPoints.Length; i++)
            {
                var p = samplePoints[i];
                screenPoints[i] = new SKPoint(p.X * xCoeff + xBias, p.Y * yCoeff + yBias);
            }

            return screenPoints;
        }

        private static void DrawSignal(SKCanvas canvas, SKPaint paint, SKPoint[] points)
        {
            var pre = points[0];

            for (int i = 1; i < points.Length; i++)
            {
                canvas.DrawLine(pre, points[i], paint);
                pre = points[i];
            }
        }

        public void Dispose()
        {
            foreach (var s in series)
            {
                s.Dispose();
            }
            acceleratedDownSampler.Dispose();
        }
    }
}
