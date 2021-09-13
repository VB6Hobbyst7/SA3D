using SATools.SAModel.ModelData;
using SATools.SAModel.ObjData;
using SATools.SAModel.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SAModel.WPF.Inspector.Viewmodel.InspectorViewmodels
{
    internal class IVmLandEntry : InspectorViewModel
    {
        private LandEntry LandEntry 
            => (LandEntry)_source;

        public string Name
        {
            get => LandEntry.Name;
            set => LandEntry.Name = value;
        }

        [Readonly]
        public Attach Attach
            => LandEntry.Attach;

        public Vector3 Position
        {
            get => LandEntry.Position;
            set => LandEntry.Position = value;
        }

        public Vector3 Rotation
        {
            get => LandEntry.Rotation;
            set => LandEntry.Rotation = value;
        }

        public Vector3 Scale
        {
            get => LandEntry.Scale;
            set => LandEntry.Scale = value;
        }


        public IVmLandEntry(object source) : base(source) { }
    }
}
