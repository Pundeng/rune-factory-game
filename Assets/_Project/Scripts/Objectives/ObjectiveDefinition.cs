using System;
using System.Collections.Generic;
using System.Linq;
using FantasyShapez.Runes;
using UnityEngine;

namespace FantasyShapez.Objectives
{
    public enum ObjectiveRequirementType
    {
        Cumulative,
        SustainedRate
    }

    [Serializable]
    public sealed class ObjectiveRequirement
    {
        [SerializeField] private ObjectiveRequirementType requirementType;
        [SerializeField] private RuneData targetRune = new(RuneBaseShape.Circle);
        [SerializeField, Min(1)] private int requiredCount = 1;
        [SerializeField, Min(0.01f)] private float targetRatePerSecond = 1f;
        [SerializeField, Min(0.01f)] private float measurementWindowSeconds = 5f;
        [SerializeField, Min(0.01f)] private float sustainDurationSeconds = 15f;

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
            requirementType = ObjectiveRequirementType.Cumulative;
        }

        private ObjectiveRequirement(
            RuneData targetRune,
            float targetRatePerSecond,
            float measurementWindowSeconds,
            float sustainDurationSeconds)
        {
            if (targetRune == null)
            {
                throw new ArgumentNullException(nameof(targetRune));
            }

            if (targetRatePerSecond <= 0f || float.IsNaN(targetRatePerSecond))
            {
                throw new ArgumentOutOfRangeException(nameof(targetRatePerSecond));
            }

            if (measurementWindowSeconds <= 0f || float.IsNaN(measurementWindowSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(measurementWindowSeconds));
            }

            if (sustainDurationSeconds < measurementWindowSeconds ||
                float.IsNaN(sustainDurationSeconds))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sustainDurationSeconds),
                    "Sustain duration must cover at least one measurement window.");
            }

            requirementType = ObjectiveRequirementType.SustainedRate;
            this.targetRune = targetRune.Copy();
            requiredCount = 1;
            this.targetRatePerSecond = targetRatePerSecond;
            this.measurementWindowSeconds = measurementWindowSeconds;
            this.sustainDurationSeconds = sustainDurationSeconds;
        }

        public static ObjectiveRequirement SustainedRate(
            RuneData targetRune,
            float targetRatePerSecond,
            float measurementWindowSeconds,
            float sustainDurationSeconds)
        {
            return new ObjectiveRequirement(
                targetRune,
                targetRatePerSecond,
                measurementWindowSeconds,
                sustainDurationSeconds);
        }

        public ObjectiveRequirementType RequirementType => requirementType;

        public RuneData TargetRune => targetRune;

        public int RequiredCount => requiredCount;

        public float TargetRatePerSecond => targetRatePerSecond;

        public float MeasurementWindowSeconds => measurementWindowSeconds;

        public float SustainDurationSeconds => sustainDurationSeconds;

        internal ObjectiveRequirement Copy()
        {
            return requirementType == ObjectiveRequirementType.SustainedRate
                ? SustainedRate(
                    targetRune,
                    targetRatePerSecond,
                    measurementWindowSeconds,
                    sustainDurationSeconds)
                : new ObjectiveRequirement(targetRune, requiredCount);
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
