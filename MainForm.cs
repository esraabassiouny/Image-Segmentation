using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageTemplate
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        RGBPixel[,] ImageMatrix;

        private void btnOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //Open the browsed image and display it
                string OpenedFilePath = openFileDialog1.FileName;
                ImageMatrix = ImageOperations.OpenImage(OpenedFilePath);
                ImageOperations.DisplayImage(ImageMatrix, pictureBox1);
            }
            txtWidth.Text = ImageOperations.GetWidth(ImageMatrix).ToString();
            txtHeight.Text = ImageOperations.GetHeight(ImageMatrix).ToString();
        }

        private async void btnGaussSmooth_Click(object sender, EventArgs e)
        {
            try
            {
                double sigma = double.Parse(txtGaussSigma.Text);
                int maskSize = (int)nudMaskSize.Value;
                double k = double.Parse(textthreshold.Text);

                MessageBox.Show("Starting image processing...", "Status", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Apply Gaussian filter
                var processedImage = await Task.Run(() => ImageOperations.GaussianFilter1D(ImageMatrix, maskSize, sigma));
                ImageMatrix = processedImage;
                ImageOperations.DisplayImage(ImageMatrix, pictureBox2);

                MessageBox.Show("Gaussian smoothing completed. Starting segmentation...", "Status", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Perform segmentation and get results
                var (segmentCount, segmentSizes) = await Task.Run(() => ImageOperations.GetSegmentationResults(ImageMatrix, k));

                // Color and display the segmented image
                var coloredImage = ColorSegmentedRegions(ImageMatrix, ImageOperations.regions);
                ImageOperations.DisplayImage(coloredImage, pictureBox2);

                MessageBox.Show($"Segmentation completed. Found {segmentCount} regions.", "Status", MessageBoxButtons.OK, MessageBoxIcon.Information);
                foreach(int size in segmentSizes) 
                {
                   MessageBox.Show($"[{size}] {size}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during processing:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private RGBPixel[,] ColorSegmentedRegions(RGBPixel[,] originalImage, DisjointSet regions)
        {
            int height = ImageOperations.GetHeight(originalImage);
            int width = ImageOperations.GetWidth(originalImage);
            RGBPixel[,] coloredImage = new RGBPixel[height, width];

            // Create a random color for each region
            Dictionary<int, RGBPixel> regionColors = new Dictionary<int, RGBPixel>();
            Random rand = new Random();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    int root = regions.Find(index);

                    if (!regionColors.ContainsKey(root))
                    {
                        // Generate a random color for this region
                        regionColors[root] = new RGBPixel
                        {
                            red = (byte)rand.Next(50, 256),  // Avoid too dark colors
                            green = (byte)rand.Next(50, 256),
                            blue = (byte)rand.Next(50, 256)
                        };
                    }

                    coloredImage[y, x] = regionColors[root];
                }
            }

            return coloredImage;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

    }
}