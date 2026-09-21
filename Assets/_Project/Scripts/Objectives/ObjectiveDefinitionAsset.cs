using System;
using System.Linq;
using FantasyShapez.Runes;
using UnityEngine;

namespace FantasyShapez.Objectives
{
    [CreateAssetMenu(
        fileName = "ObjectiveDefinition",
        menuName = "Fantasy Shapez/Objective Definition")]
    public sealed class ObjectiveDefinitionAsset : ScriptableObject
    {
        [SerializeField] private string displayName = "Objective";
        [SerializeField] private ObjectiveRequirementType requirementType;
        [SerializeField, Min(1)] private int requiredCount = 1;
        [SerializeField] private RuneData targetRune = new(RuneBaseShape.Circle);
        [SerializeField, Min(0.01f)] private float targetRatePerSecond = 1f;
        [SerializeField, Min(0.01f)] private float measurementWindowSeconds = 5f;
        [SerializeField, Min(0.01f)] private float sustainDurationSeconds = 15f;
        [SerializeField] private ObjectiveRequirement[] additionalRequirements =
            Array.Empty<ObjectiveRequirement>();

        public string DisplayName => displayName;

        public int RequiredCount => requiredCount;

        public RuneData TargetRune => targetRune.Copy();

        public void Configure(string name, RuneData target, int count)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("An objective display name is required.", nameof(name));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Required count must be positive.");
            }

            displayName = name;
            targetRune = target.Copy();
            requiredCount = count;
            requirementType = ObjectiveRequirementType.Cumulative;
            additionalRequirements = Array.Empty<ObjectiveRequirement>();
        }

        public void ConfigureSustainedRate(
            string name,
            RuneData target,
            float ratePerSecond,
            float windowSeconds,
            float durationSeconds)
        {
            Configure(
                name,
                ObjectiveRequirement.SustainedRate(
                    target,
                    ratePerSecond,
                    windowSeconds,
                    durationSeconds));
        }

        public void Configure(string name, params ObjectiveRequirement[] requirements)
        {
            if (requirements == null)
            {
                throw new ArgumentNullException(nameof(requirements));
            }

            var runtimeDefinition = new ObjectiveDefinition(name, requirements);
            displayName = runtimeDefinition.DisplayName;
            ApplyPrimaryRequirement(runtimeDefinition.Requirements[0]);
            additionalRequirements = runtimeDefinition.Requirements
                .Skip(1)
                .Select(requirement => requirement.Copy())
                .ToArray();
        }

        public ObjectiveDefinition CreateRuntimeDefinition()
        {
            ObjectiveRequirement primaryRequirement =
                requirementType == ObjectiveRequirementType.SustainedRate
                    ? ObjectiveRequirement.SustainedRate(
                        targetRune,
                        targetRatePerSecond,
                        measurementWindowSeconds,
                        sustainDurationSeconds)
                    : new ObjectiveRequirement(targetRune, requiredCount);
            ObjectiveRequirement[] requirements = new[] { primaryRequirement }
                .Concat(additionalRequirements ?? Array.Empty<ObjectiveRequirement>())
                .ToArray();
            return new ObjectiveDefinition(displayName, requirements);
        }

        private void ApplyPrimaryRequirement(ObjectiveRequirement requirement)
        {
            requirementType = requirement.RequirementType;
            targetRune = requirement.TargetRune.Copy();
            requiredCount = requirement.RequiredCount;
            targetRatePerSecond = requirement.TargetRatePerSecond;
            measurementWindowSeconds = requirement.MeasurementWindowSeconds;
            sustainDurationSeconds = requirement.SustainDurationSeconds;
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = "Objective";
            }

            requiredCount = Mathf.Max(1, requiredCount);
            targetRune ??= new RuneData(RuneBaseShape.Circle);
            targetRatePerSecond = Mathf.Max(0.01f, targetRatePerSecond);
            measurementWindowSeconds = Mathf.Max(0.01f, measurementWindowSeconds);
            sustainDurationSeconds = Mathf.Max(
                measurementWindowSeconds,
                sustainDurationSeconds);
            additionalRequirements ??= Array.Empty<ObjectiveRequirement>();
        }
    }
}
