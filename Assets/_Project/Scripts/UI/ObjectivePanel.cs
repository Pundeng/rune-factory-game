using FantasyShapez.Objectives;
using UnityEngine;

namespace FantasyShapez.UI
{
    public sealed class ObjectivePanel : MonoBehaviour
    {
        [SerializeField] private Hub hub = null;

        public void Configure(Hub objectiveHub)
        {
            hub = objectiveHub;
        }

        private void OnGUI()
        {
            if (hub == null || hub.Progress == null)
            {
                return;
            }

            var panelRect = new Rect(16f, 16f, 320f, 112f);
            GUI.Box(panelRect, GUIContent.none);
            GUILayout.BeginArea(new Rect(28f, 24f, 296f, 96f));

            if (hub.Progress.AreAllObjectivesComplete)
            {
                GUILayout.Label("MVP Objectives Complete");
            }
            else
            {
                ObjectiveDefinition current = hub.Progress.CurrentObjective;
                GUILayout.Label(current.DisplayName);
                GUILayout.Label(current.TargetRune.ToString());
                GUILayout.Label($"{hub.Progress.CurrentCount} / {current.RequiredCount}");
            }

            if (!string.IsNullOrEmpty(hub.LastDeliveryMessage))
            {
                GUILayout.Label(hub.LastDeliveryMessage);
            }

            GUILayout.EndArea();
        }
    }
}
