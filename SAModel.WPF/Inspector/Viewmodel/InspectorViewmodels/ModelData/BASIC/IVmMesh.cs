using SATools.SAModel.ModelData.BASIC;
using SATools.SAModel.Structs;
using System;
using System.Collections.ObjectModel;
using System.Numerics;

namespace SATools.SAModel.WPF.Inspector.Viewmodel.InspectorViewmodels.ModelData.BASIC
{
    internal class IVmMesh : InspectorViewModel
    {
        protected override Type ViewmodelType
            => typeof(Mesh);

        private Mesh Mesh
        {
            get => (Mesh)Source;
            set => Source = value;
        }

        public ushort MaterialID
        {
            get => Mesh.MaterialID;
            set => Mesh.MaterialID = value;
        }

        public BASICPolyType PolyType
            => Mesh.PolyType;

        [DisplayName("Poly name")]
        [Tooltip("C label of the Polygon array")]
        public string PolyName
        {
            get => Mesh.PolyName;
            set => Mesh.PolyName = value;
        }

        public ReadOnlyCollection<IPoly> Polys
            => Mesh.Polys;

        public uint PolyAttributes
        {
            get => Mesh.PolyAttributes;
            set => Mesh.PolyAttributes = value;
        }

        [DisplayName("Normal name")]
        [Tooltip("C label of the mesh normal array")]
        public string NormalName
        {
            get => Mesh.NormalName;
            set => Mesh.NormalName = value;
        }

        public Vector3[] Normals
            => Mesh.Normals;

        [DisplayName("Color name")]
        [Tooltip("C label of the color array")]
        public string ColorName
        {
            get => Mesh.ColorName;
            set => Mesh.ColorName = value;
        }

        public Color[] Colors
            => Mesh.Colors;

        [DisplayName("Texcoord name")]
        [Tooltip("C label of the texcoord array")]
        public string TexcoordName
        {
            get => Mesh.TexcoordName;
            set => Mesh.TexcoordName = value;
        }

        public Vector2[] Texcoord
            => Mesh.Texcoords;

        public IVmMesh() : base() { }

        public IVmMesh(object source) : base(source) { }
    }
}
