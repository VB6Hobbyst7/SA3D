using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Reloaded.Memory.Streams.Writers;
using SATools.SAModel.ModelData.Buffer;
using SATools.SAModel.Structs;
using static SATools.SACommon.Helper;
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
        /// <summary>
        /// Name of the attach
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Bounding sphere of the attach
        /// </summary>
        public Bounds MeshBounds { get; set; }

        /// <summary>
        /// Mesh data ready to draw and able to convert into any other format <br/>
        /// Might Require rebuffer via <see cref="GenBufferMesh"/>
        /// </summary>
        public BufferMesh[] MeshData { get; private set; }

        public bool BufferHasOpaque { get; private set; }

        public bool BufferHasTransparent { get; private set; }

        protected Attach() { }

        /// <summary>
        /// Create a new attach using existing meshdata
        /// </summary>
        /// <param name="name">The name of the new attach</param>
        /// <param name="meshdata">The meshdata to use</param>
        public Attach(BufferMesh[] meshdata)
        {
            MeshData = meshdata;
            Name = "attach_" + GenerateIdentifier();
        }

        /// <summary>
        /// Whether the attach uses weights
        /// </summary>
        public virtual bool HasWeight => MeshData.Any(x => x.ContinueWeight || x.TriangleList == null || x.TriangleList.Length == 0);

        /// <summary>
        /// Format of the attach
        /// </summary>
        public virtual AttachFormat Format => AttachFormat.Buffer;

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
            switch(format)
            {
                case AttachFormat.BASIC:
                    return BASIC.BasicAttach.Read(source, address, imageBase, DX, labels);
                case AttachFormat.CHUNK:
                    return CHUNK.ChunkAttach.Read(source, address, imageBase, labels);
                case AttachFormat.GC:
                    return GC.GCAttach.Read(source, address, imageBase, labels);
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Writes the attach and returns the address to the mesh
        /// </summary>
        /// <param name="imageBase">Address image base</param>
        /// <param name="DX">Whether the attach is for sadx</param>
        /// <param name="labels">Labels for the objects</param>
        /// <returns>address pointing to the attach</returns>
        public virtual uint Write(EndianMemoryStream writer, uint imageBase, bool DX, Dictionary<string, uint> labels)
        {
            throw new NotSupportedException("Standard attach doesnt have an available Binary format");
        }

        /// <summary>
        /// Writes the attach as an NJA struct
        /// </summary>
        /// <param name="writer">Output stream</param>
        /// <param name="DX">Whether the model is in DX format</param>
        public virtual void WriteNJA(TextWriter writer, bool DX, List<string> labels, string[] textures)
        {
            throw new NotSupportedException("Standard attach doesnt have an available NJA format");
        }

        /// <summary>
        /// Creates a BufferMesh set which acurately depicts the current model
        /// </summary>
        public void GenBufferMesh(bool optimize)
        {
            MeshData = buffer(optimize);
            BufferHasOpaque = MeshData.Any(x => !x.Material?.UseAlpha == true);
            BufferHasTransparent = MeshData.Any(x => x.Material?.UseAlpha == true);
        }

        internal virtual BufferMesh[] buffer(bool optimize) { return MeshData; }

        public virtual void RecalculateBounds()
            => throw new InvalidOperationException("");

        object ICloneable.Clone() => Clone();

        public virtual Attach Clone() => new(MeshData.ContentClone()) { Name = Name };

        public override string ToString() => $"{Name} - Buffer";
    }
}
