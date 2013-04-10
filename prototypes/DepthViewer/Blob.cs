using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace DepthViewer
{
    class Blob
    {
        public Point center;
        public Rectangle bounds;
        // number of pixels in top half of blob
        public int topCount;
        // number of pixels in bottom half of blob
        public int bottomCount;
    }
}
