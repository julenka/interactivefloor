using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace DepthViewer
{
    public class ConnectedComponents
    {
        public int NumComponents { get; set; }
        private int[] data;
        public int[] Data { get { return data; } set { data = value; } }

        private Point[] centroids;

        public Point[] Centroids
        {
            get { return centroids; }
            set { centroids = value; }
        }

        private Blob[] blobs;

        internal Blob[] Blobs
        {
            get { return blobs; }
            set { blobs = value; }
        }

        public static ConnectedComponents GetConnectedComponents(ushort[] input, int width, int height, int biggerThan)
        {
            // Do the label pass
            int currentLabel = 1;
            int[] data = new int[input.Length];
            DisjointSets sets = new DisjointSets();
            sets.AddElements(1);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    if (input[index] == 0)
                    {
                        data[index] = 0;
                        continue;
                    }
                    int[] neighborLabels = new int[4];
                    int labelIndex = 0;
                    if (x > 0)
                    {
                        int leftI = y * width + (x - 1);
                        if(input[leftI] > 0){
                            neighborLabels[labelIndex++] = data[leftI];
                        }
                        if (y > 0)
                        {
                            int topLeftI = (y - 1) * width + (x - 1);
                            if (input[topLeftI] > 0)
                            {
                                neighborLabels[labelIndex++] = data[topLeftI];
                            }
                        }
                    }
                    if (y > 0)
                    {
                        int topI = (y - 1) * width + x;
                        if (input[topI] > 0)
                        {
                            neighborLabels[labelIndex++] = data[topI];
                        }
                        if (x < width - 1)
                        {
                            int topRightI = (y-1) * width + x + 1;
                            if (input[topRightI] > 0)
                            {
                                neighborLabels[labelIndex++] = data[topRightI];
                            }
                        }
                    }

                    if (labelIndex == 0)
                    {
                        data[index] = currentLabel++;
                        sets.AddElements(1);
                    }
                    else if (labelIndex == 1)
                    {
                        data[index] = neighborLabels[0];
                    }
                    else
                    {
                        // count is larger than 0, set label to first label and make sure all sets are equivalent
                        data[index] = neighborLabels[0];
                        for (int i = 0; i < labelIndex; i++)
                        {
                                sets.Union(neighborLabels[0], neighborLabels[i]);
                        }
                    }
                }


            }
            Dictionary<int, int> map = new Dictionary<int, int>();
            int maxIndex = 0;
            // do another pass to relabel according to the member sets
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    int setLabel = data[index];
                    int parentLabel = sets.FindSet(setLabel);
                    if (!map.ContainsKey(parentLabel))
                    {
                        map[parentLabel] = maxIndex++;
                    }
                    data[index] = map[parentLabel];
                }
            }

            // Kill elements that have less than greaterThan
            int[] counts = new int[maxIndex ];
            for (int i = 0; i < data.Length; i++)
            {
                counts[data[i]]++;
            }
            for (int i =0; i < data.Length; i++)
            {
                if (counts[data[i]] < biggerThan)
                {
                    data[i] = 0;
                }
            }

            map = new Dictionary<int, int>();
            maxIndex = 0;
            // do another pass to relabel according to the member sets
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    int setLabel = data[index];
                    if (!map.ContainsKey(setLabel))
                    {
                        map[setLabel] = maxIndex++;
                    }
                    data[index] = map[setLabel];
                }
            }
            // redo the counts with the new blob ids
            counts = new int[maxIndex];
            for (int i = 0; i < data.Length; i++)
            {
                counts[data[i]]++;
            }

            // Get the blobs from the data
            Blob[] blobs = new Blob[maxIndex - 1];
            Point[] centroids = new Point[maxIndex - 1];
            // get blob specs
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = y * width + x;
                    int blobId = data[i] - 1;
                    if (blobId >= 0)
                    {
                        if (blobs[blobId] == null)
                        {
                            blobs[blobId] = new Blob();
                            blobs[blobId].center = new System.Drawing.Point();
                            blobs[blobId].bounds = new Rectangle(x, y, 0, 0);
                        }
                        blobs[blobId].center.X += x;
                        blobs[blobId].center.Y += y;
                        centroids[blobId].X += x;
                        centroids[blobId].Y += y;

                        if (x < blobs[blobId].bounds.Left)
                        {
                            blobs[blobId].bounds.X = x;
                        }
                        if (y > blobs[blobId].bounds.Bottom)
                        {
                            blobs[blobId].bounds.Height = y - blobs[blobId].bounds.Y;
                        }
                        if (x > blobs[blobId].bounds.Right)
                        {
                            blobs[blobId].bounds.Width = x - blobs[blobId].bounds.X;
                        }
                    }
                }
            }

            for (int i = 0; i < centroids.Length; i++)
            {
                centroids[i].X /= counts[i + 1];
                centroids[i].Y /= counts[i + 1];
                blobs[i].center.X /= counts[i + 1];
                blobs[i].center.Y /= counts[i + 1];

            }

            // Determine whether the top or bottom half is heavier for each blob.
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = y * width + x;
                    int blobId = data[i] - 1;
                    if (blobId >= 0)
                    {
                        if (y < blobs[blobId].bounds.Top + blobs[blobId].bounds.Height / 2)
                        {
                            blobs[blobId].topCount++;
                        }
                        else
                        {
                            blobs[blobId].bottomCount++;
                        }
                    }
                }
            }

            return new ConnectedComponents { Data = data, NumComponents = sets.SetCount, Centroids = centroids, Blobs = blobs };

        }

    }
}
