using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Drawing;
using System.Windows.Shapes;

namespace DepthViewer
{
    class FloorPoints
    {
        private Kinect kinect;
        private int minSize = 100;

        public FloorPoints()
        {
            kinect = Kinect.Instance;
        }

    }
}
