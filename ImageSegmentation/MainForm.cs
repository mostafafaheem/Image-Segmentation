using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

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

        private void btnGaussSmooth_Click(object sender, EventArgs e)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            double sigma = double.Parse(txtGaussSigma.Text);
            int maskSize = (int)nudMaskSize.Value;

            int height = ImageMatrix.GetLength(0),
                width = ImageMatrix.GetLength(1),
                numVertices = width * height;

            // El blurring beybawaz el segments fel sample test cases
            ImageMatrix = ImageOperations.GaussianFilter1D(ImageMatrix, maskSize, 0.8);

            List<Edge> redGraph = DSConstruction.ConstructEightNeighbourGraph(ImageMatrix, 'r'),
                       greenGraph = DSConstruction.ConstructEightNeighbourGraph(ImageMatrix, 'g'),
                       blueGraph = DSConstruction.ConstructEightNeighbourGraph(ImageMatrix, 'b');
            DisjointSet[] channelMSTs = new DisjointSet[3];

            channelMSTs[0] = DSConstruction.ConstructMST(redGraph, numVertices, (float)sigma);
            channelMSTs[1] = DSConstruction.ConstructMST(greenGraph, numVertices, (float)sigma);
            channelMSTs[2] = DSConstruction.ConstructMST(blueGraph, numVertices, (float)sigma);

            DisjointSet segmentedSet = Segmentation.CreateSegments(channelMSTs, numVertices, height, width);



            int numSegments = segmentedSet.GetNumSets();
            RGBPixel[,] displayGrid = Visualization.VisualizeSegments(segmentedSet, width, height);
            stopwatch.Stop();
            ImageOperations.DisplayImage(displayGrid, pictureBox2);




            TimeSpan elapsedTime = stopwatch.Elapsed;
            string elapsedTimeMessage = $"Total segments created: {numSegments}\nElapsed Time: {elapsedTime.TotalSeconds:F3} seconds";


            MessageBox.Show(elapsedTimeMessage, "Debug Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }
    }
}