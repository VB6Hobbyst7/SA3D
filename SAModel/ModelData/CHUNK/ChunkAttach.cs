using SATools.SACommon;
using SATools.SAModel.Structs;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using static SATools.SACommon.ByteConverter;
using static SATools.SACommon.HelperExtensions;
using static SATools.SACommon.StringExtensions;

namespace SATools.SAModel.ModelData.CHUNK
{
    /// <summary>
    /// Chunk format mesh data
    /// </summary>
    public class ChunkAttach : Attach
    {
        /// <summary>
        /// Vertex data of the model
        /// </summary>
        public VertexChunk[]? VertexChunks { get; }

        /// <summary>
        /// C struct name of the vertex chunk array
        /// </summary>
        public string VertexName { get; set; }

        /// <summary>
        /// Polygon data of the model
        /// </summary>
        public PolyChunk[]? PolyChunks { get; }

        /// <summary>
        /// C struct name for the polygon chunk array
        /// </summary>
        public string PolyName { get; set; }

        public override AttachFormat Format => AttachFormat.CHUNK;

        private bool _hasWeight;

        public override bool HasWeight => _hasWeight;

        public ChunkAttach(VertexChunk[]? vertexChunks, PolyChunk[]? polyChunks)
        {
            VertexChunks = vertexChunks;
            PolyChunks = polyChunks;

            List<Vector3> pos = new();
            if (VertexChunks != null)
            {
                foreach (VertexChunk cnk in VertexChunks)
                    foreach (ChunkVertex vtx in cnk.Vertices)
                        pos.Add(vtx.Position);
            }
            MeshBounds = Bounds.FromPoints(pos.ToArray());
            UpdateWeight();

            string identifier = GenerateIdentifier();
            Name = "attach_" + identifier;
            VertexName = "vertex_" + identifier;
            PolyName = "poly_" + identifier;
        }

        private ChunkAttach(VertexChunk[]? vertexChunks, string vertexName, PolyChunk[]? polyChunks, string polyName, bool hasWeight, Bounds meshBounds)
        {
            VertexChunks = vertexChunks;
            VertexName = vertexName;
            PolyChunks = polyChunks;
            PolyName = polyName;
            _hasWeight = hasWeight;
            MeshBounds = meshBounds;
        }

        /// <summary>
        /// Updates the <see cref="HasWeight"/> property, since calculating it might take longer
        /// </summary>
        public void UpdateWeight()
        {
            if (PolyChunks == null || !PolyChunks.Any(a => a is PolyChunkStrip))
            {
                _hasWeight = VertexChunks != null && VertexChunks.Any(a => a.HasWeight);
                return;
            }
            List<int> ids = new();
            if (VertexChunks != null)
                foreach (var vc in VertexChunks)
                {
                    if (vc.HasWeight)
                    {
                        _hasWeight = true;
                        return;
                    }
                    ids.AddRange(Enumerable.Range(vc.IndexOffset, vc.Vertices.Length));
                }
            _hasWeight = PolyChunks.OfType<PolyChunkStrip>().SelectMany(a => a.Strips).SelectMany(a => a.Corners).Any(a => !ids.Contains(a.Index));
        }

        /// <summary>
        /// Reads a Chunk attach from a byte array
        /// </summary>
        /// <param name="source">Byte source</param>
        /// <param name="address">Address at which the attach is located</param>
        /// <param name="imagebase">Imagebase for every address</param>
        /// <param name="labels">C struct labels</param>
        /// <returns></returns>
        public static ChunkAttach Read(byte[] source, uint address, uint imagebase, Dictionary<uint, string> labels)
        {
            string name = labels.ContainsKey(address) ? labels[address] : "attach_" + address.ToString("X8");
            string identifier = GenerateIdentifier();

            uint vertexAddress = source.ToUInt32(address);
            string vertexName = "vertex_" + identifier;
            VertexChunk[]? vertexChunks = null;
            if (vertexAddress != 0)
            {
                vertexAddress -= imagebase;
                vertexName = labels.ContainsKey(vertexAddress) ? labels[vertexAddress] : "vertex_" + vertexAddress.ToString("X8");

                List<VertexChunk> chunks = new();
                VertexChunk? cnk = VertexChunk.Read(source, ref vertexAddress);
                while (cnk != null)
                {
                    chunks.Add(cnk);
                    cnk = VertexChunk.Read(source, ref vertexAddress);
                }
                vertexChunks = chunks.ToArray();
            }

            uint polyAddress = source.ToUInt32(address += 4);
            string polyName = "poly_" + identifier;
            PolyChunk[]? polyChunks = null;
            if (polyAddress != 0)
            {
                polyAddress -= imagebase;
                polyName = labels.ContainsKey(polyAddress) ? labels[polyAddress] : "poly_" + polyAddress.ToString("X8");

                List<PolyChunk> chunks = new();
                PolyChunk cnk = PolyChunk.Read(source, ref polyAddress);
                while (cnk != null && cnk.Type != ChunkType.End)
                {
                    chunks.Add(cnk);
                    cnk = PolyChunk.Read(source, ref polyAddress);
                }
                polyChunks = chunks.ToArray();

            }
            address += 4;
            return new ChunkAttach(vertexChunks, polyChunks)
            {
                Name = name,
                VertexName = vertexName,
                PolyName = polyName,
                MeshBounds = Bounds.Read(source, ref address)
            };
        }

        public override uint Write(EndianWriter writer, uint imageBase, bool DX, Dictionary<string, uint> labels)
        {
            // writing vertices
            uint vertexAddress = 0;
            if (VertexChunks != null && VertexChunks.Length > 0)
            {
                if (labels.ContainsKey(VertexName))
                    vertexAddress = labels[VertexName];
                else
                {
                    vertexAddress = writer.Position + imageBase;
                    foreach (VertexChunk cnk in VertexChunks)
                    {
                        cnk.Write(writer);
                    }
                    // end chunk
                    byte[] bytes = new byte[8];
                    bytes[0] = 255;
                    writer.Write(bytes);
                }
            }
            uint polyAddress = 0;
            if (PolyChunks != null && PolyChunks.Length > 0)
            {
                if (labels.ContainsKey(PolyName))
                    polyAddress = labels[PolyName];
                else
                {
                    polyAddress = writer.Position + imageBase;
                    foreach (PolyChunk cnk in PolyChunks)
                    {
                        cnk.Write(writer);
                    }
                    // end chunk
                    byte[] bytes = new byte[2];
                    bytes[0] = 255;
                    writer.Write(bytes);
                }
            }

            uint address = writer.Position + imageBase;
            labels.AddLabel(Name, address);
            writer.WriteUInt32(vertexAddress);
            writer.WriteUInt32(polyAddress);
            MeshBounds.Write(writer);
            return address;
        }

        public override void WriteNJA(TextWriter writer, bool DX, List<string> labels, string[]? textures)
        {
            base.WriteNJA(writer, DX, labels, textures);
        }

        public override Attach Clone()
        {
            return new ChunkAttach(VertexChunks?.ContentClone(), VertexName, PolyChunks?.ContentClone(), PolyName, _hasWeight, MeshBounds);
        }

        public override string ToString() => $"ChunkAttach - {Name} - {(VertexChunks == null ? 0 : VertexChunks.Length)} - {(PolyChunks == null ? 0 : PolyChunks.Length)}";
    }
}
