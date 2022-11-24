using SATools.SACommon;
using SATools.SAModel.Structs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using static SATools.SACommon.ByteConverter;
using static SATools.SACommon.HelperExtensions;
using static SATools.SACommon.StringExtensions;

namespace SATools.SAModel.ModelData.BASIC
{
    /// <summary>
    /// A BASIC format attach
    /// </summary>
    [Serializable]
    public class BasicAttach : Attach
    {
        /// <summary>
        /// Name of the position data
        /// </summary>
        public string PositionName { get; set; }

        /// <summary>
        /// Position data
        /// </summary>
        public Vector3[] Positions { get; }

        /// <summary>
        /// Name of the normal data
        /// </summary>
        public string NormalName { get; set; }

        /// <summary>
        /// Normal data
        /// </summary>
        public Vector3[] Normals { get; }

        /// <summary>
        /// Name of the mesh data
        /// </summary>
        public string MeshName { get; set; }

        /// <summary>
        /// Mesh data
        /// </summary>
        public Mesh[] Meshes { get; }

        /// <summary>
        /// Name of the material data
        /// </summary>
        public string MaterialName { get; set; }

        /// <summary>
        /// Material data
        /// </summary>
        public Material[] Materials { get; }

        public override bool HasWeight => false;

        public override AttachFormat Format => AttachFormat.BASIC;

        /// <summary>
        /// Creates a new BASIC attach using existing data <br/>
        /// C struct are auto generated
        /// </summary>
        /// <param name="positions">Vertex position data</param>
        /// <param name="normals">Vertex normal data</param>
        /// <param name="meshes">Mesh data</param>
        /// <param name="materials">Material data for the meshes</param>
        public BasicAttach(Vector3[] positions, Vector3[] normals, Mesh[] meshes, Material[] materials)
        {
            Positions = positions;
            Normals = normals;
            Meshes = meshes;
            Materials = materials;

            if (normals != null && positions.Length != normals.Length)
                throw new ArgumentException("Position and Normal count doesnt match!");

            MeshBounds = Bounds.FromPoints(positions);

            string identifier = GenerateIdentifier();
            Name = "attach_" + identifier;
            MaterialName = "matlist_" + identifier;
            MeshName = "meshlist_" + identifier;
            PositionName = "vertex_" + identifier;
            if (normals != null)
                NormalName = "normal_" + identifier;
        }

        public override void RecalculateBounds()
        {
            MeshBounds = Bounds.FromPoints(Positions);
        }

        /// <summary>
        /// Reads a BASIC attach from a byte array
        /// </summary>
        /// <param name="source">Byte source</param>
        /// <param name="address">Address at which the attach is located</param>
        /// <param name="imageBase">Image base for all addresses</param>
        /// <param name="DX">Whether the attach is from SADX</param>
        /// <param name="labels">C struct labels</param>
        /// <returns></returns>
        public static BasicAttach Read(byte[] source, uint address, uint imageBase, bool DX, Dictionary<uint, string> labels)
        {
            string name;
            if (labels.ContainsKey(address))
                name = labels[address];
            else
                name = "attach_" + address.ToString("X8");

            string identifier = GenerateIdentifier();

            // creating the data sets
            Vector3[] positions = new Vector3[source.ToUInt32(address + 8)];
            Vector3[] normals;
            Mesh[] meshes = new Mesh[source.ToUInt16(address + 20)];

            // reading positions
            uint posAddr = source.ToUInt32(address);
            string posName;
            if (posAddr != 0)
            {
                posAddr -= imageBase;
                posName = labels.ContainsKey(posAddr) ? labels[posAddr] : "vertex_" + posAddr.ToString("X8");
                for (int i = 0; i < positions.Length; i++)
                    positions[i] = Vector3Extensions.Read(source, ref posAddr, IOType.Float);
            }
            else
                posName = "vertex_" + identifier;

            // reading normals
            uint nrmAddr = source.ToUInt32(address + 4);
            string nrmName = null;
            if (nrmAddr != 0)
            {
                normals = new Vector3[positions.Length];
                nrmAddr -= imageBase;
                nrmName = labels.ContainsKey(nrmAddr) ? labels[nrmAddr] : "normal_" + nrmAddr.ToString("X8");
                for (int i = 0; i < normals.Length; i++)
                    normals[i] = Vector3Extensions.Read(source, ref nrmAddr, IOType.Float);
            }
            else
            {
                normals = null;
            }

            // reading meshes
            uint maxMat = 0;
            uint meshAddr = source.ToUInt32(address + 0xC);
            string meshName;
            if (meshAddr != 0)
            {
                meshAddr -= imageBase;
                meshName = labels.ContainsKey(meshAddr) ? labels[meshAddr] : "meshlist_" + meshAddr.ToString("X8");

                for (int i = 0; i < meshes.Length; i++)
                {
                    meshes[i] = Mesh.Read(source, ref meshAddr, imageBase, labels);
                    if (DX)
                        meshAddr += 4;
                    maxMat = Math.Max(maxMat, meshes[i].MaterialID);
                }
            }
            else
                meshName = "meshlist_" + identifier;

            // reading materials
            // fixes case where model declares material array as shorter than it really is
            Material[] materials = new Material[Math.Max(source.ToUInt16(address + 22), maxMat + 1)];
            uint matAddr = source.ToUInt32(address + 16);
            string matName;
            if (matAddr != 0)
            {
                matAddr -= imageBase;
                matName = labels.ContainsKey(matAddr) ? labels[matAddr] : "matlist_" + matAddr.ToString("X8");
                for (int i = 0; i < materials.Length; i++)
                    materials[i] = Material.Read(source, ref matAddr);
            }
            else
                matName = "matlist_" + identifier;

            address += 24;
            Bounds bounds = Bounds.Read(source, ref address);

            return new BasicAttach(positions, normals, meshes, materials)
            {
                MeshBounds = bounds,
                Name = name,
                PositionName = posName,
                NormalName = nrmName,
                MeshName = meshName,
                MaterialName = matName
            };
        }

        public override uint Write(EndianWriter writer, uint imageBase, bool DX, Dictionary<string, uint> labels)
        {
            // writing positions
            uint posAddress;
            if (labels.ContainsKey(PositionName))
                posAddress = labels[PositionName];
            else
            {
                posAddress = writer.Position + imageBase;
                labels.AddLabel(PositionName, posAddress);
                foreach (Vector3 p in Positions)
                    p.Write(writer, IOType.Float);
            }

            // writing normals
            uint nrmAddress = 0;
            if (Normals != null)
            {
                if (labels.ContainsKey(NormalName))
                    nrmAddress = labels[NormalName];
                else
                {
                    nrmAddress = writer.Position + imageBase;
                    labels.AddLabel(NormalName, nrmAddress);
                    foreach (Vector3 p in Normals)
                        p.Write(writer, IOType.Float);
                }
            }

            // writing meshsets
            uint meshAddress;
            if (labels.ContainsKey(MeshName))
                meshAddress = labels[MeshName];
            else
            {
                // writing meshset data
                foreach (Mesh m in Meshes)
                    m.WriteData(writer, imageBase, labels);

                meshAddress = writer.Position + imageBase;
                labels.AddLabel(MeshName, meshAddress);
                foreach (Mesh m in Meshes)
                    m.WriteMeshset(writer, DX, labels);
            }

            // writing materials
            uint materialAddress;
            if (labels.ContainsKey(MaterialName))
                materialAddress = labels[MaterialName];
            else
            {
                materialAddress = writer.Position + imageBase;
                labels.AddLabel(MaterialName, materialAddress);
                foreach (Material m in Materials)
                    m.Write(writer);
            }

            // writing the attach

            uint outAddress = writer.Position + imageBase;
            labels.AddLabel(Name, outAddress);

            writer.WriteUInt32(posAddress);
            writer.WriteUInt32(nrmAddress);
            writer.WriteUInt32((uint)Positions.Length);
            writer.WriteUInt32(meshAddress);
            writer.WriteUInt32(materialAddress);
            writer.WriteUInt16((ushort)Meshes.Length);
            writer.WriteUInt16((ushort)Materials.Length);
            MeshBounds.Write(writer);
            if (DX)
                writer.WriteUInt32(0);

            return outAddress;
        }

        public override void WriteNJA(TextWriter writer, bool DX, List<string> labels, string[] textures)
        {
            // write position data
            if (!labels.Contains(PositionName))
            {
                writer.Write("POINT ");
                writer.Write(PositionName);
                writer.WriteLine("[]");

                writer.WriteLine("START");
                writer.WriteLine();

                foreach (Vector3 p in Positions)
                {
                    writer.Write("\tVERT ");
                    p.WriteNJA(writer, IOType.Float);
                    writer.WriteLine(",");
                }

                writer.WriteLine("END");
                writer.WriteLine();

                labels.Add(PositionName);
            }

            // write normal data
            if (!labels.Contains(NormalName))
            {
                writer.Write("NORMAL ");
                writer.Write(NormalName);
                writer.WriteLine("[]");

                writer.WriteLine("START");
                writer.WriteLine();

                foreach (Vector3 p in Normals)
                {
                    writer.Write("\tNORM ");
                    p.WriteNJA(writer, IOType.Float);
                    writer.WriteLine(",");
                }

                writer.WriteLine("END");
                writer.WriteLine();

                labels.Add(NormalName);
            }

            // writing meshset data
            foreach (Mesh m in Meshes)
                m.WriteDataNJA(writer, labels);

            // write meshsets
            if (!labels.Contains(MeshName))
            {
                writer.Write(DX ? "MESHSET " : "MESHSET_SADX ");
                writer.Write(MeshName);
                writer.WriteLine("[]");

                writer.WriteLine("START");
                writer.WriteLine();

                foreach (Mesh m in Meshes)
                {
                    m.WriteMeshsetNJA(writer, DX);
                    writer.WriteLine();
                }

                writer.WriteLine("END");
                writer.WriteLine();

                labels.Add(MeshName);
            }

            // write materials
            if (!labels.Contains(MaterialName))
            {
                writer.Write("MATERIAL ");
                writer.Write(MaterialName);
                writer.WriteLine("[]");

                writer.WriteLine("START");
                writer.WriteLine();

                foreach (Material m in Materials)
                {
                    m.WriteNJA(writer, textures);
                    writer.WriteLine();
                }

                writer.WriteLine("END");
                writer.WriteLine();

                labels.Add(MaterialName);
            }

            // write attach
            writer.Write(DX ? "MODEL_SADX \t\t" : "MODEL \t\t");
            writer.Write(Name);
            writer.WriteLine("[]");
            writer.WriteLine("START");

            writer.Write("Points \t\t");
            writer.Write(PositionName);
            writer.WriteLine(",");

            writer.Write("Normal \t\t");
            writer.Write(NormalName);
            writer.WriteLine(",");

            writer.Write("PointNum \t");
            writer.Write(Positions.Length);
            writer.WriteLine(",");

            writer.Write("Meshset \t");
            writer.Write(MeshName);
            writer.WriteLine(",");

            writer.Write("Materials \t");
            writer.Write(MaterialName);
            writer.WriteLine(",");

            writer.Write("MeshsetNum \t");
            writer.Write(Meshes.Length);
            writer.WriteLine(",");

            writer.Write("MatNum \t\t");
            writer.Write(Materials.Length);
            writer.WriteLine(",");

            MeshBounds.WriteNJA(writer);
            writer.WriteLine();

            writer.WriteLine("END");
            writer.WriteLine();
        }

        public override string ToString() => $"{Name} - BASIC";

        public override Attach Clone()
        {
            return new BasicAttach((Vector3[])Positions.Clone(), (Vector3[])Normals.Clone(), Meshes.ContentClone(), (Material[])Materials.Clone())
            {
                Name = Name,
                PositionName = PositionName,
                NormalName = NormalName,
                MeshName = MeshName,
                MaterialName = MaterialName,
                MeshBounds = MeshBounds
            };
        }
    }
}

