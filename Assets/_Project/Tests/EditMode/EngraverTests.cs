using FantasyShapez.Logistics;
using FantasyShapez.Production;
using FantasyShapez.Runes;
using NUnit.Framework;
using UnityEngine;

namespace FantasyShapez.Tests.EditMode
{
    public sealed class EngraverTests
    {
        [Test]
        public void IdleEngraver_AcceptsRuneFromInputSide()
        {
            EngraverProcess engraver = CreateEngraver();
            RuneData rune = CreateRune();

            bool accepted = engraver.TryAcceptInput(rune, GridDirection.East);

            Assert.That(accepted, Is.True);
            Assert.That(engraver.HeldRune, Is.SameAs(rune));
            Assert.That(engraver.State, Is.EqualTo(EngraverState.Processing));
        }

        [Test]
        public void OccupiedEngraver_RejectsSecondRune()
        {
            EngraverProcess engraver = CreateEngraver();
            RuneData firstRune = CreateRune();
            RuneData secondRune = CreateRune();
            engraver.TryAcceptInput(firstRune, GridDirection.East);

            bool accepted = engraver.TryAcceptInput(secondRune, GridDirection.East);

            Assert.That(accepted, Is.False);
            Assert.That(engraver.HeldRune, Is.SameAs(firstRune));
        }

        [Test]
        public void AttackEngraving_PreservesShapeAndAddsZeroRotationGlyph()
        {
            EngraverProcess engraver = CreateEngraver(GlyphType.Attack);
            RuneData source = CreateRune();
            engraver.TryAcceptInput(source, GridDirection.East);

            engraver.Advance(1f);

            Assert.That(engraver.HeldRune.BaseShape, Is.EqualTo(RuneBaseShape.Circle));
            Assert.That(
                engraver.HeldRune.Glyphs,
                Is.EquivalentTo(new[] { new GlyphData(GlyphType.Attack, GlyphRotation.Degrees0) }));
            Assert.That(source.Glyphs, Is.Empty);
        }

        [Test]
        public void SplitEngraving_AddsZeroRotationGlyph()
        {
            EngraverProcess engraver = CreateEngraver(GlyphType.Split);
            engraver.TryAcceptInput(CreateRune(), GridDirection.East);

            engraver.Advance(1f);

            Assert.That(
                engraver.HeldRune.Glyphs,
                Is.EquivalentTo(new[] { new GlyphData(GlyphType.Split, GlyphRotation.Degrees0) }));
        }

        [Test]
        public void AccelerationEngraving_AddsZeroRotationGlyphThroughStandardConfiguration()
        {
            EngraverProcess engraver = CreateEngraver(GlyphType.Acceleration);
            engraver.TryAcceptInput(CreateRune(), GridDirection.East);

            engraver.Advance(1f);

            Assert.That(
                engraver.HeldRune.Glyphs,
                Is.EquivalentTo(new[]
                {
                    new GlyphData(GlyphType.Acceleration, GlyphRotation.Degrees0)
                }));
            Assert.That(engraver.HeldRune.ToString(), Is.EqualTo("Circle | Acceleration@0"));
        }

        [Test]
        public void AccelerationRune_ContributesToUpgradeAndIsConsumed()
        {
            EngraverProcess engraver = CreateEngraver(requiredAccelerationRunes: 3);

            bool accepted = engraver.TryAcceptInput(
                CreateAccelerationRune(),
                GridDirection.East);

            Assert.That(accepted, Is.True);
            Assert.That(engraver.AccelerationUpgradeProgress, Is.EqualTo(1));
            Assert.That(engraver.IsAccelerationUpgraded, Is.False);
            Assert.That(engraver.HeldRune, Is.Null);
            Assert.That(engraver.State, Is.EqualTo(EngraverState.Idle));
        }

        [Test]
        public void NonAccelerationRune_DoesNotContributeToUpgrade()
        {
            EngraverProcess engraver = CreateEngraver(requiredAccelerationRunes: 1);
            RuneData normalRune = CreateGlyphRune(GlyphType.Attack);

            engraver.TryAcceptInput(normalRune, GridDirection.East);

            Assert.That(engraver.AccelerationUpgradeProgress, Is.Zero);
            Assert.That(engraver.IsAccelerationUpgraded, Is.False);
            Assert.That(engraver.HeldRune, Is.SameAs(normalRune));
            Assert.That(engraver.State, Is.EqualTo(EngraverState.Processing));
        }

        [Test]
        public void AccelerationUpgrade_ActivatesOnlyWhenRequirementIsMet()
        {
            EngraverProcess engraver = CreateEngraver(requiredAccelerationRunes: 3);

            engraver.TryAcceptInput(CreateAccelerationRune(), GridDirection.East);
            engraver.TryAcceptInput(CreateAccelerationRune(), GridDirection.East);

            Assert.That(engraver.AccelerationUpgradeProgress, Is.EqualTo(2));
            Assert.That(engraver.IsAccelerationUpgraded, Is.False);

            engraver.TryAcceptInput(CreateAccelerationRune(), GridDirection.East);

            Assert.That(engraver.IsAccelerationUpgraded, Is.True);
            Assert.That(engraver.AccelerationUpgradeProgress, Is.Zero);
        }

        [Test]
        public void AccelerationUpgrade_ReducesActualProcessingDuration()
        {
            EngraverProcess engraver = CreateEngraver(
                processingDuration: 4f,
                requiredAccelerationRunes: 1,
                upgradedSpeedMultiplier: 2f);
            engraver.TryAcceptInput(CreateAccelerationRune(), GridDirection.East);
            engraver.TryAcceptInput(CreateRune(), GridDirection.East);

            bool completedEarly = engraver.Advance(1f);
            bool completedAtUpgradedDuration = engraver.Advance(1f);

            Assert.That(engraver.EffectiveProcessingDuration, Is.EqualTo(2f));
            Assert.That(completedEarly, Is.False);
            Assert.That(completedAtUpgradedDuration, Is.True);
        }

        [Test]
        public void CompletedAccelerationUpgrade_DoesNotConsumeAnotherUpgradeRune()
        {
            EngraverProcess engraver = CreateEngraver(requiredAccelerationRunes: 1);
            engraver.TryAcceptInput(CreateAccelerationRune(), GridDirection.East);
            RuneData additionalAccelerationRune = CreateAccelerationRune();

            engraver.TryAcceptInput(additionalAccelerationRune, GridDirection.East);

            Assert.That(engraver.IsAccelerationUpgraded, Is.True);
            Assert.That(engraver.AccelerationUpgradeProgress, Is.Zero);
            Assert.That(engraver.HeldRune, Is.SameAs(additionalAccelerationRune));
            Assert.That(engraver.State, Is.EqualTo(EngraverState.Processing));
        }

        [Test]
        public void InspectorUpgradeConfiguration_IsSerializedAndTunable()
        {
            var gameObject = new GameObject("Engraver Upgrade Configuration Test");
            Engraver engraver = gameObject.AddComponent<Engraver>();

            JsonUtility.FromJsonOverwrite(
                "{\"requiredAccelerationRunes\":3,\"upgradedSpeedMultiplier\":4.0}",
                engraver);
            string serializedEngraver = JsonUtility.ToJson(engraver);

            Assert.That(serializedEngraver, Does.Contain("\"requiredAccelerationRunes\":3"));
            Assert.That(serializedEngraver, Does.Contain("\"upgradedSpeedMultiplier\":4.0"));

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void Engraving_PreservesExistingElementAssignments()
        {
            var fireLeft = new ElementZoneAssignment(ElementZone.Left, RuneElement.Fire);
            var source = new RuneData(
                RuneBaseShape.Circle,
                System.Array.Empty<GlyphData>(),
                new[] { fireLeft });
            EngraverProcess engraver = CreateEngraver();
            engraver.TryAcceptInput(source, GridDirection.East);

            engraver.Advance(1f);

            Assert.That(engraver.HeldRune.ElementZones, Is.EquivalentTo(new[] { fireLeft }));
        }

        [Test]
        public void CompletedEngraver_HoldsOutputWhenReceivingBeltIsBlocked()
        {
            var system = new BeltTransportSystem(1f);
            EngraverProcess engraver = CreateEngraver();
            system.RegisterOutputSource(engraver);
            BeltCell blockedOutputBelt = system.AddBelt(Vector2Int.right, GridDirection.East);
            RuneData blockingRune = CreateRune();
            blockedOutputBelt.TryAccept(blockingRune, GridDirection.East);
            RuneData source = CreateRune();
            engraver.TryAcceptInput(source, GridDirection.East);
            engraver.Advance(1f);

            system.Advance(1f);

            Assert.That(engraver.State, Is.EqualTo(EngraverState.WaitingForOutput));
            Assert.That(engraver.HeldRune, Is.Not.Null);
            Assert.That(engraver.CanAcceptInput, Is.False);
            Assert.That(blockedOutputBelt.Item.Rune, Is.SameAs(blockingRune));
        }

        [Test]
        public void Processing_WaitsForConfiguredDuration()
        {
            EngraverProcess engraver = CreateEngraver();
            RuneData source = CreateRune();
            engraver.TryAcceptInput(source, GridDirection.East);

            bool completedEarly = engraver.Advance(0.5f);

            Assert.That(completedEarly, Is.False);
            Assert.That(engraver.State, Is.EqualTo(EngraverState.Processing));
            Assert.That(engraver.HeldRune, Is.SameAs(source));

            bool completedOnTime = engraver.Advance(0.5f);

            Assert.That(completedOnTime, Is.True);
            Assert.That(engraver.State, Is.EqualTo(EngraverState.WaitingForOutput));
        }

        [Test]
        public void CompletedEngraver_ReleasesOutputWhenBeltBecomesAvailable()
        {
            var system = new BeltTransportSystem(1f);
            EngraverProcess engraver = CreateEngraver();
            system.RegisterOutputSource(engraver);
            engraver.TryAcceptInput(CreateRune(), GridDirection.East);
            engraver.Advance(1f);
            BeltCell outputBelt = system.AddBelt(Vector2Int.right, GridDirection.East);
            RuneData completedRune = engraver.HeldRune;

            system.Advance(0f);

            Assert.That(outputBelt.Item.Rune, Is.SameAs(completedRune));
            Assert.That(engraver.State, Is.EqualTo(EngraverState.Idle));
            Assert.That(engraver.HeldRune, Is.Null);
        }

        [Test]
        public void AfterOutputLeaves_EngraverAcceptsAnotherRune()
        {
            var system = new BeltTransportSystem(1f);
            EngraverProcess engraver = CreateEngraver();
            system.RegisterOutputSource(engraver);
            system.AddBelt(Vector2Int.right, GridDirection.East);
            engraver.TryAcceptInput(CreateRune(), GridDirection.East);
            engraver.Advance(1f);
            system.Advance(0f);

            bool accepted = engraver.TryAcceptInput(CreateRune(), GridDirection.East);

            Assert.That(accepted, Is.True);
        }

        [Test]
        public void BeltFromInvalidSide_CannotFeedEngraver()
        {
            var system = new BeltTransportSystem(1f);
            EngraverProcess engraver = CreateEngraver();
            system.RegisterInputReceiver(engraver);
            BeltCell invalidInputBelt = system.AddBelt(Vector2Int.down, GridDirection.North);
            RuneData rune = CreateRune();
            invalidInputBelt.TryAccept(rune, GridDirection.North);

            system.Advance(1f);

            Assert.That(invalidInputBelt.Item.Rune, Is.SameAs(rune));
            Assert.That(engraver.State, Is.EqualTo(EngraverState.Idle));
        }

        [Test]
        public void FullTransportCycle_PreservesOwnershipAndOnlyAddsConfiguredGlyph()
        {
            var system = new BeltTransportSystem(1f);
            EngraverProcess engraver = CreateEngraver();
            system.RegisterInputReceiver(engraver);
            system.RegisterOutputSource(engraver);
            BeltCell inputBelt = system.AddBelt(Vector2Int.left, GridDirection.East);
            BeltCell outputBelt = system.AddBelt(Vector2Int.right, GridDirection.East);
            RuneData source = CreateRune();
            inputBelt.TryAccept(source, GridDirection.East);

            system.Advance(1f);
            Assert.That(inputBelt.HasItem, Is.False);
            Assert.That(engraver.HeldRune, Is.SameAs(source));

            engraver.Advance(1f);
            RuneData engravedRune = engraver.HeldRune;
            system.Advance(0f);

            Assert.That(outputBelt.Item.Rune, Is.SameAs(engravedRune));
            Assert.That(engraver.HeldRune, Is.Null);
            Assert.That(source.Glyphs, Is.Empty);
            Assert.That(engravedRune.Glyphs.Count, Is.EqualTo(1));
        }

        [Test]
        public void DuplicateEngraving_PassesOriginalRuneThroughUnchanged()
        {
            var attack = new GlyphData(GlyphType.Attack, GlyphRotation.Degrees0);
            var source = new RuneData(
                RuneBaseShape.Circle,
                new[] { attack },
                System.Array.Empty<ElementZoneAssignment>());
            EngraverProcess engraver = CreateEngraver();
            engraver.TryAcceptInput(source, GridDirection.East);

            engraver.Advance(1f);

            Assert.That(engraver.LastEngravingSucceeded, Is.False);
            Assert.That(engraver.HeldRune, Is.SameAs(source));
            Assert.That(engraver.State, Is.EqualTo(EngraverState.WaitingForOutput));
        }

        [Test]
        public void RuntimeConfigurationChange_AffectsNextRuneWithoutChangingCurrentRune()
        {
            EngraverProcess engraver = CreateEngraver(GlyphType.Attack);
            engraver.TryAcceptInput(CreateRune(), GridDirection.East);
            engraver.Advance(0.5f);

            engraver.Configure(GlyphType.Split, 2f);
            engraver.Advance(0.5f);

            Assert.That(
                engraver.HeldRune.Glyphs,
                Is.EquivalentTo(new[]
                {
                    new GlyphData(GlyphType.Attack, GlyphRotation.Degrees0)
                }));

            engraver.TryTakeOutput(out _);
            engraver.TryAcceptInput(CreateRune(), GridDirection.East);

            Assert.That(engraver.Advance(1f), Is.False);
            Assert.That(engraver.Advance(1f), Is.True);
            Assert.That(
                engraver.HeldRune.Glyphs,
                Is.EquivalentTo(new[]
                {
                    new GlyphData(GlyphType.Split, GlyphRotation.Degrees0)
                }));
        }

        [Test]
        public void DiscardContents_WhileProcessingClearsHeldRune()
        {
            EngraverProcess engraver = CreateEngraver();
            engraver.TryAcceptInput(CreateRune(), GridDirection.East);

            engraver.DiscardContents();

            Assert.That(engraver.HeldRune, Is.Null);
            Assert.That(engraver.State, Is.EqualTo(EngraverState.Idle));
            Assert.That(engraver.CanAcceptInput, Is.True);
        }

        [Test]
        public void DiscardContents_WhileWaitingForOutputClearsCompletedRune()
        {
            EngraverProcess engraver = CreateEngraver();
            engraver.TryAcceptInput(CreateRune(), GridDirection.East);
            engraver.Advance(1f);

            engraver.DiscardContents();

            Assert.That(engraver.HeldRune, Is.Null);
            Assert.That(engraver.HasOutput, Is.False);
            Assert.That(engraver.State, Is.EqualTo(EngraverState.Idle));
        }

        private static EngraverProcess CreateEngraver(
            GlyphType glyphType = GlyphType.Attack,
            float processingDuration = 1f,
            int requiredAccelerationRunes = 100,
            float upgradedSpeedMultiplier = 2f)
        {
            return new EngraverProcess(
                Vector2Int.zero,
                GridDirection.East,
                glyphType,
                processingDuration,
                requiredAccelerationRunes,
                upgradedSpeedMultiplier);
        }

        private static RuneData CreateRune()
        {
            return new RuneData(RuneBaseShape.Circle);
        }

        private static RuneData CreateAccelerationRune()
        {
            return CreateGlyphRune(GlyphType.Acceleration);
        }

        private static RuneData CreateGlyphRune(GlyphType glyphType)
        {
            return new RuneData(
                RuneBaseShape.Circle,
                new[]
                {
                    new GlyphData(glyphType, GlyphRotation.Degrees0)
                },
                null);
        }
    }
}
