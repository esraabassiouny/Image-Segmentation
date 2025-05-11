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
        public static DisjointSet regions;
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

        public static void OptimizedImageSegmentation(RGBPixel[,] image, double k)
        {
            int height = image.GetLength(0);
            int width = image.GetLength(1);

            // Create task list for parallel execution
            var segmentTasks = new Task<DisjointSet>[3];

            segmentTasks[0] = Task.Run(() => SegmentSingleChannel(image, height, width, k, 'R'));
            segmentTasks[1] = Task.Run(() => SegmentSingleChannel(image, height, width, k, 'G'));
            segmentTasks[2] = Task.Run(() => SegmentSingleChannel(image, height, width, k, 'B'));

            // Wait for all channels to complete
            Task.WaitAll(segmentTasks);

            // Intersect results
            IntersectSegments(segmentTasks[0].Result,
                             segmentTasks[1].Result,
                             segmentTasks[2].Result,
                             height, width);
        }
        private static DisjointSet SegmentSingleChannel(RGBPixel[,] image, int height, int width, double k, char channel)
        {
            int size = height * width;
            var allEdges = BuildAllEdges(image, height, width, channel);
            allEdges.Sort();

            var ds = new DisjointSetWithInternalDiff(size);

            foreach (var edge in allEdges)
            {
                int root1 = ds.Find(edge.from);
                int root2 = ds.Find(edge.to);

                if (root1 != root2)
                {
                    double threshold1 = ds.InternalDiff[root1] + k / ds.GetSize(root1);
                    double threshold2 = ds.InternalDiff[root2] + k / ds.GetSize(root2);

                    if (edge.weight <= Math.Min(threshold1, threshold2))
                    {
                        ds.Union(root1, root2, edge.weight);
                    }
                }
            }
            return ds;
        }
        private static List<Edge> BuildAllEdges(RGBPixel[,] image, int height, int width, char channel)
        {
            List<Edge> edges = new List<Edge>(height * width * 8);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int current = y * width + x;
                    byte currentValue = GetChannelValue(image[y, x], channel);

                    if (x < width - 1)
                    {
                        // Right neighbor (E)
                        byte rightValue = GetChannelValue(image[y, x + 1], channel);
                        edges.Add(new Edge { from = current, to = y * width + (x + 1), weight = (byte)Math.Abs(currentValue - rightValue) });

                        // Bottom-right neighbor (SE)
                        if (y < height - 1)
                        {
                            byte bottomRightValue = GetChannelValue(image[y + 1, x + 1], channel);
                            edges.Add(new Edge { from = current, to = (y + 1) * width + (x + 1), weight = (byte)Math.Abs(currentValue - bottomRightValue) });
                        }
                    }

                    if (y < height - 1)
                    {
                        // Bottom neighbor (S)
                        byte bottomValue = GetChannelValue(image[y + 1, x], channel);
                        edges.Add(new Edge { from = current, to = (y + 1) * width + x, weight = (byte)Math.Abs(currentValue - bottomValue) });

                        // Bottom-left neighbor (SW)
                        if (x > 0)
                        {
                            byte bottomLeftValue = GetChannelValue(image[y + 1, x - 1], channel);
                            edges.Add(new Edge { from = current, to = (y + 1) * width + (x - 1), weight = (byte)Math.Abs(currentValue - bottomLeftValue) });
                        }
                    }
                }
            }

            return edges;
        }

        private static byte GetChannelValue(RGBPixel pixel, char channel)
        {
            byte v = 0;
            switch(channel)
            {
                case 'R': v = pixel.red;
                    break;
                case 'G': v = pixel.green;
                    break;
                case 'B': v = pixel.blue;
                    break;
            }
            return v;
        }

        private static void IntersectSegments(DisjointSet red, DisjointSet green, DisjointSet blue, int height, int width)
        {
            int size = width * height;
            regions = new DisjointSet(size);  // Use base DisjointSet here

            // Dictionary to map (R,G,B) combinations to representative nodes
            var componentMap = new Dictionary<(int, int, int), int>();
            var componentSizes = new Dictionary<int, int>();

            for (int i = 0; i < size; i++)
            {
                var key = (red.Find(i), green.Find(i), blue.Find(i));

                if (!componentMap.TryGetValue(key, out int representative))
                {
                    // New component found
                    representative = i;  // Use current pixel as representative
                    componentMap[key] = representative;
                    componentSizes[representative] = 1;
                }
                else
                {
                    // Union with existing component
                    regions.Union(i, representative);
                    int root = regions.Find(representative);
                    regions.SetParentAndSize(root, root, regions.GetSize(root) + 1);
                    componentSizes[representative]++;
                }
            }

            //// Update sizes in the disjoint set
            //foreach (var kvp in componentSizes)
            //{
            //    regions.SetParentAndSize(kvp.Key, kvp.Key, kvp.Value);
            //}
        }
        public static (int segmentCount, List<int> segmentSizes) GetSegmentationResults(RGBPixel[,] image, double k)
        {
            OptimizedImageSegmentation(image, k);
            var rootCounts = new Dictionary<int, int>();
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
