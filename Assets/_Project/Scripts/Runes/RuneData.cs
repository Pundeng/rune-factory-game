using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace FantasyShapez.Runes
{
    [Serializable]
    public sealed class RuneData : IEquatable<RuneData>
    {
        [SerializeField] private RuneBaseShape baseShape;
        [SerializeField] private GlyphData[] glyphs = Array.Empty<GlyphData>();
        [SerializeField] private ElementZoneAssignment[] elementZones =
            Array.Empty<ElementZoneAssignment>();

        [NonSerialized] private ReadOnlyCollection<GlyphData> glyphView;
        [NonSerialized] private ReadOnlyCollection<ElementZoneAssignment> elementZoneView;

        public RuneData(RuneBaseShape baseShape)
            : this(baseShape, Array.Empty<GlyphData>(), Array.Empty<ElementZoneAssignment>())
        {
        }

        public RuneData(
            RuneBaseShape baseShape,
            IEnumerable<GlyphData> glyphs,
            IEnumerable<ElementZoneAssignment> elementZones)
        {
            if (!Enum.IsDefined(typeof(RuneBaseShape), baseShape))
            {
                throw new ArgumentOutOfRangeException(nameof(baseShape), baseShape, null);
            }

            GlyphData[] glyphCopies = (glyphs ?? Array.Empty<GlyphData>()).ToArray();
            ElementZoneAssignment[] elementZoneCopies =
                (elementZones ?? Array.Empty<ElementZoneAssignment>()).ToArray();

            EnsureGlyphsAreUnique(glyphCopies);
            EnsureElementZonesAreUnique(elementZoneCopies);

            this.baseShape = baseShape;
            this.glyphs = glyphCopies;
            this.elementZones = elementZoneCopies;
            glyphView = Array.AsReadOnly(this.glyphs);
            elementZoneView = Array.AsReadOnly(this.elementZones);
        }

        public RuneBaseShape BaseShape => baseShape;

        public IReadOnlyList<GlyphData> Glyphs => glyphView ??= Array.AsReadOnly(glyphs);

        public IReadOnlyList<ElementZoneAssignment> ElementZones =>
            elementZoneView ??= Array.AsReadOnly(elementZones);

        public RuneData Copy()
        {
            return new RuneData(BaseShape, Glyphs, ElementZones);
        }

        public bool TryGetElement(ElementZone zone, out RuneElement element)
        {
            foreach (ElementZoneAssignment assignment in ElementZones)
            {
                if (assignment.Zone == zone)
                {
                    element = assignment.Element;
                    return true;
                }
            }

            element = default;
            return false;
        }

        public bool Equals(RuneData other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return BaseShape == other.BaseShape &&
                   new HashSet<GlyphData>(Glyphs).SetEquals(other.Glyphs) &&
                   new HashSet<ElementZoneAssignment>(ElementZones).SetEquals(other.ElementZones);
        }

        public override bool Equals(object obj)
        {
            return obj is RuneData other && Equals(other);
        }

        public override int GetHashCode()
        {
            int glyphHash = 0;
            foreach (GlyphData glyph in Glyphs)
            {
                glyphHash ^= glyph.GetHashCode();
            }

            int elementZoneHash = 0;
            foreach (ElementZoneAssignment assignment in ElementZones)
            {
                elementZoneHash ^= assignment.GetHashCode();
            }

            return HashCode.Combine(
                BaseShape,
                Glyphs.Count,
                glyphHash,
                ElementZones.Count,
                elementZoneHash);
        }

        public override string ToString()
        {
            IEnumerable<string> glyphParts = Glyphs
                .OrderBy(glyph => glyph.Type)
                .ThenBy(glyph => glyph.Rotation)
                .Select(glyph => glyph.ToString());
            IEnumerable<string> elementParts = ElementZones
                .OrderBy(assignment => assignment.Zone)
                .Select(assignment => assignment.ToString());

            return string.Join(" | ", new[] { BaseShape.ToString() }.Concat(glyphParts).Concat(elementParts));
        }

        public static bool operator ==(RuneData left, RuneData right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(RuneData left, RuneData right)
        {
            return !Equals(left, right);
        }

        private static void EnsureGlyphsAreUnique(IEnumerable<GlyphData> glyphs)
        {
            var uniqueGlyphs = new HashSet<GlyphData>();
            foreach (GlyphData glyph in glyphs)
            {
                if (!uniqueGlyphs.Add(glyph))
                {
                    throw new ArgumentException($"Duplicate glyph '{glyph}' is not allowed.", nameof(glyphs));
                }
            }
        }

        private static void EnsureElementZonesAreUnique(IEnumerable<ElementZoneAssignment> elementZones)
        {
            var assignedZones = new HashSet<ElementZone>();
            foreach (ElementZoneAssignment assignment in elementZones)
            {
                if (!assignedZones.Add(assignment.Zone))
                {
                    throw new ArgumentException(
                        $"Element zone '{assignment.Zone}' cannot have more than one assignment.",
                        nameof(elementZones));
                }
            }
        }
    }
}
