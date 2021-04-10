using Reloaded.Memory.Streams.Writers;
using SATools.SAModel.Structs;
using System;
using System.Collections.Generic;
using static SATools.SACommon.ByteConverter;

namespace SATools.SAModel.ModelData.GC
{
    /// <summary>
    /// A vertex data set, which can hold various data
    /// </summary>
    [Serializable]
    public class VertexSet : ICloneable
    {
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

        /// <summary>
        /// The vertex data
        /// </summary>
        public IDataStructOut[] Data { get; }

        /// <summary>
        /// The address of the vertex attribute (gets set after writing
        /// </summary>
        private uint _dataAddress;

        /// <summary>
        /// Creates a new empty vertex attribute using the default struct setups
        /// </summary>
        /// <param name="attributeType">The attribute type of the vertex attribute</param>
        public VertexSet(VertexAttribute attributeType, IDataStructOut[] data)
        {
            Attribute = attributeType;

            switch(Attribute)
            {
                case VertexAttribute.Position:
                    DataType = DataType.Float32;
                    StructType = StructType.Position_XYZ;
                    break;
                case VertexAttribute.Normal:
                    DataType = DataType.Float32;
                    StructType = StructType.Normal_XYZ;
                    break;
                case VertexAttribute.Color0:
                    DataType = DataType.RGBA8;
                    StructType = StructType.Color_RGBA;
                    break;
                case VertexAttribute.Tex0:
                    DataType = DataType.Signed16;
                    StructType = StructType.TexCoord_ST;
                    break;
                case VertexAttribute.Null:
                    break;
                default:
                    throw new ArgumentException($"Datatype { Attribute } is not a valid vertex type for SA2");
            }

            Data = data;
        }

        /// <summary>
        /// Create a custom empty vertex attribute
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="dataType"></param>
        /// <param name="structType"></param>
        /// <param name="fractionalBitCount"></param>
        public VertexSet(VertexAttribute attribute, DataType dataType, StructType structType, IDataStructOut[] data)
        {
            Attribute = attribute;
            DataType = dataType;
            StructType = structType;
            Data = data;
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
                return new VertexSet(VertexAttribute.Null, new IDataStructOut[0]);

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

            List<IDataStructOut> data = new List<IDataStructOut>();

            switch(attribute)
            {
                case VertexAttribute.Position:
                case VertexAttribute.Normal:
                    for(int i = 0; i < count; i++)
                    {
                        data.Add(Vector3.Read(source, ref tmpaddr, IOType.Float));
                    }
                    break;
                case VertexAttribute.Color0:
                    for(int i = 0; i < count; i++)
                    {
                        data.Add(Color.Read(source, ref tmpaddr, IOType.RGBA8));
                    }
                    break;
                case VertexAttribute.Tex0:
                    for(int i = 0; i < count; i++)
                    {
                        data.Add(Vector2.Read(source, ref tmpaddr, IOType.Short) / 256f);
                    }
                    break;
                default:
                    throw new ArgumentException($"Attribute type not valid sa2 type: {attribute}");
            }

            return new VertexSet(attribute, dataType, structType, data.ToArray());
        }

        /// <summary>
        /// Writes the vertex data to the current writing location
        /// </summary>
        /// <param name="writer">The output stream</param>
        /// <param name="imagebase">The imagebase</param>
        public void WriteData(EndianMemoryStream writer)
        {
            _dataAddress = (uint)writer.Stream.Position;

            if(Attribute == VertexAttribute.Tex0)
                foreach(Vector2 uv in Data)
                    (uv * 256).Write(writer, DataType.ToStructType());
            else
                foreach(IDataStructOut dso in Data)
                    dso.Write(writer, DataType.ToStructType());
        }

        /// <summary>
        /// Writes the vertex attribute information <br/>
        /// Assumes that <see cref="WriteData(BinaryWriter)"/> has been called prior
        /// </summary>
        /// <param name="writer">The output stream</param>
        /// <param name="imagebase">The imagebase</param>
        public void WriteAttribute(EndianMemoryStream writer, uint imagebase)
        {
            if(_dataAddress == 0)
                throw new Exception("Data has not been written yet!");
            byte[] bytes = new byte[] { (byte)Attribute, (byte)StructSize };
            writer.Write(bytes);
            writer.WriteUInt16((ushort)Data.Length);
            uint structure = (uint)StructType;
            structure |= (uint)((byte)DataType << 4);
            writer.WriteUInt32(structure);
            writer.WriteUInt32(_dataAddress + imagebase);
            writer.WriteUInt32((uint)(Data.Length * StructSize));

            _dataAddress = 0;
        }

        object ICloneable.Clone() => Clone();

        public VertexSet Clone() => new VertexSet(Attribute, DataType, StructType, (IDataStructOut[])Data.Clone());

        public override string ToString() => $"{Attribute}: {Data.Length}";
    }
}