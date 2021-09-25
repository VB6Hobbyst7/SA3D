using SATools.SAModel.ObjData;
using SATools.SAModel.ObjData.Animation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SATools.SAModel.WPF.Inspector.Viewmodel.InspectorViewmodels.ObjectData
{
    internal class IVmLandTable : InspectorViewModel
    {
        protected override Type ViewmodelType
            => typeof(LandTable);

        private LandTable LandTable
            => (LandTable)_source;

        [Tooltip("C label of the LandTable")]
        public string Name
        {
            get => LandTable.Name;
            set => LandTable.Name = value;
        }

        [Tooltip("Data format of the landtable")]
        public LandtableFormat Format
            => LandTable.Format;

        [Tooltip("Various attributes for SA1 Landtables")]
        public LandtableAttributes Attributes
        {
            get => LandTable.Attributes;
            set => LandTable.Attributes = value;
        }

        [DisplayName("Draw Distance")]
        [Tooltip("Far plane value for when rendering the landtable")]
        public float DrawDistance
        {
            get => LandTable.DrawDistance;
            set => LandTable.DrawDistance = value;
        }

        [DisplayName("Geometry Name")]
        [Tooltip("C label of the geometry collection")]
        public string GeoName
        {
            get => LandTable.GeoName;
            set => LandTable.GeoName = value;
        }

        [Tooltip("Geometry collection")]
        public List<LandEntry> Geometry
            => LandTable.Geometry;

        [DisplayName("Visual Geometry")]
        [Tooltip("Geometry with rendering info")]
        public ReadOnlyCollection<LandEntry> VisualGeometry { get; }

        [DisplayName("Collision Geometry")]
        [Tooltip("Geometry with collision info")]
        public ReadOnlyCollection<LandEntry> CollisionGeometry { get; }

        [DisplayName("Geometry Animation Name")]
        [Tooltip("C label of the geometriy animation collection")]
        public string GeoAnimName
        {
            get => LandTable.GeoAnimName;
            set => LandTable.GeoAnimName = value;
        }

        [DisplayName("Geometry Animations")]
        [Tooltip("Geometry Animation collection")]
        public List<LandEntryMotion> GeometryAnimations
            => LandTable.GeometryAnimations;

        [DisplayName("Texture File Name")]
        [Tooltip("Name of the associated texture file")]
        public string TextureFileName
        {
            get => LandTable.TextureFileName;
            set => LandTable.TextureFileName = value;
        }

        [DisplayName("Texture List Pointer")]
        [Tooltip("Internal pointer address for the texture list used")]
        [Hexadecimal]
        public uint TexListPointer
        {
            get => LandTable.TexListPtr;
            set => LandTable.TexListPtr = value;
        }

        public IVmLandTable() : base() { }

        public IVmLandTable(LandTable landTable) : base(landTable)
        {
            VisualGeometry = LandTable.Geometry.Where(x => x.SurfaceAttributes.HasFlag(SurfaceAttributes.Visible)).OrderBy(x => x.ToString()).ToList().AsReadOnly();
            CollisionGeometry = LandTable.Geometry.Where(x => x.SurfaceAttributes.IsCollision()).OrderBy(x => x.ToString()).ToList().AsReadOnly();
        }

        public override string ToString()
            => LandTable.ToString();
    }
}

