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
                    currentRequirementCounts[index] >= requirement.RequiredCount)
                {
                    continue;
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

        private bool AreCurrentRequirementsComplete()
        {
            for (int index = 0; index < CurrentObjective.Requirements.Count; index++)
            {
                if (currentRequirementCounts[index] <
                    CurrentObjective.Requirements[index].RequiredCount)
                {
                    return false;
                }
            }

            return true;
        }

        private void ResetCurrentRequirementCounts()
        {
            currentRequirementCounts = AreAllObjectivesComplete
                ? Array.Empty<int>()
                : new int[CurrentObjective.Requirements.Count];
        }
    }
}
