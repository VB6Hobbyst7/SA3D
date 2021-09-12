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
        Null = 0,
        Bits = 1,
        Bits_BlendAlpha = Bits + 0,
        Bits_MipmapDAdjust = Bits + 1,
        Bits_SpecularExponent = Bits + 2,
        Bits_CachePolygonList = Bits + 3,
        Bits_DrawPolygonList = Bits + 4,
        Tiny = 8,
        Tiny_TextureID = Tiny + 0,
        Tiny_TextureID2 = Tiny + 1,
        Material = 16,
        Material_Diffuse = Material + 1,
        Material_Ambient = Material + 2,
        Material_DiffuseAmbient = Material + 3,
        Material_Specular = Material + 4,
        Material_DiffuseSpecular = Material + 5,
        Material_AmbientSpecular = Material + 6,
        Material_DiffuseAmbientSpecular = Material + 7,
        Material_Bump = Material + 8,
        Material_Diffuse2 = Material + 9,
        Material_Ambient2 = Material + 10,
        Material_DiffuseAmbient2 = Material + 11,
        Material_Specular2 = Material + 12,
        Material_DiffuseSpecular2 = Material + 13,
        Material_AmbientSpecular2 = Material + 14,
        Material_DiffuseAmbientSpecular2 = Material + 15,
        Vertex = 32,
        Vertex_VertexSH = Vertex + 0,
        Vertex_VertexNormalSH = Vertex + 1,
        Vertex_Vertex = Vertex + 2,
        Vertex_VertexDiffuse8 = Vertex + 3,
        Vertex_VertexUserAttributes = Vertex + 4,
        Vertex_VertexNinjaAttributes = Vertex + 5,
        Vertex_VertexDiffuseSpecular5 = Vertex + 6,
        Vertex_VertexDiffuseSpecular4 = Vertex + 7,
        Vertex_VertexDiffuseSpecular16 = Vertex + 8,
        Vertex_VertexNormal = Vertex + 9,
        Vertex_VertexNormalDiffuse8 = Vertex + 10,
        Vertex_VertexNormalUserAttributes = Vertex + 11,
        Vertex_VertexNormalNinjaAttributes = Vertex + 12,
        Vertex_VertexNormalDiffuseSpecular5 = Vertex + 13,
        Vertex_VertexNormalDiffuseSpecular4 = Vertex + 14,
        Vertex_VertexNormalDiffuseSpecular16 = Vertex + 15,
        Vertex_VertexNormalX = Vertex + 16,
        Vertex_VertexNormalXDiffuse8 = Vertex + 17,
        Vertex_VertexNormalXUserAttributes = Vertex + 18,
        Volume = 56,
        Volume_Polygon3 = Volume + 0,
        Volume_Polygon4 = Volume + 1,
        Volume_Strip = Volume + 2,
        Strip = 64,
        Strip_Strip = Strip + 0,
        Strip_StripUVN = Strip + 1,
        Strip_StripUVH = Strip + 2,
        Strip_StripNormal = Strip + 3,
        Strip_StripUVNNormal = Strip + 4,
        Strip_StripUVHNormal = Strip + 5,
        Strip_StripColor = Strip + 6,
        Strip_StripUVNColor = Strip + 7,
        Strip_StripUVHColor = Strip + 8,
        Strip_Strip2 = Strip + 9,
        Strip_StripUVN2 = Strip + 10,
        Strip_StripUVH2 = Strip + 11,
        End = 255
    }

    public static class CHUNKEnumExtensions
    {
        /// <summary>
        /// Checks whether the Chunktype is representing a vertex chunk
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsVertex(this ChunkType type)
        {
            return type >= ChunkType.Vertex && type <= ChunkType.Vertex_VertexNormalXUserAttributes;
        }

        /// <summary>
        /// Checks whether a vertey chunktype uses vector 4 positions/normals
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsVec4(this ChunkType type)
        {
            return type == ChunkType.Vertex_VertexSH || type == ChunkType.Vertex_VertexNormalSH;
        }

        /// <summary>
        /// Checks whether a vertex chunktype has normals
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool HasNormal(this ChunkType type)
        {
            return type == ChunkType.Vertex_VertexNormalSH || type > ChunkType.Vertex_VertexDiffuseSpecular16;
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
