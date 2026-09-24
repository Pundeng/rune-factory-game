using System;
using System.Collections.Generic;
using UnityEngine;

namespace FantasyShapez.Food
{
    public sealed class FarmPlot : MonoBehaviour
    {
        [SerializeField] private CropDefinition[] availableCrops =
        {
            new CropDefinition("Apple",
                new FoodItemData("apple", FoodItemKind.RawIngredient), 2f)
        };
        [SerializeField, Min(1)] private int matureCapacity = 4;

        private static readonly Dictionary<Vector2Int, FarmPlot> PlotsByCell = new();
        private FarmPlotProcess process;
        private Vector2Int cell;
        private bool initialized;

        public IReadOnlyList<CropDefinition> AvailableCrops => availableCrops;

        public CropDefinition SelectedCrop => process?.SelectedCrop;

        public int MatureCount => process?.MatureCount ?? 0;

        public int MatureCapacity => matureCapacity;

        internal FarmPlotProcess Process => process;

        public void Initialize(Vector2Int anchorCell)
        {
            if (initialized)
            {
                throw new InvalidOperationException("The Farm Plot is already initialized.");
            }

            if (PlotsByCell.TryGetValue(anchorCell, out FarmPlot existing) && existing != null)
            {
                throw new InvalidOperationException($"A Farm Plot already exists at {anchorCell}.");
            }

            process = new FarmPlotProcess(matureCapacity);
            cell = anchorCell;
            initialized = true;
            PlotsByCell[anchorCell] = this;
        }

        public static FarmPlot GetAt(Vector2Int cell)
        {
            return PlotsByCell.TryGetValue(cell, out FarmPlot plot) && plot != null
                ? plot : null;
        }

        public void SelectCrop(CropDefinition crop)
        {
            if (process == null)
            {
                throw new InvalidOperationException("The Farm Plot is not initialized.");
            }

            if (crop != null && Array.IndexOf(availableCrops, crop) < 0)
            {
                throw new ArgumentException("The crop is not available on this Farm Plot.", nameof(crop));
            }

            process.SelectCrop(crop);
        }

        public bool TryHarvest(out FoodItemData item)
        {
            if (process != null)
            {
                return process.TryHarvest(out item);
            }

            item = null;
            return false;
        }

        private void Update()
        {
            process?.Advance(Time.deltaTime);
        }

        private void OnValidate()
        {
            matureCapacity = Mathf.Max(1, matureCapacity);
        }

        private void OnDestroy()
        {
            if (initialized && PlotsByCell.TryGetValue(cell, out FarmPlot registered) &&
                ReferenceEquals(registered, this))
            {
                PlotsByCell.Remove(cell);
            }
        }
    }
}
