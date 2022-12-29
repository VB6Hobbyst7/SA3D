using SATools.SACommon;
using System;

namespace SATools.SAModel.ObjectData
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
        SkipDraw = 0x08,
        SkipChildren = 0x10,
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
    public enum SurfaceAttributes : ulong
    {
        Visible = Flag64.B0,
        Solid = Flag64.B1,
        Water = Flag64.B2,
        WaterNoAlpha = Flag64.B3,

        Accelerate = Flag64.B4,
        LowAcceleration = Flag64.B5,
        NoAcceleration = Flag64.B6,
        IncreasedAcceleration = Flag64.B7,
        TubeAcceleration = Flag64.B8,

        NoFriction = Flag64.B9,
        CannotLand = Flag64.B10,
        Unclimbable = Flag64.B11,
        Stairs = Flag64.B12,
        Diggable = Flag64.B13,
        Hurt = Flag64.B14,
        DynamicCollision = Flag64.B15,
        WaterCollision = Flag64.B16,

        Gravity = Flag64.B17,

        Footprints = Flag64.B18,
        NoShadows = Flag64.B19,
        NoFog = Flag64.B20,
        LowDepth = Flag64.B21,
        UseSkyDrawDistance = Flag64.B22,
        EasyDraw = Flag64.B23,
        NoZWrite = Flag64.B24,
        DrawByMesh = Flag64.B25,
        EnableManipulation = Flag64.B26,
        Waterfall = Flag64.B27,
        Chaos0Land = Flag64.B28,

        TransformBounds = Flag64.B29,
        BoundsRadiusSmall = Flag64.B30,
        BoundsRadiusTiny = Flag64.B31,

        SA1_Unknown9 = Flag64.B52,
        SA1_Unknown11 = Flag64.B53,
        SA1_Unknown15 = Flag64.B54,
        SA1_Unknown19 = Flag64.B55,

        SA2_Unknown6 = Flag64.B56,
        SA2_Unknown9 = Flag64.B57,
        SA2_Unknown14 = Flag64.B58,
        SA2_Unknown16 = Flag64.B59,
        SA2_Unknown17 = Flag64.B60,
        SA2_Unknown18 = Flag64.B61,
        SA2_Unknown25 = Flag64.B62,
        SA2_Unknown26 = Flag64.B63,
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
        Solid = Flag32.B0,
        Water = Flag32.B1,
        NoFriction = Flag32.B2,
        NoAcceleration = Flag32.B3,

        LowAcceleration = Flag32.B4,
        UseSkyDrawDistance = Flag32.B5,
        CannotLand = Flag32.B6,
        IncreasedAcceleration = Flag32.B7,

        Diggable = Flag32.B8,
        Unknown9 = Flag32.B9,
        /// <summary>
        /// Force alpha sorting; Disable Z Write when used together with Water; Force disable Z write in all levels except Lost World 2
        /// </summary>
        Waterfall = Flag32.B10,
        Unknown11 = Flag32.B11,

        Unclimbable = Flag32.B12,
        /// <summary>
        /// Turns off Visible when Chaos 0 jumps up a pole
        /// </summary>
        Chaos0Land = Flag32.B13,
        Stairs = Flag32.B14,
        Unknown15 = Flag32.B15,

        Hurt = Flag32.B16,
        TubeAcceleration = Flag32.B17,
        LowDepth = Flag32.B18,
        Unknown19 = Flag32.B19,

        Footprints = Flag32.B20,
        Accelerate = Flag32.B21,
        WaterCollision = Flag32.B22,
        Gravity = Flag32.B23,

        NoZWrite = Flag32.B24,
        DrawByMesh = Flag32.B25,
        EnableManipulation = Flag32.B26,
        DynamicCollision = Flag32.B27,

        /// <summary>
        /// Bounds center is offset from 0,0,0 and gets influenced by rotation and scale
        /// </summary>
        TransformBounds = Flag32.B28,
        /// <summary>
        /// Radius is &lt; 20, with Tiny its &lt; 3
        /// </summary>
        BoundsRadiusSmall = Flag32.B29,
        /// <summary>
        /// Radius is &lt; 10, with Small its &lt; 3
        /// </summary>
        BoundsRadiusTiny = Flag32.B30,
        Visible = Flag32.B31
    }

    [Flags]
    public enum SA2SurfaceAttributes : uint
    {
        Solid = Flag32.B0,
        Water = Flag32.B1,
        NoFriction = Flag32.B2,
        NoAcceleration = Flag32.B3,

        LowAcceleration = Flag32.B4,
        Diggable = Flag32.B5,
        Unknown6 = Flag32.B6,
        Unclimbable = Flag32.B7,

        Stairs = Flag32.B8,
        Unknown9 = Flag32.B9,
        Hurt = Flag32.B10,
        Footprints = Flag32.B11,

        CannotLand = Flag32.B12,
        WaterNoAlpha = Flag32.B13,
        Unknown14 = Flag32.B14,
        NoShadows = Flag32.B15,

        Unknown16 = Flag32.B16,
        Unknown17 = Flag32.B17,
        Unknown18 = Flag32.B18,
        Gravity = Flag32.B19,

        TubeAcceleration = Flag32.B20,
        IncreasedAcceleration = Flag32.B21,
        NoFog = Flag32.B22,
        UseSkyDrawDistance = Flag32.B23,

        /// <summary>
        /// SA2 DC flag - Indicates to use simpler rendering calculations
        /// </summary>
        EasyDraw = Flag32.B24,
        Unknown25 = Flag32.B25,
        Unknown26 = Flag32.B26,
        /// <summary>
        /// Moving collision
        /// </summary>
        DynamicCollision = Flag32.B27,


        /// <summary>
        /// Bounds center is offset from 0,0,0 and gets influenced by rotation and scale
        /// </summary>
        TransformBounds = Flag32.B28,
        /// <summary>
        /// Radius is &lt; 20, with Tiny its &lt; 3
        /// </summary>
        BoundsRadiusSmall = Flag32.B29,
        /// <summary>
        /// Radius is &lt; 10, with Small its &lt; 3
        /// </summary>
        BoundsRadiusTiny = Flag32.B30,
        Visible = Flag32.B31
    }

    public static partial class EnumExtensions
    {
        private static readonly (SA1SurfaceAttributes sa1, SurfaceAttributes universal)[] SA1SurfaceAttributeMapping = new[]
        {
            ( SA1SurfaceAttributes.Solid, SurfaceAttributes.Solid ),
            ( SA1SurfaceAttributes.Water, SurfaceAttributes.Water ),
            ( SA1SurfaceAttributes.NoFriction, SurfaceAttributes.NoFriction ),
            ( SA1SurfaceAttributes.NoAcceleration, SurfaceAttributes.NoAcceleration ),

            ( SA1SurfaceAttributes.LowAcceleration, SurfaceAttributes.LowAcceleration ),
            ( SA1SurfaceAttributes.UseSkyDrawDistance, SurfaceAttributes.UseSkyDrawDistance ),
            ( SA1SurfaceAttributes.CannotLand, SurfaceAttributes.CannotLand ),
            ( SA1SurfaceAttributes.IncreasedAcceleration, SurfaceAttributes.IncreasedAcceleration ),

            ( SA1SurfaceAttributes.Diggable, SurfaceAttributes.Diggable ),
            ( SA1SurfaceAttributes.Unknown9, SurfaceAttributes.SA1_Unknown9 ),
            ( SA1SurfaceAttributes.Waterfall, SurfaceAttributes.Waterfall ),
            ( SA1SurfaceAttributes.Unknown11, SurfaceAttributes.SA1_Unknown11 ),

            ( SA1SurfaceAttributes.Unclimbable, SurfaceAttributes.Unclimbable ),
            ( SA1SurfaceAttributes.Chaos0Land, SurfaceAttributes.Chaos0Land ),
            ( SA1SurfaceAttributes.Stairs, SurfaceAttributes.Stairs ),
            ( SA1SurfaceAttributes.Unknown15, SurfaceAttributes.SA1_Unknown15 ),

            ( SA1SurfaceAttributes.Hurt, SurfaceAttributes.Hurt ),
            ( SA1SurfaceAttributes.TubeAcceleration, SurfaceAttributes.TubeAcceleration ),
            ( SA1SurfaceAttributes.LowDepth, SurfaceAttributes.LowDepth ),
            ( SA1SurfaceAttributes.Unknown19, SurfaceAttributes.SA1_Unknown19 ),

            ( SA1SurfaceAttributes.Footprints, SurfaceAttributes.Footprints ),
            ( SA1SurfaceAttributes.Accelerate, SurfaceAttributes.Accelerate ),
            ( SA1SurfaceAttributes.WaterCollision, SurfaceAttributes.WaterCollision ),
            ( SA1SurfaceAttributes.Gravity, SurfaceAttributes.Gravity ),

            ( SA1SurfaceAttributes.NoZWrite, SurfaceAttributes.NoZWrite ),
            ( SA1SurfaceAttributes.DrawByMesh, SurfaceAttributes.DrawByMesh ),
            ( SA1SurfaceAttributes.EnableManipulation, SurfaceAttributes.EnableManipulation ),
            ( SA1SurfaceAttributes.DynamicCollision, SurfaceAttributes.DynamicCollision ),

            ( SA1SurfaceAttributes.TransformBounds, SurfaceAttributes.TransformBounds ),
            ( SA1SurfaceAttributes.BoundsRadiusSmall, SurfaceAttributes.BoundsRadiusSmall ),
            ( SA1SurfaceAttributes.BoundsRadiusTiny, SurfaceAttributes.BoundsRadiusTiny ),
            ( SA1SurfaceAttributes.Visible, SurfaceAttributes.Visible ),
        };

        private static readonly (SA2SurfaceAttributes sa2, SurfaceAttributes universal)[] SA2SurfaceAttributeMapping = new[]
        {
            ( SA2SurfaceAttributes.Solid, SurfaceAttributes.Solid ),
            ( SA2SurfaceAttributes.Water, SurfaceAttributes.Water ),
            ( SA2SurfaceAttributes.NoFriction, SurfaceAttributes.NoFriction ),
            ( SA2SurfaceAttributes.NoAcceleration, SurfaceAttributes.NoAcceleration ),
            ( SA2SurfaceAttributes.LowAcceleration, SurfaceAttributes.LowAcceleration ),
            ( SA2SurfaceAttributes.Diggable, SurfaceAttributes.Diggable ),
            ( SA2SurfaceAttributes.Unknown6, SurfaceAttributes.SA2_Unknown6 ),
            ( SA2SurfaceAttributes.Unclimbable, SurfaceAttributes.Unclimbable ),
            ( SA2SurfaceAttributes.Stairs, SurfaceAttributes.Stairs ),
            ( SA2SurfaceAttributes.Unknown9, SurfaceAttributes.SA2_Unknown9 ),
            ( SA2SurfaceAttributes.Hurt, SurfaceAttributes.Hurt ),
            ( SA2SurfaceAttributes.Footprints, SurfaceAttributes.Footprints ),
            ( SA2SurfaceAttributes.CannotLand, SurfaceAttributes.CannotLand ),
            ( SA2SurfaceAttributes.WaterNoAlpha, SurfaceAttributes.WaterNoAlpha ),
            ( SA2SurfaceAttributes.Unknown14, SurfaceAttributes.SA2_Unknown14 ),
            ( SA2SurfaceAttributes.NoShadows, SurfaceAttributes.NoShadows ),
            ( SA2SurfaceAttributes.Unknown16, SurfaceAttributes.SA2_Unknown16 ),
            ( SA2SurfaceAttributes.Unknown17, SurfaceAttributes.SA2_Unknown17 ),
            ( SA2SurfaceAttributes.Unknown18, SurfaceAttributes.SA2_Unknown18 ),
            ( SA2SurfaceAttributes.Gravity, SurfaceAttributes.Gravity ),
            ( SA2SurfaceAttributes.TubeAcceleration, SurfaceAttributes.TubeAcceleration ),
            ( SA2SurfaceAttributes.IncreasedAcceleration, SurfaceAttributes.IncreasedAcceleration ),
            ( SA2SurfaceAttributes.NoFog, SurfaceAttributes.NoFog ),
            ( SA2SurfaceAttributes.UseSkyDrawDistance, SurfaceAttributes.UseSkyDrawDistance ),
            ( SA2SurfaceAttributes.EasyDraw, SurfaceAttributes.EasyDraw ),
            ( SA2SurfaceAttributes.Unknown25, SurfaceAttributes.SA2_Unknown25 ),
            ( SA2SurfaceAttributes.Unknown26, SurfaceAttributes.SA2_Unknown26 ),
            ( SA2SurfaceAttributes.DynamicCollision, SurfaceAttributes.DynamicCollision ),
            ( SA2SurfaceAttributes.TransformBounds, SurfaceAttributes.TransformBounds ),
            ( SA2SurfaceAttributes.BoundsRadiusSmall, SurfaceAttributes.BoundsRadiusSmall ),
            ( SA2SurfaceAttributes.BoundsRadiusTiny, SurfaceAttributes.BoundsRadiusTiny ),
            ( SA2SurfaceAttributes.Visible, SurfaceAttributes.Visible ),
        };

        /// <summary>
        /// Converts from sa1 surface flags to the combined surface flags
        /// </summary>
        /// <param name="flags"></param>
        /// <returns></returns>
        public static SurfaceAttributes ToUniversal(this SA1SurfaceAttributes flags)
        {
            SurfaceAttributes result = 0;

            foreach ((SA1SurfaceAttributes sa1, SurfaceAttributes universal) in SA1SurfaceAttributeMapping)
            {
                if (flags.HasFlag(sa1))
                    result |= universal;
            }

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

            foreach ((SA2SurfaceAttributes sa2, SurfaceAttributes universal) in SA2SurfaceAttributeMapping)
            {
                if (flags.HasFlag(sa2))
                    result |= universal;
            }

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

            foreach ((SA1SurfaceAttributes sa1, SurfaceAttributes universal) in SA1SurfaceAttributeMapping)
            {
                if (flags.HasFlag(universal))
                    result |= sa1;
            }

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

            foreach ((SA2SurfaceAttributes sa2, SurfaceAttributes universal) in SA2SurfaceAttributeMapping)
            {
                if (flags.HasFlag(universal))
                    result |= sa2;
            }

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
            foreach (AnimationAttributes f in Enum.GetValues(typeof(AnimationAttributes)))
                if (flags.HasFlag(f))
                    channels++;
            return channels;
        }
    }
}
