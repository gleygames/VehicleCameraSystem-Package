using System.Collections.Generic;

namespace Gley.CameraSystem.Dev
{
    public class BenchmarkStatistics
    {
        private readonly List<double> values = new List<double>();

        public double Median { get; private set; }
        public double Percentile95 { get; private set; }
        public double Maximum { get; private set; }
        public int Count => values.Count;

        public void Clear()
        {
            values.Clear();
            Median = 0d;
            Percentile95 = 0d;
            Maximum = 0d;
        }

        public void Add(double value)
        {
            values.Add(value);
        }

        public void Compute()
        {
            if (values.Count == 0)
            {
                Median = 0d;
                Percentile95 = 0d;
                Maximum = 0d;
                return;
            }

            values.Sort();
            int middle = values.Count / 2;
            if (values.Count % 2 == 0)
            {
                Median = (values[middle - 1] + values[middle]) * 0.5d;
            }
            else
            {
                Median = values[middle];
            }

            int percentileIndex = (int)System.Math.Ceiling(values.Count * 0.95d) - 1;
            if (percentileIndex < 0)
            {
                percentileIndex = 0;
            }

            Percentile95 = values[percentileIndex];
            Maximum = values[values.Count - 1];
        }
    }
}
