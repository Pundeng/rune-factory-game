using FantasyShapez.Resources;
using FantasyShapez.Runes;
using UnityEngine;

namespace FantasyShapez.Production
{
    public sealed class RuneExtractor : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float processingInterval = 1f;
        [SerializeField, Min(1)] private int outputCapacity = 4;
        [SerializeField] private bool isActive;
        [SerializeField] private int outputCount;
        [SerializeField] private RuneBaseShape resourceType = RuneBaseShape.Circle;

        private RuneExtractorProcess process;
        private SpriteRenderer[] renderers;
        private bool? lastVisualActiveState;
        private bool? lastVisualFullState;

        public bool HasOutput => process?.OutputBuffer.HasOutput ?? false;

        public int OutputCount => process?.OutputBuffer.Count ?? 0;

        public int OutputCapacity => outputCapacity;

        public bool IsActive => process?.CanProduce ?? false;

        public void Initialize(RuneStoneResourceNode resourceNode)
        {
            process = new RuneExtractorProcess(
                resourceNode != null ? resourceNode.Resource : null,
                processingInterval,
                outputCapacity);

            if (resourceNode != null)
            {
                resourceType = resourceNode.BaseShape;
            }

            RefreshDebugState();
        }

        public RuneData PeekOutput()
        {
            return process?.OutputBuffer.PeekOutput();
        }

        public bool TryTakeOutput(out RuneData rune)
        {
            if (process == null)
            {
                rune = null;
                return false;
            }

            bool tookOutput = process.OutputBuffer.TryTakeOutput(out rune);
            RefreshDebugState();
            return tookOutput;
        }

        private void Update()
        {
            process?.Advance(Time.deltaTime);
            RefreshDebugState();
        }

        private void RefreshDebugState()
        {
            isActive = IsActive;
            outputCount = OutputCount;

            bool isFull = process != null && !process.OutputBuffer.CanAcceptOutput;
            if (lastVisualActiveState == isActive && lastVisualFullState == isFull)
            {
                return;
            }

            renderers ??= GetComponentsInChildren<SpriteRenderer>();
            Color color = isFull
                ? new Color(0.95f, 0.7f, 0.2f, 1f)
                : isActive
                    ? new Color(0.3f, 0.8f, 0.45f, 1f)
                    : new Color(0.65f, 0.65f, 0.65f, 1f);

            foreach (SpriteRenderer spriteRenderer in renderers)
            {
                spriteRenderer.color = color;
            }

            lastVisualActiveState = isActive;
            lastVisualFullState = isFull;
        }

        private void OnValidate()
        {
            processingInterval = Mathf.Max(0.01f, processingInterval);
            outputCapacity = Mathf.Max(1, outputCapacity);
        }
    }
}
