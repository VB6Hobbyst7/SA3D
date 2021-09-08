using Reloaded.Memory.Streams.Writers;
using SATools.SACommon;
using SATools.SAModel.ModelData.Buffer;
using SATools.SAModel.ModelData.GC;
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
        public VertexSet[] VertexData { get; }

        /// <summary>
        /// Meshes with opaque rendering properties
        /// </summary>
        public Mesh[] OpaqueMeshes { get; }

        /// <summary>
        /// Meshes with translucent rendering properties
        /// </summary>
        public Mesh[] TranslucentMeshes { get; }

        public override bool HasWeight => false;

        public override AttachFormat Format => AttachFormat.GC;

        /// <summary>
        /// Creates a new GC attach
        /// </summary>
        /// <param name="name">Name of the attach</param>
        /// <param name="vertexData">Vertex data</param>
        /// <param name="opaqueMeshes">Opaque meshes</param>
        /// <param name="translucentMeshes">Translucent meshes</param>
        public GCAttach(VertexSet[] vertexData, Mesh[] opaqueMeshes, Mesh[] translucentMeshes)
        {
            VertexData = vertexData;
            OpaqueMeshes = opaqueMeshes;
            TranslucentMeshes = translucentMeshes;

            MeshBounds = Bounds.FromPoints(VertexData.FirstOrDefault(x => x.Attribute == VertexAttribute.Position).Vector3Data);

            Name = "attach_" + GenerateIdentifier();
        }

        /// <summary>
        /// Removes duplicate vertex data
        /// </summary>
        public void OptimizeVertexData()
        {
            VertexSet positions = VertexData.FirstOrDefault(x => x.Attribute == VertexAttribute.Position);
            VertexSet normals = VertexData.FirstOrDefault(x => x.Attribute == VertexAttribute.Normal);
            VertexSet colors = VertexData.FirstOrDefault(x => x.Attribute == VertexAttribute.Color0);
            VertexSet uvs = VertexData.FirstOrDefault(x => x.Attribute == VertexAttribute.Tex0);

            var (distinctPositions, positionMap) = (positions?.Vector3Data).CreateDistinctMap();
            var (distinctNormals, normalMap) = (normals?.Vector3Data).CreateDistinctMap();
            var (distinctUvs, uvMap) = (uvs?.UVData).CreateDistinctMap();
            var (distinctcolors, colorMap) = (colors?.ColorData).CreateDistinctMap();

            if(positionMap == null && normalMap == null && uvMap == null && colorMap == null)
                return;

            // adjust the indices of the polygon corners
            List<Mesh> meshes = new(OpaqueMeshes);
            meshes.AddRange(TranslucentMeshes);

            foreach(Mesh m in meshes)
            {
                for(int i = 0; i < m.Polys.Length; i++)
                {
                    Poly p = m.Polys[i];
                    for(int j = 0; j < p.Corners.Length; j++)
                    {
                        Corner c = p.Corners[j];

                        if(positionMap != null)
                            c.PositionIndex = (ushort)positionMap[c.PositionIndex];

                        if(normalMap != null)
                            c.NormalIndex = (ushort)normalMap[c.NormalIndex];

                        if(uvMap != null)
                            c.UV0Index = (ushort)uvMap[c.UV0Index];

                        if(colorMap != null)
                            c.Color0Index = (ushort)colorMap[c.Color0Index];

                        p.Corners[j] = c;
                    }
                }
            }

            void Replace(VertexSet orig, VertexSet replacement)
            {
                for(int i = 0; i < VertexData.Length; i++)
                {
                    if(VertexData[i] == orig)
                    {
                        VertexData[i] = replacement;
                        return;
                    }
                }
            }

            // replace the vertex data
            IndexAttributeParameter indexParam;
            Mesh[] source = OpaqueMeshes;
            if(source == null || source.Length == 0)
                source = TranslucentMeshes;

            indexParam = (IndexAttributeParameter)source[0].Parameters.FirstOrDefault(x => x.Type == ParameterType.IndexAttributeFlags);

            if(positionMap != null)
            {
                Replace(positions, new(distinctPositions, false));
                if(distinctPositions.Length <= 256)
                    indexParam.IndexAttributes &= ~IndexAttributeFlags.Position16BitIndex;
            }

            if(normalMap != null)
            {
                Replace(normals, new(distinctNormals, true));
                if(distinctNormals.Length <= 256)
                    indexParam.IndexAttributes &= ~IndexAttributeFlags.Normal16BitIndex;
            }

            if(uvMap != null)
            {
                Replace(uvs, new(distinctUvs));
                if(distinctUvs.Length <= 256)
                    indexParam.IndexAttributes &= ~IndexAttributeFlags.UV16BitIndex;
            }

            if(colorMap != null)
            {
                Replace(colors, new(distinctcolors));
                if(distinctcolors.Length <= 256)
                    indexParam.IndexAttributes &= ~IndexAttributeFlags.Color16BitIndex;
            }
        }

        public void OptimizePolygonData()
        {
            // we optimize the polygon data by re/calculating the strips for each mesh
            void ProcessMesh(Mesh mesh)
            {
                // getting the current triangles
                List<Corner> triangles = new();
                foreach(Poly p in mesh.Polys)
                {
                    if(p.Type == PolyType.Triangles)
                        triangles.AddRange(p.Corners);
                    else if(p.Type == PolyType.TriangleStrip)
                    {
                        bool rev = false;
                        for(int i = 0; i < p.Corners.Length - 2; i++)
                        {
                            if(rev)
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
                if(map == null)
                    return;

                int[][] strips = Strippifier.Strip(map);

                // putting them all together
                List<Poly> polygons = new();
                List<Corner> singleTris = new();

                for(int i = 0; i < strips.Length; i++)
                {
                    int[] strip = strips[i];
                    Corner[] stripCorners = strip.Select(x => distinct[x]).ToArray(); 
                    if(stripCorners.Length == 3)
                        singleTris.AddRange(stripCorners);
                    else
                        polygons.Add(new(PolyType.TriangleStrip, stripCorners));
                }
                if(singleTris.Count > 0)
                    polygons.Add(new(PolyType.Triangles, singleTris.ToArray()));

                mesh.Polys = polygons.ToArray();
            }

            foreach(Mesh m in OpaqueMeshes)
                ProcessMesh(m);

            foreach(Mesh m in TranslucentMeshes)
                ProcessMesh(m);
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
            if(labels.ContainsKey(address))
                name = labels[address];
            else
                name = "attach_" + address.ToString("X8");

            // The struct is 36/0x24 bytes long

            uint vertexAddress = source.ToUInt32(address) - imageBase;
            //uint gap = source.ToUInt32(address + 4);
            uint opaqueAddress = source.ToUInt32(address + 8) - imageBase;
            uint translucentAddress = source.ToUInt32(address + 12) - imageBase;

            int opaqueCount = source.ToInt16(address + 16);
            int translucentCount = source.ToInt16(address + 18);
            address += 20;
            Bounds bounds = Bounds.Read(source, ref address);

            // reading vertex data
            List<VertexSet> vertexData = new();
            VertexSet vertexSet = VertexSet.Read(source, vertexAddress, imageBase);
            while(vertexSet.Attribute != VertexAttribute.Null)
            {
                vertexData.Add(vertexSet);
                vertexAddress += 16;
                vertexSet = VertexSet.Read(source, vertexAddress, imageBase);
            }

            IndexAttributeFlags indexFlags = IndexAttributeFlags.HasPosition;

            List<Mesh> opaqueMeshes = new();
            for(int i = 0; i < opaqueCount; i++)
            {
                Mesh mesh = Mesh.Read(source, opaqueAddress, imageBase, ref indexFlags);
                opaqueMeshes.Add(mesh);
                opaqueAddress += 16;
            }

            indexFlags = IndexAttributeFlags.HasPosition;

            List<Mesh> translucentMeshes = new();
            for(int i = 0; i < translucentCount; i++)
            {
                Mesh mesh = Mesh.Read(source, translucentAddress, imageBase, ref indexFlags);
                translucentMeshes.Add(mesh);
                translucentAddress += 16;
            }

            return new GCAttach(vertexData.ToArray(), opaqueMeshes.ToArray(), translucentMeshes.ToArray())
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
            // writing vertex data
            foreach(VertexSet vtx in VertexData)
            {
                vtx.WriteData(writer);
            }

            uint vtxAddr = writer.Position + imageBase;

            // writing vertex attributes
            foreach(VertexSet vtx in VertexData)
            {
                vtx.WriteAttribute(writer, imageBase);
            }

            // empty vtx attribute
            byte[] nullVtx = new byte[16];
            nullVtx[0] = 0xFF;
            writer.Write(nullVtx);

            // writing geometry data
            IndexAttributeFlags indexFlags = IndexAttributeFlags.HasPosition;
            foreach(Mesh m in OpaqueMeshes)
            {
                IndexAttributeFlags? t = m.IndexFlags;
                if(t.HasValue)
                    indexFlags = t.Value;
                m.WriteData(writer, indexFlags);
            }
            foreach(Mesh m in TranslucentMeshes)
            {
                IndexAttributeFlags? t = m.IndexFlags;
                if(t.HasValue)
                    indexFlags = t.Value;
                m.WriteData(writer, indexFlags);
            }

            // writing geometry properties
            uint opaqueAddress = writer.Position + imageBase;
            foreach(Mesh m in OpaqueMeshes)
            {
                m.WriteProperties(writer, imageBase);
            }
            uint translucentAddress = writer.Position + imageBase;
            foreach(Mesh m in TranslucentMeshes)
            {
                m.WriteProperties(writer, imageBase);
            }

            uint address = writer.Position + imageBase;
            labels.AddLabel(Name, address);

            writer.WriteUInt32(vtxAddr);
            writer.WriteUInt32(0);
            writer.WriteUInt32(opaqueAddress);
            writer.WriteUInt32(translucentAddress);
            writer.WriteUInt16((ushort)OpaqueMeshes.Length);
            writer.WriteUInt16((ushort)TranslucentMeshes.Length);
            MeshBounds.Write(writer);
            return address;
        }

        public override Attach Clone() => new GCAttach(VertexData.ContentClone(), OpaqueMeshes.ContentClone(), TranslucentMeshes.ContentClone())
        {
            Name = Name,
            MeshBounds = MeshBounds
        };

        public override string ToString() => $"{Name} - GC: {VertexData.Length} - {OpaqueMeshes.Length} - {TranslucentMeshes.Length}";
    }
}
