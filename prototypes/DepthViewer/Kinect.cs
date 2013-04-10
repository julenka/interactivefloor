using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using xn;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Threading;
using System.Windows.Media.Media3D;
using System.Runtime.InteropServices;
using System.Net;

namespace DepthViewer
{
    /// <summary>
    /// Singleton class for using the Kinect.
    /// Sealed to prevent inheritance of a singleton class.
    /// </summary>
    public sealed class Kinect
    {
        // The private Kinect instance
        private static Kinect instance;

        private Context context;
        private DepthGenerator depth;
        private ImageGenerator rgb;
        private int[] depthHistogram;
        private byte[] histogramImage;
        private byte[] rgbData;
        private ushort[] depthData;
        private ushort[] oldDepthData;
        private int depthWidth;
        private int averageWindowSize = 0;
        private bool averageOverTime;



        #region Properties

        public bool AverageOverTime
        {
            get { return averageOverTime; }
            set { averageOverTime = value; }
        }
        public int AverageWindowSize
{
  get { return averageWindowSize; }
  set { averageWindowSize = value; }
}
        public byte[] HistogramImage
        {
            get
            {
                UpdateDepth();
                CalcHist();
                return histogramImage;
            }
        }
        public int DepthWidth
        {
            get { return depthWidth; }
        }
        private int depthHeight;

        public int DepthHeight
        {
            get { return depthHeight; }
        }
        private int depthOffsetY;

        public int DepthOffsetY
        {
            get { return depthOffsetY; }
        }
        private int depthOffsetX;

        public int DepthOffsetX
        {
            get { return depthOffsetX; }
        }
        private int rgbWidth;

        public int RgbWidth
        {
            get { return rgbWidth; }
        }
        private int rgbHeight;

        public int RgbHeight
        {
            get { return rgbHeight; }
        }
        public byte[] RGB
        {
            get
            {
                return rgbData;
            }
        }

        public ushort[] Depth
        {
            get
            {
                return depthData;
            }
        }

        #endregion

        // Lock for thread-safe operations
        private static readonly object padlock = new object();

        private Kinect()
        {
            this.Initialize();
        }

        // Gets the instance of the Kinect
        public static Kinect Instance
        {
            get
            {
                // Lock, since multiple threads could create more than one instance.
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new Kinect();
                    }
                    return instance;
                }
            }
        }


        public void Initialize()
        {
            context = new Context(@"..\..\..\data\openniconfig.xml");
            depth = context.FindExistingNode(NodeType.Depth) as DepthGenerator;
            rgb = context.FindExistingNode(NodeType.Image) as ImageGenerator;
            if (depth == null)
                throw new Exception(@"Error in Data\openniconfig.xml. No depth node found.");
            if (rgb == null)
                throw new Exception(@"Error in Data\openniconfig.xml. No rgb node found.");
            MapOutputMode mapMode = depth.GetMapOutputMode();

            // Initialize member variables
            depthHistogram = new int[depth.GetDeviceMaxDepth()];

            // initialize rgb array
            xn.ImageMetaData rgbMD = rgb.GetMetaData();
            rgbData = new byte[rgbMD.XRes * rgbMD.YRes * 3];
            rgbWidth = rgbMD.XRes;
            rgbHeight = rgbMD.YRes;

            xn.DepthMetaData depthMD = depth.GetMetaData();
            depthData = new ushort[depthMD.XRes * depthMD.YRes];
            oldDepthData = new ushort[depthMD.XRes * depthMD.YRes];
            histogramImage = new byte[depthMD.XRes * depthMD.YRes * 3];
            depthWidth = depthMD.XRes;
            depthHeight = depthMD.YRes;
            depthOffsetX = depthMD.XOffset;
            depthOffsetY = depthMD.YOffset;

            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 30);
            dispatcherTimer.Start();
        }

        private void UpdateContext()
        {
            context.WaitAndUpdateAll();
            UpdateRGB();
            UpdateDepth();
        }

        public unsafe void UpdateRGB()
        {
            byte* pRGB = (byte*)this.rgb.GetImageMapPtr().ToPointer();
            
            // copy pixels
            for (int i = 0; i < rgbData.Length; i++)
            {
                rgbData[i] = pRGB[i];
            }
        }

        public unsafe void UpdateDepth()
        {
            lock (depthData)
            {
                oldDepthData = depthData;
                ushort* pDepth = (ushort*)depth.GetDepthMapPtr().ToPointer();
                for (int i = 0; i < depthData.Length; i++, pDepth++)
                {
                    if (averageOverTime)
                    {
                        depthData[i] = (ushort)((*pDepth + oldDepthData[i]) / 2);
                    }
                    else
                    {
                        depthData[i] = *pDepth;
                    }
                }
                if (averageWindowSize > 0)
                {
                    ushort[] newDepth = new ushort[depthData.Length];
                    for (int y = 0; y < depthHeight; y++)
                    {
                        for (int x = 0; x < depthWidth; x++)
                        {
                            newDepth[y * depthWidth + x] = AverageDepthAroundPoint(x, y, averageWindowSize, averageWindowSize);
                        }
                        depthData = newDepth;
                    }
                }
            }
        }



        // XXX make sure this doesn't cause a segfault
        /// <summary>
        /// Gets depth of point, averaged over region specified by width and height;
        /// Specify width/height of 0,0 to get only the point requested
        /// </summary>
        /// <param name="p"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public ushort GetPointDepth(System.Windows.Point p, int width, int height)
        {
            UpdateDepth();
            return AverageDepthAroundPoint((int)p.X, (int)p.Y, width, height);
        }

        private ushort AverageDepthAroundPoint(int x, int y, int width, int height)
        {
            int index;
            ushort val;
            if (width == 0 || height == 0)
            {
                index = (int)(depthWidth * y + x);
                return depthData[index];
            }
            double average = 0;
            int count = 0;
            for (int i = -width; i < width; i++)
            {
                for (int j = -height; j < height; j++)
                {
                    int xF = x + i;
                    int yF = y + j;
                    if (xF > 0 && xF < depthWidth && yF > 0 && yF < depthHeight)
                    {
                        val = depthData[(int)((yF) * depthWidth + xF)];
                        if (val > 0)
                        {
                            average += val;
                            count++;
                        }
                    }
                }
            }
            return (ushort)(average / count);
        }

        public DepthMetaData GetDepthMetaData()
        {
            DepthMetaData depthMD = new DepthMetaData();
            depth.GetMetaData(depthMD);
            return depthMD;
        }


        private unsafe void CalcHist()
        {
            // reset
            for (int i = 0; i < depthHistogram.Length; ++i)
                depthHistogram[i] = 0;

            int points = 0;
            for (int y = 0; y < depthHeight; ++y)
            {
                for (int x = 0; x < depthWidth; ++x)
                {
                    ushort depthVal = depthData[y * depthWidth + x];
                    if (depthVal != 0)
                    {
                        this.depthHistogram[depthVal]++;
                        points++;
                    }
                }
            }

            for (int i = 1; i < this.depthHistogram.Length; i++)
            {
                this.depthHistogram[i] += this.depthHistogram[i - 1];
            }

            if (points > 0)
            {
                for (int i = 1; i < this.depthHistogram.Length; i++)
                {
                    this.depthHistogram[i] = (int)(256 * (1.0f - (this.depthHistogram[i] / (float)points)));
                }
            }
            for (int y = 0; y < depthHeight; y++)
            {
                for (int x = 0; x < depthWidth; x++)
                {
                    int histogramIndex = y * depthWidth + x;
                    int imageIndex = histogramIndex * 3;

                    ushort v = depthData[histogramIndex];
                    histogramImage[imageIndex] = (byte)depthHistogram[v];
                    histogramImage[imageIndex + 1] = (byte)depthHistogram[v];
                }
            }

        }

        public void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            UpdateContext();
        }
    }
}
