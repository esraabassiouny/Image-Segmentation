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

namespace ImageTemplate
{
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
        public int pixel1;
        public int pixel2;
        public byte weight;

        public int CompareTo(Edge other) => weight.CompareTo(other.weight);
    }

    public class ImageOperations
    {
        public static DisjointSet regions;
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
        public static int GetHeight(RGBPixel[,] ImageMatrix)
        {
            return ImageMatrix.GetLength(0);
        }
        public static int GetWidth(RGBPixel[,] ImageMatrix)
        {
            return ImageMatrix.GetLength(1);
        }
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
        public static void ImageSegmentation(RGBPixel[,] ImageMatrix, double threshold)
        {
            int height = GetHeight(ImageMatrix);
            int width = GetWidth(ImageMatrix);

            var segmentTasks = new Task<DisjointSet>[3];
            segmentTasks[0] = Task.Run(() => SegmentSingleColor(ImageMatrix, height, width, threshold, 'R'));
            segmentTasks[1] = Task.Run(() => SegmentSingleColor(ImageMatrix, height, width, threshold, 'G'));
            segmentTasks[2] = Task.Run(() => SegmentSingleColor(ImageMatrix, height, width, threshold, 'B'));

            Task.WaitAll(segmentTasks);

            IntersectRegions(segmentTasks[0].Result, segmentTasks[1].Result, segmentTasks[2].Result, height, width);
        }
        // internal diff >>  largest weight edge that is necessarily to keep the component connected
        // component diff >> minimum weight edge connecting the two components.
        private static DisjointSet SegmentSingleColor(RGBPixel[,] ImageMatrix, int height, int width, double threshold, char color)
        {
            int size = height * width;
            var edges = ConstructEdges(ImageMatrix, height, width, color);
            edges.Sort(); /*sort edges by weight  >> to check min weight edges first 1- always consider the more similar 
                           connections first 2- donot need to
                           calculate component Difference 'Time Complexity from O(M^2) to O(M LOG(M))'
                          */

            var colorRegions = new DisjointSetWithInternalDifference(size);

            foreach (var edge in edges)
            {
                int root1 = colorRegions.Find(edge.pixel1);
                int root2 = colorRegions.Find(edge.pixel2);

                if (root1 != root2)
                {
                    double internalDifference1 = colorRegions.InternalDifference[root1] + threshold / colorRegions.GetSize(root1);
                    double internalDifference2 = colorRegions.InternalDifference[root2] + threshold / colorRegions.GetSize(root2);
                    double minDifference = Math.Min(internalDifference1, internalDifference2);
                    if (edge.weight <= minDifference)
                    {
                        colorRegions.Union(root1, root2, edge.weight);
                    }
                }
            }
            return colorRegions;
        }
        private static List<Edge> ConstructEdges(RGBPixel[,] image, int height, int width, char color)
        {
            List<Edge> edges = new List<Edge>();

            // only add 4 neighbors as the other 4 added in neighboring pixels
            (int dy, int dx)[] neighbors = new (int, int)[]
            {
                (0, 1),
                (1, 0),
                (1, 1),
                (1, -1)
            };

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int current = y * width + x; //converts 2D position into 1D index
                    byte currentValue = GetColorValue(image[y, x], color);

                    foreach (var (dy, dx) in neighbors)
                    {
                        int ny = y + dy;
                        int nx = x + dx;

                        if (ny >= 0 && ny < height && nx >= 0 && nx < width)
                        {
                            int neighbor = ny * width + nx;
                            byte neighborValue = GetColorValue(image[ny, nx], color);
                            edges.Add(new Edge
                            {
                                pixel1 = current,
                                pixel2 = neighbor,
                                weight = (byte)Math.Abs(currentValue - neighborValue)
                            });
                        }
                    }
                }
            }
            return edges;
        }
        private static byte GetColorValue(RGBPixel pixel, char color)
        {
            if (color == 'R')
            {
                return pixel.red;
            }
            else if (color == 'G')
            {
                return pixel.green;
            }
            else
            {
                return pixel.blue;
            }
        }
        private static void IntersectRegions(DisjointSet red, DisjointSet green, DisjointSet blue, int height, int width)
        {
            int size = height * width;
            regions = new DisjointSet(size);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int current = y * width + x;

                    //right neighbor
                    if (x + 1 < width)
                    {
                        int right = y * width + (x + 1);
                        if (CheckUnion(red, green, blue, current, right))
                        {
                            regions.Union(current, right);
                        }
                    }

                    //bottom neighbor
                    if (y + 1 < height)
                    {
                        int bottom = (y + 1) * width + x;
                        if (CheckUnion(red, green, blue, current, bottom))
                        {
                            regions.Union(current, bottom);
                        }
                    }

                    //bottom right neighbor
                    if (y + 1 < height && x + 1 < width)
                    {
                        int bottomRight = (y + 1) * width + (x + 1);
                        if (CheckUnion(red, green, blue, current, bottomRight))
                        {
                            regions.Union(current, bottomRight);
                        }
                    }

                    //bottom left neighbor
                    if (y + 1 < height && x > 0)
                    {
                        int bottomLeft = (y + 1) * width + (x - 1);
                        if (CheckUnion(red, green, blue, current, bottomLeft))
                        {
                            regions.Union(current, bottomLeft);
                        }
                    }
                }
            }
        }

        private static bool CheckUnion(DisjointSet red, DisjointSet green, DisjointSet blue, int pixel1, int pixel2)
        {
            return red.Find(pixel1) == red.Find(pixel2) &&
                   green.Find(pixel1) == green.Find(pixel2) &&
                   blue.Find(pixel1) == blue.Find(pixel2);
        }
        public static (int, List<int>) GetSegmentationResults(RGBPixel[,] image, double threshold)
        {
            ImageSegmentation(image, threshold);
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

