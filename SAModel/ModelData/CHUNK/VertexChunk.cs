using SATools.SACommon;
using SATools.SAModel.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static SATools.SACommon.ByteConverter;

namespace SATools.SAModel.ModelData.CHUNK
{
    /// <summary>
    /// Single vertex of a vertex chunk
    /// </summary>
    public struct ChunkVertex : IEquatable<ChunkVertex>
    {
        /// <summary>
        /// Local position of the vertex
        /// </summary>
        public Vector3 Position { get; set; }

        /// <summary>
        /// Local normal of the vertex
        /// </summary>
        public Vector3 Normal { get; set; }

        /// <summary>
        /// Diffuse Color of the vertex (unused)
        /// </summary>
        public Color Diffuse { get; set; }

        /// <summary>
        /// Specular color of the vertex (unused)
        /// </summary>
        public Color Specular { get; set; }

        /// <summary>
        /// Attributes (either ninja or user)
        /// </summary>
        public uint Attributes { get; set; }

        /// <summary>
        /// Cache index
        /// </summary>
        public ushort Index
        {
            get => (ushort)(Attributes & 0xFFFF);
            set => Attributes = (Attributes & ~0xFFFFu) | value;
        }

        /// <summary>
        /// Weight of the vertex. Ranges from 0 to 1
        /// </summary>
        public float Weight
        {
            get => ((Attributes >> 16) & 0xFFu) / 255f;
            set => Attributes = (Attributes & ~0xFF0000u) | (((uint)(value * 255)) << 16);
        }

        public ChunkVertex(Vector3 position, Vector3 normal) : this()
        {
            Position = position;
            Normal = normal;
            Attributes = 0;
            Weight = 1;
        }

        public ChunkVertex(Vector3 position, Vector3 normal, uint attribs) : this()
        {
            Position = position;
            Normal = normal;
            Attributes = attribs;
            Weight = 1;
        }

        public ChunkVertex(Vector3 position, Vector3 normal, ushort index, float weight) : this()
        {
            Position = position;
            Normal = normal;
            Index = index;
            Weight = weight;
        }

        public ChunkVertex(Vector3 position, Color diffuse, Color specular) : this()
        {
            Position = position;
            Diffuse = diffuse;
            Specular = specular;
        }

        public override string ToString()
            => $"{{ {Position.X: 0.000;-0.000}, {Position.Y: 0.000;-0.000}, {Position.Z: 0.000;-0.000} }}, {{ {Normal.X: 0.000;-0.000}, {Normal.Y: 0.000;-0.000}, {Normal.Z: 0.000;-0.000} }} : {Index}, {Weight:F3}";

        public override bool Equals(object obj)
        {
            return obj is ChunkVertex vertex &&
                   Position.Equals(vertex.Position) &&
                   Normal.Equals(vertex.Normal) &&
                   Diffuse.Equals(vertex.Diffuse) &&
                   Specular.Equals(vertex.Specular) &&
                   Attributes == vertex.Attributes &&
                   Index == vertex.Index &&
                   Weight == vertex.Weight;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Position, Normal, Diffuse, Specular, Attributes, Index, Weight);
        }

        public bool Equals(ChunkVertex other) => Equals(other);
    }

    /// <summary>
    /// Set of vertex data of a chunk model
    /// </summary>
    public class VertexChunk : ICloneable
    {
        /// <summary>
        /// Type of vertex chunk
        /// </summary>
        public ChunkType Type { get; }

        /// <summary>
        /// Various attributes
        /// </summary>
        public byte Attributes { get; }

        /// <summary>
        /// Weight status of the chunk
        /// </summary>
        public WeightStatus WeightStatus => (WeightStatus)(Attributes & 3);

        /// <summary>
        /// Offset that gets added to every index in the vertices
        /// </summary>
        public ushort IndexOffset { get; set; }

        /// <summary>
        /// Whether the chunk has weighted vertex data
        /// </summary>
        public bool HasWeight => Type is ChunkType.Vertex_VertexNinjaAttributes or ChunkType.Vertex_VertexNormalNinjaAttributes;

        /// <summary>
        /// Vertices of the chunk
        /// </summary>
        public ChunkVertex[] Vertices { get; }

        /// <summary>
        /// Creates a new Vertex chunk with all relevant data
        /// </summary>
        /// <param name="type">Chunk type (has to be a vertex type)</param>
        /// <param name="attribs">Attributes of the chunk</param>
        /// <param name="indexOffset">Index offset for all vertices</param>
        /// <param name="vertices">Vertex data</param>
        public VertexChunk(ChunkType type, byte attribs, ushort indexOffset, ChunkVertex[] vertices)
        {
            if(!type.IsVertex())
                throw new ArgumentException($"Chunktype {type} not a valid vertex type");
            Type = type;
            Attributes = attribs;
            IndexOffset = indexOffset;
            Vertices = vertices;
        }

        /// <summary>
        /// Creates a new Vertex chunk with all relevant data
        /// </summary>
        /// <param name="type">Chunk type (has to be a vertex type)</param>
        /// <param name="weightstatus">Weight status</param>
        /// <param name="indexOffset">Index offset for all vertices</param>
        /// <param name="vertices">Vertex data</param>
        public VertexChunk(ChunkType type, WeightStatus weightstatus, ushort indexOffset, ChunkVertex[] vertices)
            : this(type, (byte)weightstatus, indexOffset, vertices)
        {
        }

        /// <summary>
        /// Reads a vertex chunk from a byte array
        /// </summary>
        /// <param name="source">Byte source</param>
        /// <param name="address">Address at which the chunk is located</param>
        /// <returns></returns>
        public static VertexChunk Read(byte[] source, ref uint address)
        {
            uint header1 = source.ToUInt32(address);
            uint header2 = source.ToUInt32(address + 4);
            address += 8;

            ChunkType type = (ChunkType)(header1 & 0xFF);
            if(type == ChunkType.End)
                return null;
            byte attribs = (byte)((header1 >> 8) & 0xFF);

            ushort indexOffset = (ushort)(header2 & 0xFFFF);
            ChunkVertex[] vertices = new ChunkVertex[(ushort)(header2 >> 16)];

            if(!type.IsVertex())
                throw new NotSupportedException($"Chunktype {type} at {address.ToString("X8")} not a valid vertex type");

            if(type == ChunkType.Vertex_VertexDiffuseSpecular16 || type > ChunkType.Vertex_VertexNormalDiffuseSpecular4)
                throw new NotSupportedException($"Unsupported chunk type {type} at {address.ToString("X8")}");

            bool hasNormal = type.VertexHasNormal();
            uint vec4 = type.VertexIsVec4() ? 4u : 0u;

            for(int i = 0; i < vertices.Length; i++)
            {
                ChunkVertex vtx = new()
                {
                    Position = Vector3Extensions.Read(source, ref address, IOType.Float),
                    Diffuse = Color.White,
                    Specular = Color.White,
                };
                address += vec4;

                if(hasNormal)
                {
                    vtx.Normal = Vector3Extensions.Read(source, ref address, IOType.Float);
                    address += vec4;
                }
                else
                    vtx.Normal = Vector3.UnitY;

                switch(type)
                {
                    case ChunkType.Vertex_VertexDiffuse8:
                    case ChunkType.Vertex_VertexNormalDiffuse8:
                        vtx.Diffuse = Color.Read(source, ref address, IOType.ARGB8_32);
                        break;
                    case ChunkType.Vertex_VertexDiffuseSpecular5:
                    case ChunkType.Vertex_VertexNormalDiffuseSpecular5:
                        vtx.Diffuse = Color.Read(source, ref address, IOType.RGB565);
                        vtx.Specular = Color.Read(source, ref address, IOType.RGB565);
                        break;
                    case ChunkType.Vertex_VertexDiffuseSpecular4:
                    case ChunkType.Vertex_VertexNormalDiffuseSpecular4:
                        vtx.Diffuse = Color.Read(source, ref address, IOType.ARGB4);
                        vtx.Specular = Color.Read(source, ref address, IOType.RGB565);
                        break;
                    case ChunkType.Vertex_VertexUserAttributes:
                    case ChunkType.Vertex_VertexNinjaAttributes:
                    case ChunkType.Vertex_VertexNormalUserAttributes:
                    case ChunkType.Vertex_VertexNormalNinjaAttributes:
                        vtx.Attributes = source.ToUInt32(address);
                        address += 4;
                        break;
                }

                vertices[i] = vtx;
            }

            return new VertexChunk(type, attribs, indexOffset, vertices);
        }

        /// <summary>
        /// Writes a vertex chunk to a stream, and splits it up if necessary
        /// </summary>
        /// <param name="writer">Output stream</param>
        public void Write(EndianWriter writer)
        {
            if(Vertices.Length > short.MaxValue)
                throw new InvalidOperationException($"Vertex count ({Vertices.Length}) exceeds maximum vertex count (32767)");

            ushort vertSize = Type.Size();
            ushort vertLimit = (ushort)((ushort.MaxValue - 1) / vertSize); // -1 because header2 also counts as part of the size, which is always there
            List<ChunkVertex> remainingVerts = Vertices.ToList();
            uint header1Base = (uint)Type | (uint)(Attributes << 8);
            ushort offset = IndexOffset;

            bool hasNormal = Type.VertexHasNormal();
            bool vec4 = Type.VertexIsVec4();

            while(remainingVerts.Count > 0)
            {
                ushort vertCount = remainingVerts.Count > vertLimit ? vertLimit : (ushort)remainingVerts.Count;
                ushort size = (ushort)(vertCount * vertSize + 1);

                writer.Write(header1Base | (uint)(size << 16)); // header1
                writer.Write(offset | (uint)(vertCount << 16)); // header2

                // writing the vertices
                for(int i = 0; i < vertCount; i++)
                {
                    ChunkVertex vtx = remainingVerts[i];
                    vtx.Position.Write(writer, IOType.Float);
                    if(vec4)
                        writer.Write(1.0f);

                    if(hasNormal)
                    {
                        vtx.Normal.Write(writer, IOType.Float);
                        if(vec4)
                            writer.Write(0.0f);
                    }

                    switch(Type)
                    {
                        case ChunkType.Vertex_VertexDiffuse8:
                        case ChunkType.Vertex_VertexNormalDiffuse8:
                            vtx.Diffuse.Write(writer, IOType.ARGB8_32);
                            break;
                        case ChunkType.Vertex_VertexDiffuseSpecular5:
                        case ChunkType.Vertex_VertexNormalDiffuseSpecular5:
                            vtx.Diffuse.Write(writer, IOType.RGB565);
                            vtx.Specular.Write(writer, IOType.RGB565);
                            break;
                        case ChunkType.Vertex_VertexDiffuseSpecular4:
                        case ChunkType.Vertex_VertexNormalDiffuseSpecular4:
                            vtx.Diffuse.Write(writer, IOType.ARGB4);
                            vtx.Specular.Write(writer, IOType.RGB565);
                            break;
                        case ChunkType.Vertex_VertexUserAttributes:
                        case ChunkType.Vertex_VertexNinjaAttributes:
                        case ChunkType.Vertex_VertexNormalUserAttributes:
                        case ChunkType.Vertex_VertexNormalNinjaAttributes:
                            writer.Write(vtx.Attributes);
                            break;
                    }
                }

                // writing the remaining vertices (if there are any)
                if(vertCount == vertLimit)
                {
                    remainingVerts = remainingVerts.Skip(vertCount).ToList();
                    if(Type != ChunkType.Vertex_VertexNinjaAttributes && Type != ChunkType.Vertex_VertexNormalNinjaAttributes)
                        offset += vertCount;
                }
                else
                {
                    break;
                }
            }
        }

        public object Clone() => MemberwiseClone();

        public override string ToString()
            => $"{Type}, Status {WeightStatus}, offset {IndexOffset} : Verts {Vertices.Length}";
    }
}

