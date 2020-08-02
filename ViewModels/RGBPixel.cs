using System;
using System.Runtime.InteropServices;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace ColourWheel.Controls
{
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct RGBPixel
    {
        internal static readonly RGBPixel Transparent = new RGBPixel(0, 0, 0, 0);

        [FieldOffset(3)]
        public readonly byte A;

        [FieldOffset(2)]
        public readonly byte R;

        [FieldOffset(1)]
        public readonly byte G;

        [FieldOffset(0)]
        public readonly byte B;

        /// <summary>
        /// A struct that represents a ARGB color and is aligned as
        /// a BGRA bytefield in memory.
        /// </summary>
        /// <param name="r">Red</param>
        /// <param name="g">Green</param>
        /// <param name="b">Blue</param>
        /// <param name="a">Alpha</param>
        public RGBPixel(byte r, byte g, byte b, byte a = byte.MaxValue)
        {
            this.A = a;
            this.R = r;
            this.G = g;
            this.B = b;
        }

        internal ImmutableSolidColorBrush ToImmutableBrush() => new ImmutableSolidColorBrush(Color.FromArgb(A, R, G, B));
    }
}