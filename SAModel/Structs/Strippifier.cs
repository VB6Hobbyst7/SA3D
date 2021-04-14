using System;
using System.Collections.Generic;
using System.Linq;

namespace SATools.SAModel.Structs
{
    /// <summary>
    /// Gets raised when certain 3D topology rules are ignored and/or not processable
    /// </summary>
    public class TopologyException : Exception
    {
        public TopologyException(string msg) : base(msg) { }
    }

    public class Strippifier
    {
        /// <summary>
        /// Whether to raise an exception when a topology anomaly occurs
        /// </summary>
        public static bool raiseTopoError;

        /// <summary>
        /// A single point in 3D space, used to create polygons
        /// </summary>
        private class Vertex
        {
            /// <summary>
            /// The index of this vertex
            /// </summary>
            public readonly int index;

            /// <summary>
            /// The edges connected to this vertex
            /// </summary>
            public Dictionary<Vertex, Edge> edges;

            /// <summary>
            /// The triangles of this vertex
            /// </summary>
            public List<Triangle> triangles;

            /// <summary>
            /// Returns the amount of triangles connected to this vertex that havent already been used
            /// </summary>
            public int AvailableTris => triangles.Count(x => x.used);

            /// <summary>
            /// Creates a new Vertex by index
            /// </summary>
            /// <param name="index"></param>
            public Vertex(int index)
            {
                this.index = index;
                edges = new Dictionary<Vertex, Edge>();
                triangles = new List<Triangle>();
            }

            /// <summary>
            /// Returns the edge between two vertices. If the edge doesnt exist, null is returned
            /// </summary>
            /// <param name="other">Vertex to check connection with</param>
            /// <returns></returns>
            public Edge IsConnectedWith(Vertex other)
            {
                if(edges.ContainsKey(other))
                    return edges[other];
                return null;
            }

            /// <summary>
            /// Connects a vertex with another and returns the connected edge
            /// </summary>
            /// <param name="other"></param>
            /// <returns></returns>
            public Edge Connect(Vertex other)
            {
                Edge e = new(this, other);
                edges.Add(other, e);
                other.edges.Add(this, e);
                return e;
            }
        }

        /// <summary>
        /// An edge between two <see cref="Vertex"/>
        /// </summary>
        private class Edge
        {
            /// <summary>
            /// The two vertices that the edge connects
            /// </summary>
            public readonly Vertex[] vertices;

            /// <summary>
            /// Triangles that the edge is part of
            /// </summary>
            public List<Triangle> triangles;

            /// <summary>
            /// Creates a new edge between two vertices
            /// </summary>
            /// <param name="v1"></param>
            /// <param name="v2"></param>
            public Edge(Vertex v1, Vertex v2)
            {
                vertices = new Vertex[] { v1, v2 };
                triangles = new List<Triangle>();
            }

            /// <summary>
            /// Adds a triangle to the edge
            /// </summary>
            /// <param name="tri"></param>
            public void AddTriangle(Triangle tri)
            {
                foreach(Triangle t in triangles)
                {
                    t.neighbours.Add(tri);
                    tri.neighbours.Add(t);
                }
                triangles.Add(tri);
            }
        }

        /// <summary>
        /// Triangle between three vertices
        /// </summary>
        private class Triangle
        {
            /// <summary>
            /// Index of th triangle
            /// </summary>
            public readonly int index;

            /// <summary>
            /// The three vertices that the triangle consists of
            /// </summary>
            public readonly Vertex[] vertices;

            /// <summary>
            /// The three edges that the triangle consists of
            /// </summary>
            public readonly Edge[] edges;

            /// <summary>
            /// The triangles that the triangle borders through the edges
            /// </summary>
            public List<Triangle> neighbours;

            /// <summary>
            /// Whether the triangle was already used in the strip
            /// </summary>
            public bool used;

            /// <summary>
            /// Neighbouring triangles that havent been used yet
            /// </summary>
            public Triangle[] AvailableNeighbours
            {
                get
                {
                    List<Triangle> result = new();
                    foreach(Triangle t in neighbours)
                        if(!t.used)
                            result.Add(t);
                    return result.ToArray();
                }
            }

            /// <summary>
            /// Creates a new triangle from an index and three vertices, and adds newly created edges to an output list
            /// </summary>
            /// <param name="index">Triangle index</param>
            /// <param name="vertices">Vertices (must be 3)</param>
            /// <param name="outEdges">Edge list of the entire mesh, to add new edges to</param>
            public Triangle(int index, Vertex[] vertices, List<Edge> outEdges)
            {
                this.index = index;
                this.vertices = vertices;
                edges = new Edge[3];
                neighbours = new List<Triangle>();
                used = false;

                Vertex prevVert = vertices[2];
                int i = 0;
                foreach(Vertex v in vertices)
                {
                    v.triangles.Add(this);
                    edges[i] = AddEdge(v, prevVert, outEdges);
                    prevVert = v;

                    i++;
                }

            }

            /// <summary>
            /// Creates
            /// </summary>
            /// <param name="v1"></param>
            /// <param name="v2"></param>
            /// <param name="edges"></param>
            /// <returns></returns>
            private Edge AddEdge(Vertex v1, Vertex v2, List<Edge> edges)
            {
                Edge e = v1.IsConnectedWith(v2);

                if(e == null)
                {
                    e = v1.Connect(v2);
                    edges.Add(e);
                }
                else if(raiseTopoError && e.triangles.Count > 1)
                {
                    throw new TopologyException("Some edge has more than 2 faces! Can't strippify!");
                }

                e.AddTriangle(this);

                return e;
            }

            /// <summary>
            /// Whether a vertex is contained in the triangle
            /// </summary>
            /// <param name="vert"></param>
            /// <returns></returns>
            public bool HasVertex(Vertex vert)
            {
                return vertices.Contains(vert);
            }

            /// <summary>
            /// Gets the third vertex in a triangle. <br/>
            /// If any of the two vertices are not part of the triangle, then the method returns null
            /// </summary>
            /// <param name="v1"></param>
            /// <param name="v2"></param>
            /// <returns></returns>
            public Vertex GetThirdVertex(Vertex v1, Vertex v2)
            {
                if(vertices.Contains(v1) && vertices.Contains(v2))
                    foreach(Vertex v in vertices)
                        if(v != v1 && v != v2)
                            return v;
                return null;
            }

            /// <summary>
            /// Reurns a shared edge two triangles
            /// </summary>
            /// <param name="other"></param>
            /// <returns></returns>
            public Edge GetSharedEdge(Triangle other)
            {
                foreach(Edge e in edges)
                    if(other.edges.Contains(e))
                        return e;
                return null;
            }

            /// <summary>
            /// Gets the next triangle in a strip sequence (with swapping)
            /// </summary>
            /// <param name="v1"></param>
            /// <param name="v2"></param>
            /// <returns></returns>
            public Triangle NextTriangleS(Vertex v1, Vertex v2)
            {
                Triangle[] trisToUse = AvailableNeighbours;

                if(trisToUse.Length == 0)
                    return null;
                if(trisToUse.Length == 1)
                    return trisToUse[0];

                int[] weights = new int[trisToUse.Length];
                int[] vConnection = new int[trisToUse.Length];
                int biggestConnection = 0;

                bool hasBase = v1 != null && v2 != null;

                int i = -1;
                foreach(Triangle t in trisToUse)
                {
                    i++;
                    weights[i] = t.AvailableNeighbours.Length;

                    if(weights[i] == 0)
                        return t;

                    if(hasBase)
                    {
                        // if swap is needed, add weight
                        if(t.HasVertex(v2))
                        {
                            weights[i] -= 1;
                            vConnection[i] = v1.AvailableTris;
                        }
                        else
                        {
                            weights[i] += 1;
                            vConnection[i] = v2.AvailableTris;
                        }
                    }
                    else
                    {
                        Vertex[] eVerts = t.GetSharedEdge(this).vertices;
                        vConnection[i] = eVerts[0].AvailableTris + eVerts[1].AvailableTris;
                    }

                    if(vConnection[i] > biggestConnection)
                        biggestConnection = vConnection[i];
                }

                i = -1;
                foreach(int v in vConnection)
                {
                    i++;
                    if(v < biggestConnection)
                        weights[i] -= 1;
                    else
                        weights[i] += 1;
                }

                int index = 0;
                for(int j = 1; j < trisToUse.Length; j++)
                {
                    if(weights[j] < weights[index]
                        || hasBase && weights[j] == weights[index] && trisToUse[j].HasVertex(v2))
                        index = j;
                }

                return trisToUse[index];
            }

            /// <summary>
            /// Gets the next triangle in a strip sequence (no swapping)
            /// </summary>
            /// <param name="v1"></param>
            /// <param name="v2"></param>
            /// <returns></returns>
            public Triangle NextTriangle(Vertex v1, Vertex v2)
            {
                Edge e = v1.IsConnectedWith(v2);

                foreach(Triangle t in e.triangles)
                    if(t != this && !t.used)
                        return t;
                return null;
            }

            /// <summary>
            /// Whether the culling directions between two triangles differ
            /// </summary>
            /// <param name="other"></param>
            /// <returns></returns>
            public bool HasBrokenCullFlow(Triangle other)
            {
                int t = 0;
                foreach(Vertex v in vertices)
                {
                    if(other.vertices.Contains(v))
                    {
                        int tt = Array.IndexOf(other.vertices, v);
                        return vertices[(t + 1) % 3] == other.vertices[(tt + 1) % 3];
                    }
                    t++;
                }
                return false;
            }
        }

        /// <summary>
        /// A mesh made of connections between triangles, egdes and vertices
        /// </summary>
        private class Mesh
        {
            /// <summary>
            /// All triangles in the mesh
            /// </summary>
            public Triangle[] triangles;

            /// <summary>
            /// All edges in the mesh
            /// </summary>
            public List<Edge> edges;

            /// <summary>
            /// all vertices in the mesh
            /// </summary>
            public Vertex[] vertices;

            /// <summary>
            /// Creates a new mesh from a triangle list
            /// </summary>
            /// <param name="triangleList"></param>
            public Mesh(int[] triangleList)
            {
                int vertCount = triangleList.Max(x => x) + 1;

                vertices = new Vertex[vertCount];

                for(int i = 0; i < vertCount; i++)
                {
                    vertices[i] = new Vertex(i);
                }

                edges = new List<Edge>();
                triangles = new Triangle[triangleList.Length / 3];

                for(int i = 0; i < triangles.Length; i++)
                {
                    int j = i * 3;
                    triangles[i] = new Triangle(i,
                        new Vertex[] {
                        vertices[triangleList[j]],
                        vertices[triangleList[j+1]],
                        vertices[triangleList[j+2]]
                            },
                        edges);
                }

                int triEdgeCount = edges.Count(x => x.triangles.Count > 2);
                if(triEdgeCount > 0)
                {
                    Console.WriteLine("Tripple edges: " + triEdgeCount);
                }
            }
        }

        /// <summary>
        /// Strippifies a triangle list and returns a 2D array, where each array is a single strip
        /// </summary>
        /// <param name="triList">Input triangles</param>
        /// <param name="doSwaps">Whether to use swaps</param>
        /// <param name="concat">Whether to concat the strips into one big strip (broken)</param>
        /// <returns></returns>
        public static int[][] Strip(int[] triList, bool doSwaps, bool concat)
        {
            Mesh mesh = new(triList);
            int written = 0;
            List<int[]> strips = new();

            int triCount = mesh.triangles.Length;

            void AddZTriangle(Triangle tri)
            {
                Vertex[] verts = tri.vertices;
                strips.Add(new int[] { verts[0].index, verts[2].index, verts[1].index });
                written++;
                tri.used = true;
            }

            Triangle getFirstTri()
            {
                Triangle resultTri = null;
                int curNCount = int.MaxValue;

                int i = -1;
                foreach(Triangle t in mesh.triangles)
                {
                    i++;
                    if(!t.used)
                    {
                        int tnCount = t.AvailableNeighbours.Length;
                        if(tnCount == 0)
                        {
                            AddZTriangle(t);
                            continue;
                        }
                        if(tnCount < curNCount)
                        {
                            if(tnCount == 1)
                                return t;
                            curNCount = tnCount;
                            resultTri = t;
                        }
                    }
                }

                return resultTri;
            }

            Triangle firstTri = getFirstTri();

            while(written != triCount)
            {

                Triangle currentTri = firstTri;
                currentTri.used = true;

                Triangle newTri = currentTri.NextTriangleS(null, null);

                if(currentTri.HasBrokenCullFlow(newTri))
                {
                    AddZTriangle(currentTri);
                    firstTri = getFirstTri();
                    continue;
                }

                newTri.used = true;

                Vertex[] sharedVerts = currentTri.GetSharedEdge(newTri).vertices;
                Vertex prevVert = currentTri.GetThirdVertex(sharedVerts[0], sharedVerts[1]);
                Triangle secNewTri = newTri.NextTriangleS(null, null);
                Vertex currentVert;
                Vertex nextVert;

                if(secNewTri == null)
                {
                    currentVert = sharedVerts[1];
                    nextVert = sharedVerts[0];

                    strips.Add(new int[] { prevVert.index, currentVert.index, nextVert.index, newTri.GetThirdVertex(currentVert, nextVert).index });
                    written += 2;

                    firstTri = getFirstTri();
                    continue;
                }
                else if(secNewTri.HasVertex(sharedVerts[0]))
                {
                    currentVert = sharedVerts[1];
                    nextVert = sharedVerts[0];
                }
                else
                {
                    currentVert = sharedVerts[0];
                    nextVert = sharedVerts[1];
                }

                List<int> strip = new()
                { prevVert.index, currentVert.index, nextVert.index };
                written++;

                prevVert = nextVert;
                currentVert = newTri.GetThirdVertex(currentVert, nextVert);
                currentTri = newTri;
                newTri = currentTri.HasBrokenCullFlow(secNewTri) ? null : secNewTri;
                Triangle swapTri = null;

                bool reachedEnd = false;
                bool reversedList = false;

                while(!reachedEnd)
                {
                    strip.Add(currentVert.index);
                    written++;

                    if(newTri == null)
                    {
                        if(!reversedList && firstTri.AvailableNeighbours.Length > 0)
                        {
                            reversedList = true;
                            prevVert = mesh.vertices[strip[1]];
                            currentVert = mesh.vertices[strip[0]];
                            if(doSwaps && false) // its broken, so imma disable it rn
                            {
                                newTri = firstTri.NextTriangleS(prevVert, currentVert);
                            }
                            else
                            {
                                newTri = firstTri.NextTriangle(prevVert, currentVert);
                                if(newTri == null)
                                {
                                    reachedEnd = true;
                                    continue;
                                }
                            }
                            strip.Reverse();

                            Triangle tTri = firstTri;
                            firstTri = currentTri;
                            currentTri = tTri;
                        }
                        else
                        {
                            reachedEnd = true;
                            continue;
                        }
                    }

                    if(doSwaps)
                    {
                        swapTri = newTri.NextTriangle(prevVert, currentVert);
                        if(swapTri != null && secNewTri.HasVertex(currentVert))
                        {
                            strip.Add(prevVert.index);

                            Vertex t = prevVert;
                            prevVert = currentVert;
                            currentVert = t;
                        }
                    }

                    nextVert = newTri.GetThirdVertex(prevVert, currentVert);

                    if(nextVert == null)
                    {
                        reachedEnd = true;
                        continue;
                    }

                    prevVert = currentVert;
                    currentVert = nextVert;

                    Triangle oldTri = currentTri;
                    currentTri = newTri;
                    currentTri.used = true;

                    if(oldTri.HasBrokenCullFlow(currentTri))
                        newTri = null;
                    else if(doSwaps)
                        newTri = swapTri;
                    else
                        newTri = currentTri.NextTriangle(prevVert, currentVert);
                }

                for(int i = 0; i < 3; i++)
                {
                    if(firstTri.vertices[0].index == strip[i])
                    {
                        if(firstTri.vertices[1].index == strip[(i + 1) % 3])
                        {
                            strip.Insert(0, strip[0]);
                        }
                        break;
                    }
                }

                strips.Add(strip.ToArray());

                firstTri = getFirstTri();
            }

            int[][] result;

            if(concat) // TODO strippifier Concat is broken
            {
                List<int> tResult = new(strips[0]);
                if(strips.Count > 1)
                {
                    for(int i = 1; i < strips.Count; i++)
                    {
                        tResult.Add(strips[i - 1].Last());
                        tResult.Add(strips[i][0]);
                        tResult.AddRange(strips[i]);
                    }
                }
                result = new int[][] { tResult.ToArray() };
            }
            else
                result = strips.ToArray();

            return result;
        }
    }
}