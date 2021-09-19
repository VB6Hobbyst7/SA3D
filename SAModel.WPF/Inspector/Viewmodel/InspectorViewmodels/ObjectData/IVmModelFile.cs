﻿using SATools.SAModel.ModelData;
using SATools.SAModel.ObjData;
using SATools.SAModel.ObjData.Animation;
using System;
using System.Collections.ObjectModel;

namespace SATools.SAModel.WPF.Inspector.Viewmodel.InspectorViewmodels.ObjectData
{
    internal class IVmModelFile : InspectorViewModel
    {
        protected override Type ViewmodelType
            => typeof(ModelFile);

        private ModelFile ModelFile
            => (ModelFile)_source;

        [Tooltip("Mesh format of the file")]
        public AttachFormat Format
            => ModelFile.Format;

        [DisplayName("Ninja File")]
        [Tooltip("Whether the file is a ninja format file")]
        public bool NinjaFile
            => ModelFile.NJFile;

        [Tooltip("The tip of the models object tree")]
        public NJObject Model
            => ModelFile.Model;

        [Tooltip("All objects in hierarchial order")]
        public NJObject[] Models { get; }

        [Tooltip("Animations either embedded or referenced in the file")]
        public ReadOnlyCollection<Motion> Animations
            => ModelFile.Animations;

        public IVmModelFile() : base() { }

        public IVmModelFile(ModelFile source) : base(source) => Models = Model.GetObjects();
    }
}
