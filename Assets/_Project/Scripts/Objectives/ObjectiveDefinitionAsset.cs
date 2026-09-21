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
        [SerializeField, Min(1)] private int requiredCount = 1;
        [SerializeField] private RuneData targetRune = new(RuneBaseShape.Circle);
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
            additionalRequirements = Array.Empty<ObjectiveRequirement>();
        }

        public void Configure(string name, params ObjectiveRequirement[] requirements)
        {
            if (requirements == null)
            {
                throw new ArgumentNullException(nameof(requirements));
            }

            var runtimeDefinition = new ObjectiveDefinition(name, requirements);
            displayName = runtimeDefinition.DisplayName;
            targetRune = runtimeDefinition.TargetRune.Copy();
            requiredCount = runtimeDefinition.RequiredCount;
            additionalRequirements = runtimeDefinition.Requirements
                .Skip(1)
                .Select(requirement => requirement.Copy())
                .ToArray();
        }

        public ObjectiveDefinition CreateRuntimeDefinition()
        {
            ObjectiveRequirement[] requirements = new[]
                {
                    new ObjectiveRequirement(targetRune, requiredCount)
                }
                .Concat(additionalRequirements ?? Array.Empty<ObjectiveRequirement>())
                .ToArray();
            return new ObjectiveDefinition(displayName, requirements);
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = "Objective";
            }

            requiredCount = Mathf.Max(1, requiredCount);
            targetRune ??= new RuneData(RuneBaseShape.Circle);
            additionalRequirements ??= Array.Empty<ObjectiveRequirement>();
        }
    }
}
