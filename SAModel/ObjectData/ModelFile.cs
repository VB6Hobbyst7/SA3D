using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Reloaded.Memory.Streams;
using Reloaded.Memory.Streams.Writers;
using SATools.SAModel.ModelData;
using SATools.SAModel.ObjData.Animation;
using static SATools.SACommon.ByteConverter;

namespace SATools.SAModel.ObjData
{
    /// <summary>
    /// Stores a hierarchy of NJS_Objects and Attaches
    /// </summary>
    public class ModelFile
    {
        #region File Headers
        /// <summary>
        /// SA1MDL file header; "SA1MDL"
        /// </summary>
        private const ulong SA1MDL = 0x4C444D314153u;

        /// <summary>
        /// SA2MDL file header; "SA2MDL"
        /// </summary>
        private const ulong SA2MDL = 0x4C444D324153u;

        /// <summary>
        /// SA2BMDL file header; "SA2BMDL"
        /// </summary>
        private const ulong SA2BMDL = 0x4C444D42324153u;

        /// <summary>
        /// Header mask 
        /// </summary>
        private const ulong HeaderMask = ~((ulong)0xFF << 56);

        /// <summary>
        /// Current file version
        /// </summary>
        private const ulong CurrentVersion = 3;

        /// <summary>
        /// <see cref="SA1MDL"/> with version integrated
        /// </summary>
        private const ulong SA1MDLVer = SA1MDL | (CurrentVersion << 56);

        /// <summary>
        /// <see cref="SA2MDL"/> with version integrated
        /// </summary>
        private const ulong SA2MDLVer = SA2MDL | (CurrentVersion << 56);

        /// <summary>
        /// <see cref="SA2BMDL"/> with version integrated
        /// </summary>
        private const ulong SA2BMDLVer = SA2BMDL | (CurrentVersion << 56);

        /// <summary>
        /// NJ meta model header
        /// </summary>
        private const uint NJCM = 0x4D434A4Eu;

        /// <summary>
        /// NJ basic model header
        /// </summary>
        private const uint NJBM = 0x4D424A4Eu;
        #endregion

        /// <summary>
        /// Whether the file is an NJ binary
        /// </summary>
        public bool NJ { get; }

        /// <summary>
        /// Attach format of the file
        /// </summary>
        public AttachFormat Format { get; }

        /// <summary>
        /// Hierarchy tip of the file
        /// </summary>
        public NJObject Model { get; }

        /// <summary>
        /// Animations from files references in the meta data
        /// </summary>
        public ReadOnlyCollection<Motion> Animations { get; }

        /// <summary>
        /// Meta data of the file
        /// </summary>
        public MetaData MetaData { get; }

        private ModelFile(AttachFormat format, NJObject model, Motion[] animations, MetaData metaData, bool nj)
        {
            Format = format;
            Model = model;
            Animations = new ReadOnlyCollection<Motion>(animations);
            MetaData = metaData;
            NJ = nj;
        }

        /// <summary>
        /// Reads a model file from 
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static ModelFile Read(string filename) => Read(File.ReadAllBytes(filename), filename);

        /// <summary>
        /// Reads a model file from a byte source and its file path (for relative located files) <br/>
        /// Returns null if the modelfile couldnt be read
        /// </summary>
        /// <param name="source">Source of the file</param>
        /// <param name="filename">File path of the read file. Used if the model uses more files outside of the byte source</param>
        /// <returns></returns>
        public static ModelFile Read(byte[] source, string filename = null)
        {
            bool be = BigEndian;
            BigEndian = false;

            AttachFormat? format = null;
            NJObject model;
            Dictionary<uint, ModelData.Attach> attaches = new Dictionary<uint, ModelData.Attach>();
            List<Motion> Animations = new List<Motion>();
            MetaData metaData = new MetaData();
            bool nj = false;

            // checking for NJ format first
            uint header4 = source.ToUInt32(0);
            switch(header4)
            {
                case NJBM:
                    format = AttachFormat.BASIC;
                    break;
                case NJCM:
                    format = AttachFormat.CHUNK;
                    break;
            }

            if(format.HasValue)
            {
                // the addresses start 8 bytes ahead, and since we always subtract the image base from the addresses,
                // we have to add them this time, so we invert the 8 to add 8 by subtracting
                model = NJObject.Read(source, 8, ~8u, format.Value, false, new Dictionary<uint, string>(), attaches);
                nj = true;
                goto readFile;
            }

            // checking for mdl format
            ulong header8 = source.ToUInt64(0) & HeaderMask;
            switch(header8)
            {
                case SA1MDL:
                    format = AttachFormat.BASIC;
                    break;
                case SA2MDL:
                    format = AttachFormat.CHUNK;
                    break;
                case SA2BMDL:
                    format = AttachFormat.GC;
                    break;
                default:
                    return null;
                    //throw new InvalidDataException("File is not a valid model file");
            }

            // checking the version
            byte version = source[7];
            if(version > CurrentVersion)
            {
                BigEndian = be;
                return null;
                //throw new FormatException("Not a valid SA1LVL/SA2LVL file.");
            }

            metaData = MetaData.Read(source, version, true);
            Dictionary<uint, string> labels = new Dictionary<uint, string>(metaData.Labels);

            model = NJObject.Read(source, source.ToUInt32(8), 0, format.Value, false, labels, attaches);

            // reading animations
            if(filename != null)
            {
                string path = Path.GetDirectoryName(filename);
                try
                {
                    foreach(string item in metaData.AnimFiles)
                        Animations.Add(Motion.ReadFile(Path.Combine(path, item), model.CountAnimated()));
                }
                catch
                {
                    Animations.Clear();
                }
            }

            readFile:

            BigEndian = be;
            return new(format.Value, model, Animations.ToArray(), metaData, nj);
        }

        /// <summary>
        /// Saves the model file to a selected path
        /// </summary>
        /// <param name="outputPath">The path of the file</param>
        /// <param name="DX">Whether the file is for sadx</param>
        /// <param name="NJ">Whether to write an nj binary</param>
        public void SaveToFile(string outputPath, bool NJ)
        {
            byte[] data = Write(Format, NJ, Model, MetaData);
            File.WriteAllBytes(outputPath, data);
        }

        /// <summary>
        /// Writes a model hierarchy (without meta data) to a binary file
        /// </summary>
        /// <param name="format">Format of the file</param>
        /// <param name="outputPath">The path of the file</param>
        /// <param name="NJ">Whether to write an nj binary</param>
        /// <param name="model">The root model to write to the file</param>
        public static void WriteToFile(string outputPath, AttachFormat format, bool NJ, NJObject model)
            => WriteToFile(outputPath, format, NJ, model, new MetaData());

        /// <summary>
        /// Writes a model hierarchy to a binary file
        /// </summary>
        /// <param name="format">Format of the file</param>
        /// <param name="outputPath">The path of the file</param>
        /// <param name="NJ">Whether to write an nj binary</param>
        /// <param name="model">The root model to write to the file</param>
        /// <param name="author">Author of the file</param>
        /// <param name="description">Description of the files contents</param>
        /// <param name="metadata">Other meta data</param>
        /// <param name="animFiles">Animation file paths</param>
        public static void WriteToFile(string outputPath, AttachFormat format, bool NJ, NJObject model, MetaData metaData)
        {
            File.WriteAllBytes(outputPath, Write(format, NJ, model, metaData));
        }

        /// <summary>
        /// Writes a model hierarchy (without meta data) to a stream and returns the contents
        /// </summary>
        /// <param name="format">Format of the file</param>
        /// <param name="NJ">Whether to write an nj binary</param>
        /// <param name="model">The root model to write to the file</param>
        public static byte[] Write(AttachFormat format, bool NJ, NJObject model) => Write(format, NJ, model, new MetaData());

        /// <summary>
        /// Writes a model hierarchy to stream and returns the contents
        /// </summary>
        /// <param name="format">Format of the file</param>
        /// <param name="NJ">Whether to write an nj binary</param>
        /// <param name="model">The root model to write to the file</param>
        /// <param name="author">Author of the file</param>
        /// <param name="description">Description of the files contents</param>
        /// <param name="metadata">Other meta data</param>
        /// <param name="animFiles">Animation file paths</param>
        public static byte[] Write(AttachFormat format, bool NJ, NJObject model, MetaData metaData)
        {

            using(ExtendedMemoryStream stream = new ExtendedMemoryStream())
            {
                LittleEndianMemoryStream writer = new LittleEndianMemoryStream(stream);
                uint imageBase = 0;

                if(NJ)
                {
                    switch(format)
                    {
                        case AttachFormat.BASIC:
                            writer.WriteUInt32(NJBM);
                            break;
                        case AttachFormat.CHUNK:
                            writer.WriteUInt32(NJCM);
                            break;
                        default:
                            throw new ArgumentException($"Attach format {format} not supported for NJ binaries");
                    }
                    writer.WriteUInt32(0); // file length placeholder
                    imageBase = ~(8u);
                }
                else
                {
                    switch(format)
                    {
                        case AttachFormat.BASIC:
                            writer.WriteUInt64(SA1MDLVer);
                            break;
                        case AttachFormat.CHUNK:
                            writer.WriteUInt64(SA2MDLVer);
                            break;
                        case AttachFormat.GC:
                            writer.WriteUInt64(SA2BMDLVer);
                            break;
                        default:
                            throw new ArgumentException($"Attach format {format} not supported for SAMDL files");
                    }
                    writer.WriteUInt32(0x10);
                    writer.WriteUInt32(0); // labels placeholder
                }

                Dictionary<string, uint> labels = new Dictionary<string, uint>();
                model.WriteHierarchy(writer, imageBase, false, labels);

                if(NJ)
                {
                    // replace size
                    writer.Stream.Seek(4, SeekOrigin.Begin);
                    writer.WriteUInt32((uint)writer.Stream.Length);
                    writer.Stream.Seek(0, SeekOrigin.End);
                }
                else
                {
                    metaData.Write(writer, labels);
                }

                return stream.ToArray();
            }

        }

        /// <summary>
        /// Writes a model as an NJA file
        /// </summary>
        /// <param name="outputPath">Path to write the file to (extension will be forced to .NJA)</param>
        /// <param name="DX">Whether the file is for SADX</param>
        /// <param name="model">Top level object to write</param>
        /// <param name="textures">Texture list</param>
        public static void WriteNJA(string outputPath, bool DX, NJObject model, string[] textures = null)
        {
            NJObject[] objects = model.GetObjects();
            ModelData.Attach[] attaches = objects.Select(x => x.Attach).Distinct().ToArray();

            AttachFormat fmt = AttachFormat.Buffer;
            if(attaches.Length > 0)
            {
                fmt = attaches[0].Format;
                foreach(ModelData.Attach atc in attaches)
                    if(fmt != atc.Format)
                        throw new InvalidCastException("Not all attaches are of the same type!");
                if(fmt == AttachFormat.Buffer)
                    throw new InvalidCastException("All attaches are of buffer format! Can't decide what format to write");
            }

            outputPath = Path.ChangeExtension(outputPath, ".NJA");
            using(TextWriter writer = File.CreateText(outputPath))
            {
                List<string> labels = new List<string>();
                foreach(var atc in attaches)
                {
                    atc.WriteNJA(writer, DX, labels, textures);
                }

                writer.WriteLine("OBJECT_START");
                writer.WriteLine();

                foreach(NJObject obj in objects.Reverse())
                {
                    obj.WriteNJA(writer, labels);
                }

                writer.WriteLine("OBJECT_END");
                writer.WriteLine();

                writer.WriteLine();
                writer.WriteLine("DEFAULT_START");
                writer.WriteLine();

                writer.WriteLine("#ifndef DEFAULT_OBJECT_NAME");
                writer.Write("#define DEFAULT_OBJECT_NAME ");
                writer.WriteLine(model.Name);
                writer.WriteLine("#endif");

                writer.WriteLine();
                writer.WriteLine("DEFAULT_END");
                writer.WriteLine();
            }
        }
    }
}
