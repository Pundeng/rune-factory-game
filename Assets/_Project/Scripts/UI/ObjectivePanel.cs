using FantasyShapez.Objectives;
using FantasyShapez.Production;
using UnityEngine;

namespace FantasyShapez.UI
{
    public sealed class ObjectivePanel : MonoBehaviour
    {
        [SerializeField] private Hub hub = null;
        private Engraver selectedEngraver;

        public void Configure(Hub objectiveHub)
        {
            hub = objectiveHub;
        }

        public void ShowEngraver(Engraver engraver)
        {
            selectedEngraver = engraver;
        }

        private void OnGUI()
        {
            if (hub == null || hub.Progress == null)
            {
                return;
            }

            int requirementCount = hub.Progress.AreAllObjectivesComplete
                ? 0
                : hub.Progress.CurrentObjective.Requirements.Count;
            float panelHeight = 72f + (requirementCount * 22f);
            var panelRect = new Rect(16f, 16f, 320f, panelHeight);
            GUI.Box(panelRect, GUIContent.none);
            GUILayout.BeginArea(new Rect(28f, 24f, 296f, panelHeight - 16f));

            if (hub.Progress.AreAllObjectivesComplete)
            {
                GUILayout.Label("MVP Objectives Complete");
            }
            else
            {
                ObjectiveDefinition current = hub.Progress.CurrentObjective;
                GUILayout.Label(current.DisplayName);
                for (int index = 0; index < current.Requirements.Count; index++)
                {
                    ObjectiveRequirement requirement = current.Requirements[index];
                    GUILayout.Label(
                        $"{requirement.TargetRune}: " +
                        $"{hub.Progress.GetCurrentCount(index)} / {requirement.RequiredCount}");
                }
            }

            if (!string.IsNullOrEmpty(hub.LastDeliveryMessage))
            {
                GUILayout.Label(hub.LastDeliveryMessage);
            }

            GUILayout.EndArea();

            DrawEngraverUpgradePanel();
        }

        private void DrawEngraverUpgradePanel()
        {
            if (selectedEngraver == null)
            {
                return;
            }

            var panelRect = new Rect(352f, 16f, 300f, 152f);
            GUI.Box(panelRect, GUIContent.none);
            GUILayout.BeginArea(new Rect(364f, 24f, 276f, 136f));
            GUILayout.Label("Engraver Acceleration Socket");
            GUILayout.Label(selectedEngraver.IsAccelerationSocketOccupied
                ? "Socket: Acceleration Rune installed"
                : "Socket: Empty");
            GUILayout.Label($"Hub Acceleration Runes: {hub.AccelerationRuneCount}");
            float durationPercent = 100f / selectedEngraver.UpgradedSpeedMultiplier;
            GUILayout.Label($"Installed effect: {durationPercent:0}% processing duration");

            bool previousEnabled = GUI.enabled;
            GUI.enabled = !selectedEngraver.IsAccelerationSocketOccupied &&
                hub.AccelerationRuneCount >= 1;
            if (GUILayout.Button("Install Acceleration Rune"))
            {
                selectedEngraver.TryInstallAccelerationRune(hub.TryConsumeAccelerationRune);
            }

            GUI.enabled = previousEnabled;
            if (GUILayout.Button("Close"))
            {
                selectedEngraver = null;
            }

            GUILayout.EndArea();
        }
    }
}
