using SATools.SAModel.ModelData.BASIC;
using System;
using System.Numerics;

namespace SATools.SAModel.WPF.Inspector.Viewmodel.InspectorViewmodels.ModelData.BASIC
{
    internal class IVmBasicAttach : IVmAttach
    {
        protected override Type ViewmodelType
            => typeof(BasicAttach);

        private BasicAttach Attach
            => (BasicAttach)Source;

        [DisplayName("Position Name")]
        [Tooltip("C Label of the position array")]
        public string PositionName
        {
            get => Attach.PositionName;
            set => Attach.PositionName = value;
        }

        public Vector3[] Positions
            => Attach.Positions;

        [DisplayName("Normal Name")]
        [Tooltip("C Label of the normal array")]
        public string NormalName
        {
            get => Attach.NormalName;
            set => Attach.NormalName = value;
        }
        public Vector3[] Normals
            => Attach.Normals;

        [DisplayName("Mesh Name")]
        [Tooltip("C Label of the mesh array")]
        public string MeshName
        {
            get => Attach.MeshName;
            set => Attach.MeshName = value;
        }

        public Mesh[] Meshes
            => Attach.Meshes;

        [DisplayName("Material Name")]
        [Tooltip("C Label of the material array")]
        public string MaterialName
        {
            get => Attach.MaterialName;
            set => Attach.MaterialName = value;
        }

        public Material[] Materials
            => Attach.Materials;

        public IVmBasicAttach() : base() { }

        public IVmBasicAttach(object source) : base(source) { }
    }
}
