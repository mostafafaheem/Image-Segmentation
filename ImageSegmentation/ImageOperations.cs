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
        public int v1;
        public int v2;
        public double weight;

        public Edge(int v1, int v2, double weight)
        {
            this.v1 = v1;
            this.v2 = v2;
            this.weight = weight;
        }
    }

    public class DSConstruction
    {
        /// <summary>
        /// Construct an eight-neighbour graph for the given image and the selected channel 
        /// </summary>
        /// <param name="ImageMatrix">Colored image matrix</param>
        /// <param name="colour">Gaussian mask size</param>
        /// <returns>edge list</returns>
        public static (List<Edge> redGraph, List<Edge> greenGraph, List<Edge> blueGraph, int[,] finalLabels) BuildGraph(int height, int width, RGBPixel[,] imageMatrix)
        {


            int maxEdges = height * width * 4;


            Edge[] redEdges = ArrayPool<Edge>.Shared.Rent(maxEdges);
            Edge[] greenEdges = ArrayPool<Edge>.Shared.Rent(maxEdges);
            Edge[] blueEdges = ArrayPool<Edge>.Shared.Rent(maxEdges);
            int[,] finalLabels = new int[height, width];

            int edgeCount = 0;



            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int v1 = y * width + x;
                    RGBPixel current = imageMatrix[y, x];
                    finalLabels[y, x] = -1;

                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dy == 0 && dx == 0)
                                continue;

                            int nx = x + dx;
                            int ny = y + dy;

                            if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                                continue;

                            int v2 = ny * width + nx;
                            if (v1 < v2)
                            {
                                RGBPixel neighbor = imageMatrix[ny, nx];
                                double weightR = Math.Abs(current.red - neighbor.red);
                                double weightG = Math.Abs(current.green - neighbor.green);
                                double weightB = Math.Abs(current.blue - neighbor.blue);

                                redEdges[edgeCount] = new Edge(v1, v2, weightR);
                                greenEdges[edgeCount] = new Edge(v1, v2, weightG);
                                blueEdges[edgeCount] = new Edge(v1, v2, weightB);
                                edgeCount++;
                            }
                        }
                    }
                }
            }


            List<Edge> redGraph = new List<Edge>(edgeCount);
            List<Edge> greenGraph = new List<Edge>(edgeCount);
            List<Edge> blueGraph = new List<Edge>(edgeCount);

            for (int i = 0; i < edgeCount; i++)
            {
                redGraph.Add(redEdges[i]);
                greenGraph.Add(greenEdges[i]);
                blueGraph.Add(blueEdges[i]);
            }
            ArrayPool<Edge>.Shared.Return(redEdges);
            ArrayPool<Edge>.Shared.Return(greenEdges);
            ArrayPool<Edge>.Shared.Return(blueEdges);
            return (redGraph, greenGraph, blueGraph, finalLabels);





        }

        /// <summary>
        /// Construct Minimum Spanning Tree while performing segmentation, each image segment has its own tree
        /// </summary>
        /// <param name="allEdges">Original graph edge list</param>
        /// <param name="numVertices">Total number of pixels</param>
        /// <param name="k">Threshold constant</param>
        /// <returns>minimum Spanning Tree</returns>
        public static DisjointSet SegmentGraph(List<Edge> graph, int height, int width, double k)
        {
            int n = height * width;
            DisjointSet ds = new DisjointSet(n);


            graph.Sort((e1, e2) => e1.weight.CompareTo(e2.weight));


            foreach (var edge in graph)
            {
                int v1 = edge.v1;
                int v2 = edge.v2;
                double w = edge.weight;

                int root1 = ds.Find(v1);
                int root2 = ds.Find(v2);

                if (root1 != root2)
                {
                    double intC1 = ds.GetInternalDiff(v1);
                    double intC2 = ds.GetInternalDiff(v2);
                    int sizeC1 = ds.GetSize(v1);
                    int sizeC2 = ds.GetSize(v2);

                    double mInt = Math.Min(intC1 + k / (double)sizeC1, intC2 + k / (double)sizeC2);

                    if (w <= mInt)
                    {
                        ds.Union(v1, v2, w);

                    }
                }
            }



            return ds;
        }
    }

    public class Segmentation
    {
        /// <summary>
        /// Join all three channels by checking for pixels in common segments
        /// </summary>
        /// <param name="channelSets">Array of MSTs, one for each colour channel</param>
        /// <param name="numVertices">Total number of pixels</param>
        /// <returns>final segmented MST</returns>
        public static (int[,], Dictionary<int, int>, DisjointSet) CreateSegments(DisjointSet redSegment,
     DisjointSet greenSegment,
     DisjointSet blueSegment,
     int height,
     int width, int[,] finalLabels)
        {
            int n = height * width;
            //int[,] finalLabels = new int[height, width];


            //for (int y = 0; y < height; y++)
            //    for (int x = 0; x < width; x++)
            //        finalLabels[y, x] = -1;
            DisjointSet mergedSet = new DisjointSet(n);

            Dictionary<int, int> mergedComponents = new Dictionary<int, int>();
            int nextLabel = 0;

            // optmize el height fe parellel for 
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int pixelIndex = y * width + x;


                    if (finalLabels[y, x] != -1)
                        continue;


                    finalLabels[y, x] = nextLabel;
                    mergedComponents[nextLabel] = 1;


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
                                if (dx == 0 && dy == 0)
                                    continue;

                                int nx = curX + dx;
                                int ny = curY + dy;


                                if (nx < 0 || ny < 0 || nx >= width || ny >= height || finalLabels[ny, nx] != -1)
                                    continue;

                                int neighborIdx = ny * width + nx;


                                bool sameRed = redSegment.Find(curIdx) == redSegment.Find(neighborIdx);
                                bool sameGreen = greenSegment.Find(curIdx) == greenSegment.Find(neighborIdx);
                                bool sameBlue = blueSegment.Find(curIdx) == blueSegment.Find(neighborIdx);


                                if (sameRed && sameGreen && sameBlue)
                                {
                                    finalLabels[ny, nx] = nextLabel;
                                    mergedComponents[nextLabel]++;
                                    queue.Enqueue((nx, ny));
                                    mergedSet.Union(curIdx, neighborIdx, 0.0);
                                }
                            }
                        }
                    }

                    nextLabel++;
                }
            }

            return (finalLabels, mergedComponents, mergedSet);
        }

        public static void WriteOutputFile(int[,] finalLabels, Dictionary<int, int> regionSizes, string outputPath)
        {
            var sortedSizes = regionSizes.Values.OrderByDescending(size => size).ToList();
            using (var writer = new System.IO.StreamWriter(outputPath))
            {
                writer.WriteLine(sortedSizes.Count); // Num
                foreach (int size in sortedSizes)
                {
                    writer.WriteLine(size); // Size of each segment
                }
            }
        }


    }
    public class Visualization
    {

        /// <summary>
        /// Turns the segmented MST into a pixel grid with segments labelled in different colours randomly
        /// </summary>
        /// <param name="segmentSet">Colored image matrix</param>
        /// <param name="width">Image width</param>
        /// <param name="height">Image height</param>
        /// <returns>segment labelled image</returns>
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

        /// <summary>
        /// Calculates the total number of sets 
        /// </summary>
        /// <returns>total number of sets</returns>
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