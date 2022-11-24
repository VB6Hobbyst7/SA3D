using SATools.SACommon;
using SATools.SAModel.Structs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using static SATools.SACommon.ByteConverter;
using static SATools.SACommon.HelperExtensions;
using static SATools.SACommon.StringExtensions;

namespace SATools.SAModel.ModelData.GC
{
    /// <summary>
    /// A GC format attach
    /// </summary>
    [Serializable]
    public class GCAttach : Attach
    {
        /// <summary>
        /// Seperate sets of vertex data in this attach
        /// </summary>
        public Dictionary<VertexAttribute, VertexSet> VertexData { get; }

        /// <summary>
        /// Meshes with opaque rendering properties
        /// </summary>
        public Mesh[] OpaqueMeshes { get; }

        /// <summary>
        /// Meshes with transparent rendering properties
        /// </summary>
        public Mesh[] TransparentMeshes { get; }

        public override bool HasWeight => false;

        public override AttachFormat Format => AttachFormat.GC;

        /// <summary>
        /// Creates a new GC attach and calculates the bounds
        /// </summary>
        /// <param name="name">Name of the attach</param>
        /// <param name="vertexData">Vertex data</param>
        /// <param name="opaqueMeshes">Opaque meshes</param>
        /// <param name="transprentMeshes">Transparent meshes</param>
        public GCAttach(Dictionary<VertexAttribute, VertexSet> vertexData, Mesh[] opaqueMeshes, Mesh[] transprentMeshes)
        {
            VertexData = vertexData;
            OpaqueMeshes = opaqueMeshes;
            TransparentMeshes = transprentMeshes;
            Name = "attach_" + GenerateIdentifier();
            RecalculateBounds();
        }


        internal GCAttach(VertexSet[] vertexData, Mesh[] opaqueMeshes, Mesh[] transprentMeshes)
        {
            VertexData = new();
            foreach (VertexSet v in vertexData)
            {
                if (VertexData.ContainsKey(v.Attribute))
                    throw new ArgumentException($"Vertexdata contains two sets with the attribute {v.Attribute}");
                VertexData.Add(v.Attribute, v);
            }

            OpaqueMeshes = opaqueMeshes;
            TransparentMeshes = transprentMeshes;

            Name = "attach_" + GenerateIdentifier();
        }

        public override void RecalculateBounds()
        {
            MeshBounds = Bounds.FromPoints(VertexData[VertexAttribute.Position].Vector3Data);
        }

        /// <summary>
        /// Removes duplicate vertex data
        /// </summary>
        public void OptimizeVertexData()
        {
            VertexSet? GetSet(VertexAttribute attrib)
            {
                if (VertexData.TryGetValue(attrib, out VertexSet checkPositions))
                    return checkPositions;
                return null;
            }

            VertexSet? positions = GetSet(VertexAttribute.Position);
            VertexSet? normals = GetSet(VertexAttribute.Normal);
            VertexSet? colors = GetSet(VertexAttribute.Color0);
            VertexSet? uvs = GetSet(VertexAttribute.Tex0);

            (Vector3[] distinctPositions, int[] positionMap) = (positions?.Vector3Data).CreateDistinctMap();
            (Vector3[] distinctNormals, int[] normalMap) = (normals?.Vector3Data).CreateDistinctMap();
            (Vector2[] distinctUvs, int[] uvMap) = (uvs?.UVData).CreateDistinctMap();
            (Color[] distinctcolors, int[] colorMap) = (colors?.ColorData).CreateDistinctMap();

            if (positionMap == null && normalMap == null && uvMap == null && colorMap == null)
                return;

            // adjust the indices of the polygon corners
            List<Mesh> meshes = new(OpaqueMeshes);
            meshes.AddRange(TransparentMeshes);

            foreach (Mesh m in meshes)
            {
                for (int i = 0; i < m.Polys.Length; i++)
                {
                    Poly p = m.Polys[i];
                    for (int j = 0; j < p.Corners.Length; j++)
                    {
                        Corner c = p.Corners[j];

                        if (positionMap != null)
                            c.PositionIndex = (ushort)positionMap[c.PositionIndex];

                        if (normalMap != null)
                            c.NormalIndex = (ushort)normalMap[c.NormalIndex];

                        if (uvMap != null)
                            c.UV0Index = (ushort)uvMap[c.UV0Index];

                        if (colorMap != null)
                            c.Color0Index = (ushort)colorMap[c.Color0Index];

                        p.Corners[j] = c;
                    }
                }
            }

            // replace the vertex data
            IndexAttributeParameter indexParam;
            Mesh[] source = OpaqueMeshes;
            if (source == null || source.Length == 0)
                source = TransparentMeshes;

            indexParam = (IndexAttributeParameter)source[0].Parameters.FirstOrDefault(x => x.Type == ParameterType.IndexAttributes);

            if (positionMap != null)
            {
                VertexData[VertexAttribute.Position] = new(distinctPositions, false);
                if (distinctPositions.Length <= 256)
                    indexParam.IndexAttributes &= ~IndexAttributes.Position16BitIndex;
            }

            if (normalMap != null)
            {
                VertexData[VertexAttribute.Normal] = new(distinctNormals, true);
                if (distinctNormals.Length <= 256)
                    indexParam.IndexAttributes &= ~IndexAttributes.Normal16BitIndex;
            }

            if (uvMap != null)
            {
                VertexData[VertexAttribute.Tex0] = new(distinctUvs);
                if (distinctUvs.Length <= 256)
                    indexParam.IndexAttributes &= ~IndexAttributes.UV16BitIndex;
            }

            if (colorMap != null)
            {
                VertexData[VertexAttribute.Color0] = new(distinctcolors);
                if (distinctcolors.Length <= 256)
                    indexParam.IndexAttributes &= ~IndexAttributes.Color16BitIndex;
            }
        }

        public void OptimizePolygonData()
        {
            // we optimize the polygon data by re/calculating the strips for each mesh
            Mesh ProcessMesh(Mesh mesh)
            {
                // getting the current triangles
                List<Corner> triangles = new();
                foreach (Poly p in mesh.Polys)
                {
                    if (p.Type == PolyType.Triangles)
                        triangles.AddRange(p.Corners);
                    else if (p.Type == PolyType.TriangleStrip)
                    {
                        bool rev = false;
                        for (int i = 0; i < p.Corners.Length - 2; i++)
                        {
                            if (rev)
                            {
                                triangles.Add(p.Corners[i + 1]);
                                triangles.Add(p.Corners[i]);
                            }
                            else
                            {
                                triangles.Add(p.Corners[i]);
                                triangles.Add(p.Corners[i + 1]);
                            }

                            triangles.Add(p.Corners[i + 2]);

                            rev = !rev;
                        }
                    }
                }

                // getting the distinct corners and generating strip information with them
                var (distinct, map) = triangles.CreateDistinctMap();
                if (map == null)
                    return mesh;

                int[][] strips = Strippifier.Strip(map);

                // putting them all together
                List<Poly> polygons = new();
                List<Corner> singleTris = new();

                for (int i = 0; i < strips.Length; i++)
                {
                    int[] strip = strips[i];
                    Corner[] stripCorners = strip.Select(x => distinct[x]).ToArray();
                    if (stripCorners.Length == 3)
                        singleTris.AddRange(stripCorners);
                    else
                        polygons.Add(new(PolyType.TriangleStrip, stripCorners));
                }
                if (singleTris.Count > 0)
                    polygons.Add(new(PolyType.Triangles, singleTris.ToArray()));

                return new(mesh.Parameters, polygons.ToArray());
            }

            for (int i = 0; i < OpaqueMeshes.Length; i++)
                OpaqueMeshes[i] = ProcessMesh(OpaqueMeshes[i]);

            for (int i = 0; i < TransparentMeshes.Length; i++)
                TransparentMeshes[i] = ProcessMesh(TransparentMeshes[i]);
        }

        /// <summary>
        /// Load a gc attach from a file
        /// </summary>
        /// <param name="source">Byte source from a file</param>
        /// <param name="address">Address at which the attach is located</param>
        /// <param name="imageBase">Address image base</param>
        /// <param name="labels">Labels for the data to use</param>
        /// <returns></returns>
        public static GCAttach Read(byte[] source, uint address, uint imageBase, Dictionary<uint, string> labels)
        {
            string name;
            if (labels.ContainsKey(address))
                name = labels[address];
            else
                name = "attach_" + address.ToString("X8");

            // The struct is 36/0x24 bytes long

            uint vertexAddress = source.ToUInt32(address) - imageBase;
            //uint gap = source.ToUInt32(address + 4);
            uint opaqueAddress = source.ToUInt32(address + 8) - imageBase;
            uint transparentAddress = source.ToUInt32(address + 12) - imageBase;

            int opaqueCount = source.ToInt16(address + 16);
            int transparentCount = source.ToInt16(address + 18);
            address += 20;
            Bounds bounds = Bounds.Read(source, ref address);

            // reading vertex data
            List<VertexSet> vertexData = new();
            VertexSet vertexSet = VertexSet.Read(source, vertexAddress, imageBase);
            while (vertexSet.Attribute != VertexAttribute.Null)
            {
                vertexData.Add(vertexSet);
                vertexAddress += 16;
                vertexSet = VertexSet.Read(source, vertexAddress, imageBase);
            }

            IndexAttributes indexAttribs = IndexAttributes.HasPosition;

            List<Mesh> opaqueMeshes = new();
            for (int i = 0; i < opaqueCount; i++)
            {
                Mesh mesh = Mesh.Read(source, opaqueAddress, imageBase, ref indexAttribs);
                opaqueMeshes.Add(mesh);
                opaqueAddress += 16;
            }

            indexAttribs = IndexAttributes.HasPosition;

            List<Mesh> transparentMeshes = new();
            for (int i = 0; i < transparentCount; i++)
            {
                Mesh mesh = Mesh.Read(source, transparentAddress, imageBase, ref indexAttribs);
                transparentMeshes.Add(mesh);
                transparentAddress += 16;
            }

            return new GCAttach(vertexData.ToArray(), opaqueMeshes.ToArray(), transparentMeshes.ToArray())
            {
                Name = name,
                MeshBounds = bounds
            };
        }

        public override void WriteNJA(TextWriter writer, bool DX, List<string> labels, string[] textures)
        {
            throw new NotSupportedException("GC attach doesnt have an available NJA format");
        }

        public override uint Write(EndianWriter writer, uint imageBase, bool DX, Dictionary<string, uint> labels)
        {
            VertexSet[] sets = VertexData.Values.ToArray();
            uint[] vtxAddresses = new uint[VertexData.Count];
            for (int i = 0; i < sets.Length; i++)
            {
                VertexSet vtxSet = sets[i];
                vtxAddresses[i] = writer.Position + imageBase;
                IOType outputType = vtxSet.DataType.ToStructType();

                switch (vtxSet.Attribute)
                {
                    case VertexAttribute.Position:
                    case VertexAttribute.Normal:
                        foreach (Vector3 vec in vtxSet.Vector3Data)
                            vec.Write(writer, outputType);
                        break;
                    case VertexAttribute.Color0:
                    case VertexAttribute.Color1:
                        foreach (Color col in vtxSet.ColorData)
                            col.Write(writer, outputType);
                        break;
                    case VertexAttribute.Tex0:
                    case VertexAttribute.Tex1:
                    case VertexAttribute.Tex2:
                    case VertexAttribute.Tex3:
                    case VertexAttribute.Tex4:
                    case VertexAttribute.Tex5:
                    case VertexAttribute.Tex6:
                    case VertexAttribute.Tex7:
                        foreach (Vector2 uv in vtxSet.UVData)
                            (uv * 256).Write(writer, outputType);
                        break;
                    case VertexAttribute.PositionMatrixId:
                    case VertexAttribute.Null:
                    default:
                        throw new FormatException($"Vertex set had an invalid or unavailable type: {vtxSet.Attribute}");
                }
            }

            uint vtxAddr = writer.Position + imageBase;

            // writing vertex attributes
            for (int i = 0; i < sets.Length; i++)
            {
                VertexSet vtxSet = sets[i];

                writer.Write(new byte[] { (byte)vtxSet.Attribute, (byte)vtxSet.StructSize });
                writer.WriteUInt16((ushort)vtxSet.DataLength);

                uint structure = (uint)vtxSet.StructType;
                structure |= (uint)((byte)vtxSet.DataType << 4);
                writer.WriteUInt32(structure);

                writer.WriteUInt32(vtxAddresses[i]);
                writer.WriteUInt32((uint)(vtxSet.DataLength * vtxSet.StructSize));
            }

            // empty vtx attribute
            byte[] nullVtx = new byte[16];
            nullVtx[0] = 0xFF;
            writer.Write(nullVtx);

            // writing geometry data
            uint[] WriteMeshData(Mesh[] meshes)
            {
                uint[] result = new uint[meshes.Length * 4];
                IndexAttributes indexAttribs = IndexAttributes.HasPosition;
                for (int i = 0, ri = 0; i < meshes.Length; i++, ri += 4)
                {
                    Mesh m = meshes[i];

                    IndexAttributes? t = m.IndexAttributes;
                    if (t.HasValue)
                        indexAttribs = t.Value;

                    // writing parameters
                    result[ri] = writer.Position + imageBase;
                    result[ri + 1] = (uint)m.Parameters.Length;
                    foreach (IParameter p in m.Parameters)
                        p.Write(writer);

                    // writing polygons
                    uint addr = writer.Position;
                    foreach (Poly p in m.Polys)
                        p.Write(writer, indexAttribs);
                    result[ri + 2] = addr + imageBase;
                    result[ri + 3] = writer.Position - addr;

                }
                return result;
            }

            uint[] opaqueMeshStructs = WriteMeshData(OpaqueMeshes);
            uint[] transparentMeshStructs = WriteMeshData(TransparentMeshes);

            // writing geometry properties
            uint opaqueAddress = writer.Position + imageBase;
            foreach (uint i in opaqueMeshStructs)
                writer.Write(i);

            uint transparentAddress = writer.Position + imageBase;
            foreach (uint i in transparentMeshStructs)
                writer.Write(i);

            uint address = writer.Position + imageBase;
            labels.AddLabel(Name, address);

            writer.WriteUInt32(vtxAddr);
            writer.WriteUInt32(0);
            writer.WriteUInt32(opaqueAddress);
            writer.WriteUInt32(transparentAddress);
            writer.WriteUInt16((ushort)OpaqueMeshes.Length);
            writer.WriteUInt16((ushort)TransparentMeshes.Length);
            MeshBounds.Write(writer);
            return address;
        }

        public override Attach Clone()
        {
            Dictionary<VertexAttribute, VertexSet> vertexSets = new();
            foreach (KeyValuePair<VertexAttribute, VertexSet> t in VertexData)
            {
                vertexSets.Add(t.Key, t.Value.Clone());
            }

            return new GCAttach(vertexSets, OpaqueMeshes.ContentClone(), TransparentMeshes.ContentClone())
            {
                Name = Name,
                MeshBounds = MeshBounds
            };
        }

        public override string ToString() => $"{Name} - GC: {VertexData.Count} - {OpaqueMeshes.Length} - {TransparentMeshes.Length}";
    }
}
