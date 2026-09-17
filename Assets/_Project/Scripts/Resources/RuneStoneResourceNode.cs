using FantasyShapez.Runes;
using UnityEngine;

namespace FantasyShapez.Resources
{
    public sealed class RuneStoneResourceNode : MonoBehaviour
    {
        [SerializeField] private RuneBaseShape baseShape = RuneBaseShape.Circle;

        private RuneStoneResource resource;

        public RuneBaseShape BaseShape => baseShape;

        public RuneStoneResource Resource => resource ??= new RuneStoneResource(baseShape);

        private void OnValidate()
        {
            resource = null;
        }
    }
}
