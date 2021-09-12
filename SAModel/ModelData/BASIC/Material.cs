using Reloaded.Memory.Streams.Writers;
using SATools.SACommon;
using SATools.SAModel.Structs;
using System;
using System.IO;
using static SATools.SACommon.ByteConverter;
using static SATools.SACommon.StringExtensions;

namespace SATools.SAModel.ModelData.BASIC
{
    /// <summary>
    /// BASIC format material
    /// </summary>
    [Serializable]
    public class Material : ICloneable
    {
        #region Basic Variables (internal use)

        /// <summary>
        /// Diffuse color
        /// </summary>
        public Color DiffuseColor { get; set; }

        /// <summary>
        /// Specular color
        /// </summary>
        public Color SpecularColor { get; set; }

        /// <summary>
        /// Specular exponent
        /// </summary>
        public float Exponent { get; set; }

        /// <summary>
        /// Texture ID
        /// </summary>
        public uint TextureID { get; set; }

        /// <summary>
        /// Attributes containing various information
        /// </summary>
        public uint Attributes { get; set; }

        #endregion

        #region Accessors (user use)

        /// <summary>
        /// User defined attributes
        /// </summary>
        public byte UserAttributes
        {
            get => (byte)(Attributes & 0x7Fu);
            set => Attributes = (Attributes & ~0x7Fu) | (value & 0x7Fu);
        }

        /// <summary>
        /// Editor thing (?)
        /// </summary>
        public bool PickStatus
        {
            get => (Attributes & 0x80u) != 0;
            set => _ = value ? (Attributes |= 0x80u) : (Attributes &= ~0x80u);
        }

        /// <summary>
        /// Mipmad distance adjust
        /// </summary>
        public float MipmapDAdjust
        {
            get => ((Attributes & 0xF0u) >> 4) * 0.25f;
            set => Attributes = (Attributes & ~0xF0u) | ((uint)Math.Max(0, Math.Min(0xF, Math.Round(value / 0.25, MidpointRounding.AwayFromZero))) << 4);
        }

        /// <summary>
        /// Super sampling (Anisotropic filtering)
        /// </summary>
        public bool SuperSample
        {
            get => (Attributes & 0x1000u) != 0;
            set => _ = value ? (Attributes |= 0x1000u) : (Attributes &= ~0x1000u);
        }

        /// <summary>
        /// Texture filter mode
        /// </summary>
        public FilterMode FilterMode
        {
            get => (FilterMode)((Attributes >> 13) & 3);
            set => Attributes = (Attributes & ~0x6000u) | ((uint)value << 13);
        }

        /// <summary>
        /// Texture clamp along the V axis
        /// </summary>
        public bool ClampV
        {
            get => (Attributes & 0x8000u) != 0;
            set => _ = value ? (Attributes |= 0x8000u) : (Attributes &= ~0x8000u);
        }

        /// <summary>
        /// Texture clamp along the U axis
        /// </summary>
        public bool ClampU
        {
            get => (Attributes & 0x10000u) != 0;
            set => _ = value ? (Attributes |= 0x10000u) : (Attributes &= ~0x10000u);
        }

        /// <summary>
        /// Texture mirror along the V axis
        /// </summary>
        public bool FlipV
        {
            get => (Attributes & 0x20000u) != 0;
            set => _ = value ? (Attributes |= 0x20000u) : (Attributes &= ~0x20000u);
        }

        /// <summary>
        /// Texture mirror along the U axis
        /// </summary>
        public bool FlipU
        {
            get => (Attributes & 0x40000u) != 0;
            set => _ = value ? (Attributes |= 0x40000u) : (Attributes &= ~0x40000u);
        }

        /// <summary>
        /// Ignoring specular coor
        /// </summary>
        public bool IgnoreSpecular
        {
            get => (Attributes & 0x80000u) != 0;
            set => _ = value ? (Attributes |= 0x80000u) : (Attributes &= ~0x80000u);
        }

        /// <summary>
        /// Using alpha blending
        /// </summary>
        public bool UseAlpha
        {
            get => (Attributes & 0x100000u) != 0;
            set => _ = value ? (Attributes |= 0x100000u) : (Attributes &= ~0x100000u);
        }

        /// <summary>
        /// Using textures
        /// </summary>
        public bool UseTexture
        {
            get => (Attributes & 0x200000u) != 0;
            set => _ = value ? (Attributes |= 0x200000u) : (Attributes &= ~0x200000u);
        }

        /// <summary>
        /// Using normal msb.Appending for textures
        /// </summary>
        public bool EnvironmentMap
        {
            get => (Attributes & 0x400000) != 0;
            set => _ = value ? (Attributes |= 0x400000u) : (Attributes &= ~0x400000u);
        }

        /// <summary>
        /// Disables Culling
        /// </summary>
        public bool DoubleSided
        {
            get => (Attributes & 0x800000) != 0;
            set => _ = value ? (Attributes |= 0x800000u) : (Attributes &= ~0x800000u);
        }

        /// <summary>
        /// Uses vertex colors only/instead of lighting
        /// </summary>
        public bool FlatShading
        {
            get => (Attributes & 0x1000000) != 0;
            set => _ = value ? (Attributes |= 0x1000000u) : (Attributes &= ~0x1000000u);
        }

        /// <summary>
        /// ignores diffuse lighting
        /// </summary>
        public bool IgnoreLighting
        {
            get => (Attributes & 0x2000000) != 0;
            set => _ = value ? (Attributes |= 0x2000000u) : (Attributes &= ~0x2000000u);
        }

        /// <summary>
        /// destination blend mode
        /// </summary>
        public BlendMode DestinationAlpha
        {
            get => (BlendMode)((Attributes >> 26) & 7);
            set => Attributes = (uint)((Attributes & ~0x1C000000) | ((uint)value << 26));
        }

        /// <summary>
        /// source blend mode
        /// </summary>
        public BlendMode SourceAlpha
        {
            get => (BlendMode)((Attributes >> 29) & 7);
            set => Attributes = (Attributes & ~0xE0000000) | ((uint)value << 29);
        }

        #endregion

        /// <summary>
        /// Create a new material.
        /// </summary>
        public Material()
        {
            DiffuseColor = Color.White;
            SpecularColor = new Color(0xFF, 0xFF, 0xFF, 0);
            UseAlpha = true;
            UseTexture = true;
            DoubleSided = false;
            FlatShading = false;
            IgnoreLighting = false;
            ClampU = false;
            ClampV = false;
            FlipU = false;
            FlipV = false;
            EnvironmentMap = false;
            DestinationAlpha = BlendMode.SrcAlphaInverted;
            SourceAlpha = BlendMode.SrcAlpha;
        }

        /// <summary>
        /// Reads a material from a byte array, and raises the address to not point at the material afterwards
        /// </summary>
        /// <param name="source">Byte source</param>
        /// <param name="address">Address at which the material is located</param>
        /// <returns></returns>
        public static Material Read(byte[] source, ref uint address)
        {
            Color dif = Color.Read(source, ref address, IOType.ARGB8_32);
            Color spec = Color.Read(source, ref address, IOType.ARGB8_32);
            float exp = source.ToSingle(address);
            uint texID = source.ToUInt32(address + 4);
            uint attribs = source.ToUInt32(address + 8);
            address += 12;

            return new Material()
            {
                DiffuseColor = dif,
                SpecularColor = spec,
                Exponent = exp,
                TextureID = texID,
                Attributes = attribs
            };
        }

        /// <summary>
        /// Writes the material as an NJA struct
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="textures"></param>
        public void WriteNJA(TextWriter writer, string[] textures)
        {
            // starting the material
            writer.WriteLine("MATSTART");

            // writing diffuse color
            writer.Write("Diffuse \t");
            DiffuseColor.WriteNJA(writer, IOType.ARGB8_32);
            writer.WriteLine(",");

            // writing specular color
            writer.Write("Specular \t");
            SpecularColor.WriteNJA(writer, IOType.ARGB8_32);
            writer.WriteLine(",");

            // writing specular exponent
            writer.Write("Exponent \t( ");
            writer.Write(Exponent.ToC());
            writer.WriteLine("),");

            // writing texture id + callback flags
            int callback = (int)(TextureID & 0xC0000000);
            int texid = (int)(TextureID & ~0xC0000000);
            writer.Write("AttrTexId \t( ");
            writer.Write(((StructEnums.NJD_CALLBACK)callback).ToString().Replace(", ", " | "));
            writer.Write(", ");
            if(textures == null || texid >= textures.Length)
                writer.Write(texid);
            else
                writer.Write(textures[texid].MakeIdentifier());
            writer.WriteLine("),");

            // writing attributes
            writer.Write("AttrFlags \t( ");
            writer.Write(((StructEnums.MaterialAttributes)(Attributes & ~0x7F)).ToString().Replace(", ", " | "));
            if(UserAttributes != 0)
                writer.Write(" | 0x" + UserAttributes.ToString("X"));
            writer.WriteLine(")");

            // ending the material
            writer.WriteLine("MATEND");
        }

        /// <summary>
        /// Writes the materials contents to a stream
        /// </summary>
        /// <param name="writer">Output stream</param>
        public void Write(EndianWriter writer)
        {
            DiffuseColor.Write(writer, IOType.ARGB8_32);
            SpecularColor.Write(writer, IOType.ARGB8_32);
            writer.WriteSingle(Exponent);
            writer.WriteUInt32(TextureID);
            writer.WriteUInt32(Attributes);
        }

        object ICloneable.Clone() => Clone();

        public Material Clone() => (Material)MemberwiseClone();

        public override bool Equals(object obj)
        {
            return obj is Material material &&
                   DiffuseColor == material.DiffuseColor &&
                   SpecularColor == material.SpecularColor &&
                   Exponent == material.Exponent &&
                   TextureID == material.TextureID &&
                   Attributes == material.Attributes;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(DiffuseColor, SpecularColor, Exponent, TextureID, Attributes);
        }

        public override string ToString() => $"{TextureID}";
    }
}
