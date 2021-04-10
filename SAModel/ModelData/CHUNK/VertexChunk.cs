﻿using Reloaded.Memory.Streams.Writers;
using SATools.SAModel.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using static SATools.SACommon.ByteConverter;

namespace SATools.SAModel.ModelData.CHUNK
{
    /// <summary>
    /// Single vertex of a vertex chunk
    /// </summary>
    public struct ChunkVertex
    {
        /// <summary>
        /// Local position of the vertex
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// Local normal of the vertex
        /// </summary>
        public Vector3 Normal;

        /// <summary>
        /// Diffuse Color of the vertex (unused)
        /// </summary>
        public Color Diffuse;

        /// <summary>
        /// Specular color of the vertex (unused)
        /// </summary>
        public Color Specular;

        /// <summary>
        /// Flags (either ninja flags or user flags)
        /// </summary>
        public uint Flags;

        /// <summary>
        /// Cache index
        /// </summary>
        public ushort Index
        {
            get => (ushort)(Flags & 0xFFFF);
            set => Flags = (Flags & ~0xFFFFu) | Index;
        }

        /// <summary>
        /// Weight of the vertex. Ranges from 0 to 1
        /// </summary>
        public float Weight
        {
            get => ((Flags >> 16) & 0xFFu) / 255f;
            set => Flags = (Flags & ~0xFF0000u) | (((uint)(value * 255)) << 16);
        }

        public ChunkVertex(Vector3 position, Vector3 normal, uint flags) : this()
        {
            Position = position;
            Normal = normal;
            Flags = flags;
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
        /// Various flags
        /// </summary>
        public byte Flags { get; }

        /// <summary>
        /// Weight status of the chunk
        /// </summary>
        public WeightStatus WeightStatus => (WeightStatus)(Flags & 3);

        /// <summary>
        /// Offset that gets added to every index in the vertices
        /// </summary>
        public ushort IndexOffset { get; }

        /// <summary>
        /// Whether the chunk has weighted vertex data
        /// </summary>
        public bool HasWeight => Type == ChunkType.Vertex_VertexNinjaFlags || Type == ChunkType.Vertex_VertexNormalNinjaFlags;

        /// <summary>
        /// Vertices of the chunk
        /// </summary>
        public ChunkVertex[] Vertices { get; }

        /// <summary>
        /// Creates a new Vertex chunk with all relevant data
        /// </summary>
        /// <param name="type">Chunk type (has to be a vertex type)</param>
        /// <param name="flags">Flags of the chunk</param>
        /// <param name="indexOffset">Index offset for all vertices</param>
        /// <param name="vertices">Vertex data</param>
        public VertexChunk(ChunkType type, byte flags, ushort indexOffset, ChunkVertex[] vertices)
        {
            if(!type.IsVertex())
                throw new ArgumentException($"Chunktype {type} not a valid vertex type");
            Type = type;
            Flags = flags;
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
            byte flags = (byte)((header1 >> 8) & 0xFF);

            ushort indexOffset = (ushort)(header2 & 0xFFFF);
            ChunkVertex[] vertices = new ChunkVertex[(ushort)(header2 >> 16)];

            if(!type.IsVertex())
                throw new NotSupportedException($"Chunktype {type} at {address.ToString("X8")} not a valid vertex type");

            if(type == ChunkType.Vertex_VertexDiffuseSpecular16 || type > ChunkType.Vertex_VertexNormalDiffuseSpecular4)
                throw new NotSupportedException($"Unsupported chunk type {type} at {address.ToString("X8")}");

            bool hasNormal = type.HasNormal();
            uint vec4 = type.IsVec4() ? 4u : 0u;

            for(int i = 0; i < vertices.Length; i++)
            {
                ChunkVertex vtx = new ChunkVertex
                {
                    Position = Vector3.Read(source, ref address, IOType.Float)
                };
                address += vec4;

                if(hasNormal)
                {
                    vtx.Normal = Vector3.Read(source, ref address, IOType.Float);
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
                    case ChunkType.Vertex_VertexUserFlags:
                    case ChunkType.Vertex_VertexNinjaFlags:
                    case ChunkType.Vertex_VertexNormalUserFlags:
                    case ChunkType.Vertex_VertexNormalNinjaFlags:
                        vtx.Flags = source.ToUInt32(address);
                        address += 4;
                        break;
                }

                vertices[i] = vtx;
            }

            return new VertexChunk(type, flags, indexOffset, vertices);
        }

        /// <summary>
        /// Writes a vertex chunk to a stream, and splits it up if necessary
        /// </summary>
        /// <param name="writer">Output stream</param>
        public void Write(EndianMemoryStream writer)
        {
            if(Vertices.Length > short.MaxValue)
                throw new InvalidOperationException($"Vertex count ({Vertices.Length}) exceeds maximum vertex count (32767)");

            ushort vertSize = Type.Size();
            ushort vertLimit = (ushort)((ushort.MaxValue - 1) / vertSize); // -1 because header2 also counts as part of the size, which is always there
            List<ChunkVertex> remainingVerts = Vertices.ToList();
            uint header1Base = (uint)Type | (uint)(Flags << 8);
            ushort offset = IndexOffset;

            bool hasNormal = Type.HasNormal();
            bool vec4 = Type.IsVec4();

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
                        writer.Write(1u);

                    if(hasNormal)
                    {
                        vtx.Normal.Write(writer, IOType.Float);
                        if(vec4)
                            writer.Write(1u);
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
                        case ChunkType.Vertex_VertexUserFlags:
                        case ChunkType.Vertex_VertexNinjaFlags:
                        case ChunkType.Vertex_VertexNormalUserFlags:
                        case ChunkType.Vertex_VertexNormalNinjaFlags:
                            writer.Write(vtx.Flags);
                            break;
                    }
                }

                // writing the remaining vertices (if there are any)
                if(vertCount == vertLimit)
                {
                    remainingVerts = remainingVerts.Skip(vertCount).ToList();
                    if(Type != ChunkType.Vertex_VertexNinjaFlags && Type != ChunkType.Vertex_VertexNormalNinjaFlags)
                        offset += vertCount;
                }
                else
                {
                    break;
                }
            }
        }

        public object Clone() => MemberwiseClone();
    }
}

