using System.Threading;
using System.Data;
using System.Net;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Buffers;
using Avalonia;
using Avalonia.Media;
using Avalonia.Controls;
using System;
using System.Runtime.CompilerServices;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Numerics;
using System.Collections.Concurrent;
using ReactiveUI;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Threading;

namespace ColourWheel.Controls
{
    public class ColourWheel : Panel
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int XYtoIndex(int width, int x, int y) => x + (y * width);

        private const int rowBatch = 48;

        private Stopwatch stopwatch = new Stopwatch();
        private Stopwatch stopwatch2 = new Stopwatch();

        private static readonly DirectProperty<ColourWheel, WriteableBitmap> ColorWheelBitmapProperty =
            AvaloniaProperty.RegisterDirect<ColourWheel, WriteableBitmap>(
                nameof(ColorWheelBitmap),
                o => o.ColorWheelBitmap,
                (o, v) => o.ColorWheelBitmap = v);

        private WriteableBitmap _ColorWheelBitmap;

        private WriteableBitmap ColorWheelBitmap
        {
            get { return _ColorWheelBitmap; }
            set { SetAndRaise(ColorWheelBitmapProperty, ref _ColorWheelBitmap, value); }
        }

        public ColourWheel()
        {
            this.WhenAnyValue(x => x.Bounds)
                .Throttle(TimeSpan.FromSeconds(0.1))
                .Subscribe(x => GenerateColorWheel(ConstraintSize(x.Size)));

            AffectsRender<ColourWheel>(BoundsProperty, ColorWheelBitmapProperty);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var newSize = ConstraintSize(finalSize);

            if (ColorWheelBitmap is null)
            {
                GenerateColorWheel(ConstraintSize(newSize));
            }

            return newSize;
        }

        bool _isDragging = false;

        protected override void OnPointerMoved(Avalonia.Input.PointerEventArgs e)
        {
            if (!_isDragging) return;

            var curPoint = e.GetCurrentPoint(this);

            var curPointVector = new Vector2((float)curPoint.Position.X, (float)curPoint.Position.Y);

            var boundsSize = GetCurrentSize;

            var centerVector = new Vector2((float)boundsSize.Width, (float)boundsSize.Height) / 2;

            var radius = boundsSize.Width / 2f;

            var pointToCenterVector = curPointVector - centerVector;

            var distanceToCenter = pointToCenterVector.Length();

            if (distanceToCenter > radius) return;

            Console.WriteLine((180 / Math.PI) * Math.Atan2(pointToCenterVector.X, pointToCenterVector.Y));

        }

        protected override void OnPointerReleased(Avalonia.Input.PointerReleasedEventArgs e)
        {
            _isDragging = false;
        }

        protected override void OnPointerPressed(Avalonia.Input.PointerPressedEventArgs e)
        {
            _isDragging = true;

        }

        private Size ConstraintSize(Size finalSize)
        {
            int width = (int)finalSize.Width;
            int height = (int)finalSize.Height;

            if (width <= 1 || height <= 1)
                return new Size(1, 1);

            width = Math.Min(width, height);

            if (width % 2 == 0) width--;
            if (height % 2 == 0) height--;

            return new Size(width, height);
        }

        const float PI = (float)Math.PI;
        const float TWO_PI = (float)(2 * Math.PI);

        private void GenerateColorWheel(Size arg)
        {
            stopwatch2.Start();

            int width = (int)arg.Width;
            int height = (int)arg.Height;

            var dim = new Vector2(width, height);

            var center = dim / 2;

            var radius = Math.Min(width, height) / 2f;

            var totalPixels = (int)width * (int)height;

            var bufferPixelArray = SharedPoolMem.Rent(totalPixels);

            Parallel.ForEach(Partitioner.Create(0, height, rowBatch), batch =>
            {
                for (int y = batch.Item1; y < batch.Item2; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var dxy = new Vector2(x, y) - center;

                        var distance = dxy.Length();

                        if (distance > radius)
                        {
                            bufferPixelArray[XYtoIndex(width, x, y)] = RGBPixel.Transparent;
                            continue;
                        }

                        byte a = 255;

                        var theta = (float)Math.Atan2(dxy.Y, dxy.X);

                        var hue = (theta + PI) / TWO_PI;

                        var saturation = distance / radius;

                        var aadelta = distance - (radius - 1f);

                        if (aadelta >= 0d)
                            a = (byte)((1d - aadelta) * byte.MaxValue);

                        bufferPixelArray[XYtoIndex(width, x, y)] = new HSLPixel(hue, 1f, 0.5f).ToRGBA(a);
                    }
                }
            });

            var totalbytes = totalPixels * 4;

            var pixSize = new PixelSize(width, height);
            var recSize = new Rect(new Size(width, height));


            var tmpBitmap = new WriteableBitmap(pixSize, new Avalonia.Vector(96, 96), PixelFormat.Bgra8888);

            unsafe
            {
                using (var lockFb = tmpBitmap.Lock())
                    fixed (void* src = &bufferPixelArray[0])
                        Buffer.MemoryCopy(src,
                                          lockFb.Address.ToPointer(),
                                          totalbytes,
                                          totalbytes);
            }

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                this.ColorWheelBitmap?.Dispose();
                this.ColorWheelBitmap = tmpBitmap;
            }, DispatcherPriority.Background);

            SharedPoolMem.Return(bufferPixelArray);

            var elapsed = stopwatch2.Elapsed;
            stopwatch2.Stop();
            Console.Write("Gen > ");
            Console.WriteLine(elapsed.TotalMilliseconds);
            stopwatch2.Reset();
        }

        ArrayPool<RGBPixel> SharedPoolMem = ArrayPool<RGBPixel>.Create((int)(1E8 + 1), 10);

        private Size GetCurrentSize => ConstraintSize(Bounds.Size);



        public override void Render(DrawingContext context)
        {
            stopwatch.Start();

            if (ColorWheelBitmap is null) return;

            var rect = new Rect(GetCurrentSize);

            context.DrawRectangle(Brushes.Transparent, null, rect);
            context.DrawImage(ColorWheelBitmap, rect);

            var elapsed = stopwatch.Elapsed;

            stopwatch.Stop();

            Console.Write("Rdnr > ");

            Console.WriteLine(elapsed.TotalMilliseconds);

            stopwatch.Reset();
        }
    }
}