using Reloaded.Memory.Streams.Writers;
using SATools.SACommon;
using SATools.SAModel.Structs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Numerics;
using static SATools.SACommon.ByteConverter;
using static SATools.SACommon.HelperExtensions;
using static SATools.SACommon.StringExtensions;

namespace SATools.SAModel.ModelData.BASIC
{
    /// <summary>
    /// BASIC format mesh
    /// </summary>
    [Serializable]
    public class Mesh : ICloneable
    {
        private Vector3[] _normals;

        private Color[] _colors;

        private Vector2[] _texcoords;

        /// <summary>
        /// Material ID
        /// </summary>
        public ushort MaterialID { get; set; }

        /// <summary>
        /// Polygon type used
        /// </summary>
        public BASICPolyType PolyType { get; }

        /// <summary>
        /// Name of the primitives data
        /// </summary>
        public string PolyName { get; set; }

        /// <summary>
        /// Primitive data
        /// </summary>
        public ReadOnlyCollection<Poly> Polys { get; }

        /// <summary>
        /// The amount of corners/loops in the polygons. Determines the lengths of the other arrays
        /// </summary>
        public int PolygonCornerCount { get; }

        /// <summary>
        /// Primitive attribute (unused)
        /// </summary>
        public uint PolyAttributes { get; set; }

        /// <summary>
        /// Additional Normal data name
        /// </summary>
        public string NormalName { get; set; }

        /// <summary>
        /// Additional normal data (used for morphs)
        /// </summary>
        public Vector3[] Normals
        {
            get => _normals;
            set
            {
                if(value != null && value.Length != PolygonCornerCount)
                    throw new ArgumentException($"New array has a length of {value.Length}, while {PolygonCornerCount} is expected");
                _normals = value;
            }
        }

        /// <summary>
        /// Vertex color data name
        /// </summary>
        public string ColorName { get; set; }

        /// <summary>
        /// Vertex color data
        /// </summary>
        public Color[] Colors
        {
            get => _colors;
            set
            {
                if(value != null && value.Length != PolygonCornerCount)
                    throw new ArgumentException($"New array has a length of {value.Length}, while {PolygonCornerCount} is expected");
                _colors = value;
            }
        }

        /// <summary>
        /// Texture coordinate data name
        /// </summary>
        public string TexcoordName { get; set; }

        /// <summary>
        /// Texture coordinate data
        /// </summary>
        public Vector2[] Texcoords
        {
            get => _texcoords;
            set
            {
                if(value != null && value.Length != PolygonCornerCount)
                    throw new ArgumentException($"New array has a length of {value.Length}, while {PolygonCornerCount} is expected");
                _texcoords = value;
            }
        }

        private Mesh(BASICPolyType polyType, Poly[] polys, Vector3[] normals, Color[] colors, Vector2[] texcoords)
        {
            PolyType = polyType;
            Polys = new ReadOnlyCollection<Poly>(polys);
            Normals = normals;
            Colors = colors;
            Texcoords = texcoords;
        }

        /// <summary>
        /// Creates a new (empty) mesh based on polygon data
        /// </summary>
        /// <param name="polyType">Polygon type use</param>
        /// <param name="polys">Polygons to use</param>
        /// <param name="hasNormal">Whether the mesh uses additional normal data</param>
        /// <param name="hasColor">Whether the model uses color data</param>
        /// <param name="hasTexcoords">Whether the model uses texture coordinate data</param>
        /// <param name="materialID">Material index</param>
        public Mesh(BASICPolyType polyType, Poly[] polys, bool hasNormal, bool hasColor, bool hasTexcoords, ushort materialID)
        {
            PolyType = polyType;
            MaterialID = materialID;
            Polys = new ReadOnlyCollection<Poly>(polys);
            string identifier = GenerateIdentifier();

            int cornerCount = 0;
            foreach(Poly p in polys)
                cornerCount += p.Indices.Length;
            PolygonCornerCount = cornerCount;

            PolyName = "poly_" + identifier;

            if(hasNormal)
            {
                Normals = new Vector3[cornerCount];
                NormalName = "polynormal_" + identifier;
            }
            if(hasColor)
            {
                Colors = new Color[cornerCount];
                ColorName = "vcolor_" + identifier;
            }
            if(hasTexcoords)
            {
                Texcoords = new Vector2[cornerCount];
                TexcoordName = "uv_" + identifier;
            }
        }

        /// <summary>
        /// Creates a new (empty) mesh based on polygon data and names <br/>
        /// NOTE: If a name is null, then the corresponding data set doesnt exist (doesnt apply to polyname)
        /// </summary>
        /// <param name="polyType">Polygon type use</param>
        /// <param name="polyName">Name of the polygon data</param>
        /// <param name="polys">Polygons to use</param>
        /// <param name="normalName">Name of the normal data <br/> null if data doesnt exist</param>
        /// <param name="colorName">Name of color data <br/> null if data doesnt exist</param>
        /// <param name="texcoordName">Name of texcoord data <br/> null if data doesnt exist</param>
        /// <param name="materialID">Material index</param>
        public Mesh(BASICPolyType polyType, string polyName, Poly[] polys, string normalName, string colorName, string texcoordName, ushort materialID)
            : this(polyType, polys, normalName != null, colorName != null, texcoordName != null, materialID)
        {
            PolyName = polyName;
            NormalName = normalName;
            ColorName = colorName;
            TexcoordName = texcoordName;
        }

        /// <summary>
        /// Reads a mesh from a file
        /// </summary>
        /// <param name="source">Source of the file</param>
        /// <param name="address">Address at which the mesh is located</param>
        /// <param name="imageBase">Image base of the addresses</param>
        /// <param name="labels">Labels used</param>
        /// <returns></returns>
        public static Mesh Read(byte[] source, ref uint address, uint imageBase, Dictionary<uint, string> labels)
        {
            // reading the header data
            ushort header = source.ToUInt16(address);
            ushort materialID = (ushort)(header & 0x3FFFu);
            BASICPolyType polyType = (BASICPolyType)(header >> 14);
            ushort polyCount = source.ToUInt16(address + 2);

            // getting the addresses
            // polys
            uint polyAddr = source.ToUInt32(address + 4);
            string polyName = polyAddr == 0 ? null : labels.ContainsKey(polyAddr -= imageBase) ? labels[polyAddr] : "poly_" + polyAddr.ToString("X8");

            // additional normals
            uint normalAddr = source.ToUInt32(address + 12);
            string normalName = normalAddr == 0 ? null : labels.ContainsKey(normalAddr -= imageBase) ? labels[normalAddr] : "polynormal_" + normalAddr.ToString("X8");

            // colors
            uint colorAddr = source.ToUInt32(address + 16);
            string colorName = colorAddr == 0 ? null : labels.ContainsKey(colorAddr -= imageBase) ? labels[colorAddr] : "vcolor_" + colorAddr.ToString("X8");

            // texcoords
            uint texcoordAddr = source.ToUInt32(address + 20);
            string texcoordName = texcoordAddr == 0 ? null : labels.ContainsKey(texcoordAddr -= imageBase) ? labels[texcoordAddr] : "uv_" + texcoordAddr.ToString("X8");

            // reading polygons
            Poly[] polys = Array.Empty<Poly>();
            if(polyAddr > 0)
            {
                polys = new Poly[polyCount];
                for(int i = 0; i < polyCount; i++)
                    polys[i] = Poly.Read(polyType, source, ref polyAddr);
            }


            // creating the mesh
            Mesh result = new(polyType, polyName, polys, normalName, colorName, texcoordName, materialID)
            {
                PolyAttributes = source.ToUInt32(address + 8)
            };

            // reading the remaining data
            // reading additional normals
            if(normalAddr != 0)
                for(int i = 0; i < result.Normals.Length; i++)
                    result.Normals[i] = Vector3Extensions.Read(source, ref normalAddr, IOType.Float);

            // reading colors
            if(colorAddr != 0)
                for(int i = 0; i < result.Colors.Length; i++)
                    result.Colors[i] = Color.Read(source, ref colorAddr, IOType.ARGB8_32);

            // reading texcoords
            if(texcoordAddr != 0)
                for(int i = 0; i < result.Texcoords.Length; i++)
                    result.Texcoords[i] = Vector2Extensions.Read(source, ref texcoordAddr, IOType.Short) / 256f;

            address += 24;

            return result;
        }

        /// <summary>
        /// Writes the differens data arrays to a stream as NJA structs
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="labels">C struct labels that have already been written</param>
        public void WriteDataNJA(TextWriter writer, List<string> labels)
        {
            // writing polygons
            if(!labels.Contains(PolyName) && Polys != null)
            {
                writer.Write("POLYGON ");
                writer.Write(PolyName);
                writer.WriteLine("[]");

                writer.WriteLine("START");
                writer.WriteLine();

                foreach(Poly p in Polys)
                {
                    writer.Write("\t");
                    p.WriteNJA(writer);
                    writer.WriteLine();
                }

                writer.WriteLine("END");
                writer.WriteLine();

                labels.Add(PolyName);
            }

            if(!labels.Contains(NormalName) && Normals != null)
            {
                writer.Write("POLYNORMAL ");
                writer.Write(NormalName);
                writer.WriteLine("[]");

                writer.WriteLine("START");
                writer.WriteLine();

                foreach(Vector3 n in Normals)
                {
                    writer.Write("\tPNORM ");
                    n.WriteNJA(writer, IOType.Float);
                    writer.WriteLine(",");
                }

                writer.WriteLine("END");
                writer.WriteLine();

                labels.Add(NormalName);
            }

            if(!labels.Contains(ColorName) && Colors != null)
            {
                writer.Write("VERTCOLOR ");
                writer.Write(ColorName);
                writer.WriteLine("[]");

                writer.WriteLine("START");

                foreach(Color c in Colors)
                {
                    writer.Write("\tARGB");
                    c.WriteNJA(writer, IOType.ARGB8_32);
                    writer.WriteLine(",");
                }

                writer.WriteLine("END");
                writer.WriteLine();

                labels.Add(ColorName);
            }

            if(!labels.Contains(TexcoordName) && Texcoords != null)
            {
                writer.Write("VERTUV ");
                writer.Write(TexcoordName);
                writer.WriteLine("[]");

                writer.WriteLine("START");
                writer.WriteLine();

                foreach(Vector2 texcoord in Texcoords)
                {
                    writer.Write("\tUV ");
                    (texcoord * 256).WriteNJA(writer, IOType.Short);
                    writer.WriteLine(",");
                }

                writer.WriteLine("END");
                writer.WriteLine();

                labels.Add(TexcoordName);
            }
        }

        /// <summary>
        /// Writes the different data arrays to a stream
        /// </summary>
        /// <param name="writer">Output stream</param>
        /// <param name="imageBase">Image base for all addresses</param>
        /// <param name="labels">C struct labels</param>
        public void WriteData(EndianWriter writer, uint imageBase, Dictionary<string, uint> labels)
        {

            if(!labels.ContainsKey(PolyName))
            {
                labels.AddLabel(PolyName, writer.Position + imageBase);
                foreach(Poly p in Polys)
                    p.Write(writer);
            }

            if(Normals != null && !labels.ContainsKey(NormalName))
            {
                labels.AddLabel(NormalName, writer.Position + imageBase);
                foreach(Vector3 n in Normals)
                    n.Write(writer, IOType.Float);
            }

            if(Colors != null && !labels.ContainsKey(ColorName))
            {
                labels.AddLabel(ColorName, writer.Position + imageBase);
                foreach(Color c in Colors)
                    c.Write(writer, IOType.ARGB8_32);
            }

            if(Texcoords != null && !labels.ContainsKey(TexcoordName))
            {
                labels.AddLabel(TexcoordName, writer.Position + imageBase);
                foreach(Vector2 texcoord in Texcoords)
                    (texcoord * 256f).Write(writer, IOType.Short);
            }
        }

        /// <summary>
        /// Writes the meshset properties as an NJA struct
        /// </summary>
        /// <param name="writer">The</param>
        /// <param name="DX"></param>
        public void WriteMeshsetNJA(TextWriter writer, bool DX)
        {
            writer.WriteLine("MESHSTART");

            writer.Write("TypeMatID \t( ");
            writer.Write((StructEnums.NJD_MESHSET)((int)PolyType << 0xE));
            writer.Write(", ");
            writer.Write(MaterialID & 0x3FFF);
            writer.WriteLine("),");

            writer.Write("MeshNum \t");
            writer.Write(Polys != null ? ((ushort)Polys.Count).ToString() : "NULL");
            writer.WriteLine(", ");

            writer.Write("Meshes \t\t");
            writer.Write(Polys != null ? PolyName : "NULL");
            writer.WriteLine(", ");

            writer.WriteLine("PolyAttrs \tNULL,");

            writer.Write("PolyNormal \t");
            writer.Write(Normals != null ? NormalName : "NULL");
            writer.WriteLine(", ");

            writer.Write("VertColor \t");
            writer.Write(Colors != null ? ColorName : "NULL");
            writer.WriteLine(", ");

            writer.Write("VertUV \t\t");
            writer.Write(Texcoords != null ? TexcoordName : "NULL");

            if(DX)
            {
                writer.WriteLine(",");
                writer.WriteLine("NULL");
            }
            else
                writer.WriteLine();
            writer.WriteLine("MESHEND");
        }

        /// <summary>
        /// Writes the meshset to a stream
        /// </summary>
        /// <param name="writer">Ouput stream</param>
        /// <param name="DX">Whether the mesh should be written for SADX</param>
        public void WriteMeshset(EndianWriter writer, bool DX, Dictionary<string, uint> labels)
        {
            if(!labels.ContainsKey(PolyName))
                throw new NullReferenceException("Data has not been written yet");

            ushort header = MaterialID;
            header |= (ushort)((uint)PolyType << 14);
            writer.WriteUInt16(header);
            writer.WriteUInt16((ushort)Polys.Count);
            writer.WriteUInt32(labels[PolyName]);
            writer.WriteUInt32(PolyAttributes);
            writer.WriteUInt32(Normals == null ? 0 : labels[NormalName]);
            writer.WriteUInt32(Colors == null ? 0 : labels[ColorName]);
            writer.WriteUInt32(Texcoords == null ? 0 : labels[TexcoordName]);
            if(DX)
                writer.WriteUInt32(0);
        }

        object ICloneable.Clone() => Clone();

        public Mesh Clone()
        {
            return new Mesh(PolyType, Polys.ToArray().ContentClone(), (Vector3[])Normals?.Clone(), (Color[])Colors?.Clone(), (Vector2[])Texcoords?.Clone())
            {
                MaterialID = MaterialID,
                PolyName = PolyName,
                PolyAttributes = PolyAttributes,
                NormalName = NormalName,
                ColorName = ColorName,
                TexcoordName = TexcoordName
            };
        }

    }
}
