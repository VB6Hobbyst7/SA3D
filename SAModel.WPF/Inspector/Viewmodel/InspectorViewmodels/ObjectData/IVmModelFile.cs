using SATools.SAModel.ModelData;
using SATools.SAModel.ObjectData;
using SATools.SAModel.ObjectData.Animation;
using System;
using System.Collections.ObjectModel;

namespace SATools.SAModel.WPF.Inspector.Viewmodel.InspectorViewmodels.ObjectData
{
    internal class IVmModelFile : InspectorViewModel
    {
        protected override Type ViewmodelType
            => typeof(ModelFile);

        private ModelFile ModelFile
            => (ModelFile)Source;

        [Tooltip("Mesh format of the file")]
        public AttachFormat Format
            => ModelFile.Format;

        [DisplayName("Ninja File")]
        [Tooltip("Whether the file is a ninja format file")]
        public bool NinjaFile
            => ModelFile.NJFile;

        [Tooltip("The tip of the models object tree")]
        public Node Model
            => ModelFile.Model;

        [Tooltip("All objects in hierarchial order")]
        public Node[] Models { get; }

        [Tooltip("Animations either embedded or referenced in the file")]
        public ReadOnlyCollection<Motion> Animations
            => ModelFile.Animations;

        public IVmModelFile() : base() { }

        public IVmModelFile(object source) : base(source) => Models = Model.GetObjects();
    }
}
