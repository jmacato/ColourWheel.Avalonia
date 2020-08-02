using System;

namespace ColourWheel.Controls
{
    public static class HSLFastConverter
    {
        public static RGBPixel ToRGBA(this HSLPixel input, byte alpha = 255)
        {
            // tmpMem[0] v
            // tmpMem[1] r
            // tmpMem[2] g
            // tmpMem[3] b

            // tmpMem[4] h
            // tmpMem[5] s
            // tmpMem[6] l
            
            Span<float> tmpMem = stackalloc float[7];

            tmpMem[4] = input.H;
            tmpMem[5] = input.S;
            tmpMem[6] = input.L;

            tmpMem[1] = tmpMem[6];
            tmpMem[2] = tmpMem[6];
            tmpMem[3] = tmpMem[6];

            tmpMem[0] = (tmpMem[6] <= 0.5f) ? (tmpMem[6] * (1f + tmpMem[5])) : (tmpMem[6] + tmpMem[5] - tmpMem[6] * tmpMem[5]);

            if (tmpMem[0] > 0)
            {
                float m;
                float sv;
                int sextant;

                float fract, vsf, mid1, mid2;

                m = tmpMem[6] + tmpMem[6] - tmpMem[0];
                sv = (tmpMem[0] - m) / tmpMem[0];
                tmpMem[4] *= 6f;
                sextant = (int)tmpMem[4];
                fract = tmpMem[4] - sextant;
                vsf = tmpMem[0] * sv * fract;
                mid1 = m + vsf;
                mid2 = tmpMem[0] - vsf;

                switch (sextant)
                {
                    case 0:
                        tmpMem[1] = tmpMem[0];
                        tmpMem[2] = mid1;
                        tmpMem[3] = m;
                        break;
                    case 1:
                        tmpMem[1] = mid2;
                        tmpMem[2] = tmpMem[0];
                        tmpMem[3] = m;
                        break;
                    case 2:
                        tmpMem[1] = m;
                        tmpMem[2] = tmpMem[0];
                        tmpMem[3] = mid1;
                        break;
                    case 3:
                        tmpMem[1] = m;
                        tmpMem[2] = mid2;
                        tmpMem[3] = tmpMem[0];
                        break;
                    case 4:
                        tmpMem[1] = mid1;
                        tmpMem[2] = m;
                        tmpMem[3] = tmpMem[0];
                        break;
                    case 5:
                        tmpMem[1] = tmpMem[0];
                        tmpMem[2] = m;
                        tmpMem[3] = mid2;
                        break;
                }
            }

            return new RGBPixel((byte)(tmpMem[1] * byte.MaxValue),
                                (byte)(tmpMem[2] * byte.MaxValue),
                                (byte)(tmpMem[3] * byte.MaxValue),
                                alpha);

        }
    }
}