using System;
using UnityEngine;

namespace FantasyShapez.Runes
{
    [Serializable]
    public struct ElementZoneAssignment : IEquatable<ElementZoneAssignment>
    {
        [SerializeField] private ElementZone zone;
        [SerializeField] private RuneElement element;

        public ElementZoneAssignment(ElementZone zone, RuneElement element)
        {
            if (!Enum.IsDefined(typeof(ElementZone), zone))
            {
                throw new ArgumentOutOfRangeException(nameof(zone), zone, null);
            }

            if (!Enum.IsDefined(typeof(RuneElement), element))
            {
                throw new ArgumentOutOfRangeException(nameof(element), element, null);
            }

            this.zone = zone;
            this.element = element;
        }

        public ElementZone Zone => zone;

        public RuneElement Element => element;

        public bool Equals(ElementZoneAssignment other)
        {
            return zone == other.zone && element == other.element;
        }

        public override bool Equals(object obj)
        {
            return obj is ElementZoneAssignment other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(zone, element);
        }

        public override string ToString()
        {
            return $"{zone}:{element}";
        }

        public static bool operator ==(ElementZoneAssignment left, ElementZoneAssignment right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ElementZoneAssignment left, ElementZoneAssignment right)
        {
            return !left.Equals(right);
        }
    }
}
