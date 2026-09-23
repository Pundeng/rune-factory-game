using FantasyShapez.Buildings;
using FantasyShapez.Resources;
using FantasyShapez.Runes;
using FantasyShapez.Logistics;
using UnityEngine;

namespace FantasyShapez.Production
{
    public sealed class RuneExtractor : MonoBehaviour, IRuneOutputSource, IBuildingMoveState
    {
        [SerializeField, Min(0.01f)] private float processingInterval = 1f;
        [SerializeField, Min(1)] private int outputCapacity = 4;
        [SerializeField] private bool isActive;
        [SerializeField] private int outputCount;
        [SerializeField] private RuneBaseShape resourceType = RuneBaseShape.Circle;

        private RuneExtractorProcess process;
        private BeltTransportCoordinator transportCoordinator;
        private static Sprite arrowSprite;
        private SpriteRenderer[] renderers;
        private bool? lastVisualActiveState;
        private bool? lastVisualFullState;

        public bool HasOutput => process?.OutputBuffer.HasOutput ?? false;

        public Vector2Int OutputCell { get; private set; }

        public GridDirection OutputDirection { get; private set; }

        public int OutputCount => process?.OutputBuffer.Count ?? 0;

        public int OutputCapacity => outputCapacity;

        public bool IsActive => process?.CanProduce ?? false;

        public bool CanMove => process != null;

        internal void CopyMoveStateFrom(RuneExtractor source)
        {
            if (source == null || source.process == null || process == null)
            {
                throw new System.InvalidOperationException(
                    "Both extractors must be initialized before moving state.");
            }

            RuneExtractorProcess movedProcess =
                source.process.CopyForMove(process.Resource);
            process = movedProcess;
            processingInterval = source.processingInterval;
            outputCapacity = source.outputCapacity;
            RefreshDebugState();
        }

        public void DetachForMove()
        {
            transportCoordinator?.UnregisterOutputSource(this);
        }

        public void ReattachAfterFailedMove()
        {
            transportCoordinator?.RegisterOutputSource(this);
        }

        public void Initialize(
            RuneStoneResourceNode resourceNode,
            Vector2Int anchorCell,
            GridDirection outputDirection,
            BeltTransportCoordinator coordinator)
        {
            process = new RuneExtractorProcess(
                resourceNode != null ? resourceNode.Resource : null,
                processingInterval,
                outputCapacity);

            if (resourceNode != null)
            {
                resourceType = resourceNode.BaseShape;
            }

            OutputDirection = outputDirection;
            OutputCell = anchorCell + outputDirection.ToOffset();
            transportCoordinator = coordinator;
            transportCoordinator?.RegisterOutputSource(this);
            CreateOutputArrow();

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

        private void CreateOutputArrow()
        {
            if (transform.Find("Output Arrow Shaft") != null)
            {
                return;
            }

            CreateArrowPart(
                "Output Arrow Shaft",
                new Vector2(0f, -0.02f),
                new Vector2(0.1f, 0.5f),
                0f);
            CreateArrowPart(
                "Output Arrow Left",
                new Vector2(-0.1f, 0.18f),
                new Vector2(0.1f, 0.3f),
                -45f);
            CreateArrowPart(
                "Output Arrow Right",
                new Vector2(0.1f, 0.18f),
                new Vector2(0.1f, 0.3f),
                45f);
        }

        private void CreateArrowPart(string partName, Vector2 position, Vector2 scale, float angle)
        {
            var part = new GameObject(partName);
            part.transform.SetParent(transform, false);
            part.transform.localPosition = new Vector3(position.x, position.y, -0.02f);
            part.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            part.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = GetArrowSprite();
            renderer.sortingOrder = 15;
        }

        private static Sprite GetArrowSprite()
        {
            if (arrowSprite == null)
            {
                arrowSprite = Sprite.Create(
                    Texture2D.whiteTexture,
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f),
                    1f);
                arrowSprite.name = "Runtime Output Arrow";
            }

            return arrowSprite;
        }

        private void OnDestroy()
        {
            process?.DiscardContents();
            transportCoordinator?.UnregisterOutputSource(this);
        }
    }
}
