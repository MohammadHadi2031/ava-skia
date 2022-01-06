using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using SkiaSharp;

namespace AvaSkia
{
    public class MicrochartControl : Control, IDisposable
    {
        private CustomDrawingOperation custom = new CustomDrawingOperation();
        private static Stopwatch controlStopwatch = Stopwatch.StartNew();

        private Chart chart;

        public Chart Chart
        {
            get => chart;
            set => chart = value;
        }

        public MicrochartControl()
        {
            chart = new Chart();
        }


        protected override Size MeasureOverride(Size availableSize)
            => availableSize;

        public override void Render(DrawingContext context)
        {
            this.custom.Bounds = this.Bounds;
            this.custom.Chart = this.chart;

            context.Custom(custom);
            Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Background);
        }

        public void Dispose()
        {
            chart.Dispose();
        }

        private class CustomDrawingOperation : ICustomDrawOperation
        {
            public Chart Chart { get; set; }
            public void Dispose() { }
            public bool HitTest(Point p) => false;
            public bool Equals(ICustomDrawOperation? other) => false;

            public Rect Bounds { get; set; }

            public void Render(IDrawingContextImpl context)
            {
                Debug.WriteLine($"static stopwatch: {controlStopwatch.Elapsed}");

                var stopwatch = Stopwatch.StartNew();

                //var bitmap = new SKBitmap((int)Bounds.Width, (int)Bounds.Height, false);
                //var canvas = new SKCanvas(bitmap);
                if (context is not ISkiaDrawingContextImpl skia)
                {
                    return;
                }
                var canvas = skia.SkCanvas;
                canvas.Save();

                Chart.Draw(canvas, Bounds);

                //if (context is ISkiaDrawingContextImpl skia)
                //{
                //    skia.SkCanvas.DrawBitmap(bitmap, (int)Bounds.X, (int)Bounds.Y);
                //}

                stopwatch.Stop();
                Debug.WriteLine($"microchart: {stopwatch.ElapsedMilliseconds}");
            }
        }
    }
}