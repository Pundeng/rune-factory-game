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
        private int currentCount;

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
        }

        public bool AreAllObjectivesComplete => currentObjectiveIndex >= objectives.Length;

        public ObjectiveDefinition CurrentObjective =>
            AreAllObjectivesComplete ? null : objectives[currentObjectiveIndex];

        public int CurrentCount => currentCount;

        public int CurrentObjectiveIndex => currentObjectiveIndex;

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

            if (!CurrentObjective.TargetRune.Equals(deliveredRune))
            {
                return RuneDeliveryResult.Incorrect;
            }

            currentCount++;
            if (currentCount < CurrentObjective.RequiredCount)
            {
                return RuneDeliveryResult.Correct;
            }

            currentObjectiveIndex++;
            currentCount = 0;
            return RuneDeliveryResult.CorrectAndObjectiveCompleted;
        }
    }
}
