using FantasyShapez.Food;
using FantasyShapez.Objectives;
using FantasyShapez.Production;
using FantasyShapez.Runes;
using UnityEngine;

namespace FantasyShapez.UI
{
    public sealed class ObjectivePanel : MonoBehaviour
    {
        [SerializeField] private Hub hub = null;
        private static readonly RuneSigil[] EngraverSigilOptions =
        {
            RuneSigil.Spirit,
            RuneSigil.Acceleration
        };
        private static readonly RuneElement[] InfuserElementOptions =
        {
            RuneElement.Fire,
            RuneElement.Water,
            RuneElement.Earth,
            RuneElement.Wind
        };

        private Engraver selectedEngraver;
        private ElementInfuser selectedInfuser;
        private FarmPlot selectedFarmPlot;
        private Harvester selectedHarvester;
        private Processor selectedProcessor;

        public void Configure(Hub objectiveHub)
        {
            hub = objectiveHub;
        }

        public void ShowEngraver(Engraver engraver)
        {
            selectedEngraver = engraver;
            selectedInfuser = null;
            selectedFarmPlot = null;
            selectedHarvester = null;
            selectedProcessor = null;
        }

        public void ShowElementInfuser(ElementInfuser infuser)
        {
            selectedEngraver = null;
            selectedInfuser = infuser;
            selectedFarmPlot = null;
            selectedHarvester = null;
            selectedProcessor = null;
        }

        public void ShowFarmPlot(FarmPlot farmPlot)
        {
            selectedEngraver = null;
            selectedInfuser = null;
            selectedFarmPlot = farmPlot;
            selectedHarvester = null;
            selectedProcessor = null;
        }

        public void ShowHarvester(Harvester harvester)
        {
            selectedEngraver = null;
            selectedInfuser = null;
            selectedFarmPlot = null;
            selectedHarvester = harvester;
            selectedProcessor = null;
        }

        public void ShowProcessor(Processor processor)
        {
            selectedEngraver = null;
            selectedInfuser = null;
            selectedFarmPlot = null;
            selectedHarvester = null;
            selectedProcessor = processor;
        }

        private void OnGUI()
        {
            if (hub != null && hub.Progress != null)
            {
                DrawObjectivePanel();
            }

            DrawMachineConfigurationPanel();
        }

        private void DrawObjectivePanel()
        {
            int requirementLineCount = 0;
            if (!hub.Progress.AreAllObjectivesComplete)
            {
                foreach (ObjectiveRequirement requirement in
                    hub.Progress.CurrentObjective.Requirements)
                {
                    requirementLineCount += requirement.RequirementType ==
                        ObjectiveRequirementType.SustainedRate ? 2 : 1;
                }
            }

            float panelHeight = 72f + (requirementLineCount * 22f);
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
                    if (requirement.RequirementType == ObjectiveRequirementType.SustainedRate)
                    {
                        GUILayout.Label($"{requirement.TargetRune}: " +
                            $"{hub.Progress.GetCurrentRate(index):0.##} / " +
                            $"{requirement.TargetRatePerSecond:0.##} per sec");
                        GUILayout.Label($"Sustain: " +
                            $"{hub.Progress.GetSustainProgress(index):0.#} / " +
                            $"{requirement.SustainDurationSeconds:0.#} sec");
                    }
                    else
                    {
                        GUILayout.Label(
                            $"{requirement.TargetRune}: " +
                            $"{hub.Progress.GetCurrentCount(index)} / {requirement.RequiredCount}");
                    }
                }
            }

            if (!string.IsNullOrEmpty(hub.LastDeliveryMessage))
            {
                GUILayout.Label(hub.LastDeliveryMessage);
            }

            GUILayout.EndArea();
        }

        private void DrawMachineConfigurationPanel()
        {
            if (selectedEngraver != null)
            {
                DrawEngraverPanel();
            }
            else if (selectedInfuser != null)
            {
                DrawInfuserPanel();
            }
            else if (selectedFarmPlot != null)
            {
                DrawFarmPlotPanel();
            }
            else if (selectedHarvester != null)
            {
                DrawHarvesterPanel();
            }
            else if (selectedProcessor != null)
            {
                DrawProcessorPanel();
            }
        }

        private void DrawProcessorPanel()
        {
            const float height = 190f;
            GUI.Box(new Rect(352f, 16f, 300f, height), GUIContent.none);
            GUILayout.BeginArea(new Rect(364f, 24f, 276f, height - 16f));
            GUILayout.Label("Processor");
            GUILayout.Label($"State: {selectedProcessor.ProcessingStateMessage}");
            GUILayout.Label($"Property port: {selectedProcessor.PropertyCell}");
            GUILayout.Label(selectedProcessor.SupplyMessage);
            GUILayout.Label($"Last event: {selectedProcessor.LastRecipeMessage}");
            if (selectedProcessor.HasOutput)
            {
                GUILayout.Label("Product output blocked; waiting for a belt.");
            }

            if (GUILayout.Button("Close"))
            {
                selectedProcessor = null;
            }

            GUILayout.EndArea();
        }

        private void DrawFarmPlotPanel()
        {
            float height = 132f + selectedFarmPlot.AvailableCrops.Count * 28f;
            GUI.Box(new Rect(352f, 16f, 300f, height), GUIContent.none);
            GUILayout.BeginArea(new Rect(364f, 24f, 276f, height - 16f));
            GUILayout.Label("Farm Plot");
            GUILayout.Label($"Crop: {selectedFarmPlot.SelectedCrop?.Id ?? "None"}");
            GUILayout.Label($"Mature crops: {selectedFarmPlot.MatureCount} / " +
                selectedFarmPlot.MatureCapacity);
            if (selectedFarmPlot.SelectedCrop == null)
            {
                GUILayout.Label("Select a crop to start growing.");
            }
            DrawCropSelection(selectedFarmPlot);

            if (GUILayout.Button("Close"))
            {
                selectedFarmPlot = null;
            }

            GUILayout.EndArea();
        }

        private void DrawHarvesterPanel()
        {
            FarmPlot plot = selectedHarvester.ConnectedFarmPlot;
            float height = 150f + (plot?.AvailableCrops.Count ?? 0) * 28f;
            GUI.Box(new Rect(352f, 16f, 300f, height), GUIContent.none);
            GUILayout.BeginArea(new Rect(364f, 24f, 276f, height - 16f));
            GUILayout.Label("Harvester");
            GUILayout.Label(plot != null
                ? $"Farm Plot crop: {plot.SelectedCrop?.Id ?? "None"}"
                : "No Farm Plot under Harvester");
            if (plot != null)
            {
                GUILayout.Label($"Mature crops: {plot.MatureCount} / {plot.MatureCapacity}");
                DrawCropSelection(plot);
            }
            GUILayout.Label($"Buffered crops: {selectedHarvester.OutputCount} / " +
                selectedHarvester.OutputCapacity);
            if (GUILayout.Button("Close"))
            {
                selectedHarvester = null;
            }

            GUILayout.EndArea();
        }

        private static void DrawCropSelection(FarmPlot plot)
        {
            foreach (CropDefinition crop in plot.AvailableCrops)
            {
                if (crop != null && GUILayout.Button($"Grow {crop.Id}"))
                {
                    plot.SelectCrop(crop);
                }
            }

            if (GUILayout.Button("Clear Crop"))
            {
                plot.SelectCrop(null);
            }
        }

        private void DrawEngraverPanel()
        {
            var panelRect = new Rect(352f, 16f, 300f, 214f);
            GUI.Box(panelRect, GUIContent.none);
            GUILayout.BeginArea(new Rect(364f, 24f, 276f, 198f));
            GUILayout.Label("Engraver Configuration");
            GUILayout.Label("Sigil (next rune)");
            DrawSigilButtons();
            GUILayout.Space(6f);
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

        private void DrawSigilButtons()
        {
            GUILayout.BeginHorizontal();
            foreach (RuneSigil sigil in EngraverSigilOptions)
            {
                bool previousEnabled = GUI.enabled;
                GUI.enabled = selectedEngraver.SelectedSigil != sigil;
                if (GUILayout.Button(sigil.ToString()))
                {
                    selectedEngraver.SetSelectedSigil(sigil);
                }

                GUI.enabled = previousEnabled;
            }

            GUILayout.EndHorizontal();
        }

        private void DrawInfuserPanel()
        {
            if (selectedInfuser == null)
            {
                return;
            }

            var panelRect = new Rect(352f, 16f, 300f, 128f);
            GUI.Box(panelRect, GUIContent.none);
            GUILayout.BeginArea(new Rect(364f, 24f, 276f, 112f));
            GUILayout.Label("Element Infuser Configuration");
            GUILayout.Label("Primary element (next rune)");
            GUILayout.BeginHorizontal();
            foreach (RuneElement element in InfuserElementOptions)
            {
                bool previousEnabled = GUI.enabled;
                GUI.enabled = selectedInfuser.PrimaryElement != element;
                if (GUILayout.Button(element.ToString()))
                {
                    selectedInfuser.SetPrimaryElement(element);
                }

                GUI.enabled = previousEnabled;
            }

            GUILayout.EndHorizontal();
            if (GUILayout.Button("Close"))
            {
                selectedInfuser = null;
            }

            GUILayout.EndArea();
        }
    }
}
