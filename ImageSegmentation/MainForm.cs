using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace ImageTemplate
{
    public partial class MainForm : Form
    {
        private HashSet<int> selectedSegments = new HashSet<int>();
        private RGBPixel[,] segmentedImage;
        private DisjointSet segmentedSet;
        private int imageHeight;
        private int imageWidth;
        private RGBPixel[,] originalImage; 
        Dictionary<int, int> dict;
        int[,] finallabels;

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
                
                string OpenedFilePath = openFileDialog1.FileName;
                ImageMatrix = ImageOperations.OpenImage(OpenedFilePath);
                originalImage = (RGBPixel[,])ImageMatrix.Clone(); 
                ImageOperations.DisplayImage(ImageMatrix, pictureBox1);
            }
            txtWidth.Text = ImageOperations.GetWidth(ImageMatrix).ToString();
            txtHeight.Text = ImageOperations.GetHeight(ImageMatrix).ToString();
            imageWidth = ImageOperations.GetWidth(ImageMatrix);
            imageHeight = ImageOperations.GetHeight(ImageMatrix);
        }

        private void btnGaussSmooth_Click(object sender, EventArgs e)
        {
            double sigma = double.Parse(txtGaussSigma.Text);
            int maskSize = (int)nudMaskSize.Value;
            RGBPixel[,] Bluredimg = ImageOperations.GaussianFilter1D(ImageMatrix, maskSize, sigma);
            int height = ImageOperations.GetHeight(Bluredimg),
                width = ImageOperations.GetWidth(Bluredimg);
            DisjointSet[] channelMSTs = new DisjointSet[3];
            float k = float.Parse(textBox1.Text);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var (redGraph, greenGraph, blueGraph, finalLabels) = DSConstruction.BuildGraph(height, width, Bluredimg);
            channelMSTs[0] = DSConstruction.SegmentGraph(redGraph, height, width, k);
            channelMSTs[1] = DSConstruction.SegmentGraph(greenGraph, height, width, k);
            channelMSTs[2] = DSConstruction.SegmentGraph(blueGraph, height, width, k);
             (finallabels, dict, segmentedSet) = Segmentation.CreateSegments(channelMSTs[0], channelMSTs[1], channelMSTs[2], height, width, finalLabels);
            segmentedImage = Visualization.VisualizeSegments(segmentedSet, width, height);
            stopwatch.Stop();
            long time = stopwatch.ElapsedMilliseconds;
            ImageOperations.DisplayImage(segmentedImage, pictureBox2);
            selectedSegments.Clear();
            btnMerge.Visible = false;
            Segmentation.OutputWriterr(finallabels, dict, "D:\\ALgooooo\\Image-Segmentation\\OUTT.txt");


            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "bmp files (*.bmp)|*.bmp|All files (*.*)|*.*";
            saveFileDialog1.RestoreDirectory = true;
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if (pictureBox2.Image != null)
                {
                    pictureBox2.Image.Save(saveFileDialog1.FileName, ImageFormat.Bmp);
                }
                else
                {
                    MessageBox.Show("No image to save. Error arg3 shof el satr bta3 el open tany");
                }
            }

            string elapsedTimeMessage = $"\n Time: {time} ";

            MessageBox.Show(elapsedTimeMessage, "Debug Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        private void highlight_selected_segment()
        {
            if (segmentedImage == null || segmentedSet == null) return;

            //ha3mel copy mel segmented image a highlight 3aleha l selected regions w thb2a overlay on l original
            RGBPixel[,] highlighted = (RGBPixel[,])segmentedImage.Clone();

            for (int y = 0; y < imageHeight; y++)
            {
                for (int x = 0; x < imageWidth; x++)
                {
                    int pixelIndex = y * imageWidth + x;
                    int segmentId = segmentedSet.Find(pixelIndex);
                    if (selectedSegments.Contains(segmentId))
                    {
                        //highlight in bright yellow 
                        highlighted[y, x].red = 255;
                        highlighted[y, x].green = 255;
                        highlighted[y, x].blue = 0;
                    }
                }
            }

            //display the highlighted segments , overlay 3ala l original
            ImageOperations.DisplayImage(highlighted, pictureBox2);
        }

        private void pictureBox2_Click(object sender, MouseEventArgs e)
        {
            if (segmentedImage == null || segmentedSet == null) return;

            //ba get l pixel coordinates elly l user das 3aleha
            int x = e.X;
            int y = e.Y;

            //lazem l clicked part tkon within boundaries l image
            if (x < 0 || x >= imageWidth || y < 0 || y >= imageHeight)
                return;

            // Convert coordinates to pixel index and find segment ID
            int pixelIndex = y * imageWidth + x;
            int segmentId = segmentedSet.Find(pixelIndex);

            // lw ana dost 3la haga kan already ma3molaha select, byt3mlha unselect(remove mn l selected segments)
            if (selectedSegments.Contains(segmentId))
                selectedSegments.Remove(segmentId);
            else
                selectedSegments.Add(segmentId);

            // then ba3d ma 3malt update lel selected segments list, ba call l highlight to color the selected segments
            //kol mara ba3mel update lel image 3shan a show l selected
            highlight_selected_segment();

            // Show the merge button if multiple segments are selected
            btnMerge.Visible = selectedSegments.Count > 1;
        }

        private void btnMerge_Click(object sender, EventArgs e)
        {
            if (segmentedImage == null || segmentedSet == null || selectedSegments.Count < 2) return;

            //ba merge(union) kol l selected m3 awel wahed
            int mergedSegment = selectedSegments.First();

            //merge all selected segments into the target
            foreach (var segment in selectedSegments)
            {
                if (segment == mergedSegment) continue;
                segmentedSet.Union(mergedSegment, segment,0.0);
            }

            //clear selection(highlight)
            selectedSegments.Clear();
            btnMerge.Visible = false;

            //ha visualize the selected segments ONLY l mara de
            segmentedImage = Visualization.VisualizeSegments(segmentedSet, imageWidth, imageHeight);

            // image view gedida ha show feeha l merged segments ONLY bel original colors, wel backgroung black
            RGBPixel[,] mergedSegmentView = new RGBPixel[imageHeight, imageWidth];

            //ha initialize l image b black background, b3dha arsm l selected segments 3aleha (original)
            for (int y = 0; y < imageHeight; y++)
            {
                for (int x = 0; x < imageWidth; x++)
                {
                    mergedSegmentView[y, x].red = 0;
                    mergedSegmentView[y, x].green = 0;
                    mergedSegmentView[y, x].blue = 0;
                }
            }

            
            //int finalmergedSegment = segmentedSet.Find(mergedSegment);

            //ha copy l pixels elly fel target segment bs bel orignla colors
            for (int y = 0; y < imageHeight; y++)
            {
                for (int x = 0; x < imageWidth; x++)
                {
                    int pixelIndex = y * imageWidth + x;
                    if (segmentedSet.Find(pixelIndex) == mergedSegment)
                    {
                        mergedSegmentView[y, x] = originalImage[y, x];
                    }
                }
            }

            //final result -> show the merged segment in original color with black background
            ImageOperations.DisplayImage(mergedSegmentView, pictureBox2);

            //MessageBox.Show("Segments merged successfully. Showing only the merged segment with original colors.");
        }

        private void run_knn(object sender, EventArgs e)
        {
            double sigma = double.Parse(txtGaussSigma.Text);
            int maskSize = (int)nudMaskSize.Value;
            RGBPixel[,] Bluredimg = ImageOperations.GaussianFilter1D(ImageMatrix, maskSize, sigma);


            int height = ImageOperations.GetHeight(Bluredimg),
                width = ImageOperations.GetWidth(Bluredimg);

            DisjointSet[] channelMSTs = new DisjointSet[3];

            float k = float.Parse(textBox1.Text);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            var graph = KdTree.BuildGraph(height, width, Bluredimg);


            channelMSTs[0] = DSConstruction.SegmentGraph(graph, height, width, k);

            RGBPixel[,] displayGrid = Visualization.VisualizeSegments(channelMSTs[0], width, height);

            stopwatch.Stop();
            long time = stopwatch.ElapsedMilliseconds;
            ImageOperations.DisplayImage(displayGrid, pictureBox2);

            string elapsedTimeMessage = $"\n Time: {time} ";

            MessageBox.Show(elapsedTimeMessage, "Debug Info", MessageBoxButtons.OK, MessageBoxIcon.Information);

        }
    }
}
