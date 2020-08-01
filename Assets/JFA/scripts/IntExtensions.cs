using System;
    public static class IntExtensions
    {
        public static int ToNearestPow2(this int x)
        {
            return (int) Math.Pow(2, Math.Round(Math.Log(x) / Math.Log(2)));
        }

        public static bool IsPowerOfTwo(this int x)
        {
            return (x & (x - 1)) == 0;
        }
    }

