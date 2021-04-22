using SATools.SAModel.ModelData;
using SATools.SAModel.ObjData.Animation;
using SATools.SAArchive;
using System;
using SharpGLTF.Schema2;
using SATools.SAModel.ModelData.Buffer;
using SATools.SAModel.ObjData;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Drawing;
using SharpGLTF.Memory;
using System.IO;
using SATools.SAModel.Structs;
using Color = SATools.SAModel.Structs.Color;

namespace SATools.SAModel.Convert
{
    /// <summary>
    /// Used to convert from an to the GLTF format
    /// </summary>
    public static class GLTF
    {

        public struct Contents
        {
            public NJObject Root { get; }

            public TextureSet Textures { get; }

            public Motion[] Animations { get; }

            public Contents(NJObject root, TextureSet textures, Motion[] animations)
            {
                Root = root;
                Textures = textures;
                Animations = animations;
            }
        }

        public static Contents Read(string filepath, AttachFormat format, bool importTextures, float? animationFPS)
            => Read(ModelRoot.Load(filepath), format, importTextures, animationFPS);

        public static Contents Read(ModelRoot gltfModel, AttachFormat format, bool importTextures, float? animationFPS)
        {
            // First we'll get the textures, definitely the easiest part
            TextureSet textures = null;
            if(importTextures)
            {
                textures = new();
                foreach(var t in gltfModel.LogicalTextures)
                {
                    string name = t.Name ?? $"Tex_{t}";
                    textures.Textures.Add(new SAArchive.Texture(name, GetBitmap(t.PrimaryImage.Content)));
                }
            }

            // lets first set up the object hierarchy
            Dictionary<Node, NJObject> objects = new();
            NJObject root = FromNode(gltfModel.LogicalNodes[0], objects);
            Dictionary<Mesh, Attach> nonWeightAttaches = new();

            NJObject[] objectList = new NJObject[gltfModel.LogicalNodes.Count];
            for(int i = 0; i < objectList.Length; i++)
                objectList[i] = objects[gltfModel.LogicalNodes[i]];

            foreach((Node node, NJObject njo) in objects)
            {
                if(node.Mesh == null)
                    continue;

                if(node.Skin == null)
                {
                    if(!nonWeightAttaches.TryGetValue(node.Mesh, out Attach atc))
                    {
                        atc = FromNoWeight(node.Mesh, format);
                        nonWeightAttaches.Add(node.Mesh, atc);
                    }
                    njo.Attach = atc;
                }
                else
                {
                    Skin skin = node.Skin;

                    NJObject[] bones = new NJObject[skin.JointsCount];

                    for(int i = 0; i < skin.JointsCount; i++)
                    {
                        (Node bone, _) = skin.GetJoint(i);
                        bones[i] = objects[bone];
                    }

                    var weightData = FromWeight(node.Mesh);
                    Matrix4x4 meshMatrix = node.GetWorldMatrix(null, 0);
                    AttachHelper.FromWeightedBuffer(bones, meshMatrix, weightData.vertices, weightData.polydata, format);
                }
            }

            // lastly, we load the animations
            Motion[] animations;
            if(animationFPS.HasValue)
            {
                animations = new Motion[gltfModel.LogicalAnimations.Count];

                for(int i = 0; i < animations.Length; i++)
                {
                    animations[i] = GetAnimation(gltfModel.LogicalAnimations[i], gltfModel.LogicalNodes.Count, animationFPS.Value);
                }
            }
            else
                animations = Array.Empty<Motion>();

            return new Contents(root, textures.Textures.Count == 0 ? null : textures, animations);
        }

        private static NJObject FromNode(Node node, Dictionary<Node, NJObject> objects)
        {
            NJObject result = new();
            if(string.IsNullOrWhiteSpace(node.Name))
                result.Name = "Node_" + node.LogicalIndex;
            else
                result.Name = node.Name;

            result.Position = node.LocalTransform.Translation;
            result.QuaternionRotation = node.LocalTransform.Rotation;
            result.Scale = node.LocalTransform.Scale;

            foreach(var c in node.VisualChildren)
            {
                result.AddChild(FromNode(c, objects));
            }

            objects.Add(node, result);
            return result;
        }

        private static Attach FromNoWeight(Mesh mesh, AttachFormat format)
        {
            List<BufferMesh> result = new(mesh.Primitives.Count);

            foreach(var primitive in mesh.Primitives)
            {
                // read vertices
                primitive.VertexAccessors.TryGetValue("POSITION", out Accessor positions);
                primitive.VertexAccessors.TryGetValue("NORMAL", out Accessor normals);

                var positionArray = positions.AsVector3Array();
                var normalArray = normals?.AsVector3Array();

                BufferVertex[] vertices = new BufferVertex[positionArray.Count];
                for(int i = 0; i < vertices.Length; i++)
                {
                    var pos = positionArray[i];
                    var nrm = normalArray?[i] ?? Vector3.UnitY;
                    vertices[i] = new(pos, nrm, (ushort)i);
                }

                // read corners
                primitive.VertexAccessors.TryGetValue("TEXCOORD_0", out Accessor uvs);
                primitive.VertexAccessors.TryGetValue("COLOR_0", out Accessor colors);

                var uvArray = uvs?.AsVector2Array();
                var colorArray = colors?.AsColorArray();

                BufferCorner[] corners = new BufferCorner[vertices.Length];
                for(int i = 0; i < corners.Length; i++)
                {
                    Vector2 uv = uvArray?[i] ?? default;
                    Vector4 col = colorArray?[i] ?? Vector4.UnitW;

                    corners[i] = new((ushort)i, new(col.X, col.Y, col.Z, col.W), uv);
                }

                // Read indices
                uint[] indices = GetIndices(primitive.IndexAccessor, positions.Count, primitive.DrawPrimitiveType);

                // convert material
                result.Add(new(vertices, false, corners, indices, GetMaterial(primitive.Material)));
            }

            Attach atc = AttachHelper.FromBufferMesh(result.ToArray(), format);
            atc.Name = mesh.Name;
            return atc;
        }
    
        private static (AttachHelper.VertexWeights[] vertices, BufferMesh[] polydata) FromWeight(Mesh mesh)
        {
            List<AttachHelper.VertexWeights> vertices = new();

            List<int> offsets = new();
            int offset = 0;

            foreach(var primitive in mesh.Primitives)
            {
                // read vertices
                primitive.VertexAccessors.TryGetValue("POSITION", out Accessor positions);
                primitive.VertexAccessors.TryGetValue("NORMAL", out Accessor normals);
                primitive.VertexAccessors.TryGetValue("WEIGHTS_0", out Accessor weights);
                primitive.VertexAccessors.TryGetValue("JOINTS_0", out Accessor joints);

                var positionArray = positions.AsVector3Array();
                var normalArray = normals?.AsVector3Array();
                var weightsArray = weights.AsVector4Array();
                var jointsArray = joints.AsVector4Array();

                int vertCount = positionArray.Count;

                for(int i = 0; i < vertCount; i++)
                {
                    // position
                    var pos = positionArray[i];

                    // normal (may be null)
                    var nrm = normalArray?[i] ?? Vector3.UnitY;

                    var joint = jointsArray[i];
                    var weight = weightsArray[i];

                    List<(int, float)> skinning = new();

                    if(weight.X > 0)
                        skinning.Add(((int)joint.X, weight.X));

                    if(weight.Y > 0)
                        skinning.Add(((int)joint.Y, weight.Y));

                    if(weight.Z > 0)
                        skinning.Add(((int)joint.Z, weight.Z));

                    if(weight.W > 0)
                        skinning.Add(((int)joint.W, weight.W));

                    vertices.Add(new(pos, nrm, skinning.ToArray()));
                }

                offsets.Add(offset);
                offset += positionArray.Count;
            }

            // first, lets put the poly meshes together
            BufferMesh[] polyMeshes = new BufferMesh[mesh.Primitives.Count];

            int offsetIndex = 0;
            foreach(var primitive in mesh.Primitives)
            {
                // getting positions again to get corner count
                var positions = primitive.VertexAccessors["POSITION"];

                // read corners
                primitive.VertexAccessors.TryGetValue("TEXCOORD_0", out Accessor uvs);
                primitive.VertexAccessors.TryGetValue("COLOR_0", out Accessor colors);

                var uvArray = uvs?.AsVector2Array();
                var colorArray = colors?.AsColorArray();

                BufferCorner[] corners = new BufferCorner[positions.Count];

                for(int i = 0; i < corners.Length; i++)
                {
                    Vector2 uv = uvArray?[i] ?? default;
                    Vector4 col = colorArray?[i] ?? Vector4.UnitW;
                    corners[i] = new((ushort)i, new(col.X, col.Y, col.Z, col.W), uv);
                }

                // Read indices
                uint[] indices = GetIndices(primitive.IndexAccessor, positions.Count, primitive.DrawPrimitiveType);

                ushort vertexReadOffset = (ushort)offsets[offsetIndex];
                polyMeshes[offsetIndex] = new(corners, indices, GetMaterial(primitive.Material), vertexReadOffset);
                offsetIndex++;
            }

            return (vertices.ToArray(), polyMeshes);
        }

        private static uint[] GetIndices(Accessor indices, int vertexCount, PrimitiveType type)
        {
            uint[] result = null;
            if(indices != null)
            {
                var indexArray = indices.AsIndicesArray();
                result = new uint[indexArray.Count];
                indexArray.CopyTo(result, 0);
            }

            if(type == PrimitiveType.TRIANGLE_STRIP)
            {
                bool rev = false;
                uint index = 0;
                if(result == null)
                {
                    result = new uint[(vertexCount - 2) * 3];
                    for(int i = 0; i < result.Length; i += 3)
                    {
                        if(rev)
                        {
                            result[i] = index;
                            result[i + 1] = index + 1;
                        }
                        else
                        {
                            result[i] = index + 1;
                            result[i + 1] = index;
                        }
                        result[i+2] = index + 2;

                        index++;
                    }
                }
                else
                {
                    uint[] newResult = new uint[(result.Length - 2) * 3];
                    for(int i = 0; i < result.Length; i += 3)
                    {
                        if(rev)
                        {
                            newResult[i] = result[index];
                            newResult[i + 1] = result[index + 1];
                        }
                        else
                        {
                            newResult[i] = result[index + 1];
                            newResult[i + 1] = result[index];
                        }
                        newResult[i + 2] = result[index + 2];

                        rev = !rev;
                        index++;
                    }
                }
                throw new NotImplementedException("Triangle strip todo!");
            }
            else if(type != PrimitiveType.TRIANGLES)
                throw new InvalidOperationException($"Unsupported primitive type {type} in mesh");

            return result;
        }

        private static BufferMaterial GetMaterial(Material mat)
        {
            BufferMaterial result = new();
            result.Diffuse = Color.White;
            result.Specular = Color.White;
            result.SetFlag(MaterialFlags.Flat, mat.Unlit);

            var channels = mat.Channels.ToArray();

            foreach(var c in channels)
            {
                if(c.Key == "BaseColor")
                {
                    if(c.Texture != null)
                    {
                        result.SetFlag(MaterialFlags.useTexture, true);
                        result.TextureIndex = (uint)c.Texture.LogicalIndex;

                        switch(c.TextureSampler.WrapS)
                        {
                            case TextureWrapMode.MIRRORED_REPEAT:
                                result.MirrorU = true;
                                result.WrapU = true;
                                break;
                            case TextureWrapMode.REPEAT:
                                result.WrapU = true;
                                break;
                        }

                        switch(c.TextureSampler.WrapT)
                        {
                            case TextureWrapMode.MIRRORED_REPEAT:
                                result.MirrorV = true;
                                result.WrapV = true;
                                break;
                            case TextureWrapMode.REPEAT:
                                result.WrapV = true;
                                break;
                        }

                        switch(c.TextureSampler.MinFilter)
                        {
                            case TextureMipMapFilter.NEAREST:
                            case TextureMipMapFilter.NEAREST_MIPMAP_NEAREST:
                            case TextureMipMapFilter.LINEAR_MIPMAP_NEAREST:
                                result.TextureFiltering = FilterMode.PointSampled;
                                break;
                            case TextureMipMapFilter.LINEAR:
                                result.TextureFiltering = FilterMode.Bilinear;
                                break;
                            case TextureMipMapFilter.DEFAULT:
                            default:
                            case TextureMipMapFilter.NEAREST_MIPMAP_LINEAR:
                            case TextureMipMapFilter.LINEAR_MIPMAP_LINEAR:
                                result.TextureFiltering = FilterMode.Trilinear;
                                break;
                        }
                    }
                    result.Diffuse = new(c.Parameter.X, c.Parameter.Y, c.Parameter.Z, c.Parameter.W);
                }
                else if(c.Key == "MetallicRoughness")
                {
                    result.Specular = Color.Lerp(Color.White, result.Diffuse, c.Parameter.X);
                    result.SpecularExponent = c.Parameter.Y;
                }
                else if(c.Key == "SpecularGlossiness")
                {
                    result.Specular = new(c.Parameter.X, c.Parameter.Y, c.Parameter.Z);
                    result.SpecularExponent = c.Parameter.W;
                }
            }

            return result;
        }

        private static Bitmap GetBitmap(MemoryImage image)
        {
            using Stream st = image.Open();
            return new Bitmap(st);
        }

        private static Motion GetAnimation(Animation anim, int modelCount, float fps)
        {
            uint frames = (uint)(fps * anim.Duration);
            float keyframeStep = 1f / fps;
            Motion result = new(frames, (uint)modelCount, InterpolationMode.Linear);
            result.PlaybackSpeed = fps;
            result.Name = anim.Name;

            for(int i = 0; i < anim.Channels.Count; i++)
            {
                var channel = anim.Channels[i];

                if(!result.Keyframes.TryGetValue(channel.TargetNode.LogicalIndex, out Keyframes kframes))
                {
                    kframes = new();
                    result.Keyframes.Add(channel.TargetNode.LogicalIndex, kframes);
                    kframes.WasQuaternion = true;
                }

                switch(channel.TargetNodePath)
                {
                    case PropertyPath.translation:
                        var posSampler = channel.GetTranslationSampler().CreateCurveSampler();
                        for(uint f = 0; f < frames; f++)
                            kframes.Position.Add(f, posSampler.GetPoint(keyframeStep * f));
                        break;
                    case PropertyPath.rotation:
                        var rotationSampler = channel.GetRotationSampler().CreateCurveSampler();
                        for(uint f = 0; f < frames; f++)
                            kframes.Rotation.Add(f, Vector3Extensions.FromQuaternion(rotationSampler.GetPoint(keyframeStep * f)));
                        break;
                    case PropertyPath.scale:
                        var scaleSampler = channel.GetScaleSampler().CreateCurveSampler();
                        for(uint f = 0; f < frames; f++)
                            kframes.Scale.Add(f, scaleSampler.GetPoint(keyframeStep * f));
                        break;
                }

            }

            return result;
        }

    }
}
