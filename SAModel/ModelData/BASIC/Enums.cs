using System;

namespace SATools.SAModel.ModelData.BASIC
{
    /// <summary>
    /// The different primitive types for BASIC meshes
    /// </summary>
    public enum BASICPolyType
    {
        Triangles,
        Quads,
        NPoly,
        Strips
    }

    public class StructEnums
    {
        const int BIT_0 = (1 << 0);
        const int BIT_1 = (1 << 1);
        const int BIT_2 = (1 << 2);
        const int BIT_3 = (1 << 3);
        const int BIT_4 = (1 << 4);
        const int BIT_5 = (1 << 5);
        const int BIT_6 = (1 << 6);
        const int BIT_7 = (1 << 7);
        const int BIT_8 = (1 << 8);
        const int BIT_9 = (1 << 9);
        const int BIT_10 = (1 << 10);
        const int BIT_11 = (1 << 11);
        const int BIT_12 = (1 << 12);
        const int BIT_13 = (1 << 13);
        const int BIT_14 = (1 << 14);
        const int BIT_15 = (1 << 15);
        const int BIT_16 = (1 << 16);
        const int BIT_17 = (1 << 17);
        const int BIT_18 = (1 << 18);
        const int BIT_19 = (1 << 19);
        const int BIT_20 = (1 << 20);
        const int BIT_21 = (1 << 21);
        const int BIT_22 = (1 << 22);
        const int BIT_23 = (1 << 23);
        const int BIT_24 = (1 << 24);
        const int BIT_25 = (1 << 25);
        const int BIT_26 = (1 << 26);
        const int BIT_27 = (1 << 27);
        const int BIT_28 = (1 << 28);
        const int BIT_29 = (1 << 29);
        const int BIT_30 = (1 << 30);
        const int BIT_31 = (1 << 31);

        [Flags]
        public enum NJD_EVAL
        {
            NJD_EVAL_UNIT_POS = BIT_0, /* ignore translation */
            NJD_EVAL_UNIT_ANG = BIT_1, /* ignore rotation */
            NJD_EVAL_UNIT_SCL = BIT_2, /* ignore scaling */
            NJD_EVAL_HIDE = BIT_3, /* do not draw model */
            NJD_EVAL_BREAK = BIT_4, /* terminate tracing children */
            NJD_EVAL_ZXY_ANG = BIT_5,
            NJD_EVAL_SKIP = BIT_6,
            NJD_EVAL_SHAPE_SKIP = BIT_7,
            NJD_EVAL_CLIP = BIT_8,
            NJD_EVAL_MODIFIER = BIT_9
        }

        [Flags]
        public enum MaterialFlags
        {
            NJD_SA_ONE = (BIT_29),                   /* 1 one                 */
            NJD_SA_OTHER = (BIT_30),                   /* 2 Other Color         */
            NJD_SA_INV_OTHER = (BIT_30 | BIT_29),          /* 3 Inverse Other Color */
            NJD_SA_SRC = (BIT_31),                   /* 4 SRC Alpha           */
            NJD_SA_INV_SRC = (BIT_31 | BIT_29),          /* 5 Inverse SRC Alpha   */
            NJD_SA_DST = (BIT_31 | BIT_30),          /* 6 DST Alpha           */
            NJD_SA_INV_DST = (BIT_31 | BIT_30 | BIT_29), /* 7 Inverse DST Alpha   */
            NJD_DA_ONE = (BIT_26),                   /* 1 one                 */
            NJD_DA_OTHER = (BIT_27),                   /* 2 Other Color         */
            NJD_DA_INV_OTHER = (BIT_27 | BIT_26),          /* 3 Inverse Other Color */
            NJD_DA_SRC = (BIT_28),                   /* 4 SRC Alpha           */
            NJD_DA_INV_SRC = (BIT_28 | BIT_26),          /* 5 Inverse SRC Alpha   */
            NJD_DA_DST = (BIT_28 | BIT_27),          /* 6 DST Alpha           */
            NJD_DA_INV_DST = (BIT_28 | BIT_27 | BIT_26), /* 7 Inverse DST Alpha   */
            NJD_FILTER_BILINEAR = (BIT_13),
            NJD_FILTER_TRILINEAR = (BIT_14),
            NJD_FILTER_BLEND = (BIT_14 | BIT_13),
            NJD_D_025 = (BIT_8),                           /* 0.25        */
            NJD_D_050 = (BIT_9),                           /* 0.50        */
            NJD_D_075 = (BIT_9 | BIT_8),                   /* 0.75        */
            NJD_D_100 = (BIT_10),                          /* 1.00        */
            NJD_D_125 = (BIT_10 | BIT_8),                  /* 1.25        */
            NJD_D_150 = (BIT_10 | BIT_9),                  /* 1.50        */
            NJD_D_175 = (BIT_10 | BIT_9 | BIT_8),          /* 1.75        */
            NJD_D_200 = (BIT_11),                          /* 2.00        */
            NJD_D_225 = (BIT_11 | BIT_8),                  /* 2.25        */
            NJD_D_250 = (BIT_11 | BIT_9),                  /* 2.50        */
            NJD_D_275 = (BIT_11 | BIT_9 | BIT_8),          /* 2.75        */
            NJD_D_300 = (BIT_11 | BIT_10),                 /* 3.00        */
            NJD_D_325 = (BIT_11 | BIT_10 | BIT_8),         /* 3.25        */
            NJD_D_350 = (BIT_11 | BIT_10 | BIT_9),         /* 3.50        */
            NJD_D_375 = (BIT_11 | BIT_10 | BIT_9 | BIT_8), /* 3.75        */
            NJD_FLAG_IGNORE_LIGHT = (BIT_25),
            NJD_FLAG_USE_FLAT = (BIT_24),
            NJD_FLAG_DOUBLE_SIDE = (BIT_23),
            NJD_FLAG_USE_ENV = (BIT_22),
            NJD_FLAG_USE_TEXTURE = (BIT_21),
            NJD_FLAG_USE_ALPHA = (BIT_20),
            NJD_FLAG_IGNORE_SPECULAR = (BIT_19),
            NJD_FLAG_FLIP_U = (BIT_18),
            NJD_FLAG_FLIP_V = (BIT_17),
            NJD_FLAG_CLAMP_U = (BIT_16),
            NJD_FLAG_CLAMP_V = (BIT_15),
            NJD_FLAG_USE_ANISOTROPIC = (BIT_12),
            NJD_FLAG_PICK = (BIT_7),
        }

        public enum NJD_MESHSET
        {
            NJD_MESHSET_3 = 0x0000, /* polygon3 meshset         */
            NJD_MESHSET_4 = 0x4000, /* polygon4 meshset         */
            NJD_MESHSET_N = 0x8000, /* polygonN meshset         */
            NJD_MESHSET_TRIMESH = 0xc000, /* trimesh meshset          */
        }

        [Flags]
        public enum NJD_CALLBACK
        {
            NJD_POLYGON_CALLBACK = (BIT_31), /* polygon callback   */
            NJD_MATERIAL_CALLBACK = (BIT_30)  /* material callback  */
        }

        [Flags]
        public enum NJD_MTYPE
        {
            NJD_MTYPE_POS_0 = BIT_0,
            NJD_MTYPE_ANG_1 = BIT_1,
            NJD_MTYPE_SCL_2 = BIT_2,
            NJD_MTYPE_VEC_3 = BIT_3,
            NJD_MTYPE_VERT_4 = BIT_4,
            NJD_MTYPE_NORM_5 = BIT_5,
            NJD_MTYPE_TARGET_3 = BIT_6,
            NJD_MTYPE_ROLL_6 = BIT_7,
            NJD_MTYPE_ANGLE_7 = BIT_8,
            NJD_MTYPE_RGB_8 = BIT_9,
            NJD_MTYPE_INTENSITY_9 = BIT_10,
            NJD_MTYPE_SPOT_10 = BIT_11,
            NJD_MTYPE_POINT_10 = BIT_12,
            NJD_MTYPE_QUAT_1 = BIT_13
        }

        public enum NJD_MTYPE_FN
        {
            NJD_MTYPE_LINER = 0x0000, /* use liner                */
            NJD_MTYPE_SPLINE = 0x0040, /* use spline               */
            NJD_MTYPE_USER = 0x0080, /* use user function        */
            NJD_MTYPE_MASK = 0x00c0  /* Sampling mask*/
        }
    }

}
