using SATools.SAModel.Structs;

namespace SATools.SAModel.Graphics
{
    public static class Helper
    {
        private static readonly Color[] weightColors = new Color[]
        {
            new Color(0,    0,      255),
            new Color(0,    255,    255),
            new Color(0,    255,    20 ),
            new Color(255,  255,    0 ),
            new Color(255,  0,     0),
        };

        public static Color GetWeightColor(float weight)
        {
            if(weight == 0)
                return weightColors[0];
            if(weight == 1)
                return weightColors[4];
            weight *= 4;
            float t = weight % 1;
            int s = (int)(weight - t);
            t = t * t * (3 - 2 * t);
            return Color.Lerp(weightColors[s], weightColors[s + 1], t);
        }
    }
}
