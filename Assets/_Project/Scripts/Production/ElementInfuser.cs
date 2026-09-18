using System;
using FantasyShapez.Buildings;
using FantasyShapez.Logistics;
using FantasyShapez.Runes;
using UnityEngine;

namespace FantasyShapez.Production
{
    public sealed class ElementInfuser : MonoBehaviour, IBuildingRemovalRule
    {
        [SerializeField] private RuneElement primaryElement = RuneElement.Fire;
        [SerializeField] private ElementZone targetZone = ElementZone.Left;
        [SerializeField, Min(0.01f)] private float processingDuration = 1.5f;
        [SerializeField] private ElementInfuserState state;

        private static Sprite placeholderSprite;
        private BeltTransportCoordinator transportCoordinator;
        private ElementInfuserProcess process;
        private SpriteRenderer stateIndicator;
        private ElementInfuserState? lastVisualState;

        public bool CanRemove => true;

        public void Initialize(
            Vector2Int anchorCell,
            GridDirection direction,
            BeltTransportCoordinator coordinator)
        {
            transportCoordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            process = new ElementInfuserProcess(
                anchorCell,
                direction,
                primaryElement,
                targetZone,
                processingDuration);
            transportCoordinator.RegisterInputReceiver(process);
            transportCoordinator.RegisterOutputSource(process);
            CreateOutputArrow();
            CreateElementMarker();
            CreateStateIndicator();
            RefreshVisualState();
        }

        private void Update()
        {
            if (process == null)
            {
                return;
            }

            process.Configure(primaryElement, targetZone, processingDuration);
            bool completed = process.Advance(Time.deltaTime);
            if (completed && process.LastInfusionSucceeded == false)
            {
                Debug.LogWarning(
                    $"Element Infuser could not assign {process.ActiveElement} to " +
                    $"{process.ActiveZone}; the rune will pass through unchanged.",
                    this);
            }

            RefreshVisualState();
        }

        private void RefreshVisualState()
        {
            state = process?.State ?? ElementInfuserState.Idle;
            if (lastVisualState == state)
            {
                return;
            }

            Color color = state switch
            {
                ElementInfuserState.Processing => new Color(0.95f, 0.35f, 0.25f, 1f),
                ElementInfuserState.WaitingForOutput => new Color(0.95f, 0.65f, 0.2f, 1f),
                _ => new Color(0.3f, 0.55f, 0.9f, 1f)
            };

            stateIndicator.color = color;
            lastVisualState = state;
        }

        private void CreateOutputArrow()
        {
            CreateVisualPart("Output Arrow Shaft", new Vector2(0f, 0.28f), new Vector2(0.07f, 0.22f), 0f);
            CreateVisualPart("Output Arrow Left", new Vector2(-0.07f, 0.38f), new Vector2(0.07f, 0.16f), -45f);
            CreateVisualPart("Output Arrow Right", new Vector2(0.07f, 0.38f), new Vector2(0.07f, 0.16f), 45f);
        }

        private void CreateElementMarker()
        {
            CreateVisualPart("Element Marker", new Vector2(0f, -0.06f), new Vector2(0.28f, 0.28f), 45f);
        }

        private void CreateStateIndicator()
        {
            stateIndicator = CreateVisualPart(
                "State Indicator",
                new Vector2(0f, -0.06f),
                Vector2.one * 0.14f,
                45f);
        }

        private SpriteRenderer CreateVisualPart(
            string partName,
            Vector2 position,
            Vector2 scale,
            float angle)
        {
            var part = new GameObject(partName);
            part.transform.SetParent(transform, false);
            part.transform.localPosition = new Vector3(position.x, position.y, -0.03f);
            part.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            part.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = GetPlaceholderSprite();
            renderer.color = Color.white;
            renderer.sortingOrder = 20;
            return renderer;
        }

        private static Sprite GetPlaceholderSprite()
        {
            if (placeholderSprite == null)
            {
                placeholderSprite = Sprite.Create(
                    Texture2D.whiteTexture,
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f),
                    1f);
                placeholderSprite.name = "Runtime Element Infuser Placeholder";
            }

            return placeholderSprite;
        }

        private void OnValidate()
        {
            processingDuration = Mathf.Max(0.01f, processingDuration);
        }

        private void OnDestroy()
        {
            if (process == null)
            {
                return;
            }

            if (transportCoordinator != null)
            {
                transportCoordinator.UnregisterInputReceiver(process);
                transportCoordinator.UnregisterOutputSource(process);
            }

            process.DiscardContents();
        }
    }
}
