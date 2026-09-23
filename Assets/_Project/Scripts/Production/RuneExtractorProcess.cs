using System;
using FantasyShapez.Resources;

namespace FantasyShapez.Production
{
    public sealed class RuneExtractorProcess
    {
        private readonly RuneStoneResource resource;
        private float elapsedTime;

        public RuneExtractorProcess(
            RuneStoneResource resource,
            float processingInterval,
            int outputCapacity)
        {
            if (processingInterval <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(processingInterval),
                    "Processing interval must be positive.");
            }

            this.resource = resource;
            ProcessingInterval = processingInterval;
            OutputBuffer = new RuneOutputBuffer(outputCapacity);
        }

        public float ProcessingInterval { get; }

        public RuneOutputBuffer OutputBuffer { get; }

        public bool HasValidResource => resource != null;

        public bool CanProduce => HasValidResource && OutputBuffer.CanAcceptOutput;

        public bool CanMoveWithoutStateLoss => elapsedTime == 0f &&
            OutputBuffer.Count == 0;

        public int Advance(float deltaTime)
        {
            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime), "Delta time cannot be negative.");
            }

            if (!CanProduce)
            {
                return 0;
            }

            elapsedTime += deltaTime;
            int producedCount = 0;

            while (elapsedTime >= ProcessingInterval && OutputBuffer.CanAcceptOutput)
            {
                elapsedTime -= ProcessingInterval;
                OutputBuffer.TryAdd(resource.Extract());
                producedCount++;
            }

            if (!OutputBuffer.CanAcceptOutput)
            {
                elapsedTime = 0f;
            }

            return producedCount;
        }

        public void DiscardContents()
        {
            elapsedTime = 0f;
            OutputBuffer.Clear();
        }
    }
}
