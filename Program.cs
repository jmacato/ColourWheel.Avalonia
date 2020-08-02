using System;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;
using Avalonia.Rendering;

namespace ColourWheel
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .AfterPlatformServicesSetup(_ =>
                {
                    AvaloniaLocator.CurrentMutable.Bind<IRenderTimer>().ToConstant(new LinuxHighResolutionTimer(60));
                })
                .LogToDebug()
                .UseReactiveUI();


        internal class LinuxHighResolutionTimer : IRenderTimer
        {
            [DllImport("libc", SetLastError = true)]
            static extern int nanosleep(ref TimeSpec duration, ref TimeSpec remaining);

            [DllImport("libc", SetLastError = true)]
            static extern int clock_gettime(uint clk_id, ref TimeSpec tp);

            const uint CLOCK_MONOTONIC = 1;

            ref struct TimeSpec
            {
                public readonly long tv_sec;
                public readonly long tv_nsec;

                public TimeSpec(long tv_sec, long tv_nsec)
                {
                    this.tv_sec = tv_sec;
                    this.tv_nsec = tv_nsec;
                }

                public TimeSpan ToTimeSpan()
                    => TimeSpan.FromSeconds(tv_sec) + TimeSpan.FromTicks(tv_nsec / 100);

                public static TimeSpec ConvertToTimeSpec(TimeSpan timeSpan)
                    => new TimeSpec(timeSpan.Seconds, timeSpan.Ticks * 100);
            };

            TimeSpec GetElapsedTimeSpec(ref TimeSpec start, ref TimeSpec stop)
            {
                TimeSpec elapsed_time;
                if ((stop.tv_nsec - start.tv_nsec) < 0)
                {
                    elapsed_time = new TimeSpec(stop.tv_sec - start.tv_sec - 1, stop.tv_nsec - start.tv_nsec + 1000000000);
                }
                else
                {
                    elapsed_time = new TimeSpec(stop.tv_sec - start.tv_sec, stop.tv_nsec - start.tv_nsec);
                }
                return elapsed_time;
            }

            public LinuxHighResolutionTimer(double fps)
            {
                totalInterval = TimeSpan.FromSeconds(1d / fps);

                new Thread(() =>
                {
                    while (true)
                    {
                        TickTock();
                    }
                })
                {
                    IsBackground = true
                }.Start();
            }

            private volatile bool shouldStop = false;
            private TimeSpan elapsedTotal = TimeSpan.Zero;
            private IDisposable _diposable1;
            private TimeSpan totalInterval;

            public event Action<TimeSpan> Tick;

            private void TickTock()
            {
                var start = new TimeSpec();
                var frameStop = new TimeSpec();
                var sleepStop = new TimeSpec();
                var remaining = new TimeSpec();

                TimeSpan frameTime, totalTime;

                while (!shouldStop)
                {
                    clock_gettime(CLOCK_MONOTONIC, ref start);

                    Tick?.Invoke(elapsedTotal);

                    clock_gettime(CLOCK_MONOTONIC, ref frameStop);

                    frameTime = GetElapsedTimeSpec(ref start, ref frameStop).ToTimeSpan();

                    var calc = (totalInterval - frameTime);

                    if (calc < TimeSpan.Zero)
                        calc = totalInterval;

                    var finDur = TimeSpec.ConvertToTimeSpec(calc);

                    nanosleep(ref finDur, ref remaining);

                    clock_gettime(CLOCK_MONOTONIC, ref sleepStop);

                    totalTime = GetElapsedTimeSpec(ref start, ref sleepStop).ToTimeSpan();

                    elapsedTotal += totalTime;
                }
            }

        }
    }
}
