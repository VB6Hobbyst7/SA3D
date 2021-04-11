using SATools.SAModel.Structs;

namespace SATools.SAModel.Graphics
{
    /// <summary>
    /// Graphics helper methods
    /// </summary>
    public static class Helper
    {
        /// <summary>
        /// Weight color array
        /// </summary>
        private static readonly Color[] weightColors;

        /// <summary>
        /// Returns color by weight value
        /// </summary>
        /// <param name="weight">Weight (0.0 - 1.0)</param>
        /// <returns></returns>
        public static Color GetWeightColor(float weight)
            => weightColors[(int)(weight * 255)];


        static Helper()
        {
            // calculating colors, so that fetching colors is as fast as possible
            weightColors = new Color[256];

            for(int i = 0; i < 256; i++)
            {
                Color c = Color.Black;

                double hue = (((i / 256d) * -.666d + .666d) % 1f) * 6;
                int index = (int)hue;
                byte ff = (byte)((hue - index) * 255);
                byte q = (byte)(0xFF - ff);

                switch(index)
                {
                    case 0:
                        c.R = 0xFF;
                        c.G = ff;
                        break;
                    case 1:
                        c.R = q;
                        c.G = 0xFF;
                        break;
                    case 2:
                        c.G = 0xFF;
                        c.B = ff;
                        break;
                    case 3:
                        c.G = q;
                        c.B = 0xFF;
                        break;
                    case 4:
                        c.B = 0xFF;
                        c.R = ff;
                        break;
                    case 5:
                    default:
                        c.B = q;
                        c.R = 0xFF;
                        break;
                }
                weightColors[i] = c;
            }

        }
    }
}
