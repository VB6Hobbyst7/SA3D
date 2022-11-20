namespace SATools.SAModel.Graphics
{
    /// <summary>
    /// Currently open debug menu
    /// </summary>
    public enum DebugMenu
    {
        /// <summary>
        /// Shows no debug menu
        /// </summary>
        Disabled,

        /// <summary>
        /// Shows the help debug menu
        /// </summary>
        Help,

        /// <summary>
        /// Shows the camera debug menu
        /// </summary>
        Camera,

        /// <summary>
        /// Shows the render debug menu
        /// </summary>
        RenderInfo
    }

    /// <summary>
    /// Ways to display model wireframes
    /// </summary>
    public enum WireFrameMode
    {
        /// <summary>
        /// No wireframe shown
        /// </summary>
        None,

        /// <summary>
        /// Layers wireframe over the polygons
        /// </summary>
        Overlay,

        /// <summary>
        /// Replaces polygons with outlines
        /// </summary>
        ReplaceLine,

        /// <summary>
        /// Replaces polygons with points
        /// </summary>
        ReplacePoint
    }

    /// <summary>
    /// Display modes for models
    /// </summary>
    public enum RenderMode : byte
    {
        /// <summary>
        /// Default
        /// </summary>
        Default = 0x00,

        /// <summary>
        /// Smoothe lighting
        /// </summary>
        Smooth = 0x01,

        /// <summary>
        /// Falloff instead of lighting
        /// </summary>
        Falloff = 0x02,

        /// <summary>
        /// Renders normals
        /// </summary>
        Normals = 0x03,

        /// <summary>
        /// Renders vertex colors
        /// </summary>
        Colors = 0x04,

        /// <summary>
        /// Renders vertex weights
        /// </summary>
        Weights = 0x05,

        /// <summary>
        /// Renders uv coordinates
        /// </summary>
        Texcoords = 0x06,

        /// <summary>
        /// Renders textures only
        /// </summary>
        Textures = 0x07,

        /// <summary>
        /// Displays the culling side
        /// </summary>
        CullSide = 0x08
    }

    /// <summary>
    /// Which bounds should be drawn
    /// </summary>
    public enum BoundsMode
    {
        /// <summary>
        /// Draws no bounds
        /// </summary>
        None,

        /// <summary>
        /// Draws bounds of selected objects
        /// </summary>
        Selected,

        /// <summary>
        /// Draws all bounds
        /// </summary>
        All
    }

    public enum ObjectRelationsMode
    {
        /// <summary>
        /// No object relations shown
        /// </summary>
        None,

        /// <summary>
        /// Draws lines between object locations
        /// </summary>
        Lines
    }
}
