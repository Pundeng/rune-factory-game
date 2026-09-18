using System;
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
        }

        public ObjectiveDefinition CreateRuntimeDefinition()
        {
            return new ObjectiveDefinition(displayName, targetRune, requiredCount);
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = "Objective";
            }

            requiredCount = Mathf.Max(1, requiredCount);
            targetRune ??= new RuneData(RuneBaseShape.Circle);
        }
    }
}
