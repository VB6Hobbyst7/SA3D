using Reloaded.Memory.Streams.Writers;
using SATools.SACommon;
using SATools.SAModel.Structs;
using System;
using System.Collections.Generic;
using System.Numerics;
using static SATools.SACommon.ByteConverter;

namespace SATools.SAModel.ModelData.GC
{
    /// <summary>
    /// A vertex data set, which can hold various data
    /// </summary>
    [Serializable]
    public class VertexSet : ICloneable
    {
        private readonly Vector3[] _vector3Data;

        private readonly Vector2[] _uvData;

        private readonly Color[] _colorData;

        /// <summary>
        /// The type of vertex data that is stored
        /// </summary>
        public VertexAttribute Attribute { get; }

        /// <summary>
        /// The datatype as which the data is stored
        /// </summary>
        public DataType DataType { get; }

        /// <summary>
        /// The structure in which the data is stored
        /// </summary>
        public StructType StructType { get; }

        /// <summary>
        /// The size of a single object in the list in bytes
        /// </summary>
        public uint StructSize => GCExtensions.GetStructSize(StructType, DataType);

        public Vector3[] Vector3Data
        {
            get
            {
                if(_vector3Data == null)
                    throw new InvalidOperationException("VertexSet does not contain Vector3 data!");
                return _vector3Data;
            }
        }

        public Vector2[] UVData
        {
            get
            {
                if(_uvData == null)
                    throw new InvalidOperationException("VertexSet does not contain Vector2 data!");
                return _uvData;
            }
        }

        public Color[] ColorData
        {
            get
            {
                if(_colorData == null)
                    throw new InvalidOperationException("VertexSet does not contain Color data!");
                return _colorData;
            }
        }

        public int DataLength
        {
            get
            {
                if(_vector3Data != null)
                    return _vector3Data.Length;

                if(_uvData != null)
                    return _uvData.Length;

                if(_colorData != null)
                    return _colorData.Length;

                return 0;
            }
        }

        /// <summary>
        /// The address of the vertex attribute (gets set after writing
        /// </summary>
        private uint _dataAddress;

        public VertexSet(Vector3[] vector3Data, bool normals)
        {
            _vector3Data = vector3Data;
            DataType = DataType.Float32;

            if(!normals)
            {
                Attribute = VertexAttribute.Position;
                StructType = StructType.Normal_XYZ;
            }
            else
            {
                Attribute = VertexAttribute.Normal;
                StructType = StructType.Position_XYZ;
            }
        }

        /// <summary>
        /// Creates a UV vertex set
        /// </summary>
        /// <param name="uvData"></param>
        public VertexSet(Vector2[] uvData)
        {
            Attribute = VertexAttribute.Tex0;
            _uvData = uvData;
            DataType = DataType.Signed16;
            StructType = StructType.TexCoord_ST;
        }

        /// <summary>
        /// Creates a Color vertex set
        /// </summary>
        /// <param name="colorData"></param>
        public VertexSet(Color[] colorData)
        {
            Attribute = VertexAttribute.Color0;
            _colorData = colorData;
            DataType = DataType.RGBA8;
            StructType = StructType.Color_RGBA;
        }

        /// <summary>
        /// Create a custom empty vertex attribute
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="dataType"></param>
        /// <param name="structType"></param>
        /// <param name="fractionalBitCount"></param>
        private VertexSet(VertexAttribute attribute, DataType dataType, StructType structType, Vector3[] vector3Data, Vector2[] uvData, Color[] colorData)
        {
            Attribute = attribute;
            DataType = dataType;
            StructType = structType;
            _vector3Data = vector3Data;
            _uvData = uvData;
            _colorData = colorData;
        }

        /// <summary>
        /// Read an entire vertex data set
        /// </summary>
        /// <param name="source">The files contents</param>
        /// <param name="address">The starting address of the file</param>
        /// <param name="imageBase">The image base of the addresses</param>
        public static VertexSet Read(byte[] source, uint address, uint imageBase)
        {
            VertexAttribute attribute = (VertexAttribute)source[address];
            if(attribute == VertexAttribute.Null)
                return new VertexSet(VertexAttribute.Null, default, default, null, null, null);

            uint structure = source.ToUInt32(address + 4);
            StructType structType = (StructType)(structure & 0x0F);
            DataType dataType = (DataType)((structure >> 4) & 0x0F);
            uint structSize = GCExtensions.GetStructSize(structType, dataType);
            if(source[address + 1] != structSize)
            {
                throw new Exception($"Read structure size doesnt match calculated structure size: {source[address + 1]} != {structSize}");
            }

            // reading the data
            int count = source.ToUInt16(address + 2);
            uint tmpaddr = source.ToUInt32(address + 8) - imageBase;

            Vector3[] vector3Data = null;
            Vector2[] uvData = null;
            Color[] colorData = null;

            switch(attribute)
            {
                case VertexAttribute.Position:
                case VertexAttribute.Normal:
                    vector3Data = new Vector3[count];
                    for(int i = 0; i < count; i++)
                        vector3Data[i] = Vector3Extensions.Read(source, ref tmpaddr, IOType.Float);
                    break;
                case VertexAttribute.Color0:
                    colorData = new Color[count];
                    for(int i = 0; i < count; i++)
                        colorData[i] = Color.Read(source, ref tmpaddr, IOType.RGBA8);
                    break;
                case VertexAttribute.Tex0:
                    uvData = new Vector2[count];
                    for(int i = 0; i < count; i++)
                        uvData[i] = Vector2Extensions.Read(source, ref tmpaddr, IOType.Short) / 256;
                    break;
                default:
                    throw new ArgumentException($"Attribute type not valid sa2 type: {attribute}");
            }

            return new VertexSet(attribute, dataType, structType, vector3Data, uvData, colorData);
        }

        /// <summary>
        /// Writes the vertex data to the current writing location
        /// </summary>
        /// <param name="writer">The output stream</param>
        /// <param name="imagebase">The imagebase</param>
        public void WriteData(EndianWriter writer)
        {
            _dataAddress = writer.Position;

            IOType outputType = DataType.ToStructType();

            if(_vector3Data != null)
            {
                foreach(Vector3 vec in _vector3Data)
                    vec.Write(writer, outputType);
            }
            else if(_uvData != null)
            {
                foreach(Vector2 uv in _uvData)
                    (uv * 256).Write(writer, outputType);
            }
            else if(_colorData != null)
            {
                foreach(Color col in _colorData)
                    col.Write(writer, outputType);
            }
        }

        /// <summary>
        /// Writes the vertex attribute information <br/>
        /// Assumes that <see cref="WriteData(BinaryWriter)"/> has been called prior
        /// </summary>
        /// <param name="writer">The output stream</param>
        /// <param name="imagebase">The imagebase</param>
        public void WriteAttribute(EndianWriter writer, uint imagebase)
        {
            if(_dataAddress == 0)
                throw new Exception("Data has not been written yet!");
            byte[] bytes = new byte[] { (byte)Attribute, (byte)StructSize };
            writer.Write(bytes);
            writer.WriteUInt16((ushort)DataLength);
            uint structure = (uint)StructType;
            structure |= (uint)((byte)DataType << 4);
            writer.WriteUInt32(structure);
            writer.WriteUInt32(_dataAddress + imagebase);
            writer.WriteUInt32((uint)(DataLength * StructSize));

            _dataAddress = 0;
        }

        object ICloneable.Clone()
            => Clone();

        public VertexSet Clone()
            => new(Attribute, DataType, StructType, (Vector3[])_vector3Data?.Clone(), (Vector2[])_uvData?.Clone(), (Color[])_colorData?.Clone());

        public override string ToString() => $"{Attribute}: {DataLength}";
    }
}