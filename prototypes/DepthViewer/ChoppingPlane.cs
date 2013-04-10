using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using System.Drawing;
using System.Drawing.Imaging;

using xn;

namespace DepthViewer
{
    /// <summary>
    /// Idea: compare the rgb and depth images and only look at new images for depth
    /// </summary>
    public class ChoppingPlane
    {
        System.Windows.Media.Media3D.Point3D p1;

        public System.Windows.Media.Media3D.Point3D P1
        {
            get { return p1; }
            set { p1 = value; }
        }
        System.Windows.Media.Media3D.Point3D p2;

        public System.Windows.Media.Media3D.Point3D P2
        {
            get { return p2; }
            set { p2 = value; }
        }
        System.Windows.Media.Media3D.Point3D p3;

        public System.Windows.Media.Media3D.Point3D P3
        {
            get { return p3; }
            set { p3 = value; }
        }

        Vector3D normal;

        public Vector3D Normal
        {
            get { return normal; }
            set { normal = value; }
        }

        Kinect kinect;

        public ChoppingPlane()
        {
            kinect = Kinect.Instance;
        }

        public ChoppingPlane(System.Windows.Media.Media3D.Point3D p1,
                                System.Windows.Media.Media3D.Point3D p2,
                                System.Windows.Media.Media3D.Point3D p3)
        {
            SetNewPoints(p1, p2, p3);
            kinect = Kinect.Instance;
        }

        public void SetNewPoints(System.Windows.Media.Media3D.Point3D p1,
                                System.Windows.Media.Media3D.Point3D p2,
                                System.Windows.Media.Media3D.Point3D p3)
        {
            this.p1 = p1;
            this.p2 = p2;
            this.p3 = p3;
            // Recalculate the normal to the plane
            normal = Utils.getNormal(p1, p2, p3);
        }

        public ushort[] getDepthFromPlane(int minDistance, int maxDistance)
        {
            ushort[] result = new ushort[kinect.DepthWidth * kinect.DepthHeight];

            ushort[] depth = kinect.Depth;

            // set pixels
            for (int y = 0; y < kinect.DepthHeight; ++y)
            {
                for (int x = 0; x < kinect.DepthWidth; ++x)
                {
                    int index = y * kinect.DepthWidth + x;

                    ushort val = 0;
                    ushort z = depth[index];

                    // see if this point is within 100 mm of the plane
                    Vector3D v = new Vector3D(x - p1.X, y - p1.Y, z - p1.Z);
                    double distance = Math.Abs(Vector3D.DotProduct(normal, v));
                    if (distance < maxDistance && distance > minDistance)
                    {
                        val = (ushort)distance;
                    }
                    result[index] = val;
                }
            }
            return result;
        }

    }
}
