using System;

namespace FantasyShapez.Runes
{
    public enum RuneBaseShape
    {
        Circle
    }

    public enum RuneSigil
    {
        None,
        Spirit,
        Acceleration
    }

    public enum GlyphType
    {
        Attack,
        Split,
        Acceleration
    }

    public enum GlyphRotation
    {
        Degrees0 = 0,
        Degrees90 = 90,
        Degrees180 = 180,
        Degrees270 = 270
    }

    public enum RuneElement
    {
        Fire,
        Air,
        Water,
        Earth,
        Wind
    }

    public enum ElementZone
    {
        Left,
        Right
    }

    public static class GlyphRotationExtensions
    {
        public static GlyphRotation RotateClockwise(this GlyphRotation rotation)
        {
            return rotation switch
            {
                GlyphRotation.Degrees0 => GlyphRotation.Degrees90,
                GlyphRotation.Degrees90 => GlyphRotation.Degrees180,
                GlyphRotation.Degrees180 => GlyphRotation.Degrees270,
                GlyphRotation.Degrees270 => GlyphRotation.Degrees0,
                _ => throw new ArgumentOutOfRangeException(nameof(rotation), rotation, null)
            };
        }
    }
}
