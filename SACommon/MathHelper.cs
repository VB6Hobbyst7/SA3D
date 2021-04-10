namespace SATools.SACommon
{
    /// <summary>
    /// Math helper functions
    /// </summary>
    public static class MathHelper
    {
        public const float Pi = 3.14159265358979323846f;
        public const float BAMSDeg = 0x10000 / 360f;
        public const float Rad2Deg = 180.0f / Pi;

        /// <summary>
        /// Converts BAMS to Degrees
        /// </summary>
        public static float BAMSToDeg(int BAMS)
            => BAMS / BAMSDeg;

        /// <summary>
        /// Converts Degrees to BAMS
        /// </summary>
        public static int DegToBAMS(float deg)
            => (int)(deg * BAMSDeg);

        /// <summary>
        /// Converts Degrees to Radians
        /// </summary>
        public static float DegToRad(float deg)
            => deg / Rad2Deg;

        /// <summary>
        /// Converts Radians to Degrees
        /// </summary>
        public static float RadToDeg(float rad)
            => rad * Rad2Deg;

        /// <summary>
        /// Linear interpolation between two values
        /// </summary>
        /// <param name="min">Value to interpolate from</param>
        /// <param name="max">Value to interpolate to</param>
        /// <param name="tValue">Interpolation value</param>
        /// <returns></returns>
        public static float Lerp(float min, float max, float tValue)
            => min + (max - min) * tValue;
    }
}