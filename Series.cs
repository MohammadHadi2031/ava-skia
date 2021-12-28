using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvaSkia
{
    public class Series
    {
        public List<float> Points { get; set; }
        public int Limit { get; set; }

        public Series()
        {
            Points = new List<float>(64000);
        }

        public void AddPoints(float[] points)
        {
            Points.AddRange(points);

            if (Points.Count > Limit)
            {
                Points.RemoveRange(0, Points.Count - Limit);
            }
        }

    }
}
