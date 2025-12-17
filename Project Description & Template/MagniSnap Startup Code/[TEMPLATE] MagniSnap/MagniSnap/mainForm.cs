using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MagniSnap
{
    #region
    /// 4d17639adfad0a300acd78759e07a4f2
    #endregion
    public partial class MainForm : Form
    {
        RGBPixel[,] ImageMatrix;
        bool isLassoEnabled = false;
        
        // Graph and path drawing variables
        Graph imageGraph;
        bool isGraphConstructed = false;
        Point currentAnchor = new Point(-1, -1);
        Point currentFreePoint = new Point(-1, -1);
        List<Point> currentPath;

        public MainForm()
        {
            InitializeComponent();
            indicator_pnl.Hide();
            
            // Initialize path drawing variables
            currentPath = new List<Point>();
            
            // Add Paint event handler to PictureBox for drawing paths
            mainPictureBox.Paint += MainPictureBox_Paint;
        }

        private void menuButton_Click(object sender, EventArgs e)
        {
            #region Do Change Remove Template Code
            /// 4d17639adfad0a300acd78759e07a4f2
            #endregion

            indicator_pnl.Top = ((Control)sender).Top;
            indicator_pnl.Height = ((Control)sender).Height;
            indicator_pnl.Left = ((Control)sender).Left;
            ((Control)sender).BackColor = Color.FromArgb(37, 46, 59);
            indicator_pnl.Show();
        }

        private void menuButton_Leave(object sender, EventArgs e)
        {
            ((Control)sender).BackColor = Color.FromArgb(26, 32, 40);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {         
            Application.Exit();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            #region Do Change Remove Template Code
            /// 4d17639adfad0a300acd78759e07a4f2
            #endregion

            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //Open the browsed image and display it
                string OpenedFilePath = openFileDialog1.FileName;
                ImageMatrix = ImageToolkit.OpenImage(OpenedFilePath);
                ImageToolkit.ViewImage(ImageMatrix, mainPictureBox);

                int width = ImageToolkit.GetWidth(ImageMatrix);
                txtWidth.Text = width.ToString();
                int height = ImageToolkit.GetHeight(ImageMatrix);
                txtHeight.Text = height.ToString();
                
                // Reset graph when new image is loaded
                isGraphConstructed = false;
                imageGraph = null;
                currentPath.Clear();
                currentAnchor = new Point(-1, -1);
                currentFreePoint = new Point(-1, -1);
            }
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Clear all paths and reset state
            currentPath.Clear();
            currentAnchor = new Point(-1, -1);
            currentFreePoint = new Point(-1, -1);
            mainPictureBox.Refresh();
        }

        private void btnLivewire_Click(object sender, EventArgs e)
        {
            menuButton_Click(sender, e);

            mainPictureBox.Cursor = Cursors.Cross;

            isLassoEnabled = true;
        }

        private void btnLivewire_Leave(object sender, EventArgs e)
        {
            menuButton_Leave(sender, e);

            mainPictureBox.Cursor = Cursors.Default;
            isLassoEnabled = false;
        }

        private void mainPictureBox_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (ImageMatrix != null && isLassoEnabled)
                {
                    // Get image coordinates from mouse position
                    Point imagePoint = GetImageCoordinates(e.Location);
                    
                    // Validate point is within image bounds
                    int width = ImageToolkit.GetWidth(ImageMatrix);
                    int height = ImageToolkit.GetHeight(ImageMatrix);
                    
                    if (imagePoint.X >= 0 && imagePoint.X < width && 
                        imagePoint.Y >= 0 && imagePoint.Y < height)
                    {
                        // Construct graph if not already done
                        if (!isGraphConstructed)
                        {
                            imageGraph = new Graph(ImageMatrix);
                            imageGraph.ConstructGraph();
                            isGraphConstructed = true;
                        }
                        
                        // Set anchor point
                        currentAnchor = imagePoint;
                        
                        // Run Dijkstra from anchor point to all pixels
                        imageGraph.DijkstraShortestPath(currentAnchor);
                        
                        // Clear current path (will be recalculated on mouse move)
                        currentPath.Clear();
                        
                        // Refresh to redraw
                        mainPictureBox.Refresh();
                    }
                }
            }
        }

        private void mainPictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            txtMousePosX.Text = e.X.ToString();
            txtMousePosY.Text = e.Y.ToString();

            if (ImageMatrix != null && isLassoEnabled && isGraphConstructed)
            {
                // Get image coordinates from mouse position
                Point imagePoint = GetImageCoordinates(e.Location);
                
                // Validate point is within image bounds
                int width = ImageToolkit.GetWidth(ImageMatrix);
                int height = ImageToolkit.GetHeight(ImageMatrix);
                
                if (imagePoint.X >= 0 && imagePoint.X < width && 
                    imagePoint.Y >= 0 && imagePoint.Y < height)
                {
                    currentFreePoint = imagePoint;
                    
                    // Backtrack path from free point to anchor point
                    currentPath = imageGraph.backthrough(currentFreePoint);
                    
                    // Refresh to redraw path
                    mainPictureBox.Refresh();
                }
            }
        }

        private void panel6_Paint(object sender, PaintEventArgs e)
        {

        }
        
        /// <summary>
        /// Paint event handler for PictureBox to draw the livewire path
        /// </summary>
        private void MainPictureBox_Paint(object sender, PaintEventArgs e)
        {
            if (ImageMatrix == null || !isLassoEnabled)
                return;
            
            PictureBox picBox = sender as PictureBox;
            if (picBox.Image == null)
                return;
            
            Graphics g = e.Graphics;
            
            // Draw current path from anchor to free point
            if (currentPath != null && currentPath.Count > 1 && imageGraph != null)
            {
                imageGraph.DrawPath(g, currentPath, picBox, Color.Red, 2);
            }
            
            // Draw anchor point
            if (currentAnchor.X >= 0 && currentAnchor.Y >= 0 && imageGraph != null)
            {
                imageGraph.DrawPoint(g, currentAnchor, picBox, Color.Green, 5);
            }
            
            // Draw current free point (mouse position)
            if (isGraphConstructed && currentFreePoint.X >= 0 && currentFreePoint.Y >= 0 && imageGraph != null)
            {
                imageGraph.DrawPoint(g, currentFreePoint, picBox, Color.Blue, 3);
            }
        }
        
        /// <summary>
        /// Converts screen coordinates (mouse position) to image coordinates
        /// Mouse coordinates are relative to the PictureBox
        /// </summary>
        private Point GetImageCoordinates(Point screenPoint)
        {
            if (mainPictureBox.Image == null)
                return new Point(-1, -1);
            
            // Mouse coordinates are already relative to PictureBox
            int relativeX = screenPoint.X;
            int relativeY = screenPoint.Y;
            
            // Clamp to PictureBox bounds
            if (relativeX < 0) relativeX = 0;
            if (relativeY < 0) relativeY = 0;
            if (relativeX >= mainPictureBox.Width) relativeX = mainPictureBox.Width - 1;
            if (relativeY >= mainPictureBox.Height) relativeY = mainPictureBox.Height - 1;
            
            // If PictureBox is using AutoSize, coordinates map directly
            // Otherwise, we need to account for scaling
            if (mainPictureBox.SizeMode == PictureBoxSizeMode.AutoSize)
            {
                // Clamp to image bounds
                int width = ImageToolkit.GetWidth(ImageMatrix);
                int height = ImageToolkit.GetHeight(ImageMatrix);
                if (relativeX >= width) relativeX = width - 1;
                if (relativeY >= height) relativeY = height - 1;
                return new Point(relativeX, relativeY);
            }
            else
            {
                // Calculate scaling factors
                float scaleX = (float)mainPictureBox.Image.Width / mainPictureBox.Width;
                float scaleY = (float)mainPictureBox.Image.Height / mainPictureBox.Height;
                
                int imageX = (int)(relativeX * scaleX);
                int imageY = (int)(relativeY * scaleY);
                
                // Clamp to image bounds
                int width = ImageToolkit.GetWidth(ImageMatrix);
                int height = ImageToolkit.GetHeight(ImageMatrix);
                if (imageX >= width) imageX = width - 1;
                if (imageY >= height) imageY = height - 1;
                
                return new Point(imageX, imageY);
            }
        }

        private void mainPictureBox_Click(object sender, EventArgs e)
        {

        }
    }
}
