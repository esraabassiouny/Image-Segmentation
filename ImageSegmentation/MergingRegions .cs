using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageTemplate
{
    public class MergingRegions
    {
        public static RGBPixel[,] OriginalImageMatrix;
        public static DisjointSet currentRegions;
        public static Dictionary<int, RGBPixel> regionColors;
        public static HashSet<int> selectedRegions = new HashSet<int>();
        public static HashSet<int> mergedRegions = new HashSet<int>();
        public static void MergeSelectedRegions()
        {
            if (selectedRegions.Count < 2) return;

            var enumerator = selectedRegions.GetEnumerator();
            enumerator.MoveNext();
            int firstRegion = enumerator.Current;

            foreach (int region in selectedRegions)
            {
                if (region != firstRegion)
                {
                    currentRegions.Union(region, firstRegion);
                }
            }
            mergedRegions.Add(firstRegion);
            selectedRegions.Clear();
        }

        public static RGBPixel[,] UpdateMergedOriginal()
        {
            int height = ImageOperations.GetHeight(OriginalImageMatrix);
            int width = ImageOperations.GetWidth(OriginalImageMatrix);
            RGBPixel[,] resultImage = new RGBPixel[height, width];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    int root = currentRegions.Find(index);

                    if (mergedRegions.Contains(root))
                    {
                        resultImage[y, x] = OriginalImageMatrix[y, x];
                    }
                    else
                    {
                        resultImage[y, x] = regionColors[root];
                    }
                }
            }

            HighlightSelectedRegions(resultImage);

            return resultImage;
        }

        public static void HighlightSelectedRegions(RGBPixel[,] image)
        {
            int height = image.GetLength(0);
            int width = image.GetLength(1);

            foreach (int region in selectedRegions)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = y * width + x;
                        if (currentRegions.Find(index) == region)
                        {
                            bool isBorder = false;
                            if (x > 0 && currentRegions.Find(y * width + (x - 1)) != region) isBorder = true;
                            if (x < width - 1 && currentRegions.Find(y * width + (x + 1)) != region) isBorder = true;
                            if (y > 0 && currentRegions.Find((y - 1) * width + x) != region) isBorder = true;
                            if (y < height - 1 && currentRegions.Find((y + 1) * width + x) != region) isBorder = true;

                            if (isBorder)
                            {
                                image[y, x].red = 0;
                                image[y, x].green = 0;
                                image[y, x].blue = 0;
                            }
                        }
                    }
                }
            }
        }

        public static RGBPixel[,] ColorSegmentedRegions(RGBPixel[,] originalImage, DisjointSet regions)
        {
            int height = ImageOperations.GetHeight(originalImage);
            int width = ImageOperations.GetWidth(originalImage);
            RGBPixel[,] coloredImage = new RGBPixel[height, width];

            regionColors = new Dictionary<int, RGBPixel>();

            Random rand = new Random();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    int root = regions.Find(index);

                    if (!regionColors.ContainsKey(root))
                    {
                        regionColors[root] = new RGBPixel
                        {
                            red = (byte)rand.Next(0, 256),
                            green = (byte)rand.Next(0, 256),
                            blue = (byte)rand.Next(0, 256)
                        };
                    }

                    coloredImage[y, x] = regionColors[root];
                }
            }

            return coloredImage;
        }
    }
}
