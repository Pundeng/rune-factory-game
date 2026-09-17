using System;
using FantasyShapez.Runes;

namespace FantasyShapez.Resources
{
    public sealed class RuneStoneResource
    {
        public RuneStoneResource(RuneBaseShape baseShape)
        {
            if (!Enum.IsDefined(typeof(RuneBaseShape), baseShape))
            {
                throw new ArgumentOutOfRangeException(nameof(baseShape), baseShape, null);
            }

            BaseShape = baseShape;
        }

        public RuneBaseShape BaseShape { get; }

        public RuneData Extract()
        {
            return new RuneData(BaseShape);
        }
    }
}
