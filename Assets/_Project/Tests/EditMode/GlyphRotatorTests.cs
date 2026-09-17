using FantasyShapez.Logistics;
using FantasyShapez.Production;
using FantasyShapez.Runes;
using NUnit.Framework;
using UnityEngine;

namespace FantasyShapez.Tests.EditMode
{
    public sealed class GlyphRotatorTests
    {
        [Test]
        public void IdleRotator_AcceptsRuneFromInputSide()
        {
            GlyphRotatorProcess rotator = CreateRotator();
            RuneData rune = CreateRune(GlyphRotation.Degrees0);

            bool accepted = rotator.TryAcceptInput(rune, GridDirection.East);

            Assert.That(accepted, Is.True);
            Assert.That(rotator.HeldRune, Is.SameAs(rune));
            Assert.That(rotator.State, Is.EqualTo(GlyphRotatorState.Processing));
        }

        [Test]
        public void OccupiedRotator_RejectsSecondRune()
        {
            GlyphRotatorProcess rotator = CreateRotator();
            RuneData firstRune = CreateRune(GlyphRotation.Degrees0);
            RuneData secondRune = CreateRune(GlyphRotation.Degrees0);
            rotator.TryAcceptInput(firstRune, GridDirection.East);

            bool accepted = rotator.TryAcceptInput(secondRune, GridDirection.East);

            Assert.That(accepted, Is.False);
            Assert.That(rotator.HeldRune, Is.SameAs(firstRune));
        }

        [TestCase(GlyphRotation.Degrees0, GlyphRotation.Degrees90)]
        [TestCase(GlyphRotation.Degrees90, GlyphRotation.Degrees180)]
        [TestCase(GlyphRotation.Degrees180, GlyphRotation.Degrees270)]
        [TestCase(GlyphRotation.Degrees270, GlyphRotation.Degrees0)]
        public void SplitGlyph_RotatesClockwise(
            GlyphRotation initialRotation,
            GlyphRotation expectedRotation)
        {
            GlyphRotatorProcess rotator = CreateRotator();
            rotator.TryAcceptInput(CreateRune(initialRotation), GridDirection.East);

            rotator.Advance(1f);

            Assert.That(
                rotator.HeldRune.Glyphs,
                Does.Contain(new GlyphData(GlyphType.Split, expectedRotation)));
        }

        [Test]
        public void SplitRotation_PreservesAttackGlyph()
        {
            GlyphData attack = new(GlyphType.Attack, GlyphRotation.Degrees180);
            GlyphData split = new(GlyphType.Split, GlyphRotation.Degrees0);
            var source = new RuneData(RuneBaseShape.Circle, new[] { attack, split }, null);
            GlyphRotatorProcess rotator = CreateRotator();
            rotator.TryAcceptInput(source, GridDirection.East);

            rotator.Advance(1f);

            Assert.That(rotator.HeldRune.Glyphs, Does.Contain(attack));
        }

        [Test]
        public void Rotation_PreservesBaseShape()
        {
            GlyphRotatorProcess rotator = CreateRotator();
            rotator.TryAcceptInput(CreateRune(GlyphRotation.Degrees0), GridDirection.East);

            rotator.Advance(1f);

            Assert.That(rotator.HeldRune.BaseShape, Is.EqualTo(RuneBaseShape.Circle));
        }

        [Test]
        public void Rotation_PreservesElementAssignments()
        {
            ElementZoneAssignment fireLeft = new(ElementZone.Left, RuneElement.Fire);
            var source = new RuneData(
                RuneBaseShape.Circle,
                new[] { new GlyphData(GlyphType.Split, GlyphRotation.Degrees0) },
                new[] { fireLeft });
            GlyphRotatorProcess rotator = CreateRotator();
            rotator.TryAcceptInput(source, GridDirection.East);

            rotator.Advance(1f);

            Assert.That(rotator.HeldRune.ElementZones, Is.EquivalentTo(new[] { fireLeft }));
        }

        [Test]
        public void MissingConfiguredGlyph_PassesOriginalRuneThroughUnchanged()
        {
            var source = new RuneData(
                RuneBaseShape.Circle,
                new[] { new GlyphData(GlyphType.Attack, GlyphRotation.Degrees0) },
                null);
            GlyphRotatorProcess rotator = CreateRotator();
            rotator.TryAcceptInput(source, GridDirection.East);

            rotator.Advance(1f);

            Assert.That(rotator.LastRotationSucceeded, Is.False);
            Assert.That(rotator.HeldRune, Is.SameAs(source));
            Assert.That(rotator.State, Is.EqualTo(GlyphRotatorState.WaitingForOutput));
        }

        [Test]
        public void CompletedRotator_HoldsOutputWhenReceivingBeltIsBlocked()
        {
            var system = new BeltTransportSystem(1f);
            GlyphRotatorProcess rotator = CreateRotator();
            system.RegisterOutputSource(rotator);
            BeltCell outputBelt = system.AddBelt(Vector2Int.right, GridDirection.East);
            RuneData blockingRune = CreateRune(GlyphRotation.Degrees0);
            outputBelt.TryAccept(blockingRune, GridDirection.East);
            rotator.TryAcceptInput(CreateRune(GlyphRotation.Degrees0), GridDirection.East);
            rotator.Advance(1f);

            system.Advance(1f);

            Assert.That(rotator.State, Is.EqualTo(GlyphRotatorState.WaitingForOutput));
            Assert.That(rotator.HeldRune, Is.Not.Null);
            Assert.That(rotator.CanAcceptInput, Is.False);
            Assert.That(outputBelt.Item.Rune, Is.SameAs(blockingRune));
        }

        [Test]
        public void CompletedRotator_TransfersRuneWhenOutputBeltIsAvailable()
        {
            var system = new BeltTransportSystem(1f);
            GlyphRotatorProcess rotator = CreateRotator();
            system.RegisterOutputSource(rotator);
            BeltCell outputBelt = system.AddBelt(Vector2Int.right, GridDirection.East);
            rotator.TryAcceptInput(CreateRune(GlyphRotation.Degrees0), GridDirection.East);
            rotator.Advance(1f);
            RuneData completedRune = rotator.HeldRune;

            system.Advance(0f);

            Assert.That(outputBelt.Item.Rune, Is.SameAs(completedRune));
        }

        [Test]
        public void SuccessfulOutput_ReturnsRotatorToIdle()
        {
            var system = new BeltTransportSystem(1f);
            GlyphRotatorProcess rotator = CreateRotator();
            system.RegisterOutputSource(rotator);
            system.AddBelt(Vector2Int.right, GridDirection.East);
            rotator.TryAcceptInput(CreateRune(GlyphRotation.Degrees0), GridDirection.East);
            rotator.Advance(1f);

            system.Advance(0f);

            Assert.That(rotator.State, Is.EqualTo(GlyphRotatorState.Idle));
            Assert.That(rotator.HeldRune, Is.Null);
        }

        [Test]
        public void BeltFromInvalidSide_CannotFeedRotator()
        {
            var system = new BeltTransportSystem(1f);
            GlyphRotatorProcess rotator = CreateRotator();
            system.RegisterInputReceiver(rotator);
            BeltCell invalidInputBelt = system.AddBelt(Vector2Int.down, GridDirection.North);
            RuneData rune = CreateRune(GlyphRotation.Degrees0);
            invalidInputBelt.TryAccept(rune, GridDirection.North);

            system.Advance(1f);

            Assert.That(invalidInputBelt.Item.Rune, Is.SameAs(rune));
            Assert.That(rotator.State, Is.EqualTo(GlyphRotatorState.Idle));
        }

        [Test]
        public void Processing_WaitsForConfiguredDuration()
        {
            GlyphRotatorProcess rotator = CreateRotator();
            RuneData source = CreateRune(GlyphRotation.Degrees0);
            rotator.TryAcceptInput(source, GridDirection.East);

            bool completedEarly = rotator.Advance(0.5f);
            bool completedOnTime = rotator.Advance(0.5f);

            Assert.That(completedEarly, Is.False);
            Assert.That(completedOnTime, Is.True);
            Assert.That(rotator.State, Is.EqualTo(GlyphRotatorState.WaitingForOutput));
        }

        private static GlyphRotatorProcess CreateRotator()
        {
            return new GlyphRotatorProcess(
                Vector2Int.zero,
                GridDirection.East,
                GlyphType.Split,
                1f);
        }

        private static RuneData CreateRune(GlyphRotation splitRotation)
        {
            return new RuneData(
                RuneBaseShape.Circle,
                new[] { new GlyphData(GlyphType.Split, splitRotation) },
                null);
        }
    }
}
