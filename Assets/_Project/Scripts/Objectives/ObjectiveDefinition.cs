using System;
using FantasyShapez.Runes;
using UnityEngine;

namespace FantasyShapez.Objectives
{
    [Serializable]
    public sealed class ObjectiveDefinition
    {
        [SerializeField] private string displayName = "Objective";
        [SerializeField] private RuneData targetRune = new(RuneBaseShape.Circle);
        [SerializeField, Min(1)] private int requiredCount = 1;

        public ObjectiveDefinition(string displayName, RuneData targetRune, int requiredCount)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("An objective display name is required.", nameof(displayName));
            }

            if (targetRune == null)
            {
                throw new ArgumentNullException(nameof(targetRune));
            }

            if (requiredCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requiredCount),
                    "Required count must be positive.");
            }

            this.displayName = displayName;
            this.targetRune = targetRune.Copy();
            this.requiredCount = requiredCount;
        }

        public string DisplayName => displayName;

        public RuneData TargetRune => targetRune;

        public int RequiredCount => requiredCount;

        internal ObjectiveDefinition Copy()
        {
            return new ObjectiveDefinition(displayName, targetRune, requiredCount);
        }
    }
}
