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
    public enum ObjectFlags : uint
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
    /// Combination of <see cref="SA1SurfaceFlags"/> and <see cref="SA2SurfaceFlags"/>
    /// </summary>
    [Flags]
    public enum SurfaceFlags : uint
    {
        Visible = 0x01,
        Solid = 0x02,
        Water = 0x04,
        Water2 = 0x08,
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
        Unknown27 = 0x010000,
        Unknown29 = 0x020000,
        Unknown30 = 0x040000,
    }

    /// <summary>
    /// Shows which data an animation contains
    /// </summary>
    [Flags]
    public enum AnimFlags : ushort
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
    public enum SA1SurfaceFlags : uint
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
    public enum SA2SurfaceFlags : uint
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
        CannotLand = 0x1000,
        Water2 = 0x2000,
        NoShadows = 0x8000,
        NoFog = 0x400000,
        Unknown24 = 0x1000000,
        Unknown27 = 0x8000000,
        Unknown29 = 0x20000000,
        Unknown30 = 0x40000000,
        Visible = 0x80000000
    }

    public static partial class EnumExtensions
    {
        /// <summary>
        /// Converts from sa1 surface flags to the combined surface flags
        /// </summary>
        /// <param name="flags"></param>
        /// <returns></returns>
        public static SurfaceFlags ToUniversal(this SA1SurfaceFlags flags)
        {
            SurfaceFlags result = 0;

            if(flags.HasFlag(SA1SurfaceFlags.Solid))
                result |= SurfaceFlags.Solid;
            if(flags.HasFlag(SA1SurfaceFlags.Water))
                result |= SurfaceFlags.Water;
            if(flags.HasFlag(SA1SurfaceFlags.NoFriction))
                result |= SurfaceFlags.NoFriction;
            if(flags.HasFlag(SA1SurfaceFlags.NoAcceleration))
                result |= SurfaceFlags.NoAcceleration;
            if(flags.HasFlag(SA1SurfaceFlags.CannotLand))
                result |= SurfaceFlags.CannotLand;
            if(flags.HasFlag(SA1SurfaceFlags.IncreasedAcceleration))
                result |= SurfaceFlags.IncreasedAcceleration;
            if(flags.HasFlag(SA1SurfaceFlags.Diggable))
                result |= SurfaceFlags.Diggable;
            if(flags.HasFlag(SA1SurfaceFlags.NotClimbable))
                result |= SurfaceFlags.NotClimbable;
            if(flags.HasFlag(SA1SurfaceFlags.Hurt))
                result |= SurfaceFlags.Hurt;
            if(flags.HasFlag(SA1SurfaceFlags.Footprints))
                result |= SurfaceFlags.Footprints;
            if(flags.HasFlag(SA1SurfaceFlags.Visible))
                result |= SurfaceFlags.Visible;

            return result;
        }

        /// <summary>
        /// Converts from sa2 surface flags to the combined surface flags
        /// </summary>
        /// <param name="flags"></param>
        /// <returns></returns>
        public static SurfaceFlags ToUniversal(this SA2SurfaceFlags flags)
        {
            SurfaceFlags result = 0;

            if(flags.HasFlag(SA2SurfaceFlags.Solid))
                result |= SurfaceFlags.Solid;
            if(flags.HasFlag(SA2SurfaceFlags.Water))
                result |= SurfaceFlags.Water;
            if(flags.HasFlag(SA2SurfaceFlags.NoFriction))
                result |= SurfaceFlags.NoFriction;
            if(flags.HasFlag(SA2SurfaceFlags.NoAcceleration))
                result |= SurfaceFlags.NoAcceleration;
            if(flags.HasFlag(SA2SurfaceFlags.LessAcceleration))
                result |= SurfaceFlags.LessAcceleration;
            if(flags.HasFlag(SA2SurfaceFlags.Diggable))
                result |= SurfaceFlags.Diggable;
            if(flags.HasFlag(SA2SurfaceFlags.NotClimbable))
                result |= SurfaceFlags.NotClimbable;
            if(flags.HasFlag(SA2SurfaceFlags.IgnoreSlope))
                result |= SurfaceFlags.IgnoreSlope;
            if(flags.HasFlag(SA2SurfaceFlags.Hurt))
                result |= SurfaceFlags.Hurt;
            if(flags.HasFlag(SA2SurfaceFlags.CannotLand))
                result |= SurfaceFlags.CannotLand;
            if(flags.HasFlag(SA2SurfaceFlags.Water2))
                result |= SurfaceFlags.Water2;
            if(flags.HasFlag(SA2SurfaceFlags.NoShadows))
                result |= SurfaceFlags.NoShadows;
            if(flags.HasFlag(SA2SurfaceFlags.NoFog))
                result |= SurfaceFlags.NoFog;
            if(flags.HasFlag(SA2SurfaceFlags.Unknown24))
                result |= SurfaceFlags.Unknown24;
            if(flags.HasFlag(SA2SurfaceFlags.Unknown27))
                result |= SurfaceFlags.Unknown27;
            if(flags.HasFlag(SA2SurfaceFlags.Unknown29))
                result |= SurfaceFlags.Unknown29;
            if(flags.HasFlag(SA2SurfaceFlags.Unknown30))
                result |= SurfaceFlags.Unknown30;
            if(flags.HasFlag(SA2SurfaceFlags.Visible))
                result |= SurfaceFlags.Visible;

            return result;
        }

        /// <summary>
        /// Converts from the combined surface flags to sa1 surface flags
        /// </summary>
        /// <param name="flags"></param>
        /// <returns></returns>
        public static SA1SurfaceFlags ToSA1(this SurfaceFlags flags)
        {
            SA1SurfaceFlags result = 0;

            if(flags.HasFlag(SurfaceFlags.Solid))
                result |= SA1SurfaceFlags.Solid;
            if(flags.HasFlag(SurfaceFlags.Water))
                result |= SA1SurfaceFlags.Water;
            if(flags.HasFlag(SurfaceFlags.NoFriction))
                result |= SA1SurfaceFlags.NoFriction;
            if(flags.HasFlag(SurfaceFlags.NoAcceleration))
                result |= SA1SurfaceFlags.NoAcceleration;
            if(flags.HasFlag(SurfaceFlags.CannotLand))
                result |= SA1SurfaceFlags.CannotLand;
            if(flags.HasFlag(SurfaceFlags.IncreasedAcceleration))
                result |= SA1SurfaceFlags.IncreasedAcceleration;
            if(flags.HasFlag(SurfaceFlags.Diggable))
                result |= SA1SurfaceFlags.Diggable;
            if(flags.HasFlag(SurfaceFlags.NotClimbable))
                result |= SA1SurfaceFlags.NotClimbable;
            if(flags.HasFlag(SurfaceFlags.Hurt))
                result |= SA1SurfaceFlags.Hurt;
            if(flags.HasFlag(SurfaceFlags.Footprints))
                result |= SA1SurfaceFlags.Footprints;
            if(flags.HasFlag(SurfaceFlags.Visible))
                result |= SA1SurfaceFlags.Visible;

            return result;
        }

        /// <summary>
        /// Converts from the combined surface flags to sa2 surface flags
        /// </summary>
        /// <param name="flags"></param>
        /// <returns></returns>
        public static SA2SurfaceFlags ToSA2(this SurfaceFlags flags)
        {
            SA2SurfaceFlags result = 0;

            if(flags.HasFlag(SurfaceFlags.Solid))
                result |= SA2SurfaceFlags.Solid;
            if(flags.HasFlag(SurfaceFlags.Water))
                result |= SA2SurfaceFlags.Water;
            if(flags.HasFlag(SurfaceFlags.NoFriction))
                result |= SA2SurfaceFlags.NoFriction;
            if(flags.HasFlag(SurfaceFlags.NoAcceleration))
                result |= SA2SurfaceFlags.NoAcceleration;
            if(flags.HasFlag(SurfaceFlags.LessAcceleration))
                result |= SA2SurfaceFlags.LessAcceleration;
            if(flags.HasFlag(SurfaceFlags.Diggable))
                result |= SA2SurfaceFlags.Diggable;
            if(flags.HasFlag(SurfaceFlags.NotClimbable))
                result |= SA2SurfaceFlags.NotClimbable;
            if(flags.HasFlag(SurfaceFlags.IgnoreSlope))
                result |= SA2SurfaceFlags.IgnoreSlope;
            if(flags.HasFlag(SurfaceFlags.Hurt))
                result |= SA2SurfaceFlags.Hurt;
            if(flags.HasFlag(SurfaceFlags.CannotLand))
                result |= SA2SurfaceFlags.CannotLand;
            if(flags.HasFlag(SurfaceFlags.Water2))
                result |= SA2SurfaceFlags.Water2;
            if(flags.HasFlag(SurfaceFlags.NoShadows))
                result |= SA2SurfaceFlags.NoShadows;
            if(flags.HasFlag(SurfaceFlags.NoFog))
                result |= SA2SurfaceFlags.NoFog;
            if(flags.HasFlag(SurfaceFlags.Unknown24))
                result |= SA2SurfaceFlags.Unknown24;
            if(flags.HasFlag(SurfaceFlags.Unknown27))
                result |= SA2SurfaceFlags.Unknown27;
            if(flags.HasFlag(SurfaceFlags.Unknown29))
                result |= SA2SurfaceFlags.Unknown29;
            if(flags.HasFlag(SurfaceFlags.Unknown30))
                result |= SA2SurfaceFlags.Unknown30;
            if(flags.HasFlag(SurfaceFlags.Visible))
                result |= SA2SurfaceFlags.Visible;

            return result;
        }

        /// <summary>
        /// Checks whether the Landentry is used for collision detection
        /// </summary>
        /// <param name="flags"></param>
        /// <returns></returns>
        public static bool IsCollision(this SurfaceFlags flags)
        {
            return flags.HasFlag(SurfaceFlags.Solid)
                || flags.HasFlag(SurfaceFlags.Water)
                || flags.HasFlag(SurfaceFlags.Water2);
        }

        public static int ChannelCount(this AnimFlags flags)
        {
            int channels = 0;
            foreach(AnimFlags f in Enum.GetValues(typeof(AnimFlags)))
                if(flags.HasFlag(f))
                    channels++;
            return channels;
        }
    }
}
