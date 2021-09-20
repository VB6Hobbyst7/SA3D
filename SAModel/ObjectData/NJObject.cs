using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using SATools.SAModel.ModelData;
using SATools.SAModel.Structs;
using static SATools.SACommon.ByteConverter;
using static SATools.SACommon.StringExtensions;
using SATools.SACommon;
using System.Collections.ObjectModel;

namespace SATools.SAModel.ObjData
{
    /// <summary>
    /// Hierarchy object for every adventure game
    /// </summary>
    [Serializable]
    public class NJObject
    {
        private Vector3 _position;

        private Vector3 _rotation;

        private Quaternion _quaternionRotation = Quaternion.Identity;

        private Vector3 _scale = Vector3.One;

        internal Attach _attach;

        public const uint Size = 0x34;

        /// <summary>
        /// Name / C struct label identifier of the object
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Model data of the object
        /// </summary>
        public Attach Attach
        {
            get => _attach;
            set
            {
                foreach(NJObject o in GetObjects())
                {
                    if(o == this || o.Attach == null)
                        continue;
                    if(o.Attach.Format != _attach.Format)
                        throw new FormatException($"Format of the attach doesnt match! Model is { o.Attach.Format }, while the new attach is { _attach.Format }");
                }
                _attach = value;
            }
        }

        /// <summary>
        /// The attach format of the model
        /// </summary>
        public AttachFormat AttachFormat
        {
            get
            {
                if(Attach != null)
                    return Attach.Format;

                foreach(NJObject obj in GetObjects())
                {
                    if(obj.Attach == null)
                        continue;
                    return obj.Attach.Format;
                }
                return AttachFormat.Buffer; // buffer is equal to none
            }
        }

        /// <summary>
        /// Local Position of the Object
        /// </summary>
        public Vector3 Position
        {
            get => _position;
            set
            {
                _position = value;
                UpdateMatrix();
            }
        }

        /// <summary>
        /// Local Rotation of the Object
        /// </summary>
        public Vector3 Rotation
        {
            get => _rotation;
            set
            {
                _rotation = value;
                _quaternionRotation = QuaternionExtensions.FromEuler(value, RotateZYX);
                UpdateMatrix();
            }
        }

        public Quaternion QuaternionRotation
        {
            get => _quaternionRotation;
            set
            {
                _quaternionRotation = value;
                _rotation = QuaternionExtensions.ToEuler(value, RotateZYX);
                UpdateMatrix();
            }
        }

        /// <summary>
        /// Local Scale of the Object
        /// </summary>
        public Vector3 Scale
        {
            get => _scale;
            set
            {
                _scale = value;
                UpdateMatrix();
            }
        }

        public Matrix4x4 LocalMatrix { get; private set; }
            = Matrix4x4.Identity;

        /// <summary>
        /// The objects parent in the hierarchy
        /// </summary>
        public NJObject Parent { get; private set; }

        /// <summary>
        /// Returns the Sibling of this object. returns null if it has no sibling
        /// </summary>
        private NJObject Sibling
        {
            get
            {
                if(Parent == null)
                    return null;
                int index = Parent._children.IndexOf(this);
                if(index == -1 || index == Parent.ChildCount - 1)
                    return null;
                else
                    return Parent._children[index + 1];
            }
        }

        private List<NJObject> _children;

        /// <summary>
        /// Children objects in the hierarchy
        /// </summary>
        public ReadOnlyCollection<NJObject> Children { get; }

        public int ChildCount 
            => _children.Count;

        /// <summary>
        /// Whether the euler order is "inverted"
        /// </summary>
        public bool RotateZYX { get; private set; }

        /// <summary>
        /// Whether the object can be influenced by animations
        /// </summary>
        public bool Animate { get; set; } = true;

        /// <summary>
        /// Whether the object can be influenced by morphs
        /// </summary>
        public bool Morph { get; set; } = true;

        /// <summary>
        /// Whether this
        /// </summary>
        public bool HasWeight
        {
            get
            {
                if(Attach?.HasWeight == true)
                    return true;
                foreach(NJObject obj in _children)
                    if(obj.HasWeight)
                        return true;
                return false;
            }
        }

        /// <summary>
        /// Various attributes summarizing the data inside of the object
        /// </summary>
        public ObjectAttributes Attributes
        {
            get
            {
                ObjectAttributes r = 0;

                if(Position == Vector3.Zero)
                    r |= ObjectAttributes.NoPosition;
                if(Rotation == Vector3.Zero)
                    r |= ObjectAttributes.NoRotation;
                if(Scale == Vector3.One)
                    r |= ObjectAttributes.NoScale;
                if(Attach == null)
                    r |= ObjectAttributes.NoDisplay;
                if(ChildCount == 0)
                    r |= ObjectAttributes.NoChildren;
                if(RotateZYX)
                    r |= ObjectAttributes.RotateZYX;
                if(!Animate)
                    r |= ObjectAttributes.NoAnimate;
                if(!Morph)
                    r |= ObjectAttributes.NoMorph;

                return r;
            }
        }

        /// <summary>
        /// Returns a child by the index
        /// </summary>
        /// <param name="index">Child index</param>
        /// <returns></returns>
        public NJObject this[int index] 
            => _children[index];

        /// <summary>
        /// Creates an emty object
        /// </summary>
        public NJObject()
        {
            Name = "object_" + GenerateIdentifier();
            _children = new List<NJObject>();
            Children = new(_children);
        }

        /// <summary>
        /// Creates an empty object and sets its parent
        /// </summary>
        /// <param name="Parent"></param>
        public NJObject(NJObject Parent) : this()
        {
            if(Parent == null)
                return;
            this.Parent = Parent;
            Parent._children.Add(this);
        }

        private void UpdateMatrix() 
            => LocalMatrix = QuaternionExtensions.CreateTransformMatrix(_position, _quaternionRotation, _scale);

        /// <summary>
        /// Sets the rotation order
        /// </summary>
        /// <param name="newValue">New rotation order state</param>
        /// <param name="updateRotation">Update the euler rotation, so that the order but not the rotation changes</param>
        public void SetRotationZYX(bool newValue, bool updateRotation = true)
        {
            if(RotateZYX == newValue)
                return;

            RotateZYX = newValue;

            if(updateRotation)
                _rotation = QuaternionExtensions.ToEuler(_quaternionRotation, newValue);
        }

        /// <summary>
        /// Reads an NJS object hierarchy from a byte array
        /// </summary>
        /// <param name="source">Byte source</param>
        /// <param name="address">Address at which the object is located</param>
        /// <param name="imageBase">Image base for all addresses</param>
        /// <param name="format">Attach format to read</param>
        /// <param name="labels">C struct labels</param>
        /// <param name="attaches">Already read attaches</param>
        /// <returns></returns>
        public static NJObject Read(byte[] source, uint address, uint imageBase, AttachFormat format, bool DX, Dictionary<uint, string> labels, Dictionary<uint, Attach> attaches) 
            => Read(source, address, imageBase, format, DX, null, labels, attaches);

        private static NJObject Read(byte[] source, uint address, uint imageBase, AttachFormat format, bool DX, NJObject parent, Dictionary<uint, string> labels, Dictionary<uint, Attach> attaches)
        {
            string name = labels.ContainsKey(address) ? labels[address] : "object_" + address.ToString("X8");

            // reading object attributes
            ObjectAttributes attribs = (ObjectAttributes)source.ToInt32(address);
            bool rotateZYX = attribs.HasFlag(ObjectAttributes.RotateZYX);
            bool animate = !attribs.HasFlag(ObjectAttributes.NoAnimate);
            bool morph = !attribs.HasFlag(ObjectAttributes.NoMorph);

            // reading the attach
            Attach atc = null;
            uint tmpaddr = source.ToUInt32(address += 4);
            if(tmpaddr != 0)
            {
                tmpaddr -= imageBase;
                if(attaches.ContainsKey(tmpaddr))
                    atc = attaches[tmpaddr];
                else
                {
                    atc = Attach.Read(format, source, tmpaddr, imageBase, DX, labels);
                    attaches.Add(tmpaddr, atc);
                }

            }

            // reading transform data
            address += 4;
            Vector3 position = Vector3Extensions.Read(source, ref address, IOType.Float);
            Vector3 rotation = Vector3Extensions.Read(source, ref address, IOType.BAMS32);
            Vector3 scale = Vector3Extensions.Read(source, ref address, IOType.Float);

            NJObject result = new(parent)
            {
                Name = name,
                Attach = atc,
                Position = position,
                Rotation = rotation,
                Scale = scale,
                RotateZYX = rotateZYX,
                Animate = animate,
                Morph = morph

            };

            // reading child | parent and child get set in the constructor
            tmpaddr = source.ToUInt32(address);
            if(tmpaddr != 0)
                Read(source, tmpaddr - imageBase, imageBase, format, DX, result, labels, attaches);

            // reading sibling | parent and child get set in the constructor
            tmpaddr = source.ToUInt32(address + 4);
            if(tmpaddr != 0)
                Read(source, tmpaddr - imageBase, imageBase, format, DX, parent, labels, attaches);

            return result;
        }
        /// <summary>
        /// Writes object (not its children) to a byte stream
        /// </summary>
        /// <param name="writer">Output stream</param>
        /// <param name="imageBase">Image base for all addresses</param>
        /// <param name="DX">Whether to write for SADX</param>
        /// <param name="labels">C struct labels</param>
        /// <returns></returns>
        public uint Write(EndianWriter writer, uint imageBase, Dictionary<string, uint> labels)
        {
            uint address = writer.Position + imageBase;

            writer.WriteUInt32((uint)Attributes);
            writer.WriteUInt32(Attach == null ? 0 : labels.ContainsKey(Attach.Name) ? labels[Attach.Name] : throw new NullReferenceException($"Attach \"{Attach.Name}\" of \"{Name}\" has not been written yet / cannot be found in labels!"));

            Position.Write(writer, IOType.Float);
            Rotation.Write(writer, IOType.BAMS32);
            Scale.Write(writer, IOType.Float);

            writer.WriteUInt32(_children.Count == 0 ? 0 : labels.ContainsKey(_children[0].Name) ? labels[_children[0].Name] : throw new NullReferenceException($"Child \"{_children[0].Name}\" of \"{Name}\" has not been written yet / cannot be found in labels!"));
            NJObject sibling = Sibling;
            writer.WriteUInt32(sibling == null ? 0 : labels.ContainsKey(sibling.Name) ? labels[sibling.Name] : throw new NullReferenceException($"Sibling \"{sibling.Name}\" of \"{Name}\" has not been written yet / cannot be found in labels!"));

            labels.AddLabel(Name, address);
            return address;
        }

        /// <summary>
        /// Writes the entire model hierarchy with attaches to a stream
        /// </summary>
        /// <param name="writer">Output stream</param>
        /// <param name="imageBase">Image base for all addresses</param>
        /// <param name="DX">Whether the hierarchy is for SADX</param>
        /// <param name="labels">C struct labels</param>
        /// <returns></returns>
        public uint WriteHierarchy(EndianWriter writer, uint imageBase, bool DX, bool writeBuffer, Dictionary<string, uint> labels)
        {
            // reserve object space
            uint address = writer.Position + imageBase;

            NJObject[] models = GetObjects();
            writer.Write(new byte[models.Length * Size]);
            uint modelsEnd = writer.Position;

            Attach[] attaches = models.Where(x => x.Attach != null).Select(x => x.Attach).ToArray();

            // write attaches
            foreach(var atc in attaches)
            {
                if(labels.ContainsKey(atc.Name))
                    continue;
                
                if(writeBuffer)
                    atc.WriteBuffer(writer, imageBase, labels);
                else
                    atc.Write(writer, imageBase, DX, labels);
            }

            // write models, but in reverse order
            writer.Stream.Seek(modelsEnd - Size, SeekOrigin.Begin);
            for(int i = models.Length - 1; i > 0; i--)
            {
                models[i].Write(writer, imageBase, labels);
                writer.Stream.Seek((int)Size * -2, SeekOrigin.Current);
            }
            models[0].Write(writer, imageBase, labels);
            writer.Stream.Seek(0, SeekOrigin.End);

            return address;
        }

        /// <summary>
        /// Writes the object as an NJA struct
        /// </summary>
        /// <param name="writer">Output stream</param>
        /// <param name="labels">C struct labels that have already been written</param>
        public void WriteNJA(TextWriter writer, List<string> labels)
        {
            labels.Add(Name);

            writer.Write("OBJECT ");
            writer.Write(Name);
            writer.WriteLine("[]");

            writer.WriteLine("START");

            writer.Write("EvalFlags \t( ");
            writer.Write(((NJD_EVAL)Attributes).ToString().Replace(", ", " | "));
            writer.WriteLine("),");

            writer.Write("Model \t\t");
            writer.Write(Attach != null ? Attach.Name : "NULL");
            writer.WriteLine(",");

            writer.Write("OPosition \t");
            Position.WriteNJA(writer, IOType.Float);
            writer.WriteLine(",");

            writer.Write("OAngle \t\t");
            Rotation.WriteNJA(writer, IOType.Float);
            writer.WriteLine(",");

            writer.Write("OScale \t\t");
            Scale.WriteNJA(writer, IOType.Float);
            writer.WriteLine(",");

            writer.Write("Child \t\t");
            writer.Write(ChildCount == 0 ? "NULL" : _children[0].Name);
            writer.WriteLine(",");

            NJObject sibling = Sibling;
            writer.Write("Sibling \t");
            writer.WriteLine(sibling == null ? "NULL" : sibling.Name);

            writer.WriteLine("END");
            writer.WriteLine();
        }

        /// <summary>
        /// Converts the entire Model to a different attach format
        /// </summary>
        /// <param name="newAttachFormat">The attach format to convert to</param>
        /// <param name="optimize">Optimize the converted data</param>
        /// <param name="ignoreWeights">Convert regardless of weight information being lost</param>
        public void ConvertAttachFormat(AttachFormat newAttachFormat, bool optimize, bool ignoreWeights = false, bool forceUpdate = false)
        {
            switch(newAttachFormat)
            {
                case AttachFormat.Buffer:
                    switch(AttachFormat)
                    {
                        case AttachFormat.Buffer:
                            return;
                        case AttachFormat.BASIC:
                            ModelData.BASIC.BasicAttachConverter.ConvertModelFromBasic(this, optimize);
                            break;
                        case AttachFormat.CHUNK:
                            ModelData.CHUNK.ChunkAttachConverter.ConvertModelFromChunk(this, optimize);
                            break;
                        case AttachFormat.GC:
                            ModelData.GC.GCAttachConverter.ConvertModelFromGC(this, optimize);
                            break;
                    }
                    break;
                case AttachFormat.BASIC:
                    ModelData.BASIC.BasicAttachConverter.ConvertModelToBasic(this, optimize, ignoreWeights, forceUpdate);
                    break;
                case AttachFormat.CHUNK:
                    throw new NotImplementedException();
                case AttachFormat.GC:
                    ModelData.GC.GCAttachConverter.ConvertModelToGC(this, optimize, ignoreWeights, forceUpdate);
                    break;
            }
        }

        #region hierarchy stuff

        /// <summary>
        /// Calculates the world matrix for this object (recursive)
        /// </summary>
        /// <returns></returns>
        public Matrix4x4 GetWorldMatrix()
        {
            Matrix4x4 local = LocalMatrix;

            if(Parent != null)
                local *= Parent.GetWorldMatrix();

            return local;
        }

        /// <summary>
        /// Returns an array of the entire NjsObject hierarchy starting at this object
        /// </summary>
        /// <returns></returns>
        public NJObject[] GetObjects()
        {
            List<NJObject> result = new();
            GetObjects(result);
            return result.ToArray();
        }

        /// <summary>
        /// Returns the amount of objects in the hierarchy starting at this objec
        /// </summary>
        /// <returns></returns>
        public int Count()
        {
            int result = 1;
            foreach(NJObject item in _children)
                result += item.Count();
            return result;
        }

        /// <summary>
        /// Returns the amount of objects in the hierarchy starting at this object that are animated
        /// </summary>
        /// <returns></returns>
        public int CountAnimated()
        {
            int result = Animate ? 1 : 0;
            foreach(NJObject item in _children)
                result += item.CountAnimated();
            return result;
        }

        /// <summary>
        /// Returns the amount of objects in the hierarchy starting at this object that are morphing
        /// </summary>
        /// <returns></returns>
        public int CountMorph()
        {
            int result = Morph ? 1 : 0;
            foreach(NJObject item in _children)
                result += item.CountMorph();
            return result;
        }

        /// <summary>
        /// Check if this object or the hierarchy below has a specific name
        /// </summary>
        /// <param name="name">Name to check</param>
        /// <returns></returns>
        public bool ContainsName(string name)
        {
            if(Name == name)
                return true;

            foreach(NJObject item in _children)
                if(item.ContainsName(name))
                    return true;

            return false;
        }

        private void GetObjects(List<NJObject> result)
        {
            result.Add(this);
            foreach(NJObject item in _children)
                result.AddRange(item.GetObjects());
        }

        /// <summary>
        /// Adds an object to the children
        /// </summary>
        /// <param name="child"></param>
        public void AddChild(NJObject child)
        {
            _children.Add(child);
            child.Parent = this;
        }

        /// <summary>
        /// Adds a collection of objects to the children
        /// </summary>
        /// <param name="children"></param>
        public void AddChildren(IEnumerable<NJObject> children)
        {
            foreach(NJObject child in children)
                AddChild(child);
        }

        /// <summary>
        /// Inserts a child at a specific index
        /// </summary>
        /// <param name="index"></param>
        /// <param name="child"></param>
        public void InsertChild(int index, NJObject child)
        {
            _children.Insert(index, child);
            child.Parent = this;
        }

        /// <summary>
        /// Removes a child at a specific index
        /// </summary>
        /// <param name="child"></param>
        public void RemoveChild(NJObject child)
        {
            _children.Remove(child);
            child.Parent = null;
        }

        /// <summary>
        /// Removes a child at a specific index
        /// </summary>
        /// <param name="index"></param>
        public void RemoveChildAt(int index)
        {
            NJObject child = _children[index];
            _children.RemoveAt(index);
            child.Parent = null;
        }

        /// <summary>
        /// Clears the children
        /// </summary>
        public void ClearChildren()
        {
            foreach(NJObject child in _children)
                child.Parent = null;
            _children.Clear();
        }

        #endregion

        /// <summary>
        /// Creates a duplicate of the model, which will act as a new sibling to the old model
        /// </summary>
        /// <returns></returns>
        public NJObject Duplicate()
        {
            NJObject result = (NJObject)MemberwiseClone();
            result.Name += "_Clone";
            result._children = new List<NJObject>();
            Parent?.AddChild(result);
            return result;
        }

        public override string ToString() => Attach == null ? $"{Name}; / - /" : $"{Name}; {Attach.Format} - {Attach.Name}";
    }
}
