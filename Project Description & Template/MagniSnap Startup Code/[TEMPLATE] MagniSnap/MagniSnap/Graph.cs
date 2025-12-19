using MagniSnap;
using Priority_Queue;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace MagniSnap
{
    public class Graph
    {
        private RGBPixel[,] imageMatrix;
        private int width;
        private int height;
        private int size;

        private double[] rightWeights;
        private double[] bottomWeights;

        public double[] dist;
        public int[] parent;

        public Graph(RGBPixel[,] image)
        {
            imageMatrix = image;
            height = ImageToolkit.GetHeight(image);
            width = ImageToolkit.GetWidth(image);
            size = width * height;

            rightWeights = new double[size];
            bottomWeights = new double[size];
            dist = new double[size];
            parent = new int[size];
        }

        // ================= GRAPH CONSTRUCTION =================
        public void ConstructGraph()
        {
            int idx = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2D energy = ImageToolkit.CalculatePixelEnergies(x, y, imageMatrix);

                    // Right Edge
                    if (x < width - 1)
                    {
                        // Weight = 1 / Energy. Handle 0 energy (flat color) by giving it a high cost.
                        rightWeights[idx] = (energy.X == 0) ? 1e9 : 1.0 / energy.X;
                    }
                    else
                    {
                        rightWeights[idx] = 1e9; // Border
                    }

                    // Bottom Edge
                    if (y < height - 1)
                    {
                        bottomWeights[idx] = (energy.Y == 0) ? 1e9 : 1.0 / energy.Y;
                    }
                    else
                    {
                        bottomWeights[idx] = 1e9; // Border
                    }

                    idx++;
                }
            }
        }

        // ================= DIJKSTRA =================
        public void DijkstraShortestPath(Point anchor, Point? stopPoint = null)
        {
            // Validate anchor point
            if (anchor.X < 0 || anchor.X >= width || anchor.Y < 0 || anchor.Y >= height)
                throw new ArgumentOutOfRangeException(nameof(anchor), "Anchor must be inside image bounds.");

            int startIndex = anchor.Y * width + anchor.X;
            int targetIndex = stopPoint.HasValue ? (stopPoint.Value.Y * width + stopPoint.Value.X) : -1;

            for (int i = 0; i < size; i++)
            {
                dist[i] = double.MaxValue;
                parent[i] = -1;
            }
            dist[startIndex] = 0;

            SimplePriorityQueue<int, double> pq = new SimplePriorityQueue<int, double>();
            pq.Enqueue(startIndex, 0);
            //MinHeap pq = new MinHeap(size);
            //pq.Enqueue(startIndex, 0);

            while (pq.Count > 0)
            {
                int u = pq.Dequeue();
                double uDist = dist[u];

                // Stop early if we reached the target
                if (u == targetIndex) break;

                // Skip outdated entries in the heap
                if (uDist > dist[u]) continue;

                int ux = u % width; // x-coordinate
                int uy = u / width; // y-coordinate

                // CHECK NEIGHBORS
                int v;
                double weight;
                // LEFT (u - 1)
                if (ux > 0)
                {
                    v = u - 1;
                    weight = rightWeights[v]; // Coming from left, weight is stored in v's rightWeights
                    if (uDist + weight < dist[v])
                    {
                        dist[v] = uDist + weight;
                        parent[v] = u;
                        if(pq.Contains(v))
                            pq.UpdatePriority(v, dist[v]);
                        else
                            pq.Enqueue(v, dist[v]);
                    }
                }

                // RIGHT (u + 1)
                if (ux < width - 1)
                {
                    v = u + 1;
                    weight = rightWeights[u]; // Moving right, weight is stored in current u
                    if (uDist + weight < dist[v])
                    {
                        dist[v] = uDist + weight;
                        parent[v] = u;
                        if(pq.Contains(v)) 
                            pq.UpdatePriority(v, dist[v]);
                        else
                            pq.Enqueue(v, dist[v]);
                    }
                }

                // UP (u - width)
                if (uy > 0)
                {
                    v = u - width;
                    weight = bottomWeights[v]; // Coming from top, weight is stored in v's bottomWeights
                    if (uDist + weight < dist[v])
                    {
                        dist[v] = uDist + weight;
                        parent[v] = u;
                        if (pq.Contains(v))
                            pq.UpdatePriority(v, dist[v]);
                        else
                            pq.Enqueue(v, dist[v]);
                    }
                }

                // DOWN (u + width)
                if (uy < height - 1)
                {
                    v = u + width;
                    weight = bottomWeights[u]; // Moving down, weight is stored in current u
                    if (uDist + weight < dist[v])
                    {
                        dist[v] = uDist + weight;
                        parent[v] = u;
                        if (pq.Contains(v))
                            pq.UpdatePriority(v, dist[v]);
                        else
                            pq.Enqueue(v, dist[v]);
                    }
                }
            }
        }

        public List<Point> backthrough(Point target)
        {
            List<Point> path = new List<Point>();

            // Check if target is within bounds
            if (target.X < 0 || target.X >= width || target.Y < 0 || target.Y >= height)
                return path;

            int currIndex = target.Y * width + target.X;

            // If unreachable, return empty
            if (dist[currIndex] == double.MaxValue) return path;

            // Reconstruct path
            while (currIndex != -1)
            {
                path.Add(new Point(currIndex % width, currIndex / width));
                currIndex = parent[currIndex];
            }

            path.Reverse();
            return path;
        }

        /// <summary>
        /// Draws a path on the PictureBox
        /// </summary>
        public void DrawPath(Graphics g, List<Point> path, PictureBox picBox, Color color, int penWidth)
        {
            if (path == null || path.Count < 2)
                return;

            Pen pen = new Pen(color, penWidth);

            // Convert image coordinates to screen coordinates
            Point[] screenPoints = new Point[path.Count];
            for (int i = 0; i < path.Count; i++)
            {
                screenPoints[i] = GetScreenCoordinates(path[i], picBox);
            }

            // Draw lines connecting the path points
            for (int i = 0; i < screenPoints.Length - 1; i++)
            {
                g.DrawLine(pen, screenPoints[i], screenPoints[i + 1]);
            }

            pen.Dispose();
        }

        /// <summary>
        /// Draws a point (circle) on the PictureBox
        /// </summary>
        public void DrawPoint(Graphics g, Point imagePoint, PictureBox picBox, Color color, int radius)
        {
            Point screenPoint = GetScreenCoordinates(imagePoint, picBox);
            Brush brush = new SolidBrush(color);

            int x = screenPoint.X - radius / 2;
            int y = screenPoint.Y - radius / 2;

            g.FillEllipse(brush, x, y, radius, radius);

            brush.Dispose();
        }

        /// <summary>
        /// Converts image coordinates to screen coordinates for drawing
        /// Returns coordinates relative to PictureBox
        /// </summary>
        public Point GetScreenCoordinates(Point imagePoint, PictureBox picBox)
        {
            if (picBox.Image == null)
                return new Point(-1, -1);

            if (picBox.SizeMode == PictureBoxSizeMode.AutoSize)
            {
                return new Point(imagePoint.X, imagePoint.Y);
            }
            else
            {
                // Calculate scaling factors
                float scaleX = (float)picBox.Width / picBox.Image.Width;
                float scaleY = (float)picBox.Height / picBox.Image.Height;

                int screenX = (int)(imagePoint.X * scaleX);
                int screenY = (int)(imagePoint.Y * scaleY);

                return new Point(screenX, screenY);
            }
        }


        public List<Point> GenerateConnectedPaths(List<Point> anchors)
        {
            List<Point> fullPath = new List<Point>();

            if (anchors == null || anchors.Count < 2)
                return fullPath;

            for (int i = 0; i < anchors.Count - 1; i++)
            {
                Point start = anchors[i];
                Point end = anchors[i + 1];

                DijkstraShortestPath(start, end);
                List<Point> segment = backthrough(end);

                if (segment.Count > 0)
                {
                    if (i > 0) segment.RemoveAt(0);
                    fullPath.AddRange(segment);
                }
            }
            return fullPath;
        }

        // INTERNAL HELPER CLASS: MIN HEAP
        private class MinHeap
        {
            private int[] indices;
            private double[] priorities;
            public int Count { get; private set; }

            public MinHeap(int capacity)
            {
                indices = new int[capacity];
                priorities = new double[capacity];
                Count = 0;
            }

            public void Enqueue(int index, double priority)
            {
                if (Count == indices.Length) Resize();

                indices[Count] = index;
                priorities[Count] = priority;
                HeapifyUp(Count);
                Count++;
            }

            public int Dequeue()
            {
                if (Count == 0) throw new Exception("Queue empty");

                int result = indices[0];
                Count--;
                indices[0] = indices[Count];
                priorities[0] = priorities[Count];
                HeapifyDown(0);
                return result;
            }

            private void HeapifyUp(int i)
            {
                while (i > 0)
                {
                    int p = (i - 1) / 2;
                    if (priorities[p] <= priorities[i]) break;
                    Swap(i, p);
                    i = p;
                }
            }

            private void HeapifyDown(int i)
            {
                while (true)
                {
                    int l = 2 * i + 1;
                    if (l >= Count) break;
                    int r = l + 1;
                    int min = l;
                    if (r < Count && priorities[r] < priorities[l]) min = r;

                    if (priorities[i] <= priorities[min]) break;
                    Swap(i, min);
                    i = min;
                }
            }

            private void Swap(int i, int j)
            {
                int ti = indices[i]; indices[i] = indices[j]; indices[j] = ti;
                double tp = priorities[i]; priorities[i] = priorities[j]; priorities[j] = tp;
            }

            private void Resize()
            {
                int newSize = indices.Length * 2;
                Array.Resize(ref indices, newSize);
                Array.Resize(ref priorities, newSize);
            }
        }
    }
}


//public void DijkstraShortestPath(Point anchor)
//{
//    // Validate anchor point
//    if (anchor.X < 0 || anchor.X >= width || anchor.Y < 0 || anchor.Y >= height)
//        throw new ArgumentOutOfRangeException(nameof(anchor), "Anchor must be inside image bounds.");


//    dist = new double[height, width];
//    parent = new Point[height, width];

//    for (int y = 0; y < height; y++)
//    {
//        for (int x = 0; x < width; x++)
//        {
//            dist[y, x] = double.MaxValue;
//            parent[y, x] = new Point(-1, -1);
//        }
//    }
//    dist[anchor.Y, anchor.X] = 0.0;

//    var pq = new SimplePriorityQueue<Point, double>();
//    pq.Enqueue(anchor, 0.0);

//    int[] dx = { -1, 0, 1, 0 };
//    int[] dy = { 0, -1, 0, 1 };

//    while (pq.Count > 0)
//    {
//        Point current = pq.Dequeue();
//        double currentDist = dist[current.Y, current.X];

//        for (int dir = 0; dir < 4; dir++)
//        {
//            int newX = current.X + dx[dir];
//            int newY = current.Y + dy[dir];

//            if (newX < 0 || newX >= width || newY < 0 || newY >= height)
//                continue;

//            double weight;
//            switch (dir)
//            {

//                //0 1 2 3 
//                //1 x x x
//                //2 x o x
//                //3 x x x
//                case 0: // Left
//                        // edge between (newX,newY) and (current.X,current.Y) — newX = current.X-1
//                    weight = rightWeights[current.Y, newX];
//                    break;
//                case 1: // Up (newY = current.Y - 1)
//                    weight = bottomWeights[newY, current.X];
//                    break;
//                case 2: // Right
//                    weight = rightWeights[current.Y, current.X];
//                    break;
//                case 3: // case 3 Down
//                    weight = bottomWeights[current.Y, current.X];
//                    break;

//                default:
//                    weight = 1e9;
//                    break;
//            }

//            double newDist = currentDist + weight;
//            if (newDist < dist[newY, newX])
//            {
//                dist[newY, newX] = newDist;
//                parent[newY, newX] = current;
//                Point neighbor = new Point(newX, newY);

//                if (pq.Contains(neighbor))
//                    pq.UpdatePriority(neighbor, newDist);
//                else
//                    pq.Enqueue(neighbor, newDist);
//            }
//        }
//    }
//}