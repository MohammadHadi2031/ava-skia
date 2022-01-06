using ILGPU;
using SkiaSharp;

namespace AvaSkia
{
    internal struct DownSamplerGpuData
    {
        public ArrayView<SKPoint> Outputs;
        public ArrayView<float> Inputs;
        public int MergeSize;

        public DownSamplerGpuData(
            ArrayView<SKPoint> outputs,
            ArrayView<float> inputs,
            int mergeSize)
        {
            Outputs = outputs;
            Inputs = inputs;
            MergeSize = mergeSize;
        }

        public static void DownSample(
           Index1D part,
           DownSamplerGpuData data)
        {
            var pointsCount = data.Inputs.Length;

            var i = part * data.MergeSize;
            var maxY = data.Inputs[i];
            var minY = data.Inputs[i];
            var maxX = i;
            var minX = i;

            for (int j = i + 1; j < i + data.MergeSize && j < pointsCount; j++)
            {
                var y = data.Inputs[j];

                if (maxY < y)
                {
                    maxY = y;
                    maxX = j;
                }
                else if (minY > y)
                {
                    minY = y;
                    minX = j;
                }
            }

            data.Outputs[2 * part] = new SKPoint { Y = minY, X = minX };
            data.Outputs[2 * part + 1] = new SKPoint { Y = maxY, X = maxX };
        }
    }
}
