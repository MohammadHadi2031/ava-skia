using System;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using Nikrotek.Electrophysiology.Core.DataGenerators;
using SkiaSharp;

namespace AvaSkia
{
    public partial class MainWindow : Window
    {
        private const int SamplingFrequency = 64000;


        private MicrochartControl control;
        private IDataGenerator[] generators;

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            control = new MicrochartControl();
            control.Chart.SignalPeriod = 1f / SamplingFrequency;

            var rnd = new Random();
            generators = new SinDataGenerator[16];
            for (int i = 0; i < generators.Length; i++)
            {
                generators[i] = new SinDataGenerator(rnd.Next(10, 50), SamplingFrequency, 0);
                control.Chart.Series.Add(new Series { Limit = 1 * SamplingFrequency });
            }

            var dockPanel = this.FindControl<DockPanel>("dockPanel");
            dockPanel.Children.Add(control);

            var t = new Timer(10);
            t.Elapsed += (s, e) =>
            {
                for (int i = 0; i < control.Chart.Series.Count; i++)
                {
                    var rnd = new Random();
                    var amp = rnd.Next(1, 10);
                    amp = 1;

                    var values = generators[i].GetNextValues(SamplingFrequency / 100);
                    
                    var points = values
                        .Select(v => (float)v * amp)
                        .ToArray();

                    control.Chart.Series[i].AddPoints(points);
                }
            };
            t.Start();

            var timer = new DispatcherTimer(TimeSpan.FromSeconds(1 / 100d), DispatcherPriority.Normal, (s, e) =>
            {
                control.InvalidateVisual();
            });
            //timer.Start();
        }
    }

    public class CustomSkiaPage : Control
    {
        public CustomSkiaPage()
        {
            ClipToBounds = true;
        }

        class CustomDrawOp : ICustomDrawOperation
        {
            private readonly FormattedText _noSkia;

            public CustomDrawOp(Rect bounds, FormattedText noSkia)
            {
                _noSkia = noSkia;
                Bounds = bounds;
            }

            public void Dispose()
            {
                // No-op
            }

            public Rect Bounds { get; }
            public bool HitTest(Point p) => false;
            public bool Equals(ICustomDrawOperation other) => false;
            static Stopwatch St = Stopwatch.StartNew();
            public void Render(IDrawingContextImpl context)
            {
                var canvas = (context as ISkiaDrawingContextImpl)?.SkCanvas;
                if (canvas == null)
                    context.DrawText(Brushes.Black, new Point(), _noSkia.PlatformImpl);
                else
                {
                    canvas.Save();
                    // create the first shader
                    var colors = new SKColor[] {
                        new SKColor(0, 255, 255),
                        new SKColor(255, 0, 255),
                        new SKColor(255, 255, 0),
                        new SKColor(0, 255, 255)
                    };

                    var sx = Animate(100, 2, 10);
                    var sy = Animate(1000, 5, 15);
                    var lightPosition = new SKPoint(
                        (float)(Bounds.Width / 2 + Math.Cos(St.Elapsed.TotalSeconds) * Bounds.Width / 4),
                        (float)(Bounds.Height / 2 + Math.Sin(St.Elapsed.TotalSeconds) * Bounds.Height / 4));
                    using (var sweep =
                        SKShader.CreateSweepGradient(new SKPoint((int)Bounds.Width / 2, (int)Bounds.Height / 2), colors,
                            null))
                    using (var turbulence = SKShader.CreatePerlinNoiseFractalNoise(0.05f, 0.05f, 4, 0))
                    using (var shader = SKShader.CreateCompose(sweep, turbulence, SKBlendMode.SrcATop))
                    using (var blur = SKImageFilter.CreateBlur(Animate(100, 2, 10), Animate(100, 5, 15)))
                    using (var paint = new SKPaint
                    {
                        Shader = shader,
                        ImageFilter = blur
                    })
                        canvas.DrawPaint(paint);

                    using (var pseudoLight = SKShader.CreateRadialGradient(
                        lightPosition,
                        (float)(Bounds.Width / 3),
                        new[] {
                            new SKColor(255, 200, 200, 100),
                            SKColors.Transparent,
                            new SKColor(40,40,40, 220),
                            new SKColor(20,20,20, (byte)Animate(100, 200,220)) },
                        new float[] { 0.3f, 0.3f, 0.8f, 1 },
                        SKShaderTileMode.Clamp))
                    using (var paint = new SKPaint
                    {
                        Shader = pseudoLight
                    })
                        canvas.DrawPaint(paint);
                    canvas.Restore();
                }
            }
            static int Animate(int d, int from, int to)
            {
                var ms = (int)(St.ElapsedMilliseconds / d);
                var diff = to - from;
                var range = diff * 2;
                var v = ms % range;
                if (v > diff)
                    v = range - v;
                var rv = v + from;
                if (rv < from || rv > to)
                    throw new Exception("WTF");
                return rv;
            }
        }



        public override void Render(DrawingContext context)
        {
            var noSkia = new FormattedText()
            {
                Text = "Current rendering API is not Skia"
            };
            context.Custom(new CustomDrawOp(new Rect(0, 0, Bounds.Width, Bounds.Height), noSkia));
            Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Background);
        }
    }

    public class RaderControl : Control
    {
        public RaderControl()
        {
            ClipToBounds = true;
        }

        class CustomDrawOp : ICustomDrawOperation
        {
            private readonly FormattedText _noSkia;
            private static float x = 0f;

            public CustomDrawOp(Rect bounds, FormattedText noSkia)
            {
                _noSkia = noSkia;
                Bounds = bounds;
            }

            public void Dispose()
            {
                // No-op
            }

            public Rect Bounds { get; }
            public bool HitTest(Point p) => false;
            public bool Equals(ICustomDrawOperation other) => false;
            static Stopwatch St = Stopwatch.StartNew();
            public void Render(IDrawingContextImpl context)
            {
                Debug.WriteLine(St.ElapsedMilliseconds);

                var canvas = (context as ISkiaDrawingContextImpl)?.SkCanvas;
                if (canvas == null)
                    context.DrawText(Brushes.Black, new Point(), _noSkia.PlatformImpl);
                else
                {
                    var start = St.Elapsed;
                    canvas.Save();

                    using (var paint = new SKPaint())
                    {
                        paint.Color = SKColors.Blue;
                        paint.StrokeWidth = 1;
                        paint.IsAntialias = true;
                        paint.Style = SKPaintStyle.Stroke;

                        DrawSin(canvas, paint, 10f, 50f);
                        DrawSin(canvas, paint, 15f, 150f);
                        DrawSin(canvas, paint, 35f, 250f);
                        DrawSin(canvas, paint, 25f, 350f);
                        DrawSin(canvas, paint, 13f, 450f);
                        DrawSin(canvas, paint, 30f, 550f);
                    }


                    canvas.Restore();
                    var end = St.Elapsed;

                    Debug.WriteLine((end - start).TotalMilliseconds);
                    x += 1;
                    x %= 1000;
                }
            }

            private static void DrawSin(SKCanvas? canvas, SKPaint paint, float f, float yOffset)
            {
                var preX = 0f;
                var preY = yOffset;

                for (var i = 0f; i < 1000f; i++)
                {
                    var y = yOffset + 20f * MathF.Sin(x * MathF.PI / 100f + 2f * MathF.PI * f * i / 1000f);
                    canvas!.DrawLine(preX, preY, i, y, paint);

                    preY = y;
                    preX = i;
                }
            }
        }

        public override void Render(DrawingContext context)
        {
            var noSkia = new FormattedText()
            {
                Text = "Current rendering API is not Skia"
            };
            context.Custom(new CustomDrawOp(new Rect(0, 0, Bounds.Width, Bounds.Height), noSkia));
            Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Background);
        }
    }
}