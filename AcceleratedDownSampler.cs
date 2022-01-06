using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.OpenCL;
using SkiaSharp;

namespace AvaSkia
{
    internal class AcceleratedDownSampler : IDisposable
    {
        private const int IntelDeviceIndex = 0;

        private Context context;
        private Accelerator accelerator;
        private Action<Index1D, DownSamplerGpuData> loadedKernel;

        public Accelerator Accelerator => accelerator;

        public AcceleratedDownSampler()
        {
            context = Context.Create(builder => builder.OpenCL());
            var device = context.GetCLDevices()[IntelDeviceIndex];
            accelerator = device.CreateAccelerator(context);

            loadedKernel = accelerator.LoadAutoGroupedStreamKernel<Index1D, DownSamplerGpuData>(DownSamplerGpuData.DownSample);
        }

        public void FillDownSampledPointsArray(
            int partCount,
            SKPoint[] outputs,
            Series series,
            int mergeSize)
        {
            var outputsOnDevice = accelerator.Allocate1D<SKPoint>(outputs.Length);

            series.LoadPointsToDevice();
            var inputsOnDevice = series.GpuPoints;
            var data = new DownSamplerGpuData(outputsOnDevice.View, inputsOnDevice.View, mergeSize);

            loadedKernel(partCount, data);
            accelerator.Synchronize();

            outputsOnDevice.CopyToCPU(outputs);
        }

        public void Dispose()
        {
            accelerator.Dispose();
            context.Dispose();
        }
    }
}
