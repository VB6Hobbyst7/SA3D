using SATools.SACommon;
using SATools.SAModel.ModelData.Buffer;
using SATools.SAModel.Structs;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using static SATools.SACommon.HelperExtensions;
using static SATools.SACommon.StringExtensions;

namespace SATools.SAModel.ModelData
{
    /// <summary>
    /// The different available Attach formats
    /// </summary>
    public enum AttachFormat
    {
        /// <summary>
        /// Buffer format; Exclusive to this library
        /// </summary>
        Buffer,
        /// <summary>
        /// BASIC format
        /// </summary>
        BASIC,
        /// <summary>
        /// CHUNK format
        /// </summary>
        CHUNK,
        /// <summary>
        /// GC format
        /// </summary>
        GC
    }

    /// <summary>
    /// 3D Model data
    /// </summary>
    [Serializable]
    public class Attach : ICloneable
    {
        private BufferMesh[] _meshData;

        /// <summary>
        /// Name of the attach
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Format of the attach
        /// </summary>
        public virtual AttachFormat Format
            => AttachFormat.Buffer;

        /// <summary>
        /// Bounding sphere of the attach
        /// </summary>
        public Bounds MeshBounds { get; set; }

        /// <summary>
        /// Mesh data ready to draw and able to convert into any other format <br/>
        /// </summary>
        public BufferMesh[] MeshData
        {
            get => _meshData;
            [MemberNotNull(nameof(_meshData))]
            set
            {
                _meshData = value;
                BufferHasOpaque = _meshData.Any(x => !x.Material?.UseAlpha == true);
                BufferHasTransparent = _meshData.Any(x => x.Material?.UseAlpha == true);
            }
        }

        /// <summary>
        /// Whether the Attaches buffer has opaque meshes to display
        /// </summary>
        public bool BufferHasOpaque { get; private set; }

        /// <summary>
        /// Whether the Attaches buffer has transparent meshes to display
        /// </summary>
        public bool BufferHasTransparent { get; private set; }

        /// <summary>
        /// Whether the attach uses weights
        /// </summary>
        public virtual bool HasWeight
            => MeshData.Any(x => x.ContinueWeight || x.TriangleList == null || x.TriangleList.Length == 0);

        protected Attach()
        {
            _meshData = Array.Empty<BufferMesh>();
            Name = string.Empty;
        }

        /// <summary>
        /// Create a new attach using existing meshdata
        /// </summary>
        /// <param name="name">The name of the new attach</param>
        /// <param name="meshdata">The meshdata to use</param>
        public Attach(BufferMesh[] meshdata)
        {
            MeshData = meshdata;
            Name = "attach_" + GenerateIdentifier();
            RecalculateBounds();
        }

        /// <summary>
        /// Reads an attach from a file
        /// </summary>
        /// <param name="format">Format of the attach to read</param>
        /// <param name="source">Byte source</param>
        /// <param name="address">address at which the attach is located</param>
        /// <param name="imageBase">Imagebase for all addresses</param>
        /// <param name="labels">C struct labels</param>
        /// <returns></returns>
        public static Attach Read(AttachFormat format, byte[] source, uint address, uint imageBase, bool DX, Dictionary<uint, string> labels)
        {
            return format switch
            {
                AttachFormat.BASIC => BASIC.BasicAttach.Read(source, address, imageBase, DX, labels),
                AttachFormat.CHUNK => CHUNK.ChunkAttach.Read(source, address, imageBase, labels),
                AttachFormat.GC => GC.GCAttach.Read(source, address, imageBase, labels),
                AttachFormat.Buffer => ReadBuffer(source, address, imageBase, labels),
                _ => throw new NotImplementedException(),
            };
        }

        /// <summary>
        /// Reads a buffer attach
        /// </summary>
        /// <param name="source">Byte source</param>
        /// <param name="address">Address at which the attach is stored</param>
        /// <param name="imageBase">Imagebase for all addresses</param>
        /// <param name="labels">C struct labels</param>
        /// <returns></returns>
        public static Attach ReadBuffer(byte[] source, uint address, uint imageBase, Dictionary<uint, string> labels)
        {
            uint meshCount = source.ToUInt32(address);
            uint meshAddr = source.ToUInt32(address + 4) - imageBase;

            uint[] meshAddresses = new uint[meshCount];
            for (int i = 0; i < meshCount; i++)
            {
                meshAddresses[i] = source.ToUInt32(meshAddr) - imageBase;
                meshAddr += 4;
            }

            BufferMesh[] meshes = new BufferMesh[meshCount];

            for (int i = 0; i < meshCount; i++)
            {
                meshes[i] = BufferMesh.Read(source, meshAddresses[i], imageBase);
            }

            return new Attach(meshes);
        }

        /// <summary>
        /// Writes the attach and returns the address to the mesh
        /// </summary>
        /// <param name="imageBase">Address image base</param>
        /// <param name="DX">Whether the attach is for sadx</param>
        /// <param name="labels">Labels for the objects</param>
        /// <returns>address pointing to the attach</returns>
        public virtual uint Write(EndianWriter writer, uint imageBase, bool DX, Dictionary<string, uint> labels)
        {
            // default to buffer format
            return WriteBuffer(writer, imageBase, labels);
        }

        public uint WriteBuffer(EndianWriter writer, uint imageBase, Dictionary<string, uint> labels)
        {
            // write the meshes first
            uint[] meshAddresses = new uint[MeshData.Length];
            for (int i = 0; i < MeshData.Length; i++)
            {
                meshAddresses[i] = MeshData[i].Write(writer, imageBase);
            }

            // write the pointer array
            uint arrayAddr = writer.Position + imageBase;
            for (int i = 0; i < MeshData.Length; i++)
            {
                writer.WriteUInt32(meshAddresses[i]);
            }

            uint address = writer.Position + imageBase;
            labels.AddLabel(Name, address);

            writer.WriteUInt32((uint)meshAddresses.Length);
            writer.WriteUInt32(arrayAddr);

            return address;
        }

        /// <summary>
        /// Writes the attach as an NJA struct
        /// </summary>
        /// <param name="writer">Output stream</param>
        /// <param name="DX">Whether the model is in DX format</param>
        public virtual void WriteNJA(TextWriter writer, bool DX, List<string> labels, string[]? textures)
        {
            throw new NotSupportedException("Standard attach doesnt have an available NJA format");
        }

        public (BufferMesh[] opaque, BufferMesh[] transparent) GetDisplayMeshes()
        {
            BufferMesh[] result = new BufferMesh[MeshData.Length];
            int opaqueCount = 0;
            int transparentCount = result.Length - 1;

            for (int i = 0; i < result.Length; i++)
            {
                bool? useAlpha = MeshData[i].Material?.UseAlpha;
                if (useAlpha == true)
                {
                    result[transparentCount] = MeshData[i];
                    transparentCount--;
                }
                else if (useAlpha == false)
                {
                    result[opaqueCount] = MeshData[i];
                    opaqueCount++;
                }
            }
            transparentCount++;

            BufferMesh[] transparent = new BufferMesh[result.Length - transparentCount];
            if (transparent.Length > 0)
                Array.Copy(result, transparentCount, transparent, 0, transparent.Length);
            Array.Resize(ref result, opaqueCount);

            return (result, transparent);
        }

        public virtual void RecalculateBounds()
        {
            MeshBounds = Bounds.FromPoints(MeshData.SelectManyIgnoringNull(x => x.Vertices).Select(x => x.Position));
        }

        object ICloneable.Clone()
            => Clone();

        public virtual Attach Clone()
            => new(MeshData.ContentClone()) { Name = Name };

        public override string ToString()
            => $"{Name} - Buffer";
    }
}
