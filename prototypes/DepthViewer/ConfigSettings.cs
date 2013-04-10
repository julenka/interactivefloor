using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace DepthViewer
{
    [Serializable]
    public class ConfigSettings
    {
        public ChoppingPlane Plane { get; set; }
        public Rectangle InputRegion { get; set; }
        public int ChopLow { get; set; }
        public int ChopHigh { get; set; }
        public DepthThreshold DThreshold { get; set; }
        public int ConnectedThreshold { get; set; }
        public int RgbThreshold { get; set; }
    }
}
