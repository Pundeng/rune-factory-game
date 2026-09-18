using FantasyShapez.Runes;
using NUnit.Framework;

namespace FantasyShapez.Tests.EditMode
{
    public sealed class RuneDataTests
    {
        [Test]
        public void EmptyCircleRunes_WithIdenticalData_CompareEqual()
        {
            var first = new RuneData(RuneBaseShape.Circle);
            var second = new RuneData(RuneBaseShape.Circle);

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
        }

        [Test]
        public void EngraveAttackAtZero_AddsExpectedGlyphWithoutChangingSource()
        {
            var source = new RuneData(RuneBaseShape.Circle);
            var attack = new GlyphData(GlyphType.Attack, GlyphRotation.Degrees0);

            bool succeeded = RuneOperations.TryEngrave(source, attack, out RuneData result);

            Assert.That(succeeded, Is.True);
            Assert.That(result.Glyphs, Is.EquivalentTo(new[] { attack }));
            Assert.That(source.Glyphs, Is.Empty);
        }

        [Test]
        public void AccelerationRunes_UseStructuralGlyphMatching()
        {
            RuneData accelerationAtZero = RuneWithGlyph(
                GlyphType.Acceleration,
                GlyphRotation.Degrees0);
            RuneData identicalTarget = RuneWithGlyph(
                GlyphType.Acceleration,
                GlyphRotation.Degrees0);
            RuneData accelerationAtNinety = RuneWithGlyph(
                GlyphType.Acceleration,
                GlyphRotation.Degrees90);

            Assert.That(accelerationAtZero, Is.EqualTo(identicalTarget));
            Assert.That(accelerationAtZero.GetHashCode(), Is.EqualTo(identicalTarget.GetHashCode()));
            Assert.That(accelerationAtZero, Is.Not.EqualTo(accelerationAtNinety));
            Assert.That(accelerationAtZero,
                Is.Not.EqualTo(RuneWithGlyph(GlyphType.Attack, GlyphRotation.Degrees0)));
            Assert.That(accelerationAtZero,
                Is.Not.EqualTo(RuneWithGlyph(GlyphType.Split, GlyphRotation.Degrees0)));
        }

        [Test]
        public void EngraveAcceleration_PreservesExistingGlyphsAndElements()
        {
            GlyphData attack = new(GlyphType.Attack, GlyphRotation.Degrees90);
            GlyphData split = new(GlyphType.Split, GlyphRotation.Degrees180);
            GlyphData acceleration = new(GlyphType.Acceleration, GlyphRotation.Degrees0);
            ElementZoneAssignment leftFire = new(ElementZone.Left, RuneElement.Fire);
            ElementZoneAssignment rightAir = new(ElementZone.Right, RuneElement.Air);
            var source = new RuneData(
                RuneBaseShape.Circle,
                new[] { attack, split },
                new[] { leftFire, rightAir });

            bool succeeded = RuneOperations.TryEngrave(source, acceleration, out RuneData result);

            Assert.That(succeeded, Is.True);
            Assert.That(result.Glyphs, Is.EquivalentTo(new[] { attack, split, acceleration }));
            Assert.That(result.ElementZones, Is.EquivalentTo(new[] { leftFire, rightAir }));
            Assert.That(source.Glyphs, Is.EquivalentTo(new[] { attack, split }));
        }

        [Test]
        public void RotateAccelerationClockwise_CyclesThroughQuarterTurns()
        {
            GlyphData glyph = new(GlyphType.Acceleration, GlyphRotation.Degrees0);
            RuneData rune = RuneWithGlyph(glyph.Type, glyph.Rotation);
            GlyphRotation[] expectedRotations =
            {
                GlyphRotation.Degrees90,
                GlyphRotation.Degrees180,
                GlyphRotation.Degrees270,
                GlyphRotation.Degrees0
            };

            foreach (GlyphRotation expectedRotation in expectedRotations)
            {
                bool succeeded = RuneOperations.TryRotateGlyphClockwise(
                    rune,
                    glyph,
                    out RuneData rotatedRune);

                glyph = new GlyphData(GlyphType.Acceleration, expectedRotation);
                Assert.That(succeeded, Is.True);
                Assert.That(rotatedRune.Glyphs, Is.EquivalentTo(new[] { glyph }));
                rune = rotatedRune;
            }
        }

        [Test]
        public void RotateGlyphClockwise_CyclesThroughQuarterTurns()
        {
            var glyph = new GlyphData(GlyphType.Attack, GlyphRotation.Degrees0);
            RuneOperations.TryEngrave(new RuneData(RuneBaseShape.Circle), glyph, out RuneData rune);

            GlyphRotation[] expectedRotations =
            {
                GlyphRotation.Degrees90,
                GlyphRotation.Degrees180,
                GlyphRotation.Degrees270,
                GlyphRotation.Degrees0
            };

            foreach (GlyphRotation expectedRotation in expectedRotations)
            {
                bool succeeded = RuneOperations.TryRotateGlyphClockwise(rune, glyph, out RuneData rotatedRune);

                Assert.That(succeeded, Is.True);
                glyph = new GlyphData(GlyphType.Attack, expectedRotation);
                Assert.That(rotatedRune.Glyphs, Is.EquivalentTo(new[] { glyph }));
                rune = rotatedRune;
            }
        }

        [Test]
        public void GlyphRotation_IsPartOfRuneEquality()
        {
            var attackAtZero = new RuneData(
                RuneBaseShape.Circle,
                new[] { new GlyphData(GlyphType.Attack, GlyphRotation.Degrees0) },
                null);
            var attackAtNinety = new RuneData(
                RuneBaseShape.Circle,
                new[] { new GlyphData(GlyphType.Attack, GlyphRotation.Degrees90) },
                null);

            Assert.That(attackAtZero, Is.Not.EqualTo(attackAtNinety));
        }

        [Test]
        public void ElementZone_IsPartOfRuneEquality()
        {
            var leftFire = new RuneData(
                RuneBaseShape.Circle,
                null,
                new[] { new ElementZoneAssignment(ElementZone.Left, RuneElement.Fire) });
            var rightFire = new RuneData(
                RuneBaseShape.Circle,
                null,
                new[] { new ElementZoneAssignment(ElementZone.Right, RuneElement.Fire) });

            Assert.That(leftFire, Is.Not.EqualTo(rightFire));
        }

        [Test]
        public void ElementAssignmentInsertionOrder_DoesNotAffectEquality()
        {
            ElementZoneAssignment leftFire = new(ElementZone.Left, RuneElement.Fire);
            ElementZoneAssignment rightAir = new(ElementZone.Right, RuneElement.Air);
            var first = new RuneData(RuneBaseShape.Circle, null, new[] { leftFire, rightAir });
            var second = new RuneData(RuneBaseShape.Circle, null, new[] { rightAir, leftFire });

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
        }

        [Test]
        public void GlyphInsertionOrder_DoesNotAffectEquality()
        {
            GlyphData attack = new(GlyphType.Attack, GlyphRotation.Degrees0);
            GlyphData split = new(GlyphType.Split, GlyphRotation.Degrees90);
            var first = new RuneData(RuneBaseShape.Circle, new[] { attack, split }, null);
            var second = new RuneData(RuneBaseShape.Circle, new[] { split, attack }, null);

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
        }

        [Test]
        public void ModifyingCopiedRune_DoesNotChangeOriginalOrCopy()
        {
            GlyphData attack = new(GlyphType.Attack, GlyphRotation.Degrees0);
            RuneOperations.TryEngrave(
                new RuneData(RuneBaseShape.Circle),
                attack,
                out RuneData original);
            RuneData copy = original.Copy();

            RuneOperations.TryAssignElement(copy, ElementZone.Left, RuneElement.Fire, out RuneData modifiedCopy);

            Assert.That(copy, Is.Not.SameAs(original));
            Assert.That(original, Is.EqualTo(copy));
            Assert.That(original.ElementZones, Is.Empty);
            Assert.That(copy.ElementZones, Is.Empty);
            Assert.That(modifiedCopy.ElementZones, Has.Count.EqualTo(1));
        }

        [Test]
        public void InvalidOperations_ReturnFalseAndPreserveSource()
        {
            GlyphData attack = new(GlyphType.Attack, GlyphRotation.Degrees0);
            RuneOperations.TryEngrave(
                new RuneData(RuneBaseShape.Circle),
                attack,
                out RuneData engraved);
            RuneOperations.TryAssignElement(
                engraved,
                ElementZone.Left,
                RuneElement.Fire,
                out RuneData infused);

            bool duplicateEngrave = RuneOperations.TryEngrave(infused, attack, out RuneData duplicateResult);
            bool occupiedZone = RuneOperations.TryAssignElement(
                infused,
                ElementZone.Left,
                RuneElement.Air,
                out RuneData occupiedZoneResult);
            bool missingGlyph = RuneOperations.TryRotateGlyphClockwise(
                infused,
                new GlyphData(GlyphType.Split, GlyphRotation.Degrees0),
                out RuneData missingGlyphResult);

            Assert.That(duplicateEngrave, Is.False);
            Assert.That(occupiedZone, Is.False);
            Assert.That(missingGlyph, Is.False);
            Assert.That(duplicateResult, Is.SameAs(infused));
            Assert.That(occupiedZoneResult, Is.SameAs(infused));
            Assert.That(missingGlyphResult, Is.SameAs(infused));
        }

        [Test]
        public void DebugString_IsDeterministicAndHumanReadable()
        {
            var rune = new RuneData(
                RuneBaseShape.Circle,
                new[]
                {
                    new GlyphData(GlyphType.Split, GlyphRotation.Degrees90),
                    new GlyphData(GlyphType.Attack, GlyphRotation.Degrees0)
                },
                new[]
                {
                    new ElementZoneAssignment(ElementZone.Right, RuneElement.Air),
                    new ElementZoneAssignment(ElementZone.Left, RuneElement.Fire)
                });

            Assert.That(rune.ToString(), Is.EqualTo("Circle | Attack@0 | Split@90 | Left:Fire | Right:Air"));
        }

        private static RuneData RuneWithGlyph(GlyphType type, GlyphRotation rotation)
        {
            return new RuneData(
                RuneBaseShape.Circle,
                new[] { new GlyphData(type, rotation) },
                null);
        }
    }
}
