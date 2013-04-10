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
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DepthViewer
{
    /// <summary>
    /// Interaction logic for DanceFloor.xaml
    /// </summary>
    public partial class DanceFloor : Window
    {

        private List<Rectangle> floorTiles;
        private int x_dim = 4;
        private int y_dim = 3;
        private int r_width = 330;
        private int r_height = 330;
        private Configuration parent;
        private Color baseColor = Colors.Black;
        private Color[] randomColors = { Colors.Red, Colors.Orange, Colors.Yellow, Colors.Green, Colors.Blue, Colors.Purple, Colors.Red, Colors.Orange, Colors.Yellow, Colors.Green, Colors.Blue, Colors.Purple };
        private Kinect kinect;
        private Random rand;

        public DanceFloor(Configuration p)
        {
            InitializeComponent();
            rand = new Random();
            parent = p;
            floorTiles = new List<Rectangle>();
            for (int x = 0; x < x_dim; x++)
            {
                for (int y = 0; y < y_dim; y++)
                {
                    Rectangle addMe = new Rectangle();
                    addMe.Width = r_width;
                    addMe.Height = r_height;
                    addMe.RenderTransform = new TranslateTransform(x * r_width, y * r_height);
                    addMe.Fill = new SolidColorBrush(baseColor);
                    addMe.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    addMe.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                    floorTiles.Add(addMe);
                    grid1.Children.Add(addMe);
                }
            }
            kinect = Kinect.Instance;
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 30);
            dispatcherTimer.Start();
            Console.WriteLine("Finished loading");
        }
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            int w = (int)parent.CroppingRegion.Width;
            int h = (int)parent.CroppingRegion.Height;
            double scaleXFactor = grid1.Width / w;
            double scaleYFactor = grid1.Height / h;
            foreach (Rectangle r in floorTiles)
            {
                    r.Fill = new SolidColorBrush(baseColor);
            }
            Point[] points = parent.getInputPoints();
            for (int i = 0; i < points.Length; i++)
            {
                Point p = points[i];
                if (p.X > 0)
                {
                    
                    Point newP = new Point(grid1.Width - p.X * scaleXFactor, p.Y * scaleYFactor);
                    int j = 0;
                    foreach (Rectangle r in floorTiles)
                    {
                        if (r.RenderTransform.TransformBounds(r.RenderedGeometry.Bounds).Contains(newP))
                        {
                            r.Fill = new SolidColorBrush(randomColors[j]);
                        }
                        j++;

                    }
                }
            }
        }

    }
}
