using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
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

    public struct Pixel
    {
        public int index;
        public byte weight;
    }
    public struct Edge : IComparable<Edge>
    {
        public int from;
        public int to;
        public byte weight;

        public int CompareTo(Edge other) => weight.CompareTo(other.weight);
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
        /// 

        static List<Pixel>[] RedAdjacencyList;
        static List<Pixel>[] GreenAdjacencyList;
        static List<Pixel>[] BlueAdjacencyList;
        static DisjointSet regions;


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

        public static void GraphRepresentation(RGBPixel[,] image)
        {
            int height = GetHeight(image);
            int width = GetWidth(image);
            int size = height * width;
            regions = new DisjointSet(size);
            RedAdjacencyList = new List<Pixel>[size];
            GreenAdjacencyList = new List<Pixel>[size];
            BlueAdjacencyList = new List<Pixel>[size];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int currentIndex = x * height + y;  // Swap width and height
                    //int neighborIndex = nx * height + ny; try
                    {
                        RedAdjacencyList[currentIndex] = new List<Pixel>();
                        GreenAdjacencyList[currentIndex] = new List<Pixel>();
                        BlueAdjacencyList[currentIndex] = new List<Pixel>();
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                if (dx == 0 && dy == 0) continue;

                                int nx = x + dx;
                                int ny = y + dy;

                                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                                {
                                    int neighborIndex = ny * width + nx;
                                    byte redWeight = (byte)Math.Abs(image[y,x].red - image[ny, nx].red);

                                    byte greenWeight = (byte)Math.Abs(image[y, x].green - image[ny, nx].green);
                                    byte blueWeight = (byte)Math.Abs(image[y, x].blue - image[ny, nx].blue);
                                    RedAdjacencyList[currentIndex].Add(new Pixel { index = neighborIndex, weight = redWeight });
                                    GreenAdjacencyList[currentIndex].Add(new Pixel { index = neighborIndex, weight = greenWeight });
                                    BlueAdjacencyList[currentIndex].Add(new Pixel { index = neighborIndex, weight = blueWeight });
                                    }
                                }
                            }
                        }
                    }
                }
        }

        public static void ImageSegmentation(RGBPixel[,] image, double k)
        {
            int height = GetHeight(image);
            int width = GetWidth(image);
            int size = height * width;

            // Step 1: Build graph representation
            GraphRepresentation(image);
            MessageBox.Show("GraphRepresentation done");

            // Step 2: Create and sort all edges (for all channels)
            List<Edge> allEdges = new List<Edge>();

            for (int i = 0; i < size; i++)
            {
                foreach (var pixel in RedAdjacencyList[i])
                {
                    if (pixel.index > i) // Avoid duplicate edges
                    {
                        allEdges.Add(new Edge { from = i, to = pixel.index, weight = pixel.weight });
                    }
                }
            }

            allEdges.Sort(); 

            foreach (var edge in allEdges)
            {
                int root1 = regions.Find(edge.from);
                int root2 = regions.Find(edge.to);

                if (root1 != root2)
                {
                    // Calculate internal differences
                    int intDiff1 = GetInternalDifference(root1);
                    int intDiff2 = GetInternalDifference(root2);

                    // Calculate minimum internal difference with threshold
                    double minInternal = Math.Min(intDiff1 + (k / regions.GetSize(root1)),
                                                intDiff2 + (k / regions.GetSize(root2)));

                    // If edge weight is small enough, merge regions
                    if (edge.weight <= minInternal)
                    {
                        regions.Union(root1, root2);
                    }
                }
            }
        }

        private static int GetInternalDifference(int componentRoot)
        {
            // This is simplified - in practice you'd need to:
            // 1. Find all pixels in the component
            var componentPixels = new List<int>();
            for (int i = 0; i < regions.Length; i++)
            {
                if (regions.Find(i) == componentRoot)
                    componentPixels.Add(i);
            }
            // Build edge list for this component
            var edges = new List<Edge>();
            foreach (int pixel in componentPixels)
            {
                foreach (var neighbor in RedAdjacencyList[pixel])
                {
                    if (componentPixels.Contains(neighbor.index) && neighbor.index > pixel)
                    {
                        edges.Add(new Edge
                        {
                            from = pixel,
                            to = neighbor.index,
                            weight = neighbor.weight
                        });
                    }
                }
            }

            // Find MST using Kruskal's algorithm
            edges.Sort();
            var mstEdges = new List<Edge>();
            var tempDS = new DisjointSet(regions.Length);

            foreach (var edge in edges)
            {
                int root1 = tempDS.Find(edge.from);
                int root2 = tempDS.Find(edge.to);

                if (root1 != root2)
                {
                    mstEdges.Add(edge);
                    tempDS.Union(root1, root2);

                    // Early exit if we've connected all nodes
                    if (mstEdges.Count == componentPixels.Count - 1)
                        break;
                }
            }

            // Find maximum edge in MST
            int maxEdgeWeight = 0;
            foreach (var edge in mstEdges)
            {
                if (edge.weight > maxEdgeWeight)
                    maxEdgeWeight = edge.weight;
            }

            // Cache the result in our DisjointSet
            //regions.SetInternalDifference(componentRoot, maxEdgeWeight);
            return maxEdgeWeight;
        }

        public static (int segmentCount, List<int> segmentSizes)GetSegmentationResults()
        {
            Dictionary<int, int> rootCounts = new Dictionary<int, int>();
            int totalPixels = regions.Length;

            // Count occurrences of each root (region)
            for (int i = 0; i < totalPixels; i++)
            {
                int root = regions.Find(i);
                if (rootCounts.ContainsKey(root))
                {
                    rootCounts[root]++;
                }
                else
                {
                    rootCounts.Add(root, 1);
                }
            }

            // Extract and sort the segment sizes
            List<int> sizes = new List<int>(rootCounts.Values);
            sizes.Sort((a, b) => b.CompareTo(a)); // Sort in descending order

            return (rootCounts.Count, sizes);
        }
    }
}
