using System;
using System.Collections.Generic;
using System.Numerics;
using SATools.SACommon;
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
        private readonly NJObject _model;

        /// <summary>
        /// Name of the Landentry
        /// </summary>
        public string Name
        {
            get => _model.Name;
            set => _model.Name = value;
        }

        /// <summary>
        /// World space bounds
        /// </summary>
        public Bounds ModelBounds { get; private set; }

        /// <summary>
        /// The mesh used by the geometry
        /// </summary>
        public Attach Attach
        {
            get => _model.Attach;
            set
            {
                if(value == null)
                    throw new NullReferenceException("Attach cant be null!");
                ModelBounds = new Bounds(value.MeshBounds.Position + Position, value.MeshBounds.Radius * Scale.GreatestValue());
                _model.Attach = value;
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
                _model.Position = value;
                UpdateBounds();
            }
        }

        /// <summary>
        /// World space rotation
        /// </summary>
        public Vector3 Rotation
        {
            get => _model.Rotation;
            set
            {
                _model.Rotation = value;
                UpdateBounds();
            }
        }

        /// <summary>
        /// World space scale
        /// </summary>
        public Vector3 Scale
        {
            get => _model.Scale;
            set
            {
                _model.Scale = value;
                UpdateBounds();
            }
        }

        /// <summary>
        /// World space quaternion rotations
        /// </summary>
        public Quaternion QuaternionRotation
        {
            get => _model.QuaternionRotation;
            set => _model.QuaternionRotation = value;
        }

        public Matrix4x4 WorldMatrix
            => _model.LocalMatrix;

        /// <summary>
        /// Polygon information (visual and/or collision information)
        /// </summary>
        public SurfaceAttributes SurfaceAttributes { get; set; }

        /// <summary>
        /// Whether the euler order is "inverted"
        /// </summary>
        public bool RotateZYX => _model.RotateZYX;
        
        /// <summary>
        /// Block mapping bits
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
        public LandEntry(Attach attach)
        {
            if(attach == null)
                throw new ArgumentNullException(nameof(attach), "Attach cant be null!");
            _model = new NJObject()
            {
                Attach = attach,
                Name = "col_" + GenerateIdentifier()
            };
            ModelBounds = attach.MeshBounds;
        }

        private LandEntry(NJObject model, SurfaceAttributes attribs, uint blockbit, uint unknown, Bounds modelBounds)
        {
            _model = model;
            SurfaceAttributes = attribs;
            BlockBit = blockbit;
            Unknown = unknown;
            ModelBounds = modelBounds;
        }

        /// <summary>
        /// Copies the Attach-bounds and applies the landentries transform matrix to them
        /// </summary>
        public void UpdateBounds()
        {
            Vector3 position = Vector3.Transform(Attach.MeshBounds.Position, WorldMatrix);
            float radius = Attach.MeshBounds.Radius * Scale.GreatestValue();

            ModelBounds = new(position, radius);
        }

        /// <summary>
        /// Replaces the bounds with a manually determined value <br/>
        /// !!! NOTE !!! The bounds will get automatically recalculated once any of its transforms change!
        /// </summary>
        /// <param name="bounds"></param>
        public void UpdateBounds(Bounds bounds) 
            => ModelBounds = bounds;

        /// <summary>
        /// Sets the rotation order
        /// </summary>
        /// <param name="newValue">New rotation order state</param>
        /// <param name="updateRotation">Update the euler rotation, so that the order but not the rotation changes</param>
        public void SetRotationZYX(bool newValue, bool updateRotation)
            => _model.SetRotationZYX(newValue, updateRotation);

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
        public static LandEntry Read(byte[] source, uint address, uint imageBase, AttachFormat format, LandtableFormat ltblFormat, Dictionary<uint, string> labels, Dictionary<uint, Attach> attaches)
        {
            Bounds bounds = Bounds.Read(source, ref address);
            if(ltblFormat < LandtableFormat.SA2)
                address += 8; //sa1 has unused radius y and radius z values

            uint modelAddr = source.ToUInt32(address);
            if(modelAddr == 0)
                throw new InvalidOperationException("Landentry model address is null!");
            NJObject model = NJObject.Read(source, modelAddr - imageBase, imageBase, format, ltblFormat == LandtableFormat.SADX, labels, attaches);

            uint unknown = 0;
            uint blockBit;

            SurfaceAttributes attribs;
            if(ltblFormat == LandtableFormat.Buffer)
            {
                unknown = source.ToUInt32(address + 4);
                blockBit = source.ToUInt32(address + 8);
                attribs = (SurfaceAttributes)source.ToUInt32(address + 12);
            }
            else if(ltblFormat >= LandtableFormat.SA2)
            {
                unknown = source.ToUInt32(address + 4);
                blockBit = source.ToUInt32(address + 8);
                attribs = ((SA2SurfaceAttributes)source.ToUInt32(address + 12)).ToUniversal();
            }
            else
            {
                blockBit = source.ToUInt32(address + 4);
                attribs = ((SA1SurfaceAttributes)source.ToUInt32(address + 8)).ToUniversal();
            }

            return new(model, attribs, blockBit, unknown, bounds);
        }

        /// <summary>
        /// Writes model information of the geometry to a stream
        /// </summary>
        /// <param name="writer">Ouput stream</param>
        /// <param name="imagebase">Imagebase for all addresses</param>
        /// <param name="labels">Already written labels</param>
        public void WriteModel(EndianWriter writer, uint imagebase, Dictionary<string, uint> labels)
        {
            if(labels.ContainsKey(_model.Name))
                return;
            _model.Write(writer, imagebase, labels);
        }

        /// <summary>
        /// Writes landtable information of the geometry to a stream <br/>
        /// Note: <see cref="WriteModel(EndianWriter, uint, Dictionary{string, uint})"/> needs to have been called before
        /// </summary>
        /// <param name="writer">Output stream</param>
        /// <param name="format">Landtable format</param>
        public void Write(EndianWriter writer, LandtableFormat format, Dictionary<string, uint> labels)
        {
            if(!labels.ContainsKey(_model.Name))
                throw new InvalidOperationException("Model has not been written!");

            ModelBounds.Write(writer);
            if(format < LandtableFormat.SA2)
                writer.Write(new byte[8]); //sa1 has unused radius y and radius z values

            writer.Write(labels[_model.Name]);

            if(format == LandtableFormat.Buffer)
            {
                writer.WriteUInt32(Unknown);
                writer.WriteUInt32(BlockBit);
                writer.WriteUInt32((uint)SurfaceAttributes);
            }
            else if(format >= LandtableFormat.SA2)
            {
                writer.WriteUInt32(Unknown);
                writer.WriteUInt32(BlockBit);
                writer.WriteUInt32((uint)SurfaceAttributes.ToSA2());
            }
            else
            {
                writer.WriteUInt32(BlockBit);
                writer.WriteUInt32((uint)SurfaceAttributes.ToSA1());
            }
        }

        /// <summary>
        /// Creates a shallow copy of the landenty
        /// </summary>
        /// <returns></returns>
        public LandEntry ShallowCopy() =>
            // this works because the model doesn't (shouldn't) have a parent or children anyway
            new(_model.Duplicate(), SurfaceAttributes, BlockBit, Unknown, ModelBounds);

        public override string ToString() 
            => $"{Name} : {Attach} ";
    }
}
