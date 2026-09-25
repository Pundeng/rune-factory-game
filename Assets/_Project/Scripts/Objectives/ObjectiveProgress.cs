using System;
using System.Collections.Generic;
using System.Linq;
using FantasyShapez.Runes;

namespace FantasyShapez.Objectives
{
    public enum RuneDeliveryResult
    {
        Incorrect,
        Correct,
        CorrectAndObjectiveCompleted,
        AllObjectivesAlreadyComplete
    }

    public sealed class ObjectiveProgress
    {
        private readonly ObjectiveDefinition[] objectives;
        private int currentObjectiveIndex;
        private int[] currentRequirementCounts;
        private int[] currentWindowDeliveryCounts;
        private float[] currentWindowElapsedSeconds;
        private float[] currentRatesPerSecond;
        private float[] currentSustainSeconds;

        public ObjectiveProgress(IEnumerable<ObjectiveDefinition> objectives)
        {
            if (objectives == null)
            {
                throw new ArgumentNullException(nameof(objectives));
            }

            this.objectives = objectives
                .Select(objective => objective?.Copy() ??
                    throw new ArgumentException("Objectives cannot contain null entries.", nameof(objectives)))
                .ToArray();

            if (this.objectives.Length == 0)
            {
                throw new ArgumentException("At least one objective is required.", nameof(objectives));
            }

            ResetCurrentRequirementCounts();
        }

        public bool AreAllObjectivesComplete => currentObjectiveIndex >= objectives.Length;

        public ObjectiveDefinition CurrentObjective =>
            AreAllObjectivesComplete ? null : objectives[currentObjectiveIndex];

        public int CurrentCount =>
            AreAllObjectivesComplete ? 0 : currentRequirementCounts[0];

        public int CurrentObjectiveIndex => currentObjectiveIndex;

        public bool HasProgress => currentObjectiveIndex > 0 ||
            currentRequirementCounts.Any(count => count > 0) ||
            currentWindowDeliveryCounts.Any(count => count > 0) ||
            currentRatesPerSecond.Any(rate => rate > 0f) ||
            currentSustainSeconds.Any(seconds => seconds > 0f);

        public int GetCurrentCount(int requirementIndex)
        {
            if (AreAllObjectivesComplete)
            {
                throw new InvalidOperationException("All objectives are already complete.");
            }

            if (requirementIndex < 0 ||
                requirementIndex >= currentRequirementCounts.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(requirementIndex));
            }

            return currentRequirementCounts[requirementIndex];
        }

        public float GetCurrentRate(int requirementIndex)
        {
            ValidateRequirementIndex(requirementIndex);
            return currentRatesPerSecond[requirementIndex];
        }

        public float GetSustainProgress(int requirementIndex)
        {
            ValidateRequirementIndex(requirementIndex);
            return currentSustainSeconds[requirementIndex];
        }

        public RuneDeliveryResult Deliver(RuneData deliveredRune)
        {
            if (deliveredRune == null)
            {
                throw new ArgumentNullException(nameof(deliveredRune));
            }

            if (AreAllObjectivesComplete)
            {
                return RuneDeliveryResult.AllObjectivesAlreadyComplete;
            }

            for (int index = 0; index < CurrentObjective.Requirements.Count; index++)
            {
                ObjectiveRequirement requirement = CurrentObjective.Requirements[index];
                if (!requirement.TargetRune.Equals(deliveredRune) ||
                    IsRequirementComplete(index))
                {
                    continue;
                }

                if (requirement.RequirementType == ObjectiveRequirementType.SustainedRate)
                {
                    currentWindowDeliveryCounts[index]++;
                    return RuneDeliveryResult.Correct;
                }

                currentRequirementCounts[index]++;
                if (!AreCurrentRequirementsComplete())
                {
                    return RuneDeliveryResult.Correct;
                }

                currentObjectiveIndex++;
                ResetCurrentRequirementCounts();
                return RuneDeliveryResult.CorrectAndObjectiveCompleted;
            }

            return RuneDeliveryResult.Incorrect;
        }

        public bool AdvanceTime(float deltaSeconds)
        {
            if (deltaSeconds < 0f || float.IsNaN(deltaSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            }

            if (AreAllObjectivesComplete || deltaSeconds == 0f)
            {
                return false;
            }

            float remainingSeconds = deltaSeconds;
            while (remainingSeconds > 0f && !AreAllObjectivesComplete)
            {
                float stepSeconds = remainingSeconds;
                bool hasSustainedRequirement = false;
                for (int index = 0; index < CurrentObjective.Requirements.Count; index++)
                {
                    ObjectiveRequirement requirement = CurrentObjective.Requirements[index];
                    if (requirement.RequirementType != ObjectiveRequirementType.SustainedRate)
                    {
                        continue;
                    }

                    hasSustainedRequirement = true;
                    stepSeconds = Math.Min(
                        stepSeconds,
                        requirement.MeasurementWindowSeconds -
                            currentWindowElapsedSeconds[index]);
                }

                if (!hasSustainedRequirement)
                {
                    return false;
                }

                for (int index = 0; index < CurrentObjective.Requirements.Count; index++)
                {
                    if (CurrentObjective.Requirements[index].RequirementType ==
                        ObjectiveRequirementType.SustainedRate)
                    {
                        currentWindowElapsedSeconds[index] += stepSeconds;
                    }
                }

                remainingSeconds -= stepSeconds;
                EvaluateCompletedWindows();
                if (AreCurrentRequirementsComplete())
                {
                    currentObjectiveIndex++;
                    ResetCurrentRequirementCounts();
                    return true;
                }
            }

            return false;
        }

        private bool AreCurrentRequirementsComplete()
        {
            for (int index = 0; index < CurrentObjective.Requirements.Count; index++)
            {
                if (!IsRequirementComplete(index))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsRequirementComplete(int index)
        {
            ObjectiveRequirement requirement = CurrentObjective.Requirements[index];
            return requirement.RequirementType == ObjectiveRequirementType.SustainedRate
                ? currentSustainSeconds[index] >= requirement.SustainDurationSeconds
                : currentRequirementCounts[index] >= requirement.RequiredCount;
        }

        private void EvaluateCompletedWindows()
        {
            for (int index = 0; index < CurrentObjective.Requirements.Count; index++)
            {
                ObjectiveRequirement requirement = CurrentObjective.Requirements[index];
                if (requirement.RequirementType != ObjectiveRequirementType.SustainedRate ||
                    currentWindowElapsedSeconds[index] < requirement.MeasurementWindowSeconds)
                {
                    continue;
                }

                float rate = currentWindowDeliveryCounts[index] /
                    requirement.MeasurementWindowSeconds;
                currentRatesPerSecond[index] = rate;
                currentSustainSeconds[index] = rate >= requirement.TargetRatePerSecond
                    ? currentSustainSeconds[index] + requirement.MeasurementWindowSeconds
                    : 0f;
                currentWindowDeliveryCounts[index] = 0;
                currentWindowElapsedSeconds[index] = 0f;
            }
        }

        private void ValidateRequirementIndex(int requirementIndex)
        {
            if (AreAllObjectivesComplete)
            {
                throw new InvalidOperationException("All objectives are already complete.");
            }

            if (requirementIndex < 0 ||
                requirementIndex >= currentRequirementCounts.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(requirementIndex));
            }
        }

        private void ResetCurrentRequirementCounts()
        {
            currentRequirementCounts = AreAllObjectivesComplete
                ? Array.Empty<int>()
                : new int[CurrentObjective.Requirements.Count];
            currentWindowDeliveryCounts = AreAllObjectivesComplete
                ? Array.Empty<int>()
                : new int[CurrentObjective.Requirements.Count];
            currentWindowElapsedSeconds = AreAllObjectivesComplete
                ? Array.Empty<float>()
                : new float[CurrentObjective.Requirements.Count];
            currentRatesPerSecond = AreAllObjectivesComplete
                ? Array.Empty<float>()
                : new float[CurrentObjective.Requirements.Count];
            currentSustainSeconds = AreAllObjectivesComplete
                ? Array.Empty<float>()
                : new float[CurrentObjective.Requirements.Count];
        }
    }
}
