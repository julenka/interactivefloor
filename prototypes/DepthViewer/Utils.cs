using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.Windows.Media.Media3D;

namespace DepthViewer
{
    class Utils
    {
        public static BitmapImage getBitmapImage(Bitmap bitmap)
        {
            MemoryStream ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);
            ms.Position = 0;
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            bi.StreamSource = ms;
            bi.EndInit();
            return bi;
        }

        public static Vector3D getNormal(Point3D p1, Point3D p2, Point3D p3)
        {
            Vector3D result = Vector3D.CrossProduct(p2 - p1, p3 - p1);
            result.Normalize();
            return result;
        }

        /// <summary>
        /// Crops the object array given the input array.
        /// Assumes that the input array is larger than the cropping region, if not index out of bounds will be thrown
        /// </summary>
        public static T[] crop<T>(Rectangle croppingRect, T[] input, int originalWidth)
        {
            int w = croppingRect.Width;
            int h = croppingRect.Height;
            T[] result = new T[w * h];
            int offsetX = croppingRect.X;
            int offsetY = croppingRect.Y;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    result[y * w + x] = input[(offsetY + y) * originalWidth + (offsetX + x)];
                }
            }
            return result;
        }

        public static byte[] cropRGB(Rectangle croppingRect, byte[] input, int originalWidth)
        {
            Rectangle newRect = new Rectangle(croppingRect.X * 3, croppingRect.Y, croppingRect.Width * 3, croppingRect.Height);
            return crop<byte>(newRect, input, originalWidth * 3);
        }

    }
}
