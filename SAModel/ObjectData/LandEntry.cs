using System;
using System.Collections.Generic;
using Reloaded.Memory.Streams.Writers;
using SATools.SAModel.ModelData;
using SATools.SAModel.Structs;
using static SATools.SACommon.ByteConverter;
using static SATools.SACommon.StringExtensions;

namespace SATools.SAModel.ObjData
{
    /// <summary>
    /// Stage Geometry
    /// </summary>
    public class LandEntry
    {
        /// <summary>
        /// Model for the geometry
        /// </summary>
        private NJObject _model;

        /// <summary>
        /// World space bounds
        /// </summary>
        public Bounds ModelBounds { get; private set; }

        /// <summary>
        /// The mesh used by the geometry
        /// </summary>
        public ModelData.Attach Attach
        {
            get => _model.Attach;
            set
            {
                if(value == null)
                    throw new NullReferenceException("Attach cant be null!");
                ModelBounds = new Bounds(value.MeshBounds.Position + Position, value.MeshBounds.Radius * Scale.GreatestValue);
                Attach = value;
            }
        }

        /// <summary>
        /// World space position of the geometry
        /// </summary>
        public Vector3 Position
        {
            get => _model.Position;
            set
            {
                ModelBounds = new Bounds(Attach.MeshBounds.Position + value, ModelBounds.Radius);
                _model.Position = value;
            }
        }

        /// <summary>
        /// World space rotation
        /// </summary>
        public Vector3 Rotation
        {
            get => _model.Rotation;
            set => _model.Rotation = value;
        }

        /// <summary>
        /// World space scale
        /// </summary>
        public Vector3 Scale
        {
            get => _model.Scale;
            set
            {
                ModelBounds = new Bounds(ModelBounds.Position, Attach.MeshBounds.Radius * value.GreatestValue);
                _model.Scale = value;
            }
        }

        /// <summary>
        /// Polygon information (visual and/or collision information)
        /// </summary>
        public SurfaceFlags SurfaceFlags { get; set; }

        public bool RotateZYX
        {
            get => _model.RotateZYX;
            set => _model.RotateZYX = value;
        }

        /// <summary>
        /// No idea what this does, might be unused
        /// </summary>
        public uint BlockBit { get; set; }

        /// <summary>
        /// No idea what this does at all, might be unused
        /// </summary>
        public uint Unknown { get; set; }

        /// <summary>
        /// Creates a new landentry object from and attach and world space information
        /// </summary>
        /// <param name="attach">Mesh info to use</param>
        /// <param name="position">World space position</param>
        /// <param name="rotation">World space rotation</param>
        /// <param name="scale">World space scale</param>
        /// <param name="flags">Surface flags</param>
        public LandEntry(ModelData.Attach attach)
        {
            if(attach == null)
                throw new ArgumentNullException("attach", "Attach cant be null!");
            _model = new NJObject()
            {
                Attach = attach,
                Name = "col_" + GenerateIdentifier()
            };
            ModelBounds = attach.MeshBounds;
        }

        private LandEntry(NJObject model, SurfaceFlags flags, uint blockbit, uint unknown, Bounds modelBounds)
        {
            _model = model;
            SurfaceFlags = flags;
            BlockBit = blockbit;
            Unknown = unknown;
            ModelBounds = modelBounds;
        }

        /// <summary>
        /// Reads a landentry from a byte array
        /// </summary>
        /// <param name="source">Byte source</param>
        /// <param name="address">Address at which the landentry is located</param>
        /// <param name="imageBase">Imagebase for all addresses</param>
        /// <param name="format">Attach format</param>
        /// <param name="ltblFormat">Landtable format that the landentry belongs to</param>
        /// <param name="labels"></param>
        /// <param name="attaches"></param>
        /// <returns></returns>
        public static LandEntry Read(byte[] source, uint address, uint imageBase, AttachFormat format, LandtableFormat ltblFormat, Dictionary<uint, string> labels, Dictionary<uint, ModelData.Attach> attaches)
        {
            Bounds bounds = Bounds.Read(source, ref address);
            if(ltblFormat < LandtableFormat.SA2)
                address += 8; //sa1 has unused radius y and radius z values

            uint modelAddr = source.ToUInt32(address);
            if(modelAddr == 0)
                throw new InvalidOperationException("Landentry model address is null!");
            NJObject model = NJObject.Read(source, modelAddr - imageBase, imageBase, format, ltblFormat == LandtableFormat.SADX, labels, attaches);

            uint blockBit = source.ToUInt32(address + 4); // lets just assume that sa2 also uses blockbits, not like its used anyway
            uint unknown = 0;

            SurfaceFlags flags;
            if(ltblFormat >= LandtableFormat.SA2)
            {
                unknown = source.ToUInt32(address + 8);
                flags = ((SA2SurfaceFlags)source.ToUInt32(address + 12)).ToUniversal();
            }
            else
            {
                flags = ((SA1SurfaceFlags)source.ToUInt32(address + 8)).ToUniversal();
            }

            return new(model, flags, blockBit, unknown, bounds);
        }

        /// <summary>
        /// Writes model information of the geometry to a stream
        /// </summary>
        /// <param name="writer">Ouput stream</param>
        /// <param name="imagebase">Imagebase for all addresses</param>
        /// <param name="labels">Already written labels</param>
        public void WriteModel(EndianMemoryStream writer, uint imagebase, Dictionary<string, uint> labels)
        {
            if(labels.ContainsKey(_model.Name))
                return;
            _model.Write(writer, imagebase, labels);
        }

        /// <summary>
        /// Writes landtable information of the geometry to a stream <br/>
        /// Note: <see cref="WriteModel(EndianMemoryStream, uint, Dictionary{string, uint})"/> needs to have been called before
        /// </summary>
        /// <param name="writer">Output stream</param>
        /// <param name="ltblFormat">Landtable format</param>
        public void Write(EndianMemoryStream writer, LandtableFormat ltblFormat, Dictionary<string, uint> labels)
        {
            if(!labels.ContainsKey(_model.Name))
                throw new InvalidOperationException("Model has not been written!");

            ModelBounds.Write(writer);
            if(ltblFormat < LandtableFormat.SA2)
                writer.Write(new byte[8]); //sa1 has unused radius y and radius z values

            writer.Write(labels[_model.Name]);

            writer.WriteUInt32(BlockBit);

            if(ltblFormat >= LandtableFormat.SA2)
            {
                writer.WriteUInt32(Unknown);
                writer.WriteUInt32((uint)SurfaceFlags.ToSA2());
            }
            else
            {
                writer.WriteUInt32((uint)SurfaceFlags.ToSA1());
            }
        }
    }
}
