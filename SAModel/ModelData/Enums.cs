namespace SATools.SAModel.ModelData
{
    /// <summary>
    /// Transparency blending
    /// </summary>
    public enum BlendMode
    {
        Zero = 0,
        One = 1,
        Other = 2,
        OtherInverted = 3,
        SrcAlpha = 4,
        SrcAlphaInverted = 5,
        DstAlpha = 6,
        DstAlphaInverted = 7
    }

    /// <summary>
    /// Texture filtering modes
    /// </summary>
    public enum FilterMode
    {
        PointSampled = 0,
        Bilinear = 1,
        Trilinear = 2,
        Blend = 3,
    }
}
