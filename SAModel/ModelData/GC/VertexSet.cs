using SATools.SACommon;
using SATools.SAModel.Structs;
using System;
using System.Numerics;
using static SATools.SACommon.ByteConverter;

namespace SATools.SAModel.ModelData.GC
{
    /// <summary>
    /// A vertex data set, which can hold various data
    /// </summary>
    [Serializable]
    public struct VertexSet : ICloneable
    {
        public static readonly VertexSet NullVertexSet = new(VertexAttribute.Null, default, default, null);

        private readonly object? _data;

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
        /// The size of a single element in the list in bytes
        /// </summary>
        public uint StructSize => GCExtensions.GetStructSize(StructType, DataType);

        public Vector3[] Vector3Data
        {
            get
            {
                if (_data is not Vector3[] v3data)
                    throw new InvalidOperationException("VertexSet does not contain Vector3 data!");
                return v3data;
            }
        }

        public Vector2[] UVData
        {
            get
            {
                if (_data is not Vector2[] uvdata)
                    throw new InvalidOperationException("VertexSet does not contain Vector2 data!");
                return uvdata;
            }
        }

        public Color[] ColorData
        {
            get
            {
                if (_data is not Color[] coldata)
                    throw new InvalidOperationException("VertexSet does not contain Color data!");
                return coldata;
            }
        }

        public int DataLength
            => ((Array?)_data)?.Length ?? 0;

        public VertexSet(Vector3[] vector3Data, bool normals)
        {
            _data = vector3Data;
            DataType = DataType.Float32;

            if (!normals)
            {
                Attribute = VertexAttribute.Position;
                StructType = StructType.NormalXYZ;
            }
            else
            {
                Attribute = VertexAttribute.Normal;
                StructType = StructType.PositionXYZ;
            }
        }

        /// <summary>
        /// Creates a UV vertex set
        /// </summary>
        /// <param name="uvData"></param>
        public VertexSet(Vector2[] uvData)
        {
            Attribute = VertexAttribute.Tex0;
            _data = uvData;
            DataType = DataType.Signed16;
            StructType = StructType.TexCoordST;
        }

        /// <summary>
        /// Creates a Color vertex set
        /// </summary>
        /// <param name="colorData"></param>
        public VertexSet(Color[] colorData)
        {
            Attribute = VertexAttribute.Color0;
            _data = colorData;
            DataType = DataType.RGBA8;
            StructType = StructType.ColorRGBA;
        }

        /// <summary>
        /// Create a custom empty vertex attribute
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="dataType"></param>
        /// <param name="structType"></param>
        /// <param name="fractionalBitCount"></param>
        private VertexSet(VertexAttribute attribute, DataType dataType, StructType structType, object? data)
        {
            Attribute = attribute;
            DataType = dataType;
            StructType = structType;
            _data = data;
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
            if (attribute == VertexAttribute.Null)
                return new VertexSet(VertexAttribute.Null, default, default, null);

            uint structure = source.ToUInt32(address + 4);
            StructType structType = (StructType)(structure & 0x0F);
            DataType dataType = (DataType)((structure >> 4) & 0x0F);
            uint structSize = GCExtensions.GetStructSize(structType, dataType);
            if (source[address + 1] != structSize)
            {
                throw new Exception($"Read structure size doesnt match calculated structure size: {source[address + 1]} != {structSize}");
            }

            // reading the data
            int count = source.ToUInt16(address + 2);
            uint tmpaddr = source.ToUInt32(address + 8) - imageBase;

            object data;

            switch (attribute)
            {
                case VertexAttribute.Position:
                case VertexAttribute.Normal:
                    Vector3[] vector3Data = new Vector3[count];
                    for (int i = 0; i < count; i++)
                        vector3Data[i] = Vector3Extensions.Read(source, ref tmpaddr, IOType.Float);

                    data = vector3Data;
                    break;
                case VertexAttribute.Color0:
                    Color[] colorData = new Color[count];
                    for (int i = 0; i < count; i++)
                        colorData[i] = Color.Read(source, ref tmpaddr, IOType.RGBA8);

                    data = colorData;
                    break;
                case VertexAttribute.Tex0:
                    Vector2[] uvData = new Vector2[count];
                    for (int i = 0; i < count; i++)
                        uvData[i] = Vector2Extensions.Read(source, ref tmpaddr, IOType.Short) / 256;
                    data = uvData;
                    break;
                default:
                    throw new ArgumentException($"Attribute type not valid sa2 type: {attribute}");
            }

            return new VertexSet(attribute, dataType, structType, data);
        }

        object ICloneable.Clone()
            => Clone();

        public VertexSet Clone()
            => new(Attribute, DataType, StructType, ((Array?)_data)?.Clone() ?? null);

        public override string ToString() => $"{Attribute}: {DataLength}";
    }
}