using SATools.SACommon;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using static SATools.SACommon.ByteConverter;
using static SATools.SACommon.StringExtensions;

namespace SATools.SAModel.Structs
{
    /// <summary>
    /// Bounding sphere determining the bounds of an object in 3D space
    /// </summary>
    public struct Bounds
    {
        private Vector3 _position;

        private float _radius;

        /// <summary>
        /// Position of the Bounds
        /// </summary>
        public Vector3 Position
        {
            get => _position;
            set
            {
                _position = value;
                RecalculateMatrix();
            }
        }

        /// <summary>
        /// Radius of the Bounds
        /// </summary>
        public float Radius
        {
            get => _radius;
            set
            {
                _radius = value;
                RecalculateMatrix();
            }
        }

        /// <summary>
        /// Matrix to transform a spherical mesh of diameter 1 to represent the bounds
        /// </summary>
        public Matrix4x4 Matrix { get; private set; }

        /// <summary>
        /// Creates new bounds from a position and radius
        /// </summary>
        /// <param name="position"></param>
        /// <param name="radius"></param>
        public Bounds(Vector3 position, float radius)
        {
            _position = position;
            _radius = radius;
            Matrix = Matrix4x4.CreateScale(_radius) * Matrix4x4.CreateTranslation(_position);
        }

        private void RecalculateMatrix()
        {
            Matrix = Matrix4x4.CreateScale(_radius) * Matrix4x4.CreateTranslation(_position);
        }

        /// <summary>
        /// Creates the tightest possible bounds from a list of points
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static Bounds FromPoints(IEnumerable<Vector3> points)
        {
            Vector3 position = Vector3Extensions.Center(points);
            float radius = 0;
            foreach (Vector3 p in points)
            {
                float distance = Vector3.Distance(position, p);
                if (distance > radius)
                    radius = distance;
            }
            return new Bounds(position, radius);
        }

        #region I/O

        /// <summary>
        /// Reads bounds from a byte array
        /// </summary>
        /// <param name="source">Byte source</param>
        /// <param name="address">Address at which the bounds are located</param>
        /// <returns></returns>
        public static Bounds Read(byte[] source, ref uint address)
        {
            Vector3 position = Vector3Extensions.Read(source, ref address, IOType.Float);
            float radius = source.ToSingle(address);
            address += 4;
            return new(position, radius);
        }

        /// <summary>
        /// Writes the bounds to a text stream as an NJA struct
        /// </summary>
        /// <param name="writer">Output stream</param>
        public void WriteNJA(TextWriter writer)
        {
            writer.Write("Center \t");
            writer.Write(Position.X.ToC());
            writer.Write(", ");
            writer.Write(Position.Y.ToC());
            writer.Write(", ");
            writer.Write(Position.Z.ToC());
            writer.WriteLine(",");

            writer.Write("Radius \t");
            writer.Write(Radius.ToC());
        }

        /// <summary>
        /// Writes the bounds to a stream
        /// </summary>
        /// <param name="writer">Output stream</param>
        public void Write(EndianWriter writer)
        {
            Position.Write(writer, IOType.Float);
            writer.WriteSingle(Radius);
        }

        #endregion

        public override string ToString() => $"{Position} : {Radius}";
    }
}
