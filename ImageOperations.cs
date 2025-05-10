using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Windows.Forms.VisualStyles;
using System.Threading.Tasks;
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
        public static DisjointSet regions;

        static Dictionary<int, int> internalDifferenceCache = new Dictionary<int, int>();


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

            //for (int y = 0; y < height; y++)
            //{
            //    for (int x = 0; x < width; x++)
            //    {
            //        MessageBox.Show(image[y, x].red.ToString());
            //    }
            //}
            regions = new DisjointSet(size);

            RedAdjacencyList = new List<Pixel>[size];
            GreenAdjacencyList = new List<Pixel>[size];
            BlueAdjacencyList = new List<Pixel>[size];

            Parallel.For(0, size, i =>
            {
                RedAdjacencyList[i] = new List<Pixel>(8);
                GreenAdjacencyList[i] = new List<Pixel>(8);
                BlueAdjacencyList[i] = new List<Pixel>(8);
            });

            //Parallel.For(0, height, y =>
            //{
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int currentIndex = y * width + x;
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
                                if (neighborIndex > currentIndex)
                                {
                                    byte redWeight = (byte)Math.Abs(image[y, x].red - image[ny, nx].red);
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
            };
        }

        public static void ImageSegmentation(RGBPixel[,] image, double k)
        {
            int height = GetHeight(image);
            int width = GetWidth(image);
            int size = height * width;

            GraphRepresentation(image);
            MessageBox.Show("GraphRepresentation done");

            // Process each channel in parallel
            var tasks = new Task<DisjointSet>[3];
            tasks[0] = Task.Run(() => SegmentSingleChannel(RedAdjacencyList, size, k));
            tasks[1] = Task.Run(() => SegmentSingleChannel(GreenAdjacencyList, size, k));
            tasks[2] = Task.Run(() => SegmentSingleChannel(BlueAdjacencyList, size, k));
            Task.WaitAll(tasks);

            DisjointSet redRegions = tasks[0].Result;
            DisjointSet greenRegions = tasks[1].Result;
            DisjointSet blueRegions = tasks[2].Result;

            MessageBox.Show("Segment Channels done");

            // Combine results
            regions = new DisjointSet(size);
            var combinedRegionsMap = new Dictionary<string, (int id, int size)>();
            int currentCombinedRegion = 0;

            // First pass: count occurrences of each region combination
            var regionCounts = new Dictionary<string, int>();
            for (int i = 0; i < size; i++)
            {
                string regionKey = $"{redRegions.Find(i)}_{greenRegions.Find(i)}_{blueRegions.Find(i)}";
                if (!regionCounts.ContainsKey(regionKey))
                {
                    regionCounts[regionKey] = 0;
                }
                regionCounts[regionKey]++;
            }

            // Second pass: assign regions with proper sizes
            for (int i = 0; i < size; i++)
            {
                string regionKey = $"{redRegions.Find(i)}_{greenRegions.Find(i)}_{blueRegions.Find(i)}";

                if (!combinedRegionsMap.TryGetValue(regionKey, out var regionInfo))
                {
                    // Get the size from our counting pass
                    int combinedRegionSize = regionCounts[regionKey];
                    regionInfo = (currentCombinedRegion++, combinedRegionSize);
                    combinedRegionsMap[regionKey] = regionInfo;
                }

                regions.SetParentAndSize(i, regionInfo.id, regionInfo.size);
            }
        }

        //private static DisjointSet SegmentSingleChannel(List<Pixel>[] adjacencyList, int size, double k)
        //{
        //    DisjointSet channelRegions = new DisjointSet(size);

        //    var allEdges = new List<Edge>();
        //    for (int i = 0; i < size; i++)
        //    {
        //        foreach (var pixel in adjacencyList[i])
        //        {
        //            allEdges.Add(new Edge { from = i, to = pixel.index, weight = pixel.weight });
        //        }
        //    }
        //    allEdges.Sort();

        //    foreach (var edge in allEdges)
        //    {
        //        int root1 = channelRegions.Find(edge.from);
        //        int root2 = channelRegions.Find(edge.to);

        //        if (root1 != root2)
        //        {
        //            int componentDiff = GetComponentDifference(adjacencyList, channelRegions, root1, root2);
        //            if (componentDiff == int.MaxValue)
        //                continue;

        //            // Calculate internal differences on demand
        //            int intDiff1 = GetInternalDifference(adjacencyList, channelRegions, root1);
        //            int intDiff2 = GetInternalDifference(adjacencyList, channelRegions, root2);

        //            double minInternal = Math.Min(
        //                intDiff1 + (k / channelRegions.GetSize(root1)),
        //                intDiff2 + (k / channelRegions.GetSize(root2))
        //            );

        //            if (componentDiff <= minInternal)
        //            {
        //                channelRegions.Union(root1, root2);
        //            }
        //        }
        //    }

        //    return channelRegions;
        //}

        private static DisjointSet SegmentSingleChannel(List<Pixel>[] adjacencyList, int size, double k)
        {
            DisjointSet channelRegions = new DisjointSet(size);

            // Process each pixel
            for (int i = 0; i < size; i++)
            {
                // Check all neighboring pixels
                foreach (var neighbor in adjacencyList[i])
                {
                    int j = neighbor.index;
                    int root1 = channelRegions.Find(i);
                    int root2 = channelRegions.Find(j);

                    if (root1 != root2)
                    {
                        // Get component difference (edge weight)
                        // int componentDiff = neighbor.weight;
                        int componentDiff = GetComponentDifference(adjacencyList, channelRegions, root1, root2);
                        // Calculate internal differences
                        int intDiff1 = GetInternalDifference(adjacencyList, channelRegions, root1);
                        int intDiff2 = GetInternalDifference(adjacencyList, channelRegions, root2);

                        // Calculate merge thresholds
                        double threshold1 = intDiff1 + (k / channelRegions.GetSize(root1));
                        double threshold2 = intDiff2 + (k / channelRegions.GetSize(root2));

                        // Merge condition
                        if (componentDiff <= Math.Min(threshold1, threshold2))
                        {
                            channelRegions.Union(root1, root2);
                        }
                    }
                }
            }

            return channelRegions;
        }
        //private static int GetComponentDifference(List<Pixel>[] adjacencyList, DisjointSet regions, int c1, int c2)
        //{
        //    int minWeight = int.MaxValue;

        //    // Optimized: only check one direction since edges are bidirectional
        //    var c1Pixels = new List<int>();
        //    for (int i = 0; i < regions.Length; i++)
        //        if (regions.Find(i) == c1)
        //            c1Pixels.Add(i);

        //    foreach (int pixel in c1Pixels)
        //    {
        //        foreach (var neighbor in adjacencyList[pixel])
        //        {
        //            if (regions.Find(neighbor.index) == c2)
        //            {
        //                minWeight = Math.Min(minWeight, neighbor.weight);
        //            }
        //        }
        //    }

        //    return minWeight == int.MaxValue ? int.MaxValue : minWeight;
        //}


        private static int GetComponentDifference(List<Pixel>[] adjacencyList, DisjointSet regions, int c1, int c2)
        {
            int minWeight = int.MaxValue;

            // Just check direct connections between the components
            for (int i = 0; i < regions.Length; i++)
            {
                if (regions.Find(i) == c1)
                {
                    foreach (var neighbor in adjacencyList[i])
                    {
                        if (regions.Find(neighbor.index) == c2)
                        {
                            minWeight = Math.Min(minWeight, neighbor.weight);
                        }
                    }
                }
            }

            return minWeight;
        }
        private static int GetInternalDifference(List<Pixel>[] adjacencyList, DisjointSet regions, int componentRoot)
        {
            var componentPixels = new List<int>();
            for (int i = 0; i < regions.Length; i++)
            {
                if (regions.Find(i) == componentRoot)
                    componentPixels.Add(i);
            }

            //MessageBox.Show(componentPixels.Count.ToString());
            if (componentPixels.Count <= 1)
                return 0;

            // Optimized MST calculation
            int maxEdgeWeight = 0;
            var tempDS = new DisjointSet(regions.Length);
            var edges = new List<Edge>();

            foreach (int pixel in componentPixels)
            {
                foreach (var neighbor in adjacencyList[pixel])
                {
                    if (componentPixels.Contains(neighbor.index))
                    {
                        edges.Add(new Edge { from = pixel, to = neighbor.index, weight = neighbor.weight });
                    }
                }
            }

            edges.Sort();

            foreach (var edge in edges)
            {
                int root1 = tempDS.Find(edge.from);
                int root2 = tempDS.Find(edge.to);

                if (root1 != root2)
                {
                    tempDS.Union(root1, root2);
                    maxEdgeWeight = Math.Max(maxEdgeWeight, edge.weight);
                    if (tempDS.GetSize(root1) == componentPixels.Count)
                        break;
                }
            }

            return maxEdgeWeight;
        }

        public static (int segmentCount, List<int> segmentSizes) GetSegmentationResults(RGBPixel[,] image, double k)
        {
            ImageSegmentation(image, k);
            Dictionary<int, int> rootCounts = new Dictionary<int, int>();
            int totalPixels = regions.Length;

            for (int i = 0; i < totalPixels; i++)
            {
                int root = regions.Find(i);
                rootCounts[root] = rootCounts.TryGetValue(root, out int count) ? count + 1 : 1;
            }

            List<int> sizes = new List<int>(rootCounts.Values);
            sizes.Sort((a, b) => b.CompareTo(a));
            return (rootCounts.Count, sizes);
        }
    }

}
