using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;

namespace DepthViewer
{
    [Serializable]
    public class DepthThreshold
    {
        private Kinect kinect;

        private Dictionary<ushort, int>[] depthHistogram;

        private ushort[] oldDepthValues;
        private bool averageOldDepth;

        public bool AverageOldDepth
        {
            get { return averageOldDepth; }
            set { averageOldDepth = value; }
        }
        
        private ushort[] thresholdValues;

        public ushort[] ThresholdValues
        {
            get { return thresholdValues; }
            set { thresholdValues = value; }
        }

        private DispatcherTimer calibrateTimer;
        private DispatcherTimer stopTimer;

        private int minimumCount = 1;
        private int thresholdDelta = 50;

        public int ThresholdDelta
        {
            get { return thresholdDelta; }
            set { thresholdDelta = value; }
        }

        public DepthThreshold()
        {
            kinect = Kinect.Instance;
            thresholdValues = new ushort[kinect.Depth.Length];
            oldDepthValues = new ushort[kinect.Depth.Length];
        }

        public void calibrateThreshold(TimeSpan duration, EventHandler finishedCallback)
        {
            thresholdValues = new ushort[kinect.Depth.Length];

            depthHistogram = new Dictionary<ushort, int>[kinect.Depth.Length];
            for (int i = 0; i < depthHistogram.Length; i++)
            {
                depthHistogram[i] = new Dictionary<ushort, int>();
            }

            calibrateTimer = new DispatcherTimer();
            calibrateTimer.Interval = new TimeSpan(0, 0, 0, 0, 30);
            calibrateTimer.Tick += updateHistogram;
            //calibrateTimer.Tick += updateThreshold;
            calibrateTimer.Start();

            stopTimer = new DispatcherTimer();
            stopTimer.Interval = duration;
            stopTimer.Tick += stopCalibration;
            stopTimer.Tick += finishedCallback;
            stopTimer.Start();
        }
        private void updateThreshold(object sender, EventArgs e)
        {
            ushort[] depthValues = kinect.Depth;
            for (int i = 0; i < thresholdValues.Length; i++)
            {
                thresholdValues[i] = (ushort)((depthValues[i] + thresholdValues[i]) / 2);

            }
        }
        private void updateHistogram(object sender, EventArgs e)
        {
            ushort[] data = kinect.Depth;
            for (int i = 0; i < data.Length; i++)
            {
                ushort val = data[i];
                Dictionary<ushort, int> dict = depthHistogram[i];
                if (!dict.ContainsKey(val))
                    dict[val] = 0;
                dict[val]++;
            }
        }

        private void stopCalibration(object sender, EventArgs e)
        {

            calibrateTimer.Stop();
            stopTimer.Stop();

            for (int i = 0; i < depthHistogram.Length; i++)
            {
                Dictionary<ushort, int> dict = depthHistogram[i];
                foreach(ushort k in dict.Keys.OrderBy(x => x))
                {
                    if (k > 0 && dict[k] > minimumCount)
                    {
                        thresholdValues[i] = (ushort) (k - 1);
                        break;
                    }
                }
            }
        }

        public ushort[] getDepth()
        {
            ushort[] result = kinect.Depth;
            for (int i = 0; i < result.Length; i++)
            {
                int dif = (int)thresholdValues[i] - (int)result[i];
                bool withinThreshold = thresholdValues[i] > 0 && dif < thresholdDelta && dif > 0;
                if (!withinThreshold)
                    result[i] = 0;
                else if(averageOldDepth)
                    result[i] = (ushort)((result[i] + oldDepthValues[i]) / 2);
            }
            oldDepthValues = result;
            return result;
        }
    }
}
