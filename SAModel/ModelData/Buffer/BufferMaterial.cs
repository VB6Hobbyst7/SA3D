using Reloaded.Memory.Streams.Writers;
using SATools.SAModel.Structs;
using System;
using static SATools.SACommon.ByteConverter;

namespace SATools.SAModel.ModelData.Buffer
{
    /// <summary>
    /// Rendering flags which are checked in the renderer
    /// </summary>
    [Flags]
    public enum MaterialFlags : byte
    {
        /// <summary>
        /// Renders vertex colors instead of normals
        /// </summary>
        Flat = 0x01,
        /// <summary>
        /// Ignores ambient lighting
        /// </summary>
        noAmbient = 0x02,
        /// <summary>
        /// Ignores diffuse lighting
        /// </summary>
        noDiffuse = 0x04,
        /// <summary>
        /// Ignores specular lighting
        /// </summary>
        noSpecular = 0x08,
        /// <summary>
        /// Whether textures should be rendered
        /// </summary>
        useTexture = 0x10,
        /// <summary>
        /// Whether to use normal mapping for textures
        /// </summary>
        normalMapping = 0x20,

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
        /// The material flags, directly passable to the shader
        /// </summary>
        public MaterialFlags MaterialFlags { get; set; }
        #endregion

        #region Blending

        /// <summary>
        /// Whether to utilize transparency
        /// </summary>
        public bool UseAlpha;

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
        /// Whether the U channel wraps
        /// </summary>
        public bool WrapU
        {
            get => !ClampU;
            set => ClampU = !value;
        }

        /// <summary>
        /// Whether the U channel mirrors
        /// </summary>
        public bool MirrorU { get; set; }

        /// <summary>
        /// Whether the V channel is clamped
        /// </summary>
        public bool ClampV { get; set; }

        /// <summary>
        /// Whether the V channel wraps
        /// </summary>
        public bool WrapV
        {
            get => !ClampV;
            set => ClampV = !value;
        }

        /// <summary>
        /// Whether the V channel mirrors
        /// </summary>
        public bool MirrorV { get; set; }

        #endregion

        /// <summary>
        /// Sets flag/s in the material flags
        /// </summary>
        /// <param name="flag">The flag/s</param>
        /// <param name="state">New state for the flag/s</param>
        public void SetFlag(MaterialFlags flag, bool state)
        {
            if(state)
                MaterialFlags |= flag;
            else
                MaterialFlags &= ~flag;
        }

        /// <summary>
        /// Checks if the flag//s 
        /// </summary>
        /// <param name="flag">The flag/s to check</param>
        /// <returns></returns>
        public bool HasFlag(MaterialFlags flag) => MaterialFlags.HasFlag(flag);

        /// <summary>
        /// Writes the material to a stream
        /// </summary>
        /// <param name="writer">Output stream</param>
        public void Write(EndianMemoryStream writer)
        {
            Diffuse.Write(writer, IOType.ARGB8_32);
            Specular.Write(writer, IOType.ARGB8_32);
            writer.WriteSingle(SpecularExponent);
            Ambient.Write(writer, IOType.ARGB8_32);
            writer.WriteUInt32(TextureIndex);
            writer.WriteSingle(MipmapDistanceAdjust);

            MaterialStates states = 0;
            if(UseAlpha)
                states |= MaterialStates.UseAlpha;
            if(Culling)
                states |= MaterialStates.Culling;
            if(ClampU)
                states |= MaterialStates.ClampU;
            if(ClampV)
                states |= MaterialStates.ClampV;
            if(MirrorU)
                states |= MaterialStates.MirrorU;
            if(MirrorV)
                states |= MaterialStates.MirrorV;

            uint flags = (uint)MaterialFlags;
            flags |= (uint)states << 8;
            flags |= (uint)SourceBlendMode << 16;
            flags |= (uint)DestinationBlendmode << 19;
            flags |= (uint)TextureFiltering << 22;

            writer.WriteUInt32(flags);
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

            uint flags = source.ToUInt32(address + 8);
            MaterialFlags mFlags = (MaterialFlags)(flags & (uint)MaterialFlags.Mask);
            MaterialStates states = (MaterialStates)(flags >> 8 & (uint)MaterialStates.Mask);
            BlendMode sourceAlpha = (BlendMode)((flags >> 16) & 0x07);
            BlendMode destAlpha = (BlendMode)((flags >> 19) & 0x07);
            FilterMode texFilter = (FilterMode)(flags >> 22);
            address += 12;

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
                MaterialFlags = mFlags,
                UseAlpha = states.HasFlag(MaterialStates.UseAlpha),
                Culling = states.HasFlag(MaterialStates.Culling),
                ClampU = states.HasFlag(MaterialStates.ClampU),
                ClampV = states.HasFlag(MaterialStates.ClampV),
                MirrorU = states.HasFlag(MaterialStates.MirrorU),
                MirrorV = states.HasFlag(MaterialStates.MirrorV),
            };
        }

        object ICloneable.Clone() => Clone();

        public BufferMaterial Clone() => (BufferMaterial)MemberwiseClone();
    }
}
