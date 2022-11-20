using System.Reflection;

namespace SATools.SAModel.ModelData.CHUNK
{
    /// <summary>
    /// Vertex chunk weight status
    /// </summary>
    public enum WeightStatus
    {
        /// <summary>
        /// Start of a weighted model (replaces cached vertices)
        /// </summary>
        Start,
        /// <summary>
        /// Middle of a weighted model (adds onto cached vertices
        /// </summary>
        Middle,
        /// <summary>
        /// End of a weighted model (same as <see cref="Middle"/>)
        /// </summary>
        End
    }

    /// <summary>
    /// Chunk type
    /// </summary>
    public enum ChunkType : byte
    {
#pragma warning disable CA1707
#pragma warning disable IDE0055
        Null = 0,
        Bits_BlendAlpha 		= CHUNKEnumExtensions.Bits + 0,
        Bits_MipmapDAdjust 		= CHUNKEnumExtensions.Bits + 1,
        Bits_SpecularExponent 	= CHUNKEnumExtensions.Bits + 2,
        Bits_CachePolygonList 	= CHUNKEnumExtensions.Bits + 3,
        Bits_DrawPolygonList 	= CHUNKEnumExtensions.Bits + 4,
        Tiny_TextureID 		= CHUNKEnumExtensions.Tiny + 0,
        Tiny_TextureID2 	= CHUNKEnumExtensions.Tiny + 1,
        Material                            = CHUNKEnumExtensions.Material + 0,
        Material_Diffuse 					= CHUNKEnumExtensions.Material + 1,
        Material_Ambient 					= CHUNKEnumExtensions.Material + 2,
        Material_DiffuseAmbient 			= CHUNKEnumExtensions.Material + 3,
        Material_Specular 					= CHUNKEnumExtensions.Material + 4,
        Material_DiffuseSpecular 			= CHUNKEnumExtensions.Material + 5,
        Material_AmbientSpecular 			= CHUNKEnumExtensions.Material + 6,
        Material_DiffuseAmbientSpecular 	= CHUNKEnumExtensions.Material + 7,
        Material_Bump 						= CHUNKEnumExtensions.Material + 8,
        Material_Diffuse2 					= CHUNKEnumExtensions.Material + 9,
        Material_Ambient2 					= CHUNKEnumExtensions.Material + 10,
        Material_DiffuseAmbient2 			= CHUNKEnumExtensions.Material + 11,
        Material_Specular2 					= CHUNKEnumExtensions.Material + 12,
        Material_DiffuseSpecular2 			= CHUNKEnumExtensions.Material + 13,
        Material_AmbientSpecular2 			= CHUNKEnumExtensions.Material + 14,
        Material_DiffuseAmbientSpecular2 	= CHUNKEnumExtensions.Material + 15,
        Vertex_VertexSH 						= CHUNKEnumExtensions.Vertex + 0,
        Vertex_VertexNormalSH 					= CHUNKEnumExtensions.Vertex + 1,
        Vertex_Vertex 							= CHUNKEnumExtensions.Vertex + 2,
        Vertex_VertexDiffuse8 					= CHUNKEnumExtensions.Vertex + 3,
        Vertex_VertexUserAttributes 			= CHUNKEnumExtensions.Vertex + 4,
        Vertex_VertexNinjaAttributes 			= CHUNKEnumExtensions.Vertex + 5,
        Vertex_VertexDiffuseSpecular5 			= CHUNKEnumExtensions.Vertex + 6,
        Vertex_VertexDiffuseSpecular4 			= CHUNKEnumExtensions.Vertex + 7,
        Vertex_VertexDiffuseSpecular16 			= CHUNKEnumExtensions.Vertex + 8,
        Vertex_VertexNormal 					= CHUNKEnumExtensions.Vertex + 9,
        Vertex_VertexNormalDiffuse8 			= CHUNKEnumExtensions.Vertex + 10,
        Vertex_VertexNormalUserAttributes 		= CHUNKEnumExtensions.Vertex + 11,
        Vertex_VertexNormalNinjaAttributes 		= CHUNKEnumExtensions.Vertex + 12,
        Vertex_VertexNormalDiffuseSpecular5 	= CHUNKEnumExtensions.Vertex + 13,
        Vertex_VertexNormalDiffuseSpecular4 	= CHUNKEnumExtensions.Vertex + 14,
        Vertex_VertexNormalDiffuseSpecular16 	= CHUNKEnumExtensions.Vertex + 15,
        Vertex_VertexNormalX 					= CHUNKEnumExtensions.Vertex + 16,
        Vertex_VertexNormalXDiffuse8 			= CHUNKEnumExtensions.Vertex + 17,
        Vertex_VertexNormalXUserAttributes 		= CHUNKEnumExtensions.Vertex + 18,
        Volume_Polygon3 = CHUNKEnumExtensions.Volume + 0,
        Volume_Polygon4 = CHUNKEnumExtensions.Volume + 1,
        Volume_Strip 	= CHUNKEnumExtensions.Volume + 2,
        Strip_Strip 			= CHUNKEnumExtensions.Strip + 0,
        Strip_StripUVN 			= CHUNKEnumExtensions.Strip + 1,
        Strip_StripUVH 			= CHUNKEnumExtensions.Strip + 2,
        Strip_StripNormal 		= CHUNKEnumExtensions.Strip + 3,
        Strip_StripUVNNormal 	= CHUNKEnumExtensions.Strip + 4,
        Strip_StripUVHNormal 	= CHUNKEnumExtensions.Strip + 5,
        Strip_StripColor 		= CHUNKEnumExtensions.Strip + 6,
        Strip_StripUVNColor 	= CHUNKEnumExtensions.Strip + 7,
        Strip_StripUVHColor 	= CHUNKEnumExtensions.Strip + 8,
        Strip_Strip2 			= CHUNKEnumExtensions.Strip + 9,
        Strip_StripUVN2 		= CHUNKEnumExtensions.Strip + 10,
        Strip_StripUVH2 		= CHUNKEnumExtensions.Strip + 11,
        End = 255
#pragma warning restore CA1707
#pragma warning restore IDE0055
    }

    public static class CHUNKEnumExtensions
    {
        public const byte Bits = 1;
        public const byte Tiny = 8;
        public const byte Material = 16;
        public const byte Vertex = 32;
        public const byte Volume = 56;
        public const byte Strip = 64;

        /// <summary>
        /// Checks whether the Chunktype is representing a vertex chunk
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsVertex(this ChunkType type)
        {
            return type >= ChunkType.Vertex_VertexSH && type <= ChunkType.Vertex_VertexNormalXUserAttributes;
        }

        /// <summary>
        /// Checks whether a vertex chunktype uses vector 4 positions/normals
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool VertexIsVec4(this ChunkType type)
        {
            return type == ChunkType.Vertex_VertexSH || type == ChunkType.Vertex_VertexNormalSH;
        }

        /// <summary>
        /// Checks whether a vertex chunktype has normals
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool VertexHasNormal(this ChunkType type)
        {
            return type == ChunkType.Vertex_VertexNormalSH || type > ChunkType.Vertex_VertexDiffuseSpecular16;
        }

        public static bool StripHasColor(this ChunkType type)
        {
            return type is ChunkType.Strip_StripColor or ChunkType.Strip_StripUVNColor or ChunkType.Strip_StripUVHColor;
        }

        /// <summary>
        /// Returns the struct size of a vertex chunk
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static ushort Size(this ChunkType type)
        {
            switch(type)
            {
                case ChunkType.Vertex_Vertex:
                    return 3;
                case ChunkType.Vertex_VertexSH:
                case ChunkType.Vertex_VertexDiffuse8:
                case ChunkType.Vertex_VertexUserAttributes:
                case ChunkType.Vertex_VertexNinjaAttributes:
                case ChunkType.Vertex_VertexDiffuseSpecular5:
                case ChunkType.Vertex_VertexDiffuseSpecular4:
                    return 4;
                case ChunkType.Vertex_VertexNormal:
                    return 6;
                case ChunkType.Vertex_VertexNormalDiffuse8:
                case ChunkType.Vertex_VertexNormalUserAttributes:
                case ChunkType.Vertex_VertexNormalNinjaAttributes:
                case ChunkType.Vertex_VertexNormalDiffuseSpecular5:
                case ChunkType.Vertex_VertexNormalDiffuseSpecular4:
                    return 7;
                case ChunkType.Vertex_VertexNormalSH:
                    return 8;
                default:
                    return 0;
            }
        }
    }
}
