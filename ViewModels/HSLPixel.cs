namespace ColourWheel.Controls
{
    public readonly ref struct HSLPixel
    {
        public readonly float H;
        public readonly float S;
        public readonly float L;

        public HSLPixel(float h, float s, float l)
        {
            H = h;
            S = s;
            L = l;
        }
    }
}