using System;
using System.Collections.Generic;
using System.Linq;
using FantasyShapez.Runes;
using UnityEngine;

namespace FantasyShapez.Objectives
{
    [Serializable]
    public sealed class ObjectiveRequirement
    {
        [SerializeField] private RuneData targetRune = new(RuneBaseShape.Circle);
        [SerializeField, Min(1)] private int requiredCount = 1;

        public ObjectiveRequirement(RuneData targetRune, int requiredCount)
        {
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

            this.targetRune = targetRune.Copy();
            this.requiredCount = requiredCount;
        }

        public RuneData TargetRune => targetRune;

        public int RequiredCount => requiredCount;

        internal ObjectiveRequirement Copy()
        {
            return new ObjectiveRequirement(targetRune, requiredCount);
        }
    }

    [Serializable]
    public sealed class ObjectiveDefinition
    {
        [SerializeField] private string displayName = "Objective";
        [SerializeField] private ObjectiveRequirement[] requirements =
            Array.Empty<ObjectiveRequirement>();

        public ObjectiveDefinition(string displayName, RuneData targetRune, int requiredCount)
            : this(
                displayName,
                new ObjectiveRequirement(targetRune, requiredCount))
        {
        }

        public ObjectiveDefinition(
            string displayName,
            params ObjectiveRequirement[] requirements)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("An objective display name is required.", nameof(displayName));
            }

            if (requirements == null)
            {
                throw new ArgumentNullException(nameof(requirements));
            }

            if (requirements.Length == 0)
            {
                throw new ArgumentException(
                    "At least one objective requirement is required.",
                    nameof(requirements));
            }

            this.displayName = displayName;
            this.requirements = requirements
                .Select(requirement => requirement?.Copy() ??
                    throw new ArgumentException(
                        "Objective requirements cannot contain null entries.",
                        nameof(requirements)))
                .ToArray();
        }

        public string DisplayName => displayName;

        public IReadOnlyList<ObjectiveRequirement> Requirements => requirements;

        public RuneData TargetRune => requirements[0].TargetRune;

        public int RequiredCount => requirements[0].RequiredCount;

        internal ObjectiveDefinition Copy()
        {
            return new ObjectiveDefinition(displayName, requirements);
        }
    }
}
