using System.Collections.Generic;
using System.Linq;

namespace SATools.SAModel.ModelData.Weighted
{
    public interface IOffsetableAttachResult
    {
        public int VertexCount { get; }
        public int[] AttachIndices { get; }
        public Attach[] Attaches { get; }

        public void ModifyVertexOffset(int offset);

        /// <summary>
        /// Checks for any vertex overlaps in the models and sets their vertex offset accordingly
        /// </summary>
        public static void PlanVertexOffsets<T>(T[] attaches) where T : IOffsetableAttachResult
        {
            int nodeCount = attaches.Max(x => x.AttachIndices.Max()) + 1;
            List<(int start, int end)>[] ranges = new List<(int start, int end)>[nodeCount];
            for (int i = 0; i < nodeCount; i++)
                ranges[i] = new();

            foreach (IOffsetableAttachResult cr in attaches)
            {
                int startNode = cr.AttachIndices.Min();
                int endNode = cr.AttachIndices.Max();
                HashSet<(int start, int end)> blocked = new();

                for (int i = startNode; i <= endNode; i++)
                {
                    foreach (var r in ranges[i])
                        blocked.Add(r);
                }

                int lowestAvailableStart = 0xFFFF;

                if (blocked.Count == 0)
                {
                    lowestAvailableStart = 0;
                }
                else
                {
                    foreach (var (blockedStart, blockedEnd) in blocked)
                    {
                        if (blockedEnd >= lowestAvailableStart)
                            continue;

                        int checkStart = blockedEnd;
                        int checkEnd = checkStart + cr.VertexCount;
                        bool fits = true;
                        foreach (var (start, end) in blocked)
                        {
                            if (!(start > checkEnd || end < checkStart))
                            {
                                fits = false;
                                break;
                            }
                        }

                        if (fits)
                        {
                            lowestAvailableStart = blockedEnd;
                        }
                    }
                }

                int lowestAvailableEnd = lowestAvailableStart + cr.VertexCount;


                for (int i = startNode; i <= endNode; i++)
                {
                    ranges[i].Add((lowestAvailableStart, lowestAvailableEnd));
                }

                if (lowestAvailableStart > 0)
                {
                    cr.ModifyVertexOffset(lowestAvailableStart);
                }
            }
        }
    }
}
