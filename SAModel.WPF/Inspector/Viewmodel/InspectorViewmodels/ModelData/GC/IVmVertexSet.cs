using SATools.SAModel.ModelData.GC;
using System;

namespace SATools.SAModel.WPF.Inspector.Viewmodel.InspectorViewmodels.ModelData.GC
{
    internal class IVmVertexSet : InspectorViewModel
    {
        protected override Type ViewmodelType
            => typeof(VertexSet);

        private VertexSet VertexSet
            => (VertexSet)Source;

        [Tooltip("The type of vertex data that is stored")]
        public VertexAttribute Attribute
            => VertexSet.Attribute;

        [DisplayName("Data Type")]
        [Tooltip("The datatype as which the data is stored")]
        public DataType DataType
            => VertexSet.DataType;

        [DisplayName("Struct Type")]
        [Tooltip("The structure in which the data is stored")]
        public StructType StructType
            => VertexSet.StructType;

        [DisplayName("Struct Size")]
        [Tooltip("The size of a single object in the list in bytes")]
        public uint StructSize
            => VertexSet.StructSize;

        [DisplayName("Set Data")]
        [Tooltip("May Store 3D, 2D or Color data")]
        public object Data
        {
            get
            {
                return StructType switch
                {
                    StructType.PositionXY
                    or StructType.PositionXYZ
                    or StructType.NormalXYZ
                    or StructType.NormalNBT
                    or StructType.NormalNBT3
                        => VertexSet.Vector3Data,
                    StructType.ColorRGB
                    or StructType.ColorRGBA
                        => VertexSet.ColorData,
                    StructType.TexCoordS
                    or StructType.TexCoordST
                        => VertexSet.UVData,
                    _ => null,
                };
            }
        }



        public IVmVertexSet() : base() { }

        public IVmVertexSet(object source) : base(source) { }
    }
}
