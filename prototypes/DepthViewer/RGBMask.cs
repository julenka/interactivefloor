using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace DepthViewer
{
    class RGBMask
    {
        private byte[] sourceImage;

        private Rectangle croppingRect;

        public Rectangle CroppingRect
        {
            get { return croppingRect; }
            set { 
                croppingRect = value;
                sourceCropped = Utils.cropRGB(croppingRect, sourceImage, originalWidth);
            }
        }
        public byte[] SourceImage
        {
            get { return sourceImage; }
            set { sourceImage = value; }
        }

        private int threshold = 50;

        public int Threshold
        {
            get { return threshold; }
            set { threshold = value; }
        }


        private byte[] sourceCropped;
        private int originalWidth;
        public RGBMask(byte[] source, Rectangle cropRect, int originalWidth)
        {
            sourceImage = source;
            croppingRect = cropRect;
            sourceCropped = Utils.cropRGB(croppingRect, source, originalWidth);
            this.originalWidth = originalWidth;
        }

        // Assumes that the source image is correctly cropped (i.e. has same dimensions as input image
        public ushort[] applyMask(ushort[] input, byte[] newRGB)
        {
            byte[] newCropped = Utils.cropRGB(croppingRect, newRGB, originalWidth);
            int sum = 0;
            ushort[] result = new ushort[input.Length];
            for (int i = 0; i < sourceCropped.Length; i++)
            {
                sum += Math.Abs(newCropped[i] - sourceCropped[i]);
                if (i % 3 == 2)
                {
                    if (sum < threshold)
                    {
                        result[i / 3] = 0;
                    }
                    else
                    {
                        result[i / 3] = input[i / 3];
                    }
                    sum = 0;
                }
            }
            return result;
        }
        public byte[] applyMask(byte[] input, byte[] newRGB)
        {
            byte[] newCropped = Utils.cropRGB(croppingRect, newRGB, originalWidth);
            int sum = 0;
            byte[] result = new byte[input.Length];
            for (int i = 0; i < sourceCropped.Length; i++)
            {
                sum += Math.Abs(newCropped[i] - sourceCropped[i]);
                if (i % 3 == 2)
                {
                    byte val = 0;

                    for (int j = 0; j < 3; j++)
                    {
                        if (sum >= threshold)
                        {
                            val = input[i - 2 + j];
                        }
                        result[i - 2 + j] = val;
                    }

                    sum = 0;
                }
            }
            return result;
        }

    }
}
