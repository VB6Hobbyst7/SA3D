using SATools.SACommon;
using SATools.SAModel.Structs;
using static SATools.SACommon.ByteConverter;

namespace SATools.SAModel.ModelData.GC
{
    /// <summary>
    /// The types of parameter that exist
    /// </summary>
    public enum ParameterType : uint
    {
        VtxAttrFmt = 0,
        IndexAttributes = 1,
        Lighting = 2,
        //Unused = 3, // Yes, number 3 would probably crash the game
        BlendAlpha = 4,
        AmbientColor = 5,
        Unknown6 = 6, // i assume this is diffuse...
        Unknown7 = 7, // and this is specular. but its likely neither is implemented/used
        Texture = 8,
        Unknown9 = 9,
        TexCoordGen = 10,
    }

    /// <summary>
    /// Base class for all GC parameter types. <br/>
    /// Used to store geometry information (like materials).
    /// </summary>
    public interface IParameter
    {
        /// <summary>
        /// The type of parameter
        /// </summary>
        public ParameterType Type { get; }

        /// <summary>
        /// All parameter data is stored in these 4 bytes
        /// </summary>
        public uint Data { get; set; }
    }

    public static class ParameterExtensions
    {
        /// <summary>
        /// Reads a parameter from a byte source
        /// </summary>
        /// <param name="source">Byte source</param>
        /// <param name="address">Address at which the parameter is located</param>
        /// <returns></returns>
        public static IParameter Read(byte[] source, uint address)
        {
            ParameterType paramType = (ParameterType)source.ToUInt32(address);

            IParameter result = paramType switch
            {
                ParameterType.VtxAttrFmt => new VtxAttrFmtParameter(VertexAttribute.Null),
                ParameterType.IndexAttributes => new IndexAttributeParameter(),
                ParameterType.Lighting => new LightingParameter(),
                ParameterType.BlendAlpha => new BlendAlphaParameter(),
                ParameterType.AmbientColor => new AmbientColorParameter(),
                ParameterType.Texture => new TextureParameter(),
                ParameterType.Unknown9 => new Unknown9Parameter(),
                ParameterType.TexCoordGen => new TexCoordGenParameter(),
                _ => new Parameter(paramType),
            };
            result.Data = source.ToUInt32(address + 4);

            return result;
        }

        /// <summary>
        /// Writes the parameter contents to a stream
        /// </summary>
        /// <param name="writer">The stream writer</param>
        public static void Write(this IParameter parameter, EndianWriter writer)
        {
            writer.Write((uint)parameter.Type);
            writer.Write(parameter.Data);
        }
    }

    /// <summary>
    /// General purpose parameter for parameter types that are not known
    /// </summary>
    public struct Parameter : IParameter
    {
        public ParameterType Type { get; }

        public uint Data { get; set; }

        public Parameter(ParameterType type)
        {
            Type = type;
            Data = default;
        }

        public override string ToString() => $"{Type}: 0x{Data:X4}";
    }

    /// <summary>
    /// Parameter that is relevent for Vertex data. <br/>
    /// A geometry object needs to have one for each 
    /// </summary>
    public struct VtxAttrFmtParameter : IParameter
    {
        /// <summary>
        /// Default position vertex format attribute parameter
        /// </summary>
        public static readonly VtxAttrFmtParameter Position = new() { VertexAttribute = VertexAttribute.Position, Unknown = 0x1400 };

        /// <summary>
        /// Default normal vertex format attribute parameter
        /// </summary>
        public static readonly VtxAttrFmtParameter Normal = new() { VertexAttribute = VertexAttribute.Normal, Unknown = 0x2400 };

        /// <summary>
        /// Default color0 vertex format attribute parameter
        /// </summary>
        public static readonly VtxAttrFmtParameter Color0 = new() { VertexAttribute = VertexAttribute.Color0, Unknown = 0x6A00 };

        /// <summary>
        /// Default tex0 vertex format attribute parameter
        /// </summary>
        public static readonly VtxAttrFmtParameter Tex0 = new() { VertexAttribute = VertexAttribute.Tex0, Unknown = 0x8308 };

        public ParameterType Type => ParameterType.VtxAttrFmt;

        public uint Data { get; set; }

        /// <summary>
        /// The attribute type that this parameter applies for
        /// </summary>
        public VertexAttribute VertexAttribute
        {
            get => (VertexAttribute)(Data >> 16);
            set => Data = (Data & 0xFFFF) | (((uint)value) << 16);
        }

        /// <summary>
        /// Seems to be some type of address of buffer length. <br/>
        /// Sa2 only uses a specific value for each attribute type either way
        /// </summary>
        public ushort Unknown
        {
            get => (ushort)(Data & 0xFFFF);
            set => Data = (Data & 0xFFFF0000) | value;
        }

        /// <summary>
        /// Creates a new parameter with the default value according to each attribute type <br/> (which are the only ones that work ingame)
        /// </summary>
        /// <param name="vertexAttrib">The vertex attribute type that the parameter is for</param>
        public VtxAttrFmtParameter(VertexAttribute vertexAttrib)
        {
            Data = 0;
            VertexAttribute = vertexAttrib;

            // Setting the default values
            Unknown = vertexAttrib switch
            {
                VertexAttribute.Position => Position.Unknown, // 0x1400
                VertexAttribute.Normal => Normal.Unknown,   // 0x2400
                VertexAttribute.Color0 => Color0.Unknown,  // 0x6A00
                VertexAttribute.Tex0 => Tex0.Unknown,    // 0x8308
                _ => 0,
            };
        }

        public override string ToString() => $"{Type}: {VertexAttribute} - {Unknown}";
    }

    /// <summary>
    /// Holds information about the vertex data thats stored in the geometry
    /// </summary>
    public struct IndexAttributeParameter : IParameter
    {
        public ParameterType Type => ParameterType.IndexAttributes;

        public uint Data { get; set; }

        /// <summary>
        /// Holds information about the vertex data thats stored in the geometry 
        /// </summary>
        public IndexAttributes IndexAttributes
        {
            get => (IndexAttributes)Data;
            set => Data = (uint)value;
        }

        public override string ToString() => $"{Type}: {(uint)IndexAttributes}";
    }

    /// <summary>
    /// Holds lighting information
    /// </summary>
    public struct LightingParameter : IParameter
    {
        /// <summary>
        /// Lighting parameter with default values
        /// </summary>
        public static readonly LightingParameter DefaultLighting = new() { LightingAttributes = 0xB11, ShadowStencil = 1 };

        public ParameterType Type => ParameterType.Lighting;

        public uint Data { get; set; }

        /// <summary>
        /// Lighting attributes. Pretty much unknown how they work
        /// </summary>
        public ushort LightingAttributes
        {
            get => (ushort)(Data & 0xFFFF);
            set => Data = (Data & 0xFFFF0000) | value;
        }

        /// <summary>
        /// Which shadow stencil the geometry should use. <br/>
        /// Ranges from 0 - 15
        /// </summary>
        public byte ShadowStencil
        {
            get => (byte)((Data >> 16) & 0xF);
            set => Data = (Data & 0xFFF0FFFF) | (uint)((value & 0xF) << 16);
        }

        public byte Unknown1
        {
            get => (byte)((Data >> 20) & 0xF);
            set => Data = (Data & 0xFF0FFFFF) | (uint)((value & 0xF) << 20);
        }

        public byte Unknown2
        {
            get => (byte)((Data >> 24) & 0xFF);
            set => Data = (Data & 0x00FFFFFF) | (uint)(value << 24);
        }

        public override string ToString() => $"{Type}: {LightingAttributes} - {ShadowStencil} - {Unknown1} - {Unknown2}";
    }

    /// <summary>
    /// The blending information for the surface of the geometry
    /// </summary>
    public struct BlendAlphaParameter : IParameter
    {
        public static readonly BlendAlphaParameter DefaultBlending = new() { SourceAlpha = BlendMode.SrcAlpha, DestAlpha = BlendMode.SrcAlphaInverted };

        public ParameterType Type => ParameterType.BlendAlpha;

        public uint Data { get; set; }

        /// <summary>
        /// Blendmode for the source alpha
        /// </summary>
        public BlendMode SourceAlpha
        {
            get => (BlendMode)((Data >> 11) & 7);
            set => Data = (Data & 0xFFFFC7FF) | (((uint)value & 7) << 11);
        }

        /// <summary>
        /// Blendmode for the destination alpha
        /// </summary>
        public BlendMode DestAlpha
        {
            get => (BlendMode)((Data >> 8) & 7);
            set => Data = (Data & 0xFFFFF8FF) | (((uint)value & 7) << 8);
        }

        public override string ToString() => $"{Type}: {SourceAlpha} -> {DestAlpha}";
    }

    /// <summary>
    /// Ambient color of the geometry
    /// </summary>
    public struct AmbientColorParameter : IParameter
    {
        public static readonly AmbientColorParameter White = new() { Data = uint.MaxValue };

        public ParameterType Type => ParameterType.AmbientColor;

        public uint Data { get; set; }

        /// <summary>
        /// Ambient color of the mesh
        /// </summary>
        public Color AmbientColor
        {
            get => new() { RGBA = Data };
            set => Data = value.RGBA;
        }

        public override string ToString() => $"{Type}: {AmbientColor}";
    }

    /// <summary>
    /// Texture information for the geometry
    /// </summary>
    public struct TextureParameter : IParameter
    {
        public ParameterType Type => ParameterType.Texture;

        public uint Data { get; set; }

        /// <summary>
        /// The id of the texture
        /// </summary>
        public ushort TextureID
        {
            get => (ushort)(Data & 0xFFFF);
            set => Data = (Data & 0xFFFF0000) | value;
        }

        /// <summary>
        /// Texture Tiling properties
        /// </summary>
        public GCTileMode Tiling
        {
            get => (GCTileMode)(Data >> 16);
            set => Data = (Data & 0xFFFF) | (((uint)value) << 16);
        }

        public override string ToString() => $"{Type}: {TextureID} - {(uint)Tiling}";
    }

    /// <summary>
    /// No idea what this is for, but its needed
    /// </summary>
    public struct Unknown9Parameter : IParameter
    {
        public static readonly Unknown9Parameter DefaultValues = new() { Unknown1 = 4 };

        public ParameterType Type => ParameterType.Unknown9;

        public uint Data { get; set; }

        /// <summary>
        /// No idea what this does. Default is 4
        /// </summary>
        public ushort Unknown1
        {
            get => (ushort)(Data & 0xFFFF);
            set => Data = (Data & 0xFFFF0000) | value;
        }

        /// <summary>
        /// No idea what this does. Default is 0
        /// </summary>
        public ushort Unknown2
        {
            get => (ushort)(Data >> 16);
            set => Data = (Data & 0xFFFF) | ((uint)value << 16);
        }

        public override string ToString() => $"{Type}: {Unknown1} - {Unknown2}";
    }

    /// <summary>
    /// Determines where or how the geometry gets the texture coordinates
    /// </summary>
    public struct TexCoordGenParameter : IParameter
    {
        public static readonly TexCoordGenParameter DefaultValues = new()
        { TexCoordID = TexCoordID.TexCoord0, TexGenType = TexGenType.Matrix2x4, TexGenSrc = TexGenSrc.TexCoord0, MatrixID = TexGenMatrix.Identity };

        public ParameterType Type => ParameterType.TexCoordGen;

        public uint Data { get; set; }

        public TexCoordID TexCoordID
        {
            get => (TexCoordID)((Data >> 16) & 0xFF);
            set => Data = (Data & 0xFF00FFFF) | ((uint)value << 16);
        }

        /// <summary>
        /// The function to use for generating the texture coordinates
        /// </summary>
        public TexGenType TexGenType
        {
            get => (TexGenType)((Data >> 12) & 0xF);
            set => Data = (Data & 0xFFFF0FFF) | ((uint)value << 12);
        }

        /// <summary>
        /// The source which should be used to generate the texture coordinates
        /// </summary>
        public TexGenSrc TexGenSrc
        {
            get => (TexGenSrc)((Data >> 4) & 0xFF);
            set => Data = (Data & 0xFFFFF00F) | ((uint)value << 4);
        }

        /// <summary>
        /// The id of the matrix to use for generating the texture coordinates
        /// </summary>
        public TexGenMatrix MatrixID
        {
            get => (TexGenMatrix)(Data & 0xF);
            set => Data = (Data & 0xFFFFFFF0) | ((uint)value);
        }

        public override string ToString() => $"{Type}: {TexCoordID} - {TexGenType} - {TexGenSrc} - {MatrixID}";
    }
}
