using System.IO;
using Reloaded.Memory.Streams.Writers;

namespace SATools.SAModel.Structs
{
    public enum IOType
    {
        /// <summary>
        /// Short
        /// </summary>
        Short,
        /// <summary>
        /// Float
        /// </summary>
        Float,
        /// <summary>
        /// Short rotation value
        /// </summary>
        BAMS16,
        /// <summary>
        /// Integer rotation value
        /// </summary>
        BAMS32,

        /// <summary>
        /// Float quaternion (only reading and writing)
        /// </summary>
        Quaternion,

        /// <summary>
        /// ARGB Color; Each channel takes a byte
        /// </summary>
        ARGB8_32,
        /// <summary>
        /// ARGB Color, but written as two shorts (important for big endian ARGB)
        /// </summary>
        ARGB8_16,
        /// <summary>
        /// Color; Each channel uses 4 bits
        /// </summary>
        ARGB4,
        /// <summary>
        /// Colors; Red and blue use 5 bits, green 6 bits
        /// </summary>
        RGB565,
        /// <summary>
        /// BGRA Color; Each channel takes a byte
        /// </summary>
        RGBA8,
    }

    public interface IDataStructOut
    {
        /// <summary>
        /// Writes the struct to a stream
        /// </summary>
        /// <param name="writer">Output stream</param>
        /// <param name="type">Datatype to write object as</param>
        void Write(EndianMemoryStream writer, IOType type);

        /// <summary>
        /// Writes the struct to a text stream as an NJAscii struct
        /// </summary>
        /// <param name="writer">Output text stream</param>
        /// <param name="type">Output type</param>
        void WriteNJA(TextWriter writer, IOType type);
    }

}
