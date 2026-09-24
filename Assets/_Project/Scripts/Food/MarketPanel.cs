using System;
using System.Linq;
using UnityEngine;

namespace FantasyShapez.Food
{
    public sealed class MarketPanel : MonoBehaviour
    {
        [SerializeField] private Market market = null;

        private void OnGUI()
        {
            if (market == null || market.Inventory == null)
            {
                return;
            }

            int rowCount = market.Inventory.DeliveredCounts.Count;
            float height = 92f + rowCount * 22f;
            var rect = new Rect(16f, Mathf.Max(16f, Screen.height - height - 16f),
                300f, height);
            GUI.Box(rect, GUIContent.none);
            GUILayout.BeginArea(new Rect(rect.x + 12f, rect.y + 8f,
                rect.width - 24f, rect.height - 16f));
            GUILayout.Label("Market");
            GUILayout.Label($"Total delivered: {market.Inventory.TotalDelivered}");
            if (!string.IsNullOrEmpty(market.LastDeliveryMessage))
            {
                GUILayout.Label(market.LastDeliveryMessage);
            }

            foreach (var delivered in market.Inventory.DeliveredCounts
                .OrderBy(entry => entry.Key.Kind)
                .ThenBy(entry => entry.Key.Id, StringComparer.Ordinal))
            {
                string kind = delivered.Key.Kind == FoodItemKind.RawIngredient
                    ? "raw" : "processed";
                GUILayout.Label($"{delivered.Key.Id} ({kind}): {delivered.Value}");
            }

            GUILayout.EndArea();
        }
    }
}
