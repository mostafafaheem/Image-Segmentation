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

    public class Edge : IComparable<Edge>
    {
        public int From { get; set; }
        public int To { get; set; }
        public float Weight { get; set; }

        public int CompareTo(Edge other)
        {
            return Weight.CompareTo(other.Weight);
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
        public static List<Edge> ConstructEightNeighbourGraph(RGBPixel[,] ImageMatrix, char colour)
        {
            List<Edge> edges = new List<Edge>();

            int[,] adjacentPixels = new int[,] {
                { -1, -1 }, { -1, 0 }, { -1, 1 }, { 0, -1 }
            };

            Func<RGBPixel, byte> getChannel;
            switch (colour)
            {
                case 'r': getChannel = p => p.red; break;
                case 'g': getChannel = p => p.green; break;
                case 'b': getChannel = p => p.blue; break;
                default: throw new ArgumentException("Invalid color parameter. Must be 'r', 'g', or 'b'.", nameof(colour));
            }

            int height = ImageMatrix.GetLength(0),
                width = ImageMatrix.GetLength(1),
                neighbourCnt = adjacentPixels.GetLength(0);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int from = y * width + x;
                    byte fromValue = getChannel(ImageMatrix[y, x]);

                    for (int d = 0; d < neighbourCnt; d++)
                    {
                        int ny = y + adjacentPixels[d, 0];
                        int nx = x + adjacentPixels[d, 1];

                        if (ny < 0 || ny >= height || nx < 0 || nx >= width)
                            continue;

                        int to = ny * width + nx;
                        byte toValue = getChannel(ImageMatrix[ny, nx]);
                        float weight = Math.Abs(fromValue - toValue);

                        edges.Add(new Edge { From = from, To = to, Weight = weight });
                    }
                }
            }
            return edges;
        }

        /// <summary>
        /// Construct Minimum Spanning Tree while performing segmentation, each image segment has its own tree
        /// </summary>
        /// <param name="allEdges">Original graph edge list</param>
        /// <param name="numVertices">Total number of pixels</param>
        /// <param name="k">Threshold constant</param>
        /// <returns>minimum Spanning Tree</returns>
        public static DisjointSet ConstructMST(List<Edge> allEdges, int numVertices, float k)
        {

            allEdges.Sort((a, b) => a.Weight.CompareTo(b.Weight));

            DisjointSet mstSet = new DisjointSet(numVertices);
            
            float[] internalDiff = new float[numVertices];
            for (int i = 0; i < numVertices; i++)
                internalDiff[i] = 0;

            foreach (Edge edge in allEdges)
            {
                int setA = mstSet.Find(edge.From);
                int setB = mstSet.Find(edge.To);

                if (setA != setB)
                {
                    float threshold = Math.Min(
                        internalDiff[setA] + k / mstSet.GetSize(setA),
                        internalDiff[setB] + k / mstSet.GetSize(setB)
                    );
                    if (edge.Weight <= threshold)
                    {
                        int newSet = mstSet.Union(setA, setB);
                        internalDiff[newSet] = edge.Weight;
                    }
                }
            }
            // Beyjoin el small components bas lesa not sure meno
            //foreach (Edge edge in allEdges)
            //{
            //    int minSize = (int)k;
            //    int a = mstSet.Find(edge.From);
            //    int b = mstSet.Find(edge.To);

            //    if (a != b && (mstSet.GetSize(a) < minSize || mstSet.GetSize(b) < minSize))
            //        mstSet.Union(a, b);
            //}

            return mstSet;
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
        public static DisjointSet CreateSegments(DisjointSet[] channelSets, int numVertices)
        {
            DisjointSet segmentSet = new DisjointSet(numVertices);
            Dictionary<string, int> representativeMap = new Dictionary<string, int>();

            for (int j = 0; j < numVertices; j++)
            {
                int redComponent = channelSets[0].Find(j);
                int greenComponent = channelSets[1].Find(j);
                int blueComponent = channelSets[2].Find(j);

                string componentKey = $"{redComponent}_{greenComponent}_{blueComponent}";

                if (!representativeMap.ContainsKey(componentKey))
                {
                    representativeMap[componentKey] = j;
                }
                else
                {
                    segmentSet.Union(j, representativeMap[componentKey]);
                }
            }
            return segmentSet;
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
        private int[] parent { get; set; }
        private int[] rank { get; set; }
        private int[] size { get; set; }

        public DisjointSet(int size)
        {
            parent = new int[size];
            rank = new int[size];
            this.size = new int[size];

            for (int i = 0; i < size; i++)
            {
                parent[i] = i;
                rank[i] = 0;
                this.size[i] = 1;
            }
        }
        /// <summary>
        /// Finds the set of a number 
        /// </summary>
        /// <param name="x">Number whose set is to be found</param>
        /// <returns>set root</returns>

        public int Find(int x)
        {
            if (parent[x] != x)
                parent[x] = Find(parent[x]);
            return parent[x];
        }
        /// <summary>
        /// Joins 2 distinct set into one 
        /// </summary>
        /// <param name="x">Number in the 1st set to be joined</param>
        /// <param name="y">Number in the 2nd set to be joined</param>
        /// <returns>set root after union</returns>

        public int Union(int x, int y)
        {
            int rootX = Find(x);
            int rootY = Find(y);

            if (rootX == rootY)
                return rootX;

            if (rank[rootX] < rank[rootY])
            {
                parent[rootX] = rootY;
                size[rootY] += size[rootX];
                return rootY;
            }
            else if (rank[rootX] > rank[rootY])
            {
                parent[rootY] = rootX;
                size[rootX] += size[rootY];
                return rootX;
            }
            else
            {
                parent[rootY] = rootX;
                rank[rootX]++;
                size[rootX] += size[rootY];
                return rootX;
            }
        }
        /// <summary>
        /// Gets the size of a set 
        /// </summary>
        /// <param name="x">set member</param>
        /// <returns>set size</returns>

        public int GetSize(int x)
        {
            int root = Find(x);
            return size[root];
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