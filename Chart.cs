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
    public class Chart
    {
        public float SignalPeriod { get; set; }
        public List<Series> Series { get; set; }
        
        public Chart()
        {
            Series = new List<Series>();
        }

        public void Draw(SKCanvas canvas, Rect bounds)
        {
            var count = Series.Count;
            var height = bounds.Height / count;

            var tasks = new Task[count];
            var bitmaps = new SKBitmap[count];

            for (int i = 0; i < count; i++)
            {
                var b = new Rect(0, 0, (int)bounds.Width, (int)height);
                var series = Series[i];
                var bitmap = new SKBitmap((int)b.Width, (int)b.Height);
                var c = new SKCanvas(bitmap);
                c.Save();
                bitmaps[i] = bitmap;

                tasks[i] = Task.Run(() =>
                {
                    DrawSeries(c, b, series);
                });
            }

            Task.WaitAll(tasks);

            for (int i = 0; i < count; i++)
            {
                var y = (float)(i * height);
                canvas.DrawBitmap(bitmaps[i], new SKPoint(0f, y));
            }
        }

        private static void DrawSeries(SKCanvas canvas, Rect bounds, Series series)
        {
            var mergeSize = (int)Math.Floor(series.Limit / bounds.Width);
            //mergeSize = 1;
            var downSampledPoints = GetDownSampledPoints(series.Points, mergeSize);

            var maxY = downSampledPoints.Max(x => x.Y);
            var minY = downSampledPoints.Min(x => x.Y);

            var yCoeff = (float)(bounds.Height / (maxY - minY));
            var yBias = -minY * yCoeff + (float)bounds.Y;

            var xCoeff = (float)(bounds.Width / series.Limit);
            var xBias = 0f;

            var screenPoints = GetScreenPoints(downSampledPoints, xCoeff, xBias, yCoeff, yBias);

            using (var paint = new SKPaint())
            {
                paint.Color = SKColors.Blue;
                paint.StrokeWidth = 1;
                paint.IsAntialias = true;
                paint.Style = SKPaintStyle.Stroke;

                DrawSignal(canvas, paint, screenPoints);
            }
        }

        private static SamplePoint[] GetDownSampledPoints(List<float> points, int mergeSize)
        {
            var pointsCount = points.Count;

            var len = 2 * (int)Math.Ceiling(pointsCount / (double)mergeSize);
            var results = new SamplePoint[len];
            int index = 0;

            for (int i = 0; i < pointsCount; i += mergeSize)
            {
                var maxY = points[i];
                var minY = points[i];
                var maxX = i;
                var minX = i;

                for (int j = i + 1; j < i + mergeSize && j < pointsCount; j++)
                {
                    var y = points[j];

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

                results[index++] = new SamplePoint { Y = minY, X = minX };
                results[index++] = new SamplePoint { Y = maxY, X = maxX };
            }

            return results;
        }

        private static SKPoint[] GetScreenPoints(
            SamplePoint[] samplePoints,
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
    }
}
