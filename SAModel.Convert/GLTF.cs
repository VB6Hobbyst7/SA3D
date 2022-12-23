using Colourful;
using SATools.SAArchive;
using SATools.SACommon;
using SATools.SAModel.ModelData;
using SATools.SAModel.ModelData.Buffer;
using SATools.SAModel.ModelData.Weighted;
using SATools.SAModel.ObjData;
using SATools.SAModel.ObjData.Animation;
using SharpGLTF.Memory;
using SharpGLTF.Schema2;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using Color = SATools.SAModel.Structs.Color;

namespace SATools.SAModel.Convert
{
    /// <summary>
    /// Used to convert from or to the GLTF format
    /// </summary>
    public static class GLTF
    {
        #region Reading

        public readonly struct Contents
        {
            public ObjectNode Root { get; }

            public TextureSet Textures { get; }

            public Motion[] Animations { get; }

            public Contents(ObjectNode root, TextureSet textures, Motion[] animations)
            {
                Root = root;
                Textures = textures;
                Animations = animations;
            }
        }

        static readonly IColorConverter<LinearRGBColor, RGBColor> colorConverter
                = new ConverterBuilder().FromLinearRGB().ToRGB(RGBWorkingSpaces.sRGB).Build();


        public static Contents Read(string filepath, bool importTextures, float? animationFPS)
            => Read(ModelRoot.Load(filepath), importTextures, animationFPS);

        public static Contents Read(ModelRoot gltfModel, bool importTextures, float? animationFPS)
        {
            // First we'll get the textures, by far the easiest part
            TextureSet textures = null;
            if (importTextures && gltfModel.LogicalTextures.Count > 0)
            {
                textures = new();
                foreach (var t in gltfModel.LogicalTextures)
                {
                    string name = t.Name ?? $"Tex_{t}";
                    textures.Textures.Add(new SAArchive.Texture(name, GetBitmap(t.PrimaryImage.Content)));
                }
            }

            // lets first set up the object hierarchy
            Dictionary<Node, ObjectNode> objectsPairs = new();
            Dictionary<ObjectNode, Node> invertedObjectsPairs = new();

            List<ObjectNode> roots = new();
            foreach (var n in gltfModel.LogicalNodes)
            {
                if (n.VisualParent == null)
                {
                    roots.Add(FromNode(n, objectsPairs));
                }
            }

            foreach (var n in objectsPairs)
            {
                invertedObjectsPairs.Add(n.Value, n.Key);
            }

            if (roots.Count == 0)
                throw new InvalidDataException("GLTF contains no nodes");

            ObjectNode root;
            bool extraRoot = false;
            if (roots.Count > 1)
            {
                extraRoot = true;
                root = new ObjectNode()
                {
                    Name = "Root"
                };

                root.AddChildren(roots);
            }
            else
            {
                root = roots[0];
            }

            ObjectNode[] objects = root.GetObjects();

            //Note: not supporting reused meshes
            List<WeightedBufferAttach> weightedAttaches = new();

            foreach (ObjectNode njo in objects)
            {
                if (extraRoot && njo == root)
                    continue;

                Node node = invertedObjectsPairs[njo];

                if (node.Mesh == null)
                    continue;

                int[] skinMap;
                Matrix4x4 meshMatrix = node.GetWorldMatrix(null, 0);

                if (node.Skin != null)
                {
                    Skin skin = node.Skin;
                    skinMap = new int[skin.JointsCount];
                    for (int i = 0; i < skin.JointsCount; i++)
                    {
                        (Node bone, _) = skin.GetJoint(i);
                        skinMap[i] = Array.IndexOf(objects, objectsPairs[bone]);
                    }
                }
                else
                {
                    skinMap = new int[] { Array.IndexOf(objects, njo) };
                }

                weightedAttaches.Add(FromMesh(node.Mesh, skinMap, meshMatrix, objects));
            }

            WeightedBufferAttach.FromWeightedBuffer(root, weightedAttaches.ToArray(), true);

            // lastly, we load the animations
            Motion[] animations;
            if (animationFPS.HasValue)
            {
                animations = new Motion[gltfModel.LogicalAnimations.Count];

                for (int i = 0; i < animations.Length; i++)
                {
                    animations[i] = GetAnimation(gltfModel.LogicalAnimations[i], gltfModel.LogicalNodes.Count, animationFPS.Value);
                }
            }
            else
                animations = Array.Empty<Motion>();

            return new Contents(root, textures, animations);
        }

        private static ObjectNode FromNode(Node node, Dictionary<Node, ObjectNode> objects)
        {
            ObjectNode result = new();
            if (string.IsNullOrWhiteSpace(node.Name))
                result.Name = "Node_" + node.LogicalIndex;
            else
                result.Name = node.Name;

            result.Position = node.LocalTransform.Translation;
            result.QuaternionRotation = node.LocalTransform.Rotation;
            result.Scale = node.LocalTransform.Scale;

            foreach (var c in node.VisualChildren)
            {
                result.AddChild(FromNode(c, objects));
            }

            objects.Add(node, result);
            return result;
        }

        private static WeightedBufferAttach FromMesh(Mesh mesh, int[] skinMap, Matrix4x4 meshMatrix, ObjectNode[] nodes)
        {
            List<WeightedVertex> vertices = new();
            List<BufferCorner[]> corners = new();
            List<BufferMaterial> materials = new();

            foreach (var primitive in mesh.Primitives)
            {
                // Vertices
                primitive.VertexAccessors.TryGetValue("POSITION", /****/ out Accessor? positions);
                primitive.VertexAccessors.TryGetValue("NORMAL", /******/ out Accessor? normals);
                primitive.VertexAccessors.TryGetValue("WEIGHTS_0", /***/ out Accessor? weights);
                primitive.VertexAccessors.TryGetValue("JOINTS_0", /****/ out Accessor? joints);

                if (positions == null)
                    throw new NullReferenceException($"No positions in gltf mesh {mesh.Name}!");

                var positionArray = positions.AsVector3Array();
                var normalArray = normals?.AsVector3Array();
                var weightsArray = weights?.AsVector4Array();
                var jointsArray = joints?.AsVector4Array();

                int vertCount = positionArray.Count;
                WeightedVertex[] vertexSet = new WeightedVertex[vertCount];

                for (int i = 0; i < vertCount; i++)
                {
                    // position
                    var pos = Vector3.Transform(positionArray[i], meshMatrix);
                    var nrm = normalArray != null ? Vector3.TransformNormal(normalArray[i], meshMatrix) : Vector3.UnitY;

                    WeightedVertex vert = new(pos, nrm, nodes.Length);

                    if (weightsArray != null && jointsArray != null)
                    {
                        var joint = jointsArray[i];
                        var weight = weightsArray[i];

                        if (weight.X > 0)
                            vert.Weights[skinMap[(int)joint.X]] = weight.X;

                        if (weight.Y > 0)
                            vert.Weights[skinMap[(int)joint.Y]] = weight.Y;

                        if (weight.Z > 0)
                            vert.Weights[skinMap[(int)joint.Z]] = weight.Z;

                        if (weight.W > 0)
                            vert.Weights[skinMap[(int)joint.W]] = weight.W;
                    }
                    else
                    {
                        vert.Weights[skinMap[0]] = 1;
                    }

                    vertexSet[i] = vert;
                }


                // Polygon corners
                primitive.VertexAccessors.TryGetValue("TEXCOORD_0", /**/ out Accessor? uvs);
                primitive.VertexAccessors.TryGetValue("COLOR_0", /*****/ out Accessor? colors);

                var uvArray = uvs?.AsVector2Array();
                var colorArray = colors?.AsColorArray();
                int vertOffset = vertices.Count;

                BufferCorner[] meshCorners = new BufferCorner[vertCount];

                Func<int, int> GetIndex;

                if(vertexSet.CreateDistinctMap(out WeightedVertex[]? vertsDistinct, out int[]? vertMap))
                {
                    vertexSet = vertsDistinct;
                    GetIndex = (i) => vertMap[i];
                }
                else
                {
                    GetIndex = (i) => i;
                }

                for (int i = 0; i < meshCorners.Length; i++)
                {
                    Vector2 uv = uvArray?[i] ?? default;
                    Vector4 col = colorArray?[i] ?? Vector4.UnitW;

                    var linearCol = colorConverter.Convert(new(col.X, col.Y, col.Z));

                    int index = GetIndex(i);

                    meshCorners[i] = new(
                        (ushort)(GetIndex(i) + vertOffset), 
                        new((float)linearCol.R, (float)linearCol.G, (float)linearCol.B, col.W), 
                        uv);
                }

                // Read indices
                uint[] indices = GetIndices(primitive.IndexAccessor, meshCorners.Length, primitive.DrawPrimitiveType);

                if (indices != null)
                {
                    BufferCorner[] unwrappedCorners = new BufferCorner[indices.Length];
                    for (int i = 0; i < indices.Length; i++)
                    {
                        unwrappedCorners[i] = meshCorners[indices[i]];
                    }
                    meshCorners = unwrappedCorners;
                }

                vertices.AddRange(vertexSet);
                corners.Add(meshCorners);
                materials.Add(GetMaterial(primitive.Material));
            }

            return WeightedBufferAttach.Create(vertices.ToArray(), corners.ToArray(), materials.ToArray(), nodes);
        }

        private static uint[] GetIndices(Accessor indices, int vertexCount, PrimitiveType type)
        {
            uint[] result = null;
            if (indices != null)
            {
                var indexArray = indices.AsIndicesArray();
                result = new uint[indexArray.Count];
                indexArray.CopyTo(result, 0);
            }

            if (type == PrimitiveType.TRIANGLE_STRIP)
            {
                bool rev = false;
                uint index = 0;
                if (result == null)
                {
                    result = new uint[(vertexCount - 2) * 3];
                    for (int i = 0; i < result.Length; i += 3)
                    {
                        if (rev)
                        {
                            result[i] = index;
                            result[i + 1] = index + 1;
                        }
                        else
                        {
                            result[i] = index + 1;
                            result[i + 1] = index;
                        }
                        result[i + 2] = index + 2;

                        index++;
                    }
                }
                else
                {
                    uint[] newResult = new uint[(result.Length - 2) * 3];
                    for (int i = 0; i < result.Length; i += 3)
                    {
                        if (rev)
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
            else if (type != PrimitiveType.TRIANGLES)
                throw new InvalidOperationException($"Unsupported primitive type {type} in mesh");

            return result;
        }

        private static BufferMaterial GetMaterial(Material mat)
        {
            BufferMaterial result = new()
            {
                Diffuse = Color.White,
                Specular = Color.White,
                SpecularExponent = 32f
            };

            if (mat == null)
                return result;

            result.SetAttribute(MaterialAttributes.Flat, mat.Unlit);

            var channels = mat.Channels.ToArray();

            foreach (var c in channels)
            {
                if (c.Key == "BaseColor")
                {
                    if (c.Texture != null)
                    {
                        result.SetAttribute(MaterialAttributes.useTexture, true);
                        result.TextureIndex = (uint)c.Texture.LogicalIndex;

                        switch (c.TextureSampler.WrapS)
                        {
                            case TextureWrapMode.MIRRORED_REPEAT:
                                result.MirrorU = true;
                                result.ClampU = false;
                                break;
                            case TextureWrapMode.REPEAT:
                                result.ClampU = false;
                                break;
                        }

                        switch (c.TextureSampler.WrapT)
                        {
                            case TextureWrapMode.MIRRORED_REPEAT:
                                result.MirrorV = true;
                                result.ClampU = false;
                                break;
                            case TextureWrapMode.REPEAT:
                                result.ClampU = false;
                                break;
                        }

                        result.TextureFiltering = c.TextureSampler.MinFilter switch
                        {
                            TextureMipMapFilter.NEAREST
                            or TextureMipMapFilter.NEAREST_MIPMAP_NEAREST
                            or TextureMipMapFilter.LINEAR_MIPMAP_NEAREST
                                => FilterMode.PointSampled,
                            TextureMipMapFilter.LINEAR => FilterMode.Bilinear,
                            _ => FilterMode.Trilinear,
                        };
                    }
                    result.Diffuse = new((float)c.Parameters[0].Value, (float)c.Parameters[1].Value, (float)c.Parameters[2].Value, (float)c.Parameters[3].Value);
                }
                else if (c.Key == "MetallicRoughness")
                {
                    result.Specular = Color.Lerp(Color.White, result.Diffuse, (float)c.Parameters[0].Value);
                    result.SpecularExponent = (float)c.Parameters[1].Value;
                }
                else if (c.Key == "SpecularGlossiness")
                {
                    result.Specular = new((float)c.Parameters[0].Value, (float)c.Parameters[1].Value, (float)c.Parameters[2].Value);
                    result.SpecularExponent = (float)c.Parameters[3].Value;
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

            for (int i = 0; i < anim.Channels.Count; i++)
            {
                var channel = anim.Channels[i];

                if (!result.Keyframes.TryGetValue(channel.TargetNode.LogicalIndex, out Keyframes kframes))
                {
                    kframes = new();
                    result.Keyframes.Add(channel.TargetNode.LogicalIndex, kframes);
                }

                switch (channel.TargetNodePath)
                {
                    case PropertyPath.translation:
                        var posSampler = channel.GetTranslationSampler().CreateCurveSampler();
                        for (uint f = 0; f < frames; f++)
                            kframes.Position.Add(f, posSampler.GetPoint(keyframeStep * f));
                        break;
                    case PropertyPath.rotation:
                        var rotationSampler = channel.GetRotationSampler().CreateCurveSampler();
                        for (uint f = 0; f < frames; f++)
                            kframes.Quaternion.Add(f, rotationSampler.GetPoint(keyframeStep * f));
                        break;
                    case PropertyPath.scale:
                        var scaleSampler = channel.GetScaleSampler().CreateCurveSampler();
                        for (uint f = 0; f < frames; f++)
                            kframes.Scale.Add(f, scaleSampler.GetPoint(keyframeStep * f));
                        break;
                }

            }

            return result;
        }

        #endregion

    }
}
