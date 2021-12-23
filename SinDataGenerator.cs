
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nikrotek.Electrophysiology.Core.DataGenerators
{
    public class SinDataGenerator : IDataGenerator
    {
        public double F { get; set; }
        public double Fs { get; set; }
        public double Fi0 { get; set; }

        private int _count;

        public SinDataGenerator(double f, double fs, double fi0)
        {
            F = f;
            Fs = fs;
            Fi0 = fi0;
        }

        public double[] GetNextValues(int count)
        {
            int start = _count;

            var data = Enumerable
                .Range(start, count)
                .Select(d => Math.Sin(2 * Math.PI * F * d / Fs + Fi0))
                .ToArray();

            _count += count;
            return data;
        }
    }
}
