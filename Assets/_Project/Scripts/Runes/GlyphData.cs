using System;
using UnityEngine;

namespace FantasyShapez.Runes
{
    [Serializable]
    public struct GlyphData : IEquatable<GlyphData>
    {
        [SerializeField] private GlyphType type;
        [SerializeField] private GlyphRotation rotation;

        public GlyphData(GlyphType type, GlyphRotation rotation)
        {
            if (!Enum.IsDefined(typeof(GlyphType), type))
            {
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            if (!Enum.IsDefined(typeof(GlyphRotation), rotation))
            {
                throw new ArgumentOutOfRangeException(nameof(rotation), rotation, null);
            }

            this.type = type;
            this.rotation = rotation;
        }

        public GlyphType Type => type;

        public GlyphRotation Rotation => rotation;

        public GlyphData RotateClockwise()
        {
            return new GlyphData(type, rotation.RotateClockwise());
        }

        public bool Equals(GlyphData other)
        {
            return type == other.type && rotation == other.rotation;
        }

        public override bool Equals(object obj)
        {
            return obj is GlyphData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(type, rotation);
        }

        public override string ToString()
        {
            return $"{type}@{(int)rotation}";
        }

        public static bool operator ==(GlyphData left, GlyphData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GlyphData left, GlyphData right)
        {
            return !left.Equals(right);
        }
    }
}
