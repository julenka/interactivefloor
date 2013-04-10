using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using xn;
using System.Threading;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Threading;
using System.Windows.Media.Media3D;
using System.Xml.Serialization;
using Microsoft.Win32;

namespace DepthViewer
{

    /// <summary>
    /// The configuration window allows you to configure your Kinect and get input points based on your configuration.
    /// You can call getInputPoints() to get a list of input points, and getBlobs() to get a list of blobs, which contain
    /// input point information as well as other information (see the Blob class for more).
    /// </summary>
    public partial class Configuration : Window
    {
        #region Member Variables
        private string defaultConfigFileLocation = @"..\..\..\data\inputConfig.xml";
        ChoppingPlane choppingPlane = new ChoppingPlane();
        private RGBMask rgbMask;

        List<System.Windows.Media.Media3D.Point3D> clickedPoints = new List<System.Windows.Media.Media3D.Point3D>();
        Kinect kinect;
        private System.Drawing.Rectangle croppingRegion = new System.Drawing.Rectangle();
        private DepthThreshold depthThreshold;

        private List<FloorPointControl> floorPoints;

        private bool isCalibrating;
        #endregion

        #region Properties
        public int RgbThreshold
        {
            get { return rgbMask.Threshold; }
            set
            {
                rgbThreshSlider.Value = value;
                rgbMask.Threshold = value;
                //rgbThreshTxt.Text = "" + value;
            }
        }
        internal int ThresholdDelta
        {
            get { return depthThreshold.ThresholdDelta; }
            set
            {
                depthDeltaSlider.Value = value;
                depthThreshold.ThresholdDelta = value;
                //depthDeltaTxt.Text = "" + value;
            }
        }

        public int ConnectedThreshold
        {
            get { return (int)connectedSlider.Value; }
            set
            {
                connectedSlider.Value = value;
                //connectedTxt.Text = "" + value;
            }
        }
        public ChoppingPlane ChoppingPlane
        {
            get { return choppingPlane; }
            set
            {
                choppingPlane = value;
            }
        }

        public int ChopLow
        {
            get { return (int)chopLowSlider.Value; }
            set
            {
                chopLowSlider.Value = value;
                //chopLowTxt.Text = "" + value;
            }
        }

        public int ChopHigh
        {
            get { return (int)chopHighSlider.Value; }
            set
            {
                chopHighSlider.Value = value;
                //chopHighTxt.Text = "" + value;
            }
        }


        public System.Drawing.Rectangle CroppingRegion
        {
            get { return croppingRegion; }
            set { croppingRegion = value; }
        }
        #endregion

        public Configuration()
        {
            InitializeComponent();
            floorPoints = new List<FloorPointControl>();
            kinect = Kinect.Instance;
            //kinect.AverageWindowSize = 2;
            
            depthThreshold = new DepthThreshold();
            rgbMask = new RGBMask(kinect.RGB, new System.Drawing.Rectangle(0, 0, kinect.DepthWidth, kinect.DepthHeight), kinect.RgbWidth);

            loadConfig(defaultConfigFileLocation);
            depthThreshold.AverageOldDepth = true;

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 30);
            dispatcherTimer.Start();
            Console.WriteLine("Finished loading");
        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
        }
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            depthThreshold.ThresholdDelta = (int)depthDeltaSlider.Value;
            rgbMask.Threshold = (int)rgbThreshSlider.Value;
            if (isCalibrating)
                return;
            

            if ((bool)radioButton1.IsChecked)
            {
                // Update RGB and show
                // Create the image.
                viewer.Source = BitmapSource.Create(
                  kinect.RgbWidth,
                  kinect.RgbHeight,
                  96,
                  96, PixelFormats.Rgb24, null, kinect.RGB, kinect.RgbWidth * 3);
            }
            else if ((bool)radioButton2.IsChecked)
            {
                // Update depth and show
                viewer.Source = BitmapSource.Create(
                  kinect.DepthWidth,
                  kinect.DepthHeight,
                  96,
                  96, PixelFormats.Rgb24, null, kinect.HistogramImage, kinect.DepthWidth * 3);
            }
            else if ((bool)connComp.IsChecked)
            {
                // SHow the connected components image
                viewer.Source = BitmapSource.Create(
                      croppingRegion.Width,
                      croppingRegion.Height,
                    96,
                    96, PixelFormats.Rgb24, null, getConnectedComponentsImage(), croppingRegion.Width * 3);
            }
            else if ((bool)afterRGBFilter.IsChecked)
            {
                viewer.Source = BitmapSource.Create(
                  kinect.RgbWidth,
                  kinect.RgbHeight,
                  96,
                  96, PixelFormats.Rgb24, null, rgbMask.applyMask(kinect.RGB, kinect.RGB), kinect.RgbWidth * 3);
            }
            else
            {
                // Show the processed image
                if ((bool)chopPlane.IsChecked)
                {
                    // Show the chopped plane view
                    viewer.Source = BitmapSource.Create(
                      kinect.DepthWidth,
                      kinect.DepthHeight,
                          96,
                          96, PixelFormats.Rgb24, null, getChoppedImage(), kinect.DepthWidth * 3);
                }
                else
                {
                    // Show the depth threshold view
                    viewer.Source = BitmapSource.Create(
                          kinect.DepthWidth,
                          kinect.DepthHeight,
                              96,
                              96, PixelFormats.Rgb24, null, getThresholdedImage(), kinect.DepthWidth * 3);
                }
            }

            if ((bool)inputPointsCheckBox.IsChecked)
            {
                showPoints();
            }

            configCanvas.Visibility = (bool)configCheckBox.IsChecked ? Visibility.Visible : Visibility.Hidden;


        }

        private void updateSettingsView()
        {
            cropRect.RenderTransform = new TranslateTransform(croppingRegion.Left, croppingRegion.Top);
            cropRect.Width = croppingRegion.Width;
            cropRect.Height = croppingRegion.Height;
            p1View.RenderTransform = new TranslateTransform(choppingPlane.P1.X, choppingPlane.P1.Y);
            p2View.RenderTransform = new TranslateTransform(choppingPlane.P2.X, choppingPlane.P2.Y);
            p3View.RenderTransform = new TranslateTransform(choppingPlane.P3.X, choppingPlane.P3.Y);
        }

        private void showPoints()
        {
            ConnectedComponents connected = getConnectedComponents();

            // Render the points 
            int w = (int)croppingRegion.Width;
            int h = (int)croppingRegion.Height;
            double scaleXFactor = viewer.Width / w;
            double scaleYFactor = viewer.Height / h;

            while (floorPoints.Count < connected.Centroids.Length)
            {
                FloorPointControl newFp = new FloorPointControl();
                floorPoints.Add(newFp);
                inputCanvas.Children.Add(newFp);
            }

            int cropWidth = croppingRegion.Width;
            int cropHeight = croppingRegion.Height;
            for (int i = 0; i < connected.Blobs.Length; i++)
            {
                Blob b = connected.Blobs[i];
                if (b != null)
                {
                    floorPoints[i].RenderTransform = new TranslateTransform(b.center.X / (double)cropWidth *  configCanvas.Width, b.center.Y / (double)cropHeight * configCanvas.Height);
                    if (b.bottomCount > b.topCount)
                    {
                        floorPoints[i].label1.Content = "F";
                    }
                    else
                    {
                        floorPoints[i].label1.Content = "B";
                    }
                    floorPoints[i].Visibility = System.Windows.Visibility.Visible;
                }
            }
            for (int i = connected.Blobs.Length; i < floorPoints.Count; i++)
            {
                floorPoints[i].Visibility = System.Windows.Visibility.Hidden;
            }
        }

        private byte[] getConnectedComponentsImage()
        {
            ConnectedComponents connected = getConnectedComponents();
            byte[] result = new byte[connected.Data.Length * 3];
            Color[] colors = { Colors.Black, Colors.Red, Colors.Green, Colors.Blue, Colors.Purple, Colors.PowderBlue, Colors.Peru };
            int[] data = connected.Data;
            for (int rI = 0, dI = 0; rI < result.Length; rI += 3, dI += 1)
            {
                Color c = colors[data[dI] % colors.Length];
                result[rI] = c.R;
                result[rI + 1] = c.G;
                result[rI + 2] = c.B;
            }
            return result;
        }

        private byte[] getChoppedImage()
        {
            ushort[] chopped = getPointsNearPlane(false);
            //chopped = rgbMask.applyMask(chopped, croppingRegion, kinect.RGB);
            byte[] result;
            byte val;
            result = new byte[chopped.Length * 3];
            for (int rI = 0, cI = 0; rI < result.Length; rI += 3, cI++)
            {
                if (chopped[cI] > 0)
                {
                    val = (byte)((chopped[cI] - (double)ChopLow) / ChopHigh * 255);

                    result[rI] = val;
                    result[rI + 1] = val;
                    result[rI + 2] = val;
                }

            }

            return result;
        }


        private byte[] getThresholdedImage()
        {
            ushort[] chopped = getWithinDepthThreshold(false);
            //chopped = rgbMask.applyMask(chopped, croppingRegion, kinect.RGB);
            byte[] result;
            byte val;
            result = new byte[chopped.Length * 3];
            for (int rI = 0, cI = 0; rI < result.Length; rI += 3, cI++)
            {
                if (chopped[cI] > 0)
                {
                    // just make the pixel white for now
                    result[rI] = 255;
                    result[rI + 1] = 255;
                    result[rI + 2] = 255;
                }

            }

            return result;
        }
        public ushort[] getPointsNearPlane(bool crop)
        {
            ushort[] chopped = choppingPlane.getDepthFromPlane(ChopLow, ChopHigh);
            chopped = rgbMask.applyMask(chopped, kinect.RGB);
            if (crop)
                chopped = Utils.crop<ushort>(croppingRegion, chopped, kinect.DepthWidth);
            return chopped;
        }

        public ushort[] getWithinDepthThreshold(bool crop)
        {
            ushort[] chopped = depthThreshold.getDepth();
            if (crop)
                chopped = Utils.crop<ushort>(croppingRegion, chopped, kinect.DepthWidth);
            return chopped;
        }

        public ConnectedComponents getConnectedComponents()
        {
            ushort[] chopped;
            if ((bool)chopPlane.IsChecked)
            {
                chopped = getPointsNearPlane(true);
            }
            else
            {
                chopped = getWithinDepthThreshold(true);
            }
            int w = croppingRegion.Width;
            int h = croppingRegion.Height;
            return ConnectedComponents.GetConnectedComponents(chopped, w, h, ConnectedThreshold);
        }

        public Point[] getInputPoints()
        {
            ConnectedComponents connected = getConnectedComponents();
            Point[] result = new Point[connected.Centroids.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = new Point(connected.Centroids[i].X, connected.Centroids[i].Y);
            }
            return result;
        }

        public Blob[] getInputBlobs()
        {
            return getConnectedComponents().Blobs;
        }

        private unsafe void rgbImage_MouseDown(object sender, MouseButtonEventArgs e)
        {

            System.Windows.Point p = e.GetPosition(viewer);

            ushort z = kinect.GetPointDepth(p, 15, 15);


            clickedPoints.Add(new System.Windows.Media.Media3D.Point3D(p.X, p.Y, z));

            if (clickedPoints.Count == 3)
            {

                choppingPlane.SetNewPoints(clickedPoints[0], clickedPoints[1], clickedPoints[2]);

                int bottomX = Math.Min((int)clickedPoints[0].X, (int)clickedPoints[2].X);
                int bottomY = Math.Min((int)clickedPoints[0].Y, (int)clickedPoints[2].Y);
                int topX = Math.Max((int)clickedPoints[0].X, (int)clickedPoints[2].X);
                int topY = Math.Max((int)clickedPoints[0].Y, (int)clickedPoints[2].Y);
                croppingRegion = new System.Drawing.Rectangle(bottomX, bottomY, topX - bottomX, topY - bottomY);
                updateSettingsView();

                clickedPoints.Clear();
                textBlock1.Visibility = System.Windows.Visibility.Visible;

                saveConfig(defaultConfigFileLocation);

            }
        }

        #region Demos
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            new DanceFloor(this).Show();
        }

        #endregion
        #region Loading and Saving Configuration
        private void saveConfig(String path)
        {
            // Saves the configuration to a file.
            ConfigSettings toSave = new ConfigSettings { 
                InputRegion = croppingRegion, 
                Plane = choppingPlane, 
                ChopHigh = this.ChopHigh, 
                ChopLow = this.ChopLow,
                ConnectedThreshold = this.ConnectedThreshold,
                DThreshold = this.depthThreshold,
                RgbThreshold = this.RgbThreshold};
                XmlSerializer serializer = new XmlSerializer(typeof(ConfigSettings));
            using (FileStream fS = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                serializer.Serialize(fS, toSave);
            }
        }
        private void loadConfig(String path)
        {
            if (!File.Exists(path))
            {
                MessageBox.Show("File doesn't exist! Cropping region and chopping plane are empty", path);
                choppingPlane = new ChoppingPlane();
                croppingRegion = new System.Drawing.Rectangle();
                return;
            }
            ConfigSettings loaded;
            using (FileStream fS = new FileStream(path, FileMode.Open))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ConfigSettings));
                loaded = (ConfigSettings)serializer.Deserialize(fS);
            }
            croppingRegion = loaded.InputRegion;
            choppingPlane = loaded.Plane;
            if (loaded.DThreshold != null)
            {
                depthThreshold = loaded.DThreshold;
            }
            ConnectedThreshold = loaded.ConnectedThreshold;
            RgbThreshold = loaded.RgbThreshold;
            ChopLow = loaded.ChopLow;
            ChopHigh = loaded.ChopHigh;
            updateSettingsView();
        }
        private void Load_Click(object sender, RoutedEventArgs e)
        {
            // Get the path of the file using the open file dialogue
            OpenFileDialog d = new OpenFileDialog();
            d.ShowDialog();
            loadConfig(d.FileName);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog d = new SaveFileDialog();
            d.ShowDialog();
            saveConfig(d.FileName);
        }
        #endregion

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            isCalibrating = true;
            calibratingTB.Visibility = System.Windows.Visibility.Visible;
            this.IsEnabled = false;
            depthThreshold.calibrateThreshold(TimeSpan.FromSeconds(10), calibrationDone);
        }

        private void calibrationDone(object sender, EventArgs e)
        {
            isCalibrating = false;
            this.IsEnabled = true;
            calibratingTB.Visibility = System.Windows.Visibility.Hidden;
        }
    }

    class DoubleToIntConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double? d = value as double?;
            return (int)d;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int? v = value as int?;
            return (double)v;
        }
    }
}
