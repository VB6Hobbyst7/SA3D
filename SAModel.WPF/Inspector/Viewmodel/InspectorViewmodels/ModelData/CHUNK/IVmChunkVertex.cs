using SATools.SAModel.ModelData.CHUNK;
using SATools.SAModel.Structs;
using System;
using System.Numerics;

namespace SATools.SAModel.WPF.Inspector.Viewmodel.InspectorViewmodels.ModelData.CHUNK
{
    internal class IVmChunkVertex : InspectorViewModel
    {
        protected override Type ViewmodelType
            => typeof(ChunkVertex);

        private ChunkVertex Vertex
        {
            get => (ChunkVertex)Source;
            set => Source = value;
        }

        [Tooltip("Local position of the vertex")]
        public Vector3 Position
        {
            get => Vertex.Position;
            set
            {
                var v = Vertex;
                v.Position = value;
                Vertex = v;
            }
        }


        [Tooltip("Local normal of the vertex")]
        public Vector3 Normal
        {
            get => Vertex.Normal;
            set
            {
                var v = Vertex;
                v.Normal = value;
                Vertex = v;
            }
        }


        [Tooltip("Diffuse Color of the vertex (unused)")]
        public Color Diffuse
        {
            get => Vertex.Diffuse;
            set
            {
                var v = Vertex;
                v.Diffuse = value;
                Vertex = v;
            }
        }


        [Tooltip("Specular color of the vertex (unused)")]
        public Color Specular
        {
            get => Vertex.Specular;
            set
            {
                var v = Vertex;
                v.Specular = value;
                Vertex = v;
            }
        }


        [Tooltip("Attributes (either ninja or user)")]
        [Hexadecimal]
        public uint Attributes
        {
            get => Vertex.Attributes;
            set
            {
                var v = Vertex;
                v.Attributes = value;
                Vertex = v;
            }
        }


        [Tooltip("Cache index")]
        public ushort Index
        {
            get => Vertex.Index;
            set
            {
                var v = Vertex;
                v.Index = value;
                Vertex = v;
            }
        }


        [Tooltip("Weight of the vertex. Ranges from 0 to 1")]
        public float Weight
        {
            get => Vertex.Weight;
            set
            {
                var v = Vertex;
                v.Weight = value;
                Vertex = v;
            }
        }

        public IVmChunkVertex() : base() { }

        public IVmChunkVertex(object source) : base(source) { }
    }
}
