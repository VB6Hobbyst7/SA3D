using System;

namespace SATools.SAModel.ObjData
{
    /// <summary>
    /// LandTable formats
    /// </summary>
    public enum LandtableFormat : int
    {
        SA1 = 0,
        SADX = 1,
        SA2 = 2,
        SA2B = 3,
        Buffer = 4
    }

    public enum LandtableAttributes : uint
    {
        EnableMotions = 0x1,
        LoadTexlist = 0x2,
        CustomDrawDistance = 0x4,
        LoadTextureFile = 0x8,
    }

    /// <summary>
    /// Meta data type
    /// </summary>
    public enum MetaType : uint
    {
        Label = 0x4C42414C,
        Animation = 0x4D494E41,
        Morph = 0x46524F4D,
        Author = 0x48545541,
        Tool = 0x4C4F4F54,
        Description = 0x43534544,
        Texture = 0x584554,
        End = 0x444E45
    }

    /// <summary>
    /// NJS object flags
    /// </summary>
    [Flags]
    public enum ObjectAttributes : uint
    {
        NoPosition = 0x01,
        NoRotation = 0x02,
        NoScale = 0x04,
        NoDisplay = 0x08,
        NoChildren = 0x10,
        RotateZYX = 0x20,
        NoAnimate = 0x40,
        NoMorph = 0x80
    }

    /// <summary>
    /// Struct enum (only used for NJA)
    /// </summary>
    [Flags]
    internal enum NJD_EVAL
    {
        NJD_EVAL_UNIT_POS = 0x01, /* ignore translation */
        NJD_EVAL_UNIT_ANG = 0x02, /* ignore rotation */
        NJD_EVAL_UNIT_SCL = 0x04, /* ignore scaling */
        NJD_EVAL_HIDE = 0x08, /* do not draw model */
        NJD_EVAL_BREAK = 0x10, /* terminate tracing children */
        NJD_EVAL_ZXY_ANG = 0x20,
        NJD_EVAL_SKIP = 0x40,
        NJD_EVAL_SHAPE_SKIP = 0x80,
        NJD_EVAL_CLIP = 0x0100,
        NJD_EVAL_MODIFIER = 0x0200
    }

    /// <summary>
    /// Geometry surface information <br/>
    /// Combination of <see cref="SA1SurfaceAttributes"/> and <see cref="SA2SurfaceAttributes"/>
    /// </summary>
    [Flags]
    public enum SurfaceAttributes : uint
    {
        Visible = 0x01,
        Solid = 0x02,
        Water = 0x04,
        WaterNoAlpha = 0x08,
        NoFriction = 0x10,
        NoAcceleration = 0x20,
        LessAcceleration = 0x40,
        IncreasedAcceleration = 0x80,
        CannotLand = 0x0100,
        NotClimbable = 0x0200,
        IgnoreSlope = 0x0400,
        Diggable = 0x0600,
        Hurt = 0x0800,
        Footprints = 0x1000,
        NoShadows = 0x2000,
        NoFog = 0x4000,
        Unknown24 = 0x8000,

        /// <summary>
        /// SA2; Moving collision
        /// </summary>
        DynamicCollision = 0x010000,

        /// <summary>
        /// SA2; Bounds center is offset from 0,0,0 and gets influenced by rotation and scale
        /// </summary>
        OffsetBoundsCenter = 0x020000,

        /// <summary>
        /// SA2; Radius is < 20, with Tiny its < 3
        /// </summary>
        BoundsRadiusSmall = 0x040000,

        /// <summary>
        /// SA2; Radius is < 10, with Small its < 3
        /// </summary>
        BoundsRadiusTiny = 0x080000,
    }

    /// <summary>
    /// Shows which data an animation contains
    /// </summary>
    [Flags]
    public enum AnimationAttributes : ushort
    {
        Position = 0x1,
        Rotation = 0x2,
        Scale = 0x4,
        Vector = 0x8,
        Vertex = 0x10,
        Normal = 0x20,
        Target = 0x40,
        Roll = 0x80,
        Angle = 0x100,
        LightColor = 0x200,
        Intensity = 0x400,
        Spot = 0x800,
        Point = 0x1000,
        Quaternion = 0x2000
    }

    public enum InterpolationMode
    {
        Linear,
        Spline,
        User
    }

    /// <summary>
    /// SA1 geometry surface information
    /// </summary>
    [Flags]
    public enum SA1SurfaceAttributes : uint
    {
        Solid = 0x1,
        Water = 0x2,
        NoFriction = 0x4,
        NoAcceleration = 0x8,
        CannotLand = 0x40,
        IncreasedAcceleration = 0x80,
        Diggable = 0x100,
        NotClimbable = 0x1000,
        Hurt = 0x10000,
        Footprints = 0x100000,
        Visible = 0x80000000
    }

    [Flags]
    public enum SA2SurfaceAttributes : uint
    {
        Solid = 0x1,
        Water = 0x2,
        NoFriction = 0x4,
        NoAcceleration = 0x8,
        LessAcceleration = 0x10,
        Diggable = 0x20,
        NotClimbable = 0x80,
        IgnoreSlope = 0x100,
        Hurt = 0x400,
        Footprints = 0x800,
        CannotLand = 0x1000,
        WaterNoAlpha = 0x2000,
        NoShadows = 0x8000,
        IncreasedAcceleration = 0x100000,
        NoFog = 0x400000,
        Unknown24 = 0x1000000,

        /// <summary>
        /// Moving collision
        /// </summary>
        DynamicCollision = 0x8000000,

        /// <summary>
        /// Bounds center is offset from 0,0,0 and gets influenced by rotation and scale
        /// </summary>
        OffsetBoundsCenter = 0x10000000,

        /// <summary>
        /// Radius is &lt; 20, with Tiny its &lt; 3
        /// </summary>
        BoundsRadiusSmall = 0x20000000,

        /// <summary>
        /// Radius is &lt; 10, with Small its &lt; 3
        /// </summary>
        BoundsRadiusTiny = 0x40000000,

        Visible = 0x80000000
    }

    public static partial class EnumExtensions
    {
        /// <summary>
        /// Converts from sa1 surface flags to the combined surface flags
        /// </summary>
        /// <param name="flags"></param>
        /// <returns></returns>
        public static SurfaceAttributes ToUniversal(this SA1SurfaceAttributes flags)
        {
            SurfaceAttributes result = 0;

            if(flags.HasFlag(SA1SurfaceAttributes.Solid))
                result |= SurfaceAttributes.Solid;
            if(flags.HasFlag(SA1SurfaceAttributes.Water))
                result |= SurfaceAttributes.Water;
            if(flags.HasFlag(SA1SurfaceAttributes.NoFriction))
                result |= SurfaceAttributes.NoFriction;
            if(flags.HasFlag(SA1SurfaceAttributes.NoAcceleration))
                result |= SurfaceAttributes.NoAcceleration;
            if(flags.HasFlag(SA1SurfaceAttributes.CannotLand))
                result |= SurfaceAttributes.CannotLand;
            if(flags.HasFlag(SA1SurfaceAttributes.IncreasedAcceleration))
                result |= SurfaceAttributes.IncreasedAcceleration;
            if(flags.HasFlag(SA1SurfaceAttributes.Diggable))
                result |= SurfaceAttributes.Diggable;
            if(flags.HasFlag(SA1SurfaceAttributes.NotClimbable))
                result |= SurfaceAttributes.NotClimbable;
            if(flags.HasFlag(SA1SurfaceAttributes.Hurt))
                result |= SurfaceAttributes.Hurt;
            if(flags.HasFlag(SA1SurfaceAttributes.Footprints))
                result |= SurfaceAttributes.Footprints;
            if(flags.HasFlag(SA1SurfaceAttributes.Visible))
                result |= SurfaceAttributes.Visible;

            return result;
        }

        /// <summary>
        /// Converts from sa2 surface flags to the combined surface flags
        /// </summary>
        /// <param name="flags"></param>
        /// <returns></returns>
        public static SurfaceAttributes ToUniversal(this SA2SurfaceAttributes flags)
        {
            SurfaceAttributes result = 0;

            if(flags.HasFlag(SA2SurfaceAttributes.Solid))
                result |= SurfaceAttributes.Solid;
            if(flags.HasFlag(SA2SurfaceAttributes.Water))
                result |= SurfaceAttributes.Water;
            if(flags.HasFlag(SA2SurfaceAttributes.NoFriction))
                result |= SurfaceAttributes.NoFriction;
            if(flags.HasFlag(SA2SurfaceAttributes.NoAcceleration))
                result |= SurfaceAttributes.NoAcceleration;
            if(flags.HasFlag(SA2SurfaceAttributes.LessAcceleration))
                result |= SurfaceAttributes.LessAcceleration;
            if(flags.HasFlag(SA2SurfaceAttributes.Diggable))
                result |= SurfaceAttributes.Diggable;
            if(flags.HasFlag(SA2SurfaceAttributes.NotClimbable))
                result |= SurfaceAttributes.NotClimbable;
            if(flags.HasFlag(SA2SurfaceAttributes.IgnoreSlope))
                result |= SurfaceAttributes.IgnoreSlope;
            if(flags.HasFlag(SA2SurfaceAttributes.Hurt))
                result |= SurfaceAttributes.Hurt;
            if(flags.HasFlag(SA2SurfaceAttributes.Footprints))
                result |= SurfaceAttributes.Footprints;
            if(flags.HasFlag(SA2SurfaceAttributes.CannotLand))
                result |= SurfaceAttributes.CannotLand;
            if(flags.HasFlag(SA2SurfaceAttributes.WaterNoAlpha))
                result |= SurfaceAttributes.WaterNoAlpha;
            if(flags.HasFlag(SA2SurfaceAttributes.NoShadows))
                result |= SurfaceAttributes.NoShadows;
            if(flags.HasFlag(SA2SurfaceAttributes.IncreasedAcceleration))
                result |= SurfaceAttributes.IncreasedAcceleration;
            if(flags.HasFlag(SA2SurfaceAttributes.NoFog))
                result |= SurfaceAttributes.NoFog;
            if(flags.HasFlag(SA2SurfaceAttributes.Unknown24))
                result |= SurfaceAttributes.Unknown24;
            if(flags.HasFlag(SA2SurfaceAttributes.DynamicCollision))
                result |= SurfaceAttributes.DynamicCollision;
            if(flags.HasFlag(SA2SurfaceAttributes.OffsetBoundsCenter))
                result |= SurfaceAttributes.OffsetBoundsCenter;
            if(flags.HasFlag(SA2SurfaceAttributes.BoundsRadiusSmall))
                result |= SurfaceAttributes.BoundsRadiusSmall;
            if(flags.HasFlag(SA2SurfaceAttributes.BoundsRadiusTiny))
                result |= SurfaceAttributes.BoundsRadiusTiny;
            if(flags.HasFlag(SA2SurfaceAttributes.Visible))
                result |= SurfaceAttributes.Visible;

            return result;
        }

        /// <summary>
        /// Converts from the combined surface flags to sa1 surface flags
        /// </summary>
        /// <param name="flags"></param>
        /// <returns></returns>
        public static SA1SurfaceAttributes ToSA1(this SurfaceAttributes flags)
        {
            SA1SurfaceAttributes result = 0;

            if(flags.HasFlag(SurfaceAttributes.Solid))
                result |= SA1SurfaceAttributes.Solid;
            if(flags.HasFlag(SurfaceAttributes.Water))
                result |= SA1SurfaceAttributes.Water;
            if(flags.HasFlag(SurfaceAttributes.NoFriction))
                result |= SA1SurfaceAttributes.NoFriction;
            if(flags.HasFlag(SurfaceAttributes.NoAcceleration))
                result |= SA1SurfaceAttributes.NoAcceleration;
            if(flags.HasFlag(SurfaceAttributes.CannotLand))
                result |= SA1SurfaceAttributes.CannotLand;
            if(flags.HasFlag(SurfaceAttributes.IncreasedAcceleration))
                result |= SA1SurfaceAttributes.IncreasedAcceleration;
            if(flags.HasFlag(SurfaceAttributes.Diggable))
                result |= SA1SurfaceAttributes.Diggable;
            if(flags.HasFlag(SurfaceAttributes.NotClimbable))
                result |= SA1SurfaceAttributes.NotClimbable;
            if(flags.HasFlag(SurfaceAttributes.Hurt))
                result |= SA1SurfaceAttributes.Hurt;
            if(flags.HasFlag(SurfaceAttributes.Footprints))
                result |= SA1SurfaceAttributes.Footprints;
            if(flags.HasFlag(SurfaceAttributes.Visible))
                result |= SA1SurfaceAttributes.Visible;

            return result;
        }

        /// <summary>
        /// Converts from the combined surface flags to sa2 surface flags
        /// </summary>
        /// <param name="flags"></param>
        /// <returns></returns>
        public static SA2SurfaceAttributes ToSA2(this SurfaceAttributes flags)
        {
            SA2SurfaceAttributes result = 0;

            if(flags.HasFlag(SurfaceAttributes.Solid))
                result |= SA2SurfaceAttributes.Solid;
            if(flags.HasFlag(SurfaceAttributes.Water))
                result |= SA2SurfaceAttributes.Water;
            if(flags.HasFlag(SurfaceAttributes.NoFriction))
                result |= SA2SurfaceAttributes.NoFriction;
            if(flags.HasFlag(SurfaceAttributes.NoAcceleration))
                result |= SA2SurfaceAttributes.NoAcceleration;
            if(flags.HasFlag(SurfaceAttributes.LessAcceleration))
                result |= SA2SurfaceAttributes.LessAcceleration;
            if(flags.HasFlag(SurfaceAttributes.Diggable))
                result |= SA2SurfaceAttributes.Diggable;
            if(flags.HasFlag(SurfaceAttributes.NotClimbable))
                result |= SA2SurfaceAttributes.NotClimbable;
            if(flags.HasFlag(SurfaceAttributes.IgnoreSlope))
                result |= SA2SurfaceAttributes.IgnoreSlope;
            if(flags.HasFlag(SurfaceAttributes.Hurt))
                result |= SA2SurfaceAttributes.Hurt;
            if(flags.HasFlag(SurfaceAttributes.Footprints))
                result |= SA2SurfaceAttributes.Footprints;
            if(flags.HasFlag(SurfaceAttributes.CannotLand))
                result |= SA2SurfaceAttributes.CannotLand;
            if(flags.HasFlag(SurfaceAttributes.WaterNoAlpha))
                result |= SA2SurfaceAttributes.WaterNoAlpha;
            if(flags.HasFlag(SurfaceAttributes.NoShadows))
                result |= SA2SurfaceAttributes.NoShadows;
            if(flags.HasFlag(SurfaceAttributes.IncreasedAcceleration))
                result |= SA2SurfaceAttributes.IncreasedAcceleration;
            if(flags.HasFlag(SurfaceAttributes.NoFog))
                result |= SA2SurfaceAttributes.NoFog;
            if(flags.HasFlag(SurfaceAttributes.Unknown24))
                result |= SA2SurfaceAttributes.Unknown24;
            if(flags.HasFlag(SurfaceAttributes.DynamicCollision))
                result |= SA2SurfaceAttributes.DynamicCollision;
            if(flags.HasFlag(SurfaceAttributes.OffsetBoundsCenter))
                result |= SA2SurfaceAttributes.OffsetBoundsCenter;
            if(flags.HasFlag(SurfaceAttributes.BoundsRadiusSmall))
                result |= SA2SurfaceAttributes.BoundsRadiusSmall;
            if(flags.HasFlag(SurfaceAttributes.BoundsRadiusTiny))
                result |= SA2SurfaceAttributes.BoundsRadiusTiny;
            if(flags.HasFlag(SurfaceAttributes.Visible))
                result |= SA2SurfaceAttributes.Visible;

            return result;
        }

        /// <summary>
        /// Checks whether the Landentry is used for collision detection
        /// </summary>
        /// <param name="flags"></param>
        /// <returns></returns>
        public static bool IsCollision(this SurfaceAttributes flags)
        {
            return flags.HasFlag(SurfaceAttributes.Solid)
                || flags.HasFlag(SurfaceAttributes.Water)
                || flags.HasFlag(SurfaceAttributes.WaterNoAlpha);
        }

        public static int ChannelCount(this AnimationAttributes flags)
        {
            int channels = 0;
            foreach(AnimationAttributes f in Enum.GetValues(typeof(AnimationAttributes)))
                if(flags.HasFlag(f))
                    channels++;
            return channels;
        }
    }
}
