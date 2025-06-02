using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.ComponentModel;
using System.Linq;
using System.Diagnostics;
using System.Windows.Forms.VisualStyles;
using System.IO;
using System.Buffers;
///Algorithms Project
///Intelligent Scissors
///

namespace ImageTemplate
{
    /// <summary>
    /// Holds the pixel color in 3 byte values: red, green and blue
    /// </summary>
    public struct RGBPixel
    {
        public byte red, green, blue;
    }
    public struct RGBPixelD
    {
        public double red, green, blue;
    }


    /// <summary>
    /// Library of static functions that deal with images
    /// </summary>
    public class ImageOperations
    {
        /// <summary>
        /// Open an image and load it into 2D array of colors (size: Height x Width)
        /// </summary>
        /// <param name="ImagePath">Image file path</param>
        /// <returns>2D array of colors</returns>
        public static RGBPixel[,] OpenImage(string ImagePath)
        {
            Bitmap original_bm = new Bitmap(ImagePath);
            int Height = original_bm.Height;
            int Width = original_bm.Width;

            RGBPixel[,] Buffer = new RGBPixel[Height, Width];

            unsafe
            {
                BitmapData bmd = original_bm.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, original_bm.PixelFormat);
                int x, y;
                int nWidth = 0;
                bool Format32 = false;
                bool Format24 = false;
                bool Format8 = false;

                if (original_bm.PixelFormat == PixelFormat.Format24bppRgb)
                {
                    Format24 = true;
                    nWidth = Width * 3;
                }
                else if (original_bm.PixelFormat == PixelFormat.Format32bppArgb || original_bm.PixelFormat == PixelFormat.Format32bppRgb || original_bm.PixelFormat == PixelFormat.Format32bppPArgb)
                {
                    Format32 = true;
                    nWidth = Width * 4;
                }
                else if (original_bm.PixelFormat == PixelFormat.Format8bppIndexed)
                {
                    Format8 = true;
                    nWidth = Width;
                }
                int nOffset = bmd.Stride - nWidth;
                byte* p = (byte*)bmd.Scan0;
                for (y = 0; y < Height; y++)
                {
                    for (x = 0; x < Width; x++)
                    {
                        if (Format8)
                        {
                            Buffer[y, x].red = Buffer[y, x].green = Buffer[y, x].blue = p[0];
                            p++;
                        }
                        else
                        {
                            Buffer[y, x].red = p[2];
                            Buffer[y, x].green = p[1];
                            Buffer[y, x].blue = p[0];
                            if (Format24) p += 3;
                            else if (Format32) p += 4;
                        }
                    }
                    p += nOffset;
                }
                original_bm.UnlockBits(bmd);
            }

            return Buffer;
        }

        /// <summary>
        /// Get the height of the image 
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <returns>Image Height</returns>
        public static int GetHeight(RGBPixel[,] ImageMatrix)
        {
            return ImageMatrix.GetLength(0);
        }

        /// <summary>
        /// Get the width of the image 
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <returns>Image Width</returns>
        public static int GetWidth(RGBPixel[,] ImageMatrix)
        {
            return ImageMatrix.GetLength(1);
        }

        /// <summary>
        /// Display the given image on the given PictureBox object
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <param name="PicBox">PictureBox object to display the image on it</param>
        public static void DisplayImage(RGBPixel[,] ImageMatrix, PictureBox PicBox)
        {
            // Create Image:
            //==============
            int Height = ImageMatrix.GetLength(0);
            int Width = ImageMatrix.GetLength(1);

            Bitmap ImageBMP = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);

            unsafe
            {
                BitmapData bmd = ImageBMP.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, ImageBMP.PixelFormat);
                int nWidth = 0;
                nWidth = Width * 3;
                int nOffset = bmd.Stride - nWidth;
                byte* p = (byte*)bmd.Scan0;
                for (int i = 0; i < Height; i++)
                {
                    for (int j = 0; j < Width; j++)
                    {
                        p[2] = ImageMatrix[i, j].red;
                        p[1] = ImageMatrix[i, j].green;
                        p[0] = ImageMatrix[i, j].blue;
                        p += 3;
                    }

                    p += nOffset;
                }
                ImageBMP.UnlockBits(bmd);
            }
            PicBox.Image = ImageBMP;
        }


        /// <summary>
        /// Apply Gaussian smoothing filter to enhance the edge detection 
        /// </summary>
        /// <param name="ImageMatrix">Colored image matrix</param>
        /// <param name="filterSize">Gaussian mask size</param>
        /// <param name="sigma">Gaussian sigma</param>
        /// <returns>smoothed color image</returns>
        public static RGBPixel[,] GaussianFilter1D(RGBPixel[,] ImageMatrix, int filterSize, double sigma)
        {
            int Height = GetHeight(ImageMatrix);
            int Width = GetWidth(ImageMatrix);

            RGBPixelD[,] VerFiltered = new RGBPixelD[Height, Width];
            RGBPixel[,] Filtered = new RGBPixel[Height, Width];


            // Create Filter in Spatial Domain:
            //=================================
            //make the filter ODD size
            if (filterSize % 2 == 0) filterSize++;

            double[] Filter = new double[filterSize];

            //Compute Filter in Spatial Domain :
            //==================================
            double Sum1 = 0;
            int HalfSize = filterSize / 2;
            for (int y = -HalfSize; y <= HalfSize; y++)
            {
                //Filter[y+HalfSize] = (1.0 / (Math.Sqrt(2 * 22.0/7.0) * Segma)) * Math.Exp(-(double)(y*y) / (double)(2 * Segma * Segma)) ;
                Filter[y + HalfSize] = Math.Exp(-(double)(y * y) / (double)(2 * sigma * sigma));
                Sum1 += Filter[y + HalfSize];
            }
            for (int y = -HalfSize; y <= HalfSize; y++)
            {
                Filter[y + HalfSize] /= Sum1;
            }

            //Filter Original Image Vertically:
            //=================================
            int ii, jj;
            RGBPixelD Sum;
            RGBPixel Item1;
            RGBPixelD Item2;

            for (int j = 0; j < Width; j++)
                for (int i = 0; i < Height; i++)
                {
                    Sum.red = 0;
                    Sum.green = 0;
                    Sum.blue = 0;
                    for (int y = -HalfSize; y <= HalfSize; y++)
                    {
                        ii = i + y;
                        if (ii >= 0 && ii < Height)
                        {
                            Item1 = ImageMatrix[ii, j];
                            Sum.red += Filter[y + HalfSize] * Item1.red;
                            Sum.green += Filter[y + HalfSize] * Item1.green;
                            Sum.blue += Filter[y + HalfSize] * Item1.blue;
                        }
                    }
                    VerFiltered[i, j] = Sum;
                }

            //Filter Resulting Image Horizontally:
            //===================================
            for (int i = 0; i < Height; i++)
                for (int j = 0; j < Width; j++)
                {
                    Sum.red = 0;
                    Sum.green = 0;
                    Sum.blue = 0;
                    for (int x = -HalfSize; x <= HalfSize; x++)
                    {
                        jj = j + x;
                        if (jj >= 0 && jj < Width)
                        {
                            Item2 = VerFiltered[i, jj];
                            Sum.red += Filter[x + HalfSize] * Item2.red;
                            Sum.green += Filter[x + HalfSize] * Item2.green;
                            Sum.blue += Filter[x + HalfSize] * Item2.blue;
                        }
                    }
                    Filtered[i, j].red = (byte)Sum.red;
                    Filtered[i, j].green = (byte)Sum.green;
                    Filtered[i, j].blue = (byte)Sum.blue;
                }

            return Filtered;
        }

    }

    public struct Edge
    {
        public int vertex1;
        public int vertex2;
        public double weight;

        public Edge(int vertex1, int vertex2, double weight)
        {
            this.vertex1 = vertex1;
            this.vertex2 = vertex2;
            this.weight = weight;
        }
    }

    public class KdTree
    {
        public List<double> Point;
        public KdTree Left;
        public KdTree Right;

        public KdTree(List<double> pt)
        {
            Point = new List<double>(pt);
            Left = null;
            Right = null;
        }

        public static KdTree Build(List<List<double>> points, int l, int r, int depth = 0)
        {
            if (l > r) return null;

            int axis = depth % 5;
            int mid = l + (r - l) / 2;

            QuickSelect(points, l, r, mid, axis);

            KdTree node = new KdTree(points[mid]);
            node.Left = Build(points, l, mid - 1, depth + 1);
            node.Right = Build(points, mid + 1, r, depth + 1);
            return node;
        }

        private static void QuickSelect(List<List<double>> points, int l, int r, int k, int axis)
        {
            Random rand = new Random();

            while (l < r)
            {
                int pivotIndex = rand.Next(l, r + 1);
                int pivotNewIndex = HoarePartition(points, l, r, pivotIndex, axis);

                if (k <= pivotNewIndex)
                {
                    r = pivotNewIndex;
                }
                else
                {
                    l = pivotNewIndex + 1;
                }
            }
        }

        private static int HoarePartition(List<List<double>> points, int l, int r, int pivotIndex, int axis)
        {
            var pivotValue = points[pivotIndex][axis];
            Swap(points, pivotIndex, l);

            int i = l - 1;
            int j = r + 1;

            while (true)
            {
                do { i++; } while (points[i][axis] < pivotValue);
                do { j--; } while (points[j][axis] > pivotValue);

                if (i >= j)
                    return j;

                Swap(points, i, j);
            }
        }

        private static void Swap(List<List<double>> points, int i, int j)
        {
            var tmp = points[i];
            points[i] = points[j];
            points[j] = tmp;
        }

        public static double GetRGBDist(List<double> a, List<double> b)
        {
            double dist = 0;
            for (int i = 0; i < 5; i++)
            {
                dist += Math.Pow(a[i] - b[i], 2);
            }
            return Math.Sqrt(dist);
        }

        public void KNearestANN(List<double> target, int k, int depth, SortedSet<(double, List<double>)> st, int width)
        {
            if (this == null) return;

            double dist = GetRGBDist(Point, target);
            st.Add((dist, Point));

            if (st.Count > k)
            {
                st.Remove(st.Max);
            }

            int axis = depth % 5;
            double diff = target[axis] - Point[axis];

            KdTree first = diff < 0 ? Left : Right;
            KdTree second = diff < 0 ? Right : Left;

            first?.KNearestANN(target, k, depth + 1, st, width);

            if (st.Count < k)
            {
                second?.KNearestANN(target, k, depth + 1, st, width);
            }
        }

        public List<List<double>> KNearestSearchANN(List<double> target, int k, int width)
        {
            var st = new SortedSet<(double, List<double>)>(Comparer<(double, List<double>)>.Create((a, b) =>
            {
                int cmp = a.Item1.CompareTo(b.Item1);
                if (cmp != 0) return cmp;
                for (int i = 0; i < a.Item2.Count; i++)
                {
                    cmp = a.Item2[i].CompareTo(b.Item2[i]);
                    if (cmp != 0) return cmp;
                }
                return 0;
            }));

            KNearestANN(target, k, 0, st, width);

            List<List<double>> result = new List<List<double>>();
            foreach (var p in st)
            {
                result.Add(p.Item2);
            }
            return result;
        }

        public static List<Edge> BuildGraph(int height, int width, RGBPixel[,] imageMatrix)
        {
            int how = 6;
            int maxEdges = height * width * (how + 3);

            Edge[] edges = ArrayPool<Edge>.Shared.Rent(maxEdges);

            List<List<double>> allPixels = new List<List<double>>();
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    RGBPixel current = imageMatrix[i, j];

                    List<double> pixel = new List<double> { i, j, current.red, current.green, current.blue };
                    allPixels.Add(pixel);
                }
            }

            HashSet<(int, int)> st = new HashSet<(int, int)>();

            int edgeCount = 0;

            KdTree kdTree = KdTree.Build(allPixels, 0, allPixels.Count - 1);


            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    int index1D = i * width + j;
                    RGBPixel current = imageMatrix[i, j];

                    var target = new List<double> { i, j, current.red, current.green, current.blue };

                    List<List<double>> kNearestNeighbors = kdTree.KNearestSearchANN(target, how, width);

                    foreach (var neighbor in kNearestNeighbors)
                    {
                        int ni = (int)neighbor[0];
                        int nj = (int)neighbor[1];

                        if (ni == i && nj == j) continue;

                        int neighborIndex = ni * width + nj;

                        int idx1 = Math.Min(index1D, neighborIndex);
                        int idx2 = Math.Max(index1D, neighborIndex);


                        if (st.Contains((idx1, idx2))) continue;
                        st.Add((idx1, idx2));

                        RGBPixel np = imageMatrix[ni, nj];

                        List<double> curColor = new List<double> { i, j, current.red, current.green, current.blue };
                        List<double> neighborColor = new List<double> { ni, nj, np.red, np.green, np.blue };

                        double weight = KdTree.GetRGBDist(curColor, neighborColor);

                        edges[edgeCount] = new Edge(index1D, neighborIndex, weight);
                        edgeCount++;
                    }
                }
            }

            List<Edge> g = new List<Edge>(edgeCount);

            for (int i = 0; i < edgeCount; i++)
            {
                g.Add(edges[i]);
            }

            ArrayPool<Edge>.Shared.Return(edges);

            return g;
        }
    }

    public class DSConstruction
    {
        public static (List<Edge> redgraph, List<Edge> greengraph, List<Edge> bluegraph, int[,] finalLabels) BuildGraph(int height, int width, RGBPixel[,] imageMatrix)
        {

            //fe redudunt keda keda 
            int edgecounter = 0;
            int maximumedges = height * width * 4;
            Edge[] rededges = ArrayPool<Edge>.Shared.Rent(maximumedges);
            Edge[] greenedges = ArrayPool<Edge>.Shared.Rent(maximumedges);
            Edge[] blueedges = ArrayPool<Edge>.Shared.Rent(maximumedges);
            int[,] finalLabels = new int[height, width];


            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int vertex1 = y * width + x;
                    RGBPixel current = imageMatrix[y, x];
                    finalLabels[y, x] = -1;

                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int neighbourx = x + dx;
                            int neighboury = y + dy;
                            if (neighbourx < 0 || neighbourx >= width || neighboury < 0 || neighboury >= height || (dy == 0 && dx == 0))
                                continue;
                            int vertex2 = neighboury * width + neighbourx;
                            if (vertex1 < vertex2)
                            {
                                RGBPixel neighbor = imageMatrix[neighboury, neighbourx];
                                double weightRed = Math.Abs(current.red - neighbor.red);
                                double weightGreen = Math.Abs(current.green - neighbor.green);
                                double weightBlue = Math.Abs(current.blue - neighbor.blue);
                                rededges[edgecounter] = new Edge(vertex1, vertex2, weightRed);
                                greenedges[edgecounter] = new Edge(vertex1, vertex2, weightGreen);
                                blueedges[edgecounter] = new Edge(vertex1, vertex2, weightBlue);
                                edgecounter++;
                            }
                        }
                    }
                }
            }


            List<Edge> redgraph = new List<Edge>(edgecounter);
            List<Edge> greengraph = new List<Edge>(edgecounter);
            List<Edge> bluegraph = new List<Edge>(edgecounter);

            for (int i = 0; i < edgecounter; i++)
            {
                redgraph.Add(rededges[i]);
                greengraph.Add(greenedges[i]);
                bluegraph.Add(blueedges[i]);
            }
            ArrayPool<Edge>.Shared.Return(rededges);
            ArrayPool<Edge>.Shared.Return(greenedges);
            ArrayPool<Edge>.Shared.Return(blueedges);
            return (redgraph, greengraph, bluegraph, finalLabels);

        }

        public static DisjointSet SegmentGraph(List<Edge> graph, int height, int width, double k)
        {
            int WH = height * width;
            DisjointSet ds = new DisjointSet(WH);
            graph.Sort((e1, e2) => e1.weight.CompareTo(e2.weight));

            foreach (var edge in graph)
            {
                int vertex1 = edge.vertex1;
                int vertex2 = edge.vertex2;
                double w = edge.weight;

                int root1 = ds.Find(vertex1);
                int root2 = ds.Find(vertex2);
                if (root1 != root2)
                {
                    double intensity1 = ds.GetInternalDiff(vertex1);
                    double intensity2 = ds.GetInternalDiff(vertex2);

                    int sizeC1 = ds.GetSize(vertex1);
                    int sizeC2 = ds.GetSize(vertex2);

                    double mInt = Math.Min(intensity1 + k / (double)sizeC1, intensity2 + k / (double)sizeC2);

                    if (w <= mInt)
                    {
                        ds.Union(vertex1, vertex2, w);

                    }
                }
            }


            return ds;
        }
    }

    public class Segmentation
    {
        public static (int[,], Dictionary<int, int>, DisjointSet) CreateSegments(DisjointSet redSeg,
     DisjointSet greenSeg,
     DisjointSet blueSeg,
     int height,
     int width, int[,] finalLabels)
        {
            int WH = height * width;
            DisjointSet mergedSet = new DisjointSet(WH);
            Dictionary<int, int> Mergednum = new Dictionary<int, int>();
            int labelID = 0;


            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (finalLabels[y, x] != -1)
                        continue;
                    int pixelIndex = y * width + x;

                    finalLabels[y, x] = labelID;
                    Mergednum[labelID] = 1;


                    Queue<(int, int)> queue = new Queue<(int, int)>();
                    queue.Enqueue((x, y));

                    while (queue.Count > 0)
                    {
                        var (curX, curY) = queue.Dequeue();
                        int curIdx = curY * width + curX;


                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {


                                int neighbourx = curX + dx;
                                int neighboury = curY + dy;


                                if (neighbourx < 0 || neighboury < 0 || neighbourx >= width || neighboury >= height || finalLabels[neighboury, neighbourx] != -1 || (dx == 0 && dy == 0))
                                    continue;

                                int neighborIdx = neighboury * width + neighbourx;


                                bool RedIntersect = redSeg.Find(curIdx) == redSeg.Find(neighborIdx);
                                bool GreenIntersect = greenSeg.Find(curIdx) == greenSeg.Find(neighborIdx);
                                bool BlueIntersect = blueSeg.Find(curIdx) == blueSeg.Find(neighborIdx);


                                if (RedIntersect && GreenIntersect && BlueIntersect)
                                {
                                    finalLabels[neighboury, neighbourx] = labelID;
                                    Mergednum[labelID]++;
                                    queue.Enqueue((neighbourx, neighboury));
                                    mergedSet.Union(curIdx, neighborIdx, 0.0);
                                }
                            }
                        }
                    }

                    labelID++;
                }
            }

            return (finalLabels, Mergednum, mergedSet);
        }

        public static void OutputWriterr(int[,] finalLabels, Dictionary<int, int> regionSizes, string outputPath)
        {
            var sortedSizes = regionSizes.Values.OrderByDescending(size => size).ToList();
            using (var writer = new System.IO.StreamWriter(outputPath))
            {
                writer.WriteLine(sortedSizes.Count);
                foreach (int size in sortedSizes)
                {
                    writer.WriteLine(size);
                }
            }
        }


    }
    public class Visualization
    {
        public static RGBPixel[,] VisualizeSegments(DisjointSet segmentSet, int width, int height)
        {
            RGBPixel[,] result = new RGBPixel[height, width];

            Dictionary<int, RGBPixel> colorMap = new Dictionary<int, RGBPixel>();

            Random rand = new Random(42);

            int numSegments = segmentSet.GetNumSets();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int pixelIndex = y * width + x;
                    int segmentId = segmentSet.Find(pixelIndex);

                    if (!colorMap.ContainsKey(segmentId))
                    {
                        RGBPixel colour;
                        colour.red = (byte)rand.Next(256);
                        colour.green = (byte)rand.Next(256);
                        colour.blue = (byte)rand.Next(256);

                        colorMap[segmentId] = colour;
                    }

                    result[y, x] = colorMap[segmentId];
                }
            }
            return result;
        }


    }

    public class DisjointSet
    {
        private int[] parent;
        private int[] rank;
        private int[] size;
        private double[] internalDiff;

        public DisjointSet(int n)
        {
            parent = new int[n];
            rank = new int[n];
            size = new int[n];
            internalDiff = new double[n];

            for (int i = 0; i < n; i++)
            {
                parent[i] = i;
                rank[i] = 0;
                size[i] = 1;
                internalDiff[i] = 0.0;
            }
        }


        public int Find(int u)
        {
            // mtnsash el half way compressing -* __ *-
            if (u != parent[u])
            {
                parent[u] = Find(parent[u]);
            }
            return parent[u];
        }


        public void Union(int u, int v, double edgeWeight)
        {
            int rootU = Find(u);
            int rootV = Find(v);

            if (rootU == rootV) return;


            if (rank[rootU] < rank[rootV])
            {

                parent[rootU] = rootV;
                size[rootV] += size[rootU];
                internalDiff[rootV] = Math.Max(internalDiff[rootV], Math.Max(internalDiff[rootU], edgeWeight));
            }
            else if (rank[rootV] < rank[rootU])
            {

                parent[rootV] = rootU;
                size[rootU] += size[rootV];
                internalDiff[rootU] = Math.Max(internalDiff[rootU], Math.Max(internalDiff[rootV], edgeWeight));
            }
            else
            {

                parent[rootV] = rootU;
                rank[rootU]++;
                size[rootU] += size[rootV];
                internalDiff[rootU] = Math.Max(internalDiff[rootU], Math.Max(internalDiff[rootV], edgeWeight));
            }
        }


        public int GetSize(int u)
        {
            return size[Find(u)];
        }


        public double GetInternalDiff(int u)
        {
            return internalDiff[Find(u)];
        }

        public int GetNumSets()
        {
            int count = 0;
            for (int i = 0; i < parent.Length; i++)
            {
                if (parent[i] == i)
                    count++;
            }
            return count;
        }
    }
}
