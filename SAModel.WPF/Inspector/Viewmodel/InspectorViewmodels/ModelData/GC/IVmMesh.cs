using SATools.SAModel.ModelData.GC;
using System;

namespace SATools.SAModel.WPF.Inspector.Viewmodel.InspectorViewmodels.ModelData.GC
{
    internal class IVmMesh : InspectorViewModel
    {
        protected override Type ViewmodelType
            => typeof(Mesh);

        private Mesh Mesh
            => Source == null ? default : (Mesh)Source;

        [SmoothScrollCollection]
        public IParameter[] Parameters
            => Mesh.Parameters;

        public Poly[] Polys
            => Mesh.Polys;

        public IVmMesh() : base() { }

        public IVmMesh(object source) : base(source) { }
    }
}
