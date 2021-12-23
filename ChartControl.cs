using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using AvaSkia;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Charts
{
    internal class ChartControl : Control
    {
        private CustomDrawingOperation custom;

        private List<SamplePoint> _points;

        public ChartControl()
        {
            ClipToBounds = true;
            _points = new List<SamplePoint>();

            //custom = new CustomDrawingOperation();
        }

        public void AddRange(IEnumerable<SamplePoint> points)
        {
            _points.AddRange(points);
        }

        public override void Render(DrawingContext context)
        {
            var noSkia = new FormattedText()
            {
                Text = "Current rendering API is not Skia"
            };

            context.Custom(new CustomDrawingOperation(new Rect(0, 0, Bounds.Width, Bounds.Height), noSkia, new float[0], new float[0]));
            Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Background);
        }


        class CustomDrawingOperation : ICustomDrawOperation
        {
            private readonly FormattedText _noSkia;
            private static float x = 0f;
            private static Stopwatch St = Stopwatch.StartNew();

            private float[] _xPoints;
            private float[] _yPoints;

            public CustomDrawingOperation(
                Rect bounds,
                FormattedText noSkia,
                float[] xPoints,
                float[] yPoints)
            {
                _noSkia = noSkia;
                Bounds = bounds;
                _xPoints = xPoints;
                _yPoints = yPoints;
            }

            public void Dispose()
            {
            }

            public Rect Bounds { get; set; }
            public bool HitTest(Point p) => false;
            public bool Equals(ICustomDrawOperation? other) => false;

            public void Render(IDrawingContextImpl context)
            {
                Debug.WriteLine(St.ElapsedMilliseconds);

                if (context is not ISkiaDrawingContextImpl skia)
                {
                    context.DrawText(Brushes.Black, new Point(), _noSkia.PlatformImpl);
                }


                var bitmap = new SKBitmap((int)Bounds.Width, (int)Bounds.Height, false);
                var canvas = new SKCanvas(bitmap);


                var start = St.Elapsed;
                canvas.Save();

                using (var paint = new SKPaint())
                {
                    paint.Color = SKColors.Blue;
                    paint.StrokeWidth = 1;
                    paint.IsAntialias = true;
                    paint.Style = SKPaintStyle.Stroke;

                    DrawSignal(canvas, paint, _xPoints, _yPoints);
                }

                canvas.Restore();
                var end = St.Elapsed;

                Debug.WriteLine((end - start).TotalMilliseconds);
              
            }

            private static void DrawSignal(SKCanvas? canvas, SKPaint paint, float[] xPoints, float[] yPoints)
            {
                if (xPoints.Length != yPoints.Length)
                {
                    throw new ArgumentException($"YPoints and XPoints should have equal count");
                }

                var preX = xPoints[0];
                var preY = yPoints[0];

                for (int i = 1; i < xPoints.Length; i++)
                {
                    canvas!.DrawLine(preX, preY, xPoints[i], yPoints[i], paint);

                    preX = xPoints[i];
                    preY = yPoints[i];
                }
            }
        }
    }
}
