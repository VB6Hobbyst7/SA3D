using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SATools.SAModel.ModelData.Weighted
{
    public struct WeightedVertex : IComparable<WeightedVertex>, IEquatable<WeightedVertex>
    {
        public Vector3 Position { get; set; }

        public Vector3 Normal { get; set; }

        public float[] Weights { get; set; }

        public bool HasWeights
            => Weights.Length > 0;

        public WeightedVertex(Vector3 position, Vector3 normal, int nodeCount)
        {
            Position = position;
            Normal = normal;
            Weights = new float[nodeCount];
        }

        public WeightedVertex(Vector3 position, Vector3 normal, float[] weights)
        {
            Position = position;
            Normal = normal;
            Weights = weights;
        }

        /// <summary>
        /// Raw value composite. Used in PythonNet
        /// </summary>
        public WeightedVertex(float px, float py, float pz, float nx, float ny, float nz, float[] weights)
        {
            Position = new(px, py, pz);
            Normal = new(nx, ny, nz);
            Weights = weights;
        }

        public int GetWeightCount()
        {
            int count = 0;
            for (int i = 0; i < Weights.Length; i++)
            {
                float weight = Weights[i];
                if (weight > 0f)
                {
                    count++;
                }
            }
            return count;
        }

        public (int nodeIndex, float weight)[] GetWeightMap()
        {
            List<(int nodeIndex, float weight)> result = new();
            for (int i = 0; i < Weights.Length; i++)
            {
                float weight = Weights[i];
                if (weight > 0f)
                {
                    result.Add((i, weight));
                }
            }
            return result.ToArray();
        }

        public int GetFirstWeightIndex()
        {
            for (int i = 0; i < Weights.Length; i++)
                if (Weights[i] > 0f)
                    return i;
            return -1;
        }

        public int GetLastWeightIndex()
        {
            for (int i = Weights.Length - 1; i >= 0; i--)
                if (Weights[i] > 0f)
                    return i;
            return -1;
        }

        public int GetMaxWeightIndex()
        {
            int result = -1;
            float weightCheck = 0;
            for (int i = 0; i < Weights.Length; i++)
            {
                float weight = Weights[i];
                if (weight > weightCheck)
                {
                    weightCheck = weight;
                    result = i;
                }
            }
            return result;
        }

        #region Comparisons

        public override bool Equals(object? obj)
        {
            return obj is WeightedVertex other
                && Position == other.Position
                && Normal == other.Normal
                && Weights.SequenceEqual(other.Weights);
        }

        public override int GetHashCode()
            => HashCode.Combine(Position, Normal, Weights);

        public int CompareTo(WeightedVertex other)
        {
            for (int i = 0; i < Weights.Length; i++)
            {
                float dif = Weights[i] - other.Weights[i];
                if (dif == 0f)
                    continue;

                return dif < 0f ? -1 : 1;
            }

            return 0;
        }

        public static bool operator ==(WeightedVertex left, WeightedVertex right)
            => left.Equals(right);

        public static bool operator !=(WeightedVertex left, WeightedVertex right)
            => !(left == right);

        bool IEquatable<WeightedVertex>.Equals(WeightedVertex other)
            => Equals(other);

        #endregion

        public override string ToString()
        {
            int weightCount = 0;
            string result = "";

            for (int i = 0; i < Weights.Length; i++)
            {
                float weight = Weights[i];
                if (weight == 0f)
                    continue;
                weightCount++;
                result += i + ", ";
            }

            return $"{weightCount} - {result}";
        }

        public WeightedVertex Clone()
            => new(Position, Normal, (float[])Weights.Clone());
    }
}
