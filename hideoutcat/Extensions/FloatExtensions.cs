namespace HideoutCat.Extensions;

public static class FloatExtensions
{
    extension(float value)
    {
        public float RemapClamped(float fromMin, float fromMax, float toMin, float toMax)
        {
            if (value < fromMin)
            {
                value = fromMin;
            }
            else if (value > fromMax)
            {
                value = fromMax;
            }

            return value.Remap(fromMin, fromMax, toMin, toMax);
        }

        private float Remap(float fromMin, float fromMax, float toMin, float toMax)
        {
            if (fromMax - fromMin == 0f)
            {
                return (toMin + toMax) / 2f;
            }

            return toMin + (value - fromMin) / (fromMax - fromMin) * (toMax - toMin);
        }
    }
}