using SATools.SAModel.ModelData.CHUNK;
using SATools.SAModel.Structs;
using System;
using System.Numerics;

namespace SATools.SAModel.WPF.Inspector.Viewmodel.InspectorViewmodels.ModelData.CHUNK.PolyChunks
{
    internal class IVmStrip : InspectorViewModel
    {
        protected override Type ViewmodelType
        => typeof(PolyChunkStrip.Strip);

        private PolyChunkStrip.Strip Strip
        {
            get => (PolyChunkStrip.Strip)Source;
            set => Source = value;
        }

        [Tooltip("Culling direction")]
        public bool Reversed
            => Strip.Reversed;

        [Tooltip("Corners in the strip \n The first two corners are only used for their index")]
        public PolyChunkStrip.Strip.Corner[] Corners
            => Strip.Corners;

        public IVmStrip() { }

        public IVmStrip(object source) : base(source) { }
    }

    internal class IVmCorner : InspectorViewModel
    {
        protected override Type ViewmodelType
            => typeof(PolyChunkStrip.Strip.Corner);

        private PolyChunkStrip.Strip.Corner Corner
        {
            get => (PolyChunkStrip.Strip.Corner)Source;
            set => Source = value;
        }

        [Tooltip("Vertex Cache index")]
        public ushort Index
        {
            get => Corner.Index;
            set
            {
                var c = Corner;
                c.Index = value;
                Corner = c;
            }
        }

        [Tooltip("Corner")]
        public Vector2 Texcoord
        {
            get => Corner.Texcoord;
            set
            {
                var c = Corner;
                c.Texcoord = value;
                Corner = c;
            }
        }

        [Tooltip("Normal of the corner")]
        public Vector3 Normal
        {
            get => Corner.Normal;
            set
            {
                var c = Corner;
                c.Normal = value;
                Corner = c;
            }
        }

        [Tooltip("Vertex color")]
        public Color Color
        {
            get => Corner.Color;
            set
            {
                var c = Corner;
                c.Color = value;
                Corner = c;
            }
        }

        [Tooltip("First custom flag")]
        public ushort UserFlag1
        {
            get => Corner.UserFlag1;
            set
            {
                var c = Corner;
                c.UserFlag1 = value;
                Corner = c;
            }
        }

        [Tooltip("Second custom flag")]
        public ushort UserFlag2
        {
            get => Corner.UserFlag2;
            set
            {
                var c = Corner;
                c.UserFlag2 = value;
                Corner = c;
            }
        }

        [Tooltip("Third custom flag")]
        public ushort UserFlag3
        {
            get => Corner.UserFlag3;
            set
            {
                var c = Corner;
                c.UserFlag3 = value;
                Corner = c;
            }
        }

        public IVmCorner() { }

        public IVmCorner(object source) : base(source) { }
    }
}
