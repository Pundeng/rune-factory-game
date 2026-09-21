using System;
using FantasyShapez.Runes;

namespace FantasyShapez.Objectives
{
    public sealed class AccelerationRuneInventory
    {
        private static readonly RuneData AccelerationRune = new(
            RuneBaseShape.Circle,
            RuneSigil.Acceleration);
        private static readonly RuneData LegacyAccelerationRune = new(
            RuneBaseShape.Circle,
            new[] { new GlyphData(GlyphType.Acceleration, GlyphRotation.Degrees0) },
            null);

        public int Count { get; private set; }

        public bool StoreDeliveredRune(RuneData rune)
        {
            if (rune == null)
            {
                throw new ArgumentNullException(nameof(rune));
            }

            if (!AccelerationRune.Equals(rune) && !LegacyAccelerationRune.Equals(rune))
            {
                return false;
            }

            Count++;
            return true;
        }

        public bool TryConsume()
        {
            if (Count <= 0)
            {
                return false;
            }

            Count--;
            return true;
        }
    }
}
