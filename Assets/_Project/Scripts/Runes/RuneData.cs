using System;
using System.Collections.Generic;
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
        [SerializeField] private RuneSigil sigil;
        [SerializeField] private bool hasPrimaryElement;
        [SerializeField] private RuneElement primaryElement;

        public RuneData(RuneBaseShape baseShape)
            : this(baseShape, Array.Empty<GlyphData>(), Array.Empty<ElementZoneAssignment>())
        {
        }

        public RuneData(
            RuneBaseShape baseShape,
            RuneSigil sigil,
            RuneElement? primaryElement = null)
            : this(
                baseShape,
                sigil,
                primaryElement,
                Array.Empty<GlyphData>(),
                Array.Empty<ElementZoneAssignment>())
        {
        }

        public RuneData(
            RuneBaseShape baseShape,
            IEnumerable<GlyphData> glyphs,
            IEnumerable<ElementZoneAssignment> elementZones)
            : this(baseShape, RuneSigil.None, null, glyphs, elementZones)
        {
        }

        public RuneData(
            RuneBaseShape baseShape,
            RuneSigil sigil,
            RuneElement? primaryElement,
            IEnumerable<GlyphData> glyphs,
            IEnumerable<ElementZoneAssignment> elementZones)
        {
            if (!Enum.IsDefined(typeof(RuneBaseShape), baseShape))
            {
                throw new ArgumentOutOfRangeException(nameof(baseShape), baseShape, null);
            }

            if (!Enum.IsDefined(typeof(RuneSigil), sigil))
            {
                throw new ArgumentOutOfRangeException(nameof(sigil), sigil, null);
            }

            if (primaryElement.HasValue &&
                !Enum.IsDefined(typeof(RuneElement), primaryElement.Value))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(primaryElement),
                    primaryElement,
                    null);
            }

            GlyphData[] glyphCopies = (glyphs ?? Array.Empty<GlyphData>()).ToArray();
            ElementZoneAssignment[] elementZoneCopies =
                (elementZones ?? Array.Empty<ElementZoneAssignment>()).ToArray();

            EnsureGlyphsAreUnique(glyphCopies);
            EnsureElementZonesAreUnique(elementZoneCopies);

            this.baseShape = baseShape;
            this.glyphs = glyphCopies;
            this.elementZones = elementZoneCopies;
            this.sigil = sigil;
            hasPrimaryElement = primaryElement.HasValue;
            this.primaryElement = primaryElement.GetValueOrDefault();
        }

        public RuneBaseShape BaseShape => baseShape;

        public RuneSigil Sigil => sigil;

        public RuneElement? PrimaryElement =>
            hasPrimaryElement ? primaryElement : null;

        public IReadOnlyList<GlyphData> Glyphs =>
            Array.AsReadOnly(glyphs ?? Array.Empty<GlyphData>());

        public IReadOnlyList<ElementZoneAssignment> ElementZones =>
            Array.AsReadOnly(elementZones ?? Array.Empty<ElementZoneAssignment>());

        public RuneData Copy()
        {
            return new RuneData(BaseShape, Sigil, PrimaryElement, Glyphs, ElementZones);
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
                   Sigil == other.Sigil &&
                   PrimaryElement == other.PrimaryElement &&
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
                Sigil,
                PrimaryElement,
                Glyphs.Count,
                glyphHash,
                ElementZones.Count,
                elementZoneHash);
        }

        public override string ToString()
        {
            var parts = new List<string> { BaseShape.ToString() };
            if (Sigil != RuneSigil.None)
            {
                parts.Add(Sigil.ToString());
            }

            if (PrimaryElement.HasValue)
            {
                parts.Add(PrimaryElement.Value.ToString());
            }

            IEnumerable<string> glyphParts = Glyphs
                .OrderBy(glyph => glyph.Type)
                .ThenBy(glyph => glyph.Rotation)
                .Select(glyph => glyph.ToString());
            IEnumerable<string> elementParts = ElementZones
                .OrderBy(assignment => assignment.Zone)
                .Select(assignment => assignment.ToString());

            parts.AddRange(glyphParts);
            parts.AddRange(elementParts);
            return string.Join(" | ", parts);
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
