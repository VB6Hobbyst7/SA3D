using SATools.SACommon;
using SATools.SAModel.Structs;
using System;
using static SATools.SACommon.ByteConverter;

namespace SATools.SAModel.ModelData.Buffer
{
    /// <summary>
    /// Rendering attributes which are checked in the renderer
    /// </summary>
    [Flags]
    public enum MaterialAttributes : byte
    {
        /// <summary>
        /// Renders vertex colors instead of normals
        /// </summary>
        Flat = 0x01,
        /// <summary>
        /// Ignores ambient lighting
        /// </summary>
        NoAmbient = 0x02,
        /// <summary>
        /// Ignores diffuse lighting
        /// </summary>
        NoDiffuse = 0x04,
        /// <summary>
        /// Ignores specular lighting
        /// </summary>
        NoSpecular = 0x08,
        /// <summary>
        /// Whether textures should be rendered
        /// </summary>
        UseTexture = 0x10,
        /// <summary>
        /// Whether to use normal mapping for textures
        /// </summary>
        NormalMapping = 0x20,

        Mask = 0xFF
    }

    /// <summary>
    /// Only used for reading and writing the material
    /// </summary>
    [Flags]
    internal enum MaterialStates
    {
        UseAlpha = 0x01,
        Culling = 0x02,
        ClampU = 0x04,
        ClampV = 0x08,
        MirrorU = 0x10,
        MirrorV = 0x20,

        Mask = 0xFF,
    }

    /// <summary>
    /// Rendering properties of the mesh
    /// </summary>
    [Serializable]
    public class BufferMaterial : ICloneable
    {
        #region Color properties

        /// <summary>
        /// The diffuse color
        /// </summary>
        public Color Diffuse { get; set; }

        /// <summary>
        /// The specular color
        /// </summary>
        public Color Specular { get; set; }

        /// <summary>
        /// The specular exponent
        /// </summary>
        public float SpecularExponent { get; set; }

        /// <summary>
        /// The Ambient color
        /// </summary>
        public Color Ambient { get; set; }

        /// <summary>
        /// The material attributes, directly passable to the shader
        /// </summary>
        public MaterialAttributes MaterialAttributes { get; set; }
        #endregion

        #region Blending

        /// <summary>
        /// Whether to utilize transparency
        /// </summary>
        public bool UseAlpha { get; set; }

        /// <summary>
        /// Enables face culling
        /// </summary>
        public bool Culling { get; set; }

        /// <summary>
        /// Source blend mode
        /// </summary>
        public BlendMode SourceBlendMode { get; set; }

        /// <summary>
        /// Destination blend mode
        /// </summary>
        public BlendMode DestinationBlendmode { get; set; }

        #endregion

        #region Texture

        /// <summary>
        /// Texture Index
        /// </summary>
        public uint TextureIndex { get; set; }

        /// <summary>
        /// Texture filtering mode
        /// </summary>
        public FilterMode TextureFiltering { get; set; }

        /// <summary>
        /// Anisotropic filtering
        /// </summary>
        public bool AnisotropicFiltering { get; set; }

        /// <summary>
        /// Mipmap distance stuff
        /// </summary>
        public float MipmapDistanceAdjust { get; set; }

        /// <summary>
        /// Whether the U channel is clamped
        /// </summary>
        public bool ClampU { get; set; }

        /// <summary>
        /// Whether the U channel mirrors
        /// </summary>
        public bool MirrorU { get; set; }

        /// <summary>
        /// Whether the V channel is clamped
        /// </summary>
        public bool ClampV { get; set; }

        /// <summary>
        /// Whether the V channel mirrors
        /// </summary>
        public bool MirrorV { get; set; }

        #endregion

        #region GC related info

        /// <summary>
        /// Data container for all gamecube related info
        /// </summary>
        private uint _gcData;

        /// <summary>
        /// Shadow stencil for GC meshes
        /// </summary>
        public byte ShadowStencil
        {
            get => (byte)((_gcData >> 24) & 0xFF);
            set
            {
                _gcData &= 0xFFFFFF;
                _gcData |= (uint)value << 24;
            }
        }

        public GC.TexCoordID TexCoordID
        {
            get => (GC.TexCoordID)((_gcData >> 16) & 0xFF);
            set
            {
                _gcData &= 0xFF00FFFF;
                _gcData |= (uint)value << 16;
            }
        }

        /// <summary>
        /// The function to use for generating the texture coordinates
        /// </summary>
        public GC.TexGenType TexGenType
        {
            get => (GC.TexGenType)((_gcData >> 12) & 0xF);
            set
            {
                _gcData &= 0xFFFF0FFF;
                _gcData |= (uint)value << 12;
            }
        }

        /// <summary>
        /// The source which should be used to generate the texture coordinates
        /// </summary>
        public GC.TexGenSrc TexGenSrc
        {
            get => (GC.TexGenSrc)((_gcData >> 4) & 0xFF);
            set
            {
                _gcData &= 0xFFFFF00F;
                _gcData |= (uint)value << 4;
            }
        }

        /// <summary>
        /// The id of the matrix to use for generating the texture coordinates
        /// </summary>
        public GC.TexGenMatrix MatrixID
        {
            get => (GC.TexGenMatrix)(_gcData & 0xF);
            set
            {
                _gcData &= 0xFFFFFFF0;
                _gcData |= (uint)value;
            }
        }

        #endregion

        public BufferMaterial()
        {
            Diffuse = Color.White;
            Specular = Color.White;
            SpecularExponent = 8;
            Ambient = Color.Black;
            SourceBlendMode = BlendMode.SrcAlpha;
            DestinationBlendmode = BlendMode.SrcAlphaInverted;
            TextureFiltering = FilterMode.Bilinear;
            ShadowStencil = 1;
            TexCoordID = GC.TexCoordID.TexCoord0;
            TexGenType = GC.TexGenType.Matrix2x4;
            TexGenSrc = GC.TexGenSrc.Tex0;
            MatrixID = GC.TexGenMatrix.Identity;
        }

        /// <summary>
        /// Set material attribute/s
        /// </summary>
        /// <param name="attrib">The attribute/s</param>
        /// <param name="state">New state for the attribute/s</param>
        public void SetAttribute(MaterialAttributes attrib, bool state)
        {
            if (state)
                MaterialAttributes |= attrib;
            else
                MaterialAttributes &= ~attrib;
        }

        /// <summary>
        /// Checks if the materials attribute/s is/are set
        /// </summary>
        /// <param name="attrib">The attribute/s to check</param>
        /// <returns></returns>
        public bool HasAttribute(MaterialAttributes attrib) => MaterialAttributes.HasFlag(attrib);

        /// <summary>
        /// Writes the material to a stream
        /// </summary>
        /// <param name="writer">Output stream</param>
        public void Write(EndianWriter writer)
        {
            Diffuse.Write(writer, IOType.ARGB8_32);
            Specular.Write(writer, IOType.ARGB8_32);
            writer.WriteSingle(SpecularExponent);
            Ambient.Write(writer, IOType.ARGB8_32);
            writer.WriteUInt32(TextureIndex);
            writer.WriteSingle(MipmapDistanceAdjust);

            MaterialStates states = 0;
            if (UseAlpha)
                states |= MaterialStates.UseAlpha;
            if (Culling)
                states |= MaterialStates.Culling;
            if (ClampU)
                states |= MaterialStates.ClampU;
            if (ClampV)
                states |= MaterialStates.ClampV;
            if (MirrorU)
                states |= MaterialStates.MirrorU;
            if (MirrorV)
                states |= MaterialStates.MirrorV;

            uint attribs = (uint)MaterialAttributes;
            attribs |= (uint)states << 8;
            attribs |= (uint)SourceBlendMode << 16;
            attribs |= (uint)DestinationBlendmode << 19;
            attribs |= (uint)TextureFiltering << 22;

            writer.WriteUInt32(attribs);

            writer.WriteUInt32(_gcData);
        }

        /// <summary>
        /// Reads a buffer material from a byte array
        /// </summary>
        /// <param name="source">Byte source</param>
        /// <param name="address">Address at which the buffer material is located</param>
        /// <returns></returns>
        public static BufferMaterial Read(byte[] source, ref uint address)
        {
            Color diffuse = Color.Read(source, ref address, IOType.ARGB8_32);
            Color specular = Color.Read(source, ref address, IOType.ARGB8_32);
            float exponent = source.ToSingle(address);
            address += 4;
            Color ambient = Color.Read(source, ref address, IOType.ARGB8_32);
            uint texID = source.ToUInt32(address);
            float mipmapDA = source.ToSingle(address + 4);

            uint attribs = source.ToUInt32(address + 8);
            MaterialAttributes mAttribs = (MaterialAttributes)(attribs & (uint)MaterialAttributes.Mask);
            MaterialStates states = (MaterialStates)(attribs >> 8 & (uint)MaterialStates.Mask);
            BlendMode sourceAlpha = (BlendMode)((attribs >> 16) & 0x07);
            BlendMode destAlpha = (BlendMode)((attribs >> 19) & 0x07);
            FilterMode texFilter = (FilterMode)(attribs >> 22);
            address += 12;

            uint gcData = source.ToUInt32(address);
            address += 4;

            return new BufferMaterial()
            {
                Diffuse = diffuse,
                Specular = specular,
                SpecularExponent = exponent,
                Ambient = ambient,
                TextureIndex = texID,
                MipmapDistanceAdjust = mipmapDA,
                SourceBlendMode = sourceAlpha,
                DestinationBlendmode = destAlpha,
                TextureFiltering = texFilter,
                MaterialAttributes = mAttribs,
                UseAlpha = states.HasFlag(MaterialStates.UseAlpha),
                Culling = states.HasFlag(MaterialStates.Culling),
                ClampU = states.HasFlag(MaterialStates.ClampU),
                ClampV = states.HasFlag(MaterialStates.ClampV),
                MirrorU = states.HasFlag(MaterialStates.MirrorU),
                MirrorV = states.HasFlag(MaterialStates.MirrorV),
                _gcData = gcData,
            };
        }

        object ICloneable.Clone() => Clone();

        public BufferMaterial Clone() => (BufferMaterial)MemberwiseClone();
    }
}
