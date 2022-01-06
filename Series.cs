using ILGPU;
using ILGPU.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvaSkia
{
    public class Series : IDisposable
    {
        private MemoryBuffer1D<float, Stride1D.Dense> gpuPoints;

        public float[] Points { get; set; }
        public int Limit { get; set; }
        public int Cursor { get; set; }
        public MemoryBuffer1D<float, Stride1D.Dense> GpuPoints => gpuPoints;

        public Series(int limit, Accelerator accelerator)
        {
            Points = new float[limit];
            Limit = limit;
            Cursor = 0;
            gpuPoints = accelerator.Allocate1D<float>(limit);
        }

        public void AddPoints(float[] newPoints)
        {
            if (Cursor + newPoints.Length > Points.Length)
            {
                var diff = Points.Length - Cursor;
                
                Array.Copy(newPoints, 0, Points, Cursor, diff);
                Cursor = 0;

                Array.Copy(newPoints, diff, Points, Cursor, newPoints.Length - diff);
                Cursor = newPoints.Length - diff;
            }
            else
            {
                Array.Copy(newPoints, 0, Points, Cursor, newPoints.Length);
                Cursor += newPoints.Length;
                Cursor %= Points.Length;
            }
        }

        public void LoadPointsToDevice()
        {
            gpuPoints.CopyFromCPU(Points);
        }

        public void Dispose()
        {
            gpuPoints.Dispose();
        }
    }
}
