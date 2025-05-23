using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;

namespace ImageTemplate
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            this.KeyPreview = true;
        }

        RGBPixel[,] ImageMatrix;


        private void btnOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string OpenedFilePath = openFileDialog1.FileName;
                ImageMatrix = ImageOperations.OpenImage(OpenedFilePath);
                MergingRegions.OriginalImageMatrix = (RGBPixel[,])ImageMatrix.Clone();
                ImageOperations.DisplayImage(ImageMatrix, pictureBox1);
                MergingRegions.mergedRegions.Clear();
            }
            txtWidth.Text = ImageOperations.GetWidth(ImageMatrix).ToString();
            txtHeight.Text = ImageOperations.GetHeight(ImageMatrix).ToString();
        }

        private async void btnSegmentation_Click(object sender, EventArgs e)
        {
            double sigma = double.Parse(txtGaussSigma.Text);
            int maskSize = (int)nudMaskSize.Value;
            double threshold = double.Parse(textthreshold.Text);

            if (checkBox1.Checked)
            {
                var processedImage = await Task.Run(() => ImageOperations.GaussianFilter1D(ImageMatrix, maskSize, sigma));
                ImageMatrix = processedImage;
            }

            Stopwatch timer = Stopwatch.StartNew();
            // apply segmentation
            var (segmentCount, segmentSizes) = await Task.Run(() => ImageOperations.GetSegmentationResults(ImageMatrix, threshold));

            MergingRegions.currentRegions = ImageOperations.regions;
            MergingRegions.mergedRegions.Clear();
            // Color Segmented Regions
            var coloredImage = MergingRegions.ColorSegmentedRegions(ImageMatrix, MergingRegions.currentRegions);
            timer.Stop();
            long time = timer.ElapsedMilliseconds;

            ImageOperations.DisplayImage(coloredImage, pictureBox2);
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "bmp files (*.bmp)|*.bmp|All files (*.*)|*.*";
            saveFileDialog1.RestoreDirectory = true;
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                pictureBox2.Image.Save(saveFileDialog1.FileName, ImageFormat.Bmp);
            }

            string outputFilePath = Path.Combine(Directory.GetParent(Application.StartupPath)
                .Parent.Parent.FullName,
                "output.txt");
            using (StreamWriter writer = new StreamWriter(outputFilePath))
            {
                writer.WriteLine("time = " + time);
                writer.WriteLine(segmentCount);
                foreach (int size in segmentSizes)
                {
                    writer.WriteLine(size);
                }
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
        }

        private void pictureBox2_MouseClick_1(object sender, MouseEventArgs e)
        {
            if (MergingRegions.currentRegions == null) return;

            int width = pictureBox2.Image.Width;
            int height = pictureBox2.Image.Height;

            int x = (e.X);
            int y = (e.Y);

            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                int index = y * width + x;
                int root = MergingRegions.currentRegions.Find(index);
                MergingRegions.selectedRegions.Add(root);
                var resultImage = MergingRegions.UpdateMergedOriginal();
                ImageOperations.DisplayImage(resultImage, pictureBox2);

            }
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.M && MergingRegions.selectedRegions.Count > 1)
            {
                MergingRegions.MergeSelectedRegions();
                var resultImage = MergingRegions.UpdateMergedOriginal();
                ImageOperations.DisplayImage(resultImage, pictureBox2);
            }

        }
    }
}