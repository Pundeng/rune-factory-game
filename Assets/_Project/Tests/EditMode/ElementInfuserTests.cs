using FantasyShapez.Logistics;
using FantasyShapez.Production;
using FantasyShapez.Runes;
using NUnit.Framework;
using UnityEngine;

namespace FantasyShapez.Tests.EditMode
{
    public sealed class ElementInfuserTests
    {
        [TestCase(ElementZone.Left, RuneElement.Fire)]
        [TestCase(ElementZone.Right, RuneElement.Air)]
        [TestCase(ElementZone.Left, RuneElement.Water)]
        [TestCase(ElementZone.Right, RuneElement.Earth)]
        public void Infusion_AssignsConfiguredElementToConfiguredZone(
            ElementZone zone,
            RuneElement element)
        {
            ElementInfuserProcess infuser = CreateInfuser(element, zone);
            infuser.TryAcceptInput(CreateRune(), GridDirection.East);

            infuser.Advance(1f);

            Assert.That(infuser.HeldRune.TryGetElement(zone, out RuneElement assigned), Is.True);
            Assert.That(assigned, Is.EqualTo(element));
        }

        [Test]
        public void Infusion_PreservesShapeGlyphsRotationsAndUnrelatedElements()
        {
            GlyphData attack = new(GlyphType.Attack, GlyphRotation.Degrees180);
            ElementZoneAssignment rightAir = new(ElementZone.Right, RuneElement.Air);
            var source = new RuneData(RuneBaseShape.Circle, new[] { attack }, new[] { rightAir });
            ElementInfuserProcess infuser = CreateInfuser(RuneElement.Fire, ElementZone.Left);
            infuser.TryAcceptInput(source, GridDirection.East);

            infuser.Advance(1f);

            Assert.That(infuser.HeldRune.BaseShape, Is.EqualTo(RuneBaseShape.Circle));
            Assert.That(infuser.HeldRune.Glyphs, Is.EquivalentTo(new[] { attack }));
            Assert.That(
                infuser.HeldRune.ElementZones,
                Is.EquivalentTo(new[]
                {
                    rightAir,
                    new ElementZoneAssignment(ElementZone.Left, RuneElement.Fire)
                }));
        }

        [Test]
        public void TwoInfusers_RetainBothZoneAssignments()
        {
            ElementInfuserProcess leftInfuser = CreateInfuser(RuneElement.Fire, ElementZone.Left);
            ElementInfuserProcess rightInfuser = CreateInfuser(RuneElement.Air, ElementZone.Right);
            leftInfuser.TryAcceptInput(CreateRune(), GridDirection.East);
            leftInfuser.Advance(1f);
            leftInfuser.TryTakeOutput(out RuneData leftInfusedRune);

            rightInfuser.TryAcceptInput(leftInfusedRune, GridDirection.East);
            rightInfuser.Advance(1f);

            Assert.That(
                rightInfuser.HeldRune.ElementZones,
                Is.EquivalentTo(new[]
                {
                    new ElementZoneAssignment(ElementZone.Left, RuneElement.Fire),
                    new ElementZoneAssignment(ElementZone.Right, RuneElement.Air)
                }));
        }

        [Test]
        public void OccupiedInfuser_RejectsSecondRune()
        {
            ElementInfuserProcess infuser = CreateInfuser();
            RuneData firstRune = CreateRune();
            RuneData secondRune = CreateRune();
            infuser.TryAcceptInput(firstRune, GridDirection.East);

            bool accepted = infuser.TryAcceptInput(secondRune, GridDirection.East);

            Assert.That(accepted, Is.False);
            Assert.That(infuser.HeldRune, Is.SameAs(firstRune));
        }

        [Test]
        public void CompletedInfuser_HoldsOutputWhileBeltIsBlocked()
        {
            var system = new BeltTransportSystem(1f);
            ElementInfuserProcess infuser = CreateInfuser();
            system.RegisterOutputSource(infuser);
            BeltCell outputBelt = system.AddBelt(Vector2Int.right, GridDirection.East);
            RuneData blockingRune = CreateRune();
            outputBelt.TryAccept(blockingRune, GridDirection.East);
            infuser.TryAcceptInput(CreateRune(), GridDirection.East);
            infuser.Advance(1f);
            system.Advance(0f);

            Assert.That(infuser.State, Is.EqualTo(ElementInfuserState.WaitingForOutput));
            Assert.That(infuser.HeldRune, Is.Not.Null);
            Assert.That(outputBelt.Item.Rune, Is.SameAs(blockingRune));
        }

        [Test]
        public void CompletedInfuser_ReleasesOutputWhenBeltBecomesAvailable()
        {
            var system = new BeltTransportSystem(1f);
            ElementInfuserProcess infuser = CreateInfuser();
            system.RegisterOutputSource(infuser);
            infuser.TryAcceptInput(CreateRune(), GridDirection.East);
            infuser.Advance(1f);
            BeltCell outputBelt = system.AddBelt(Vector2Int.right, GridDirection.East);
            RuneData completedRune = infuser.HeldRune;

            system.Advance(0f);

            Assert.That(outputBelt.Item.Rune, Is.SameAs(completedRune));
            Assert.That(infuser.State, Is.EqualTo(ElementInfuserState.Idle));
        }

        [Test]
        public void BeltFromInvalidSide_CannotFeedInfuser()
        {
            var system = new BeltTransportSystem(1f);
            ElementInfuserProcess infuser = CreateInfuser();
            system.RegisterInputReceiver(infuser);
            BeltCell invalidInputBelt = system.AddBelt(Vector2Int.down, GridDirection.North);
            RuneData rune = CreateRune();
            invalidInputBelt.TryAccept(rune, GridDirection.North);

            system.Advance(1f);

            Assert.That(invalidInputBelt.Item.Rune, Is.SameAs(rune));
            Assert.That(infuser.State, Is.EqualTo(ElementInfuserState.Idle));
        }

        [Test]
        public void OccupiedTargetZone_PassesOriginalRuneThroughUnchanged()
        {
            var source = new RuneData(
                RuneBaseShape.Circle,
                new[] { new GlyphData(GlyphType.Split, GlyphRotation.Degrees90) },
                new[] { new ElementZoneAssignment(ElementZone.Left, RuneElement.Water) });
            ElementInfuserProcess infuser = CreateInfuser(RuneElement.Fire, ElementZone.Left);
            infuser.TryAcceptInput(source, GridDirection.East);

            infuser.Advance(1f);

            Assert.That(infuser.LastInfusionSucceeded, Is.False);
            Assert.That(infuser.HeldRune, Is.SameAs(source));
            Assert.That(infuser.HeldRune, Is.EqualTo(source));
            Assert.That(infuser.State, Is.EqualTo(ElementInfuserState.WaitingForOutput));
        }

        [Test]
        public void RuntimeElementAndZoneChange_AffectsNextRuneWithoutChangingCurrentRune()
        {
            ElementInfuserProcess infuser = CreateInfuser(RuneElement.Fire, ElementZone.Left);
            infuser.TryAcceptInput(CreateRune(), GridDirection.East);
            infuser.Advance(0.5f);

            infuser.Configure(RuneElement.Air, ElementZone.Right, 1f);
            infuser.Advance(0.5f);

            Assert.That(
                infuser.HeldRune.ElementZones,
                Is.EquivalentTo(new[]
                {
                    new ElementZoneAssignment(ElementZone.Left, RuneElement.Fire)
                }));

            infuser.TryTakeOutput(out _);
            infuser.TryAcceptInput(CreateRune(), GridDirection.East);
            infuser.Advance(1f);

            Assert.That(
                infuser.HeldRune.ElementZones,
                Is.EquivalentTo(new[]
                {
                    new ElementZoneAssignment(ElementZone.Right, RuneElement.Air)
                }));
        }

        [Test]
        public void Processing_WaitsForConfiguredDuration()
        {
            ElementInfuserProcess infuser = CreateInfuser();
            infuser.TryAcceptInput(CreateRune(), GridDirection.East);

            bool completedEarly = infuser.Advance(0.5f);
            bool completedOnTime = infuser.Advance(0.5f);

            Assert.That(completedEarly, Is.False);
            Assert.That(completedOnTime, Is.True);
            Assert.That(infuser.State, Is.EqualTo(ElementInfuserState.WaitingForOutput));
        }

        [Test]
        public void DiscardContents_WhileOccupiedClearsHeldRune()
        {
            ElementInfuserProcess infuser = CreateInfuser();
            infuser.TryAcceptInput(CreateRune(), GridDirection.East);

            infuser.DiscardContents();

            Assert.That(infuser.HeldRune, Is.Null);
            Assert.That(infuser.HasOutput, Is.False);
            Assert.That(infuser.State, Is.EqualTo(ElementInfuserState.Idle));
        }

        private static ElementInfuserProcess CreateInfuser(
            RuneElement element = RuneElement.Fire,
            ElementZone zone = ElementZone.Left)
        {
            return new ElementInfuserProcess(
                Vector2Int.zero,
                GridDirection.East,
                element,
                zone,
                1f);
        }

        private static RuneData CreateRune()
        {
            return new RuneData(RuneBaseShape.Circle);
        }
    }
}
