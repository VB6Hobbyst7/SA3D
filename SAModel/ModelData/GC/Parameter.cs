using Reloaded.Memory.Streams.Writers;
using SATools.SAModel.Structs;
using System;
using static SATools.SACommon.ByteConverter;

namespace SATools.SAModel.ModelData.GC
{
    /// <summary>
    /// The types of parameter that exist
    /// </summary>
    public enum ParameterType : uint
    {
        VtxAttrFmt = 0,
        IndexAttributeFlags = 1,
        Lighting = 2,
        Unused = 3,
        BlendAlpha = 4,
        AmbientColor = 5,
        Unknown_6 = 6,
        Unknown_7 = 7,
        Texture = 8,
        Unknown_9 = 9,
        TexCoordGen = 10,
    }

    /// <summary>
    /// Base class for all GC parameter types. <br/>
    /// Used to store geometry information (like materials).
    /// </summary>
    [Serializable]
    public abstract class Parameter : ICloneable
    {
        /// <summary>
        /// The type of parameter
        /// </summary>
        public ParameterType Type { get; }

        /// <summary>
        /// All parameter data is stored in these 4 bytes
        /// </summary>
        protected uint _data;

        /// <summary>
        /// Base constructor for an empty parameter. <br/>
        /// Used only in child classes.
        /// </summary>
        /// <param name="type">The type of parameter to create</param>
        protected Parameter(ParameterType type)
        {
            Type = type;
            _data = 0;
        }

        /// <summary>
        /// Create a parameter object from a file and address
        /// </summary>
        /// <param name="source">The file contents</param>
        /// <param name="address">The address at which the parameter is located</param>
        /// <returns>Any of the parameter types</returns>
        public static Parameter Read(byte[] source, uint address)
        {
            Parameter result = null;
            ParameterType paramType = (ParameterType)source.ToUInt32(address);

            switch(paramType)
            {
                case ParameterType.VtxAttrFmt:
                    result = new VtxAttrFmtParameter(VertexAttribute.Null);
                    break;
                case ParameterType.IndexAttributeFlags:
                    result = new IndexAttributeParameter();
                    break;
                case ParameterType.Lighting:
                    result = new LightingParameter();
                    break;
                case ParameterType.BlendAlpha:
                    result = new BlendAlphaParameter();
                    break;
                case ParameterType.AmbientColor:
                    result = new AmbientColorParameter();
                    break;
                case ParameterType.Texture:
                    result = new TextureParameter();
                    break;
                case ParameterType.Unknown_9:
                    result = new Unknown9Parameter();
                    break;
                case ParameterType.TexCoordGen:
                    result = new TexCoordGenParameter();
                    break;
            }

            result._data = source.ToUInt32(address + 4);

            return result;
        }

        /// <summary>
        /// Writes the parameter contents to a stream
        /// </summary>
        /// <param name="writer">The stream writer</param>
        public void Write(EndianMemoryStream writer)
        {
            writer.Write((uint)Type);
            writer.Write(_data);
        }

        object ICloneable.Clone() => Clone();

        public Parameter Clone() => (Parameter)MemberwiseClone();

        public override string ToString() => $"{Type}";
    }

    /// <summary>
    /// Parameter that is relevent for Vertex data. <br/>
    /// A geometry object needs to have one for each 
    /// </summary>
    [Serializable]
    public class VtxAttrFmtParameter : Parameter
    {
        /// <summary>
        /// The attribute type that this parameter applies for
        /// </summary>
        public VertexAttribute VertexAttribute
        {
            get
            {
                return (VertexAttribute)(_data >> 16);
            }
            set
            {
                _data &= 0xFFFF;
                _data |= ((uint)value) << 16;
            }
        }

        /// <summary>
        /// Seems to be some type of address of buffer length. <br/>
        /// Sa2 only uses a specific value for each attribute type either way
        /// </summary>
        public ushort Unknown
        {
            get
            {
                return (ushort)(_data & 0xFFFF);
            }
            set
            {
                _data &= 0xFFFF0000;
                _data |= value;
            }
        }

        /// <summary>
        /// Creates a new parameter with the default value according to each attribute type <br/> (which are the only ones that work ingame)
        /// </summary>
        /// <param name="vertexAttrib">The vertex attribute type that the parameter is for</param>
        public VtxAttrFmtParameter(VertexAttribute vertexAttrib) : base(ParameterType.VtxAttrFmt)
        {
            VertexAttribute = vertexAttrib;

            // Setting the default values
            switch(vertexAttrib)
            {
                case VertexAttribute.Position:
                    Unknown = 5120;
                    break;
                case VertexAttribute.Normal:
                    Unknown = 9216;
                    break;
                case VertexAttribute.Color0:
                    Unknown = 27136;
                    break;
                case VertexAttribute.Tex0:
                    Unknown = 33544;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Allows to manually create a Vertex attribute parameter
        /// </summary>
        /// <param name="Unknown"></param>
        /// <param name="vertexAttrib">The vertex attribute type that the parameter is for</param>
        public VtxAttrFmtParameter(ushort Unknown, VertexAttribute vertexAttrib) : base(ParameterType.VtxAttrFmt)
        {
            this.Unknown = Unknown;
            VertexAttribute = vertexAttrib;
        }

        public override string ToString() => $"{Type}: {VertexAttribute} - {Unknown}";
    }

    /// <summary>
    /// Holds information about the vertex data thats stored in the geometry
    /// </summary>
    [Serializable]
    public class IndexAttributeParameter : Parameter
    {
        /// <summary>
        /// Holds information about the vertex data thats stored in the geometry 
        /// </summary>
        public IndexAttributeFlags IndexAttributes
        {
            get
            {
                return (IndexAttributeFlags)_data;
            }
            set
            {
                _data = (uint)value;
            }
        }

        /// <summary>
        /// Creates an empty index attribute parameter
        /// </summary>
        public IndexAttributeParameter() : base(ParameterType.IndexAttributeFlags)
        {
            //this always exists
            IndexAttributes &= IndexAttributeFlags.HasPosition;
        }

        /// <summary>
        /// Creates an index attribute parameter based on existing flags
        /// </summary>
        /// <param name="flags"></param>
        public IndexAttributeParameter(IndexAttributeFlags flags) : base(ParameterType.IndexAttributeFlags)
        {
            IndexAttributes = flags;
        }

        public override string ToString() => $"{Type}: {(uint)IndexAttributes}";
    }

    /// <summary>
    /// Holds lighting information
    /// </summary>
    [Serializable]
    public class LightingParameter : Parameter
    {
        /// <summary>
        /// Lighting flags. Pretty much unknown how they work
        /// </summary>
        public ushort LightingFlags
        {
            get
            {
                return (ushort)(_data & 0xFFFF);
            }
            set
            {
                _data &= 0xFFFF0000;
                _data |= value;
            }
        }

        /// <summary>
        /// Which shadow stencil the geometry should use. <br/>
        /// Ranges from 0 - 15
        /// </summary>
        public byte ShadowStencil
        {
            get
            {
                return (byte)((_data >> 16) & 0xF);
            }
            set
            {
                _data &= 0xFFF0FFFF;
                _data |= (uint)((value & 0xF) << 16);
            }
        }

        public byte Unknown1
        {
            get
            {
                return (byte)((_data >> 20) & 0xF);
            }
            set
            {
                _data &= 0xFFF0FFFF;
                _data |= (uint)((value & 0xF) << 20);
            }
        }

        public byte Unknown2
        {
            get
            {
                return (byte)((_data >> 24) & 0xFF);
            }
            set
            {
                _data &= 0xFFF0FFFF;
                _data |= (uint)(value << 24);
            }
        }

        /// <summary>
        /// Creates a lighting parameter with the default data
        /// </summary>
        public LightingParameter() : base(ParameterType.Lighting)
        {
            //default value
            LightingFlags = 0xB11;
            ShadowStencil = 1;
        }

        public LightingParameter(ushort lightingFlags, byte shadowStencil) : base(ParameterType.Lighting)
        {
            LightingFlags = lightingFlags;
            ShadowStencil = shadowStencil;
        }

        public override string ToString() => $"{Type}: {LightingFlags} - {ShadowStencil} - {Unknown1} - {Unknown2}";
    }

    /// <summary>
    /// The blending information for the surface of the geometry
    /// </summary>
    [Serializable]
    public class BlendAlphaParameter : Parameter
    {
        /// <summary>
        /// Blendmode for the source alpha
        /// </summary>
        public BlendMode SourceAlpha
        {
            get
            {
                return (BlendMode)((_data >> 11) & 7);
            }
            set
            {
                uint inst = (uint)value;
                _data &= 0xFFFFC7FF; // ~(7 << 11)
                _data |= (inst & 7) << 11;
            }
        }

        /// <summary>
        /// Blendmode for the destination alpha
        /// </summary>
        public BlendMode DestAlpha
        {
            get
            {
                return (BlendMode)((_data >> 8) & 7);
            }
            set
            {
                uint inst = (uint)value;
                _data &= 0xFFFFF8FF; // ~(7 << 8)
                _data |= (inst & 7) << 8;
            }
        }

        public BlendAlphaParameter() : base(ParameterType.BlendAlpha)
        {

        }

        public override string ToString() => $"{Type}: {SourceAlpha} -> {DestAlpha}";
    }

    /// <summary>
    /// Ambient color of the geometry
    /// </summary>
    [Serializable]
    public class AmbientColorParameter : Parameter
    {
        /// <summary>
        /// Ambient color of the mesh
        /// </summary>
        public Color AmbientColor
        {
            get
            {
                Color col = new Color()
                {
                    RGBA = _data
                };
                return col;
            }
            set
            {
                _data = value.RGBA;
            }
        }

        public AmbientColorParameter() : base(ParameterType.AmbientColor)
        {
            _data = uint.MaxValue; // white is default
        }

        public override string ToString() => $"{Type}: {AmbientColor}";
    }

    /// <summary>
    /// Texture information for the geometry
    /// </summary>
    [Serializable]
    public class TextureParameter : Parameter
    {
        /// <summary>
        /// The id of the texture
        /// </summary>
        public ushort TextureID
        {
            get
            {
                return (ushort)(_data & 0xFFFF);
            }
            set
            {
                _data &= 0xFFFF0000;
                _data |= value;
            }
        }

        /// <summary>
        /// Texture Tiling properties
        /// </summary>
        public GCTileMode Tiling
        {
            get
            {
                return (GCTileMode)(_data >> 16);
            }
            set
            {
                _data &= 0xFFFF;
                _data |= ((uint)value) << 16;
            }
        }

        public TextureParameter() : base(ParameterType.Texture)
        {
            TextureID = 0;
            Tiling = GCTileMode.WrapU | GCTileMode.WrapV;
        }

        public TextureParameter(ushort TexID, GCTileMode tileMode) : base(ParameterType.Texture)
        {
            TextureID = TexID;
            Tiling = tileMode;
        }

        public override string ToString() => $"{Type}: {TextureID} - {(uint)Tiling}";
    }

    /// <summary>
    /// No idea what this is for, but its needed
    /// </summary>
    [Serializable]
    public class Unknown9Parameter : Parameter
    {
        /// <summary>
        /// No idea what this does. Default is 4
        /// </summary>
        public ushort Unknown1
        {
            get
            {
                return (ushort)(_data & 0xFFFF);
            }
            set
            {
                _data &= 0xFFFF0000;
                _data |= (uint)value;
            }
        }

        /// <summary>
        /// No idea what this does. Default is 0
        /// </summary>
        public ushort Unknown2
        {
            get
            {
                return (ushort)(_data >> 16);
            }
            set
            {
                _data &= 0xFFFF;
                _data |= (uint)value << 16;
            }
        }

        public Unknown9Parameter() : base(ParameterType.Unknown_9)
        {
            // default values
            Unknown1 = 4;
            Unknown2 = 0;
        }

        public override string ToString() => $"{Type}: {Unknown1} - {Unknown2}";
    }

    /// <summary>
    /// Determines where or how the geometry gets the texture coordinates
    /// </summary>
    [Serializable]
    public class TexCoordGenParameter : Parameter
    {
        public TexCoordID TexCoordID
        {
            get
            {
                return (TexCoordID)((_data >> 16) & 0xFF);
            }
            set
            {
                _data &= 0xFF00FFFF;
                _data |= (uint)value << 16;
            }
        }

        /// <summary>
        /// The function to use for generating the texture coordinates
        /// </summary>
        public TexGenType TexGenType
        {
            get
            {
                return (TexGenType)((_data >> 12) & 0xF);
            }
            set
            {
                _data &= 0xFFFF0FFF;
                _data |= (uint)value << 12;
            }
        }

        /// <summary>
        /// The source which should be used to generate the texture coordinates
        /// </summary>
        public TexGenSrc TexGenSrc
        {
            get
            {
                return (TexGenSrc)((_data >> 4) & 0xFF);
            }
            set
            {
                _data &= 0xFFFFF00F;
                _data |= (uint)value << 4;
            }
        }

        /// <summary>
        /// The id of the matrix to use for generating the texture coordinates
        /// </summary>
        public TexGenMatrix MatrixID
        {
            get
            {
                return (TexGenMatrix)(_data & 0xF);
            }
            set
            {
                _data &= 0xFFFFFFF0;
                _data |= (uint)value;
            }
        }

        public TexCoordGenParameter() : base(ParameterType.TexCoordGen)
        {

        }

        /// <summary>
        /// Create a custom Texture coordinate generation parameter
        /// </summary>
        /// <param name="texCoordID">The output location of the generated texture coordinates</param>
        /// <param name="texGenType">The function to use for generating the texture coordinates</param>
        /// <param name="texGenSrc">The source which should be used to generate the texture coordinates</param>
        /// <param name="matrixID">The id of the matrix to use for generating the texture coordinates</param>
        public TexCoordGenParameter(TexCoordID texCoordID, TexGenType texGenType, TexGenSrc texGenSrc, TexGenMatrix matrixID) : base(ParameterType.TexCoordGen)
        {
            TexCoordID = texCoordID;
            TexGenType = texGenType;
            TexGenSrc = texGenSrc;
            MatrixID = matrixID;
        }

        public override string ToString() => $"{Type}: {TexCoordID} - {TexGenType} - {TexGenSrc} - {MatrixID}";
    }
}
