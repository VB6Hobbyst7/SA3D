using System.IO;
using Reloaded.Memory.Streams.Writers;
using static SATools.SACommon.ByteConverter;
using static SATools.SACommon.StringExtensions;

namespace SATools.SAModel.Structs
{
    /// <summary>
    /// Bounding sphere determining the bounds of an object in 3D space
    /// </summary>
    public struct Bounds
    {
        /// <summary>
        /// Position of the Bounds
        /// </summary>
        public Vector3 Position { get; set; }

        /// <summary>
        /// Radius of the Bounds
        /// </summary>
        public float Radius { get; set; }

        /// <summary>
        /// Creates new bounds from a position and radius
        /// </summary>
        /// <param name="position"></param>
        /// <param name="radius"></param>
        public Bounds(Vector3 position, float radius)
        {
            Position = position;
            Radius = radius;
        }

        /// <summary>
        /// Creates the tightest possible bounds from a list of points
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static Bounds FromPoints(Vector3[] points)
        {
            Vector3 position = Vector3.Center(points);
            float radius = 0;
            foreach(Vector3 p in points)
            {
                float distance = Vector3.Distance(position, p);
                if(distance > radius)
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
            Vector3 position = Vector3.Read(source, ref address, IOType.Float);
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
        public void Write(EndianMemoryStream writer)
        {
            Position.Write(writer, IOType.Float);
            writer.WriteSingle(Radius);
        }

        #endregion

        public override string ToString() => $"{Position} : {Radius}";
    }
}
