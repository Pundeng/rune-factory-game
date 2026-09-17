using System;
using System.Collections.Generic;
using System.Linq;

namespace FantasyShapez.Runes
{
    public static class RuneOperations
    {
        public static bool TryEngrave(RuneData source, GlyphData glyph, out RuneData result)
        {
            EnsureSourceExists(source);
            if (source.Glyphs.Contains(glyph))
            {
                result = source;
                return false;
            }

            var glyphs = new List<GlyphData>(source.Glyphs) { glyph };
            result = new RuneData(source.BaseShape, glyphs, source.ElementZones);
            return true;
        }

        public static bool TryRotateGlyphClockwise(
            RuneData source,
            GlyphData selectedGlyph,
            out RuneData result)
        {
            EnsureSourceExists(source);
            var glyphs = new List<GlyphData>(source.Glyphs);
            int selectedIndex = glyphs.IndexOf(selectedGlyph);
            if (selectedIndex < 0)
            {
                result = source;
                return false;
            }

            GlyphData rotatedGlyph = selectedGlyph.RotateClockwise();
            if (glyphs.Contains(rotatedGlyph))
            {
                result = source;
                return false;
            }

            glyphs[selectedIndex] = rotatedGlyph;
            result = new RuneData(source.BaseShape, glyphs, source.ElementZones);
            return true;
        }

        public static bool TryAssignElement(
            RuneData source,
            ElementZone zone,
            RuneElement element,
            out RuneData result)
        {
            EnsureSourceExists(source);
            if (source.TryGetElement(zone, out _))
            {
                result = source;
                return false;
            }

            var assignments = new List<ElementZoneAssignment>(source.ElementZones)
            {
                new(zone, element)
            };
            result = new RuneData(source.BaseShape, source.Glyphs, assignments);
            return true;
        }

        private static void EnsureSourceExists(RuneData source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
        }
    }
}
