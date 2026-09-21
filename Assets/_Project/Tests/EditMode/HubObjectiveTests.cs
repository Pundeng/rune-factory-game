using FantasyShapez.Logistics;
using FantasyShapez.Objectives;
using FantasyShapez.Production;
using FantasyShapez.Runes;
using NUnit.Framework;
using UnityEngine;

namespace FantasyShapez.Tests.EditMode
{
    public sealed class HubObjectiveTests
    {
        [Test]
        public void MatchingRune_IncrementsProgress()
        {
            ObjectiveProgress progress = CreateProgress(Objective("Circle", EmptyCircle(), 2));

            RuneDeliveryResult result = progress.Deliver(EmptyCircle());

            Assert.That(result, Is.EqualTo(RuneDeliveryResult.Correct));
            Assert.That(progress.CurrentCount, Is.EqualTo(1));
        }

        [Test]
        public void IncorrectRune_DoesNotIncrementProgress()
        {
            ObjectiveProgress progress = CreateProgress(Objective("Attack", AttackAt(0), 2));

            RuneDeliveryResult result = progress.Deliver(EmptyCircle());

            Assert.That(result, Is.EqualTo(RuneDeliveryResult.Incorrect));
            Assert.That(progress.CurrentCount, Is.Zero);
        }

        [Test]
        public void AttackAtZero_DoesNotMatchAttackAtNinety()
        {
            ObjectiveProgress progress = CreateProgress(Objective("Attack", AttackAt(0), 1));

            RuneDeliveryResult result = progress.Deliver(AttackAt(90));

            Assert.That(result, Is.EqualTo(RuneDeliveryResult.Incorrect));
            Assert.That(progress.AreAllObjectivesComplete, Is.False);
        }

        [Test]
        public void EmptyCircle_DoesNotMatchRuneWithGlyph()
        {
            ObjectiveProgress progress = CreateProgress(Objective("Circle", EmptyCircle(), 1));

            RuneDeliveryResult incorrectResult = progress.Deliver(AttackAt(0));
            RuneDeliveryResult correctResult = progress.Deliver(EmptyCircle());

            Assert.That(incorrectResult, Is.EqualTo(RuneDeliveryResult.Incorrect));
            Assert.That(correctResult,
                Is.EqualTo(RuneDeliveryResult.CorrectAndObjectiveCompleted));
        }

        [Test]
        public void AttackAtZero_MatchesAttackAtZero()
        {
            ObjectiveProgress progress = CreateProgress(Objective("Attack", AttackAt(0), 1));

            RuneDeliveryResult result = progress.Deliver(AttackAt(0));

            Assert.That(result, Is.EqualTo(RuneDeliveryResult.CorrectAndObjectiveCompleted));
        }

        [Test]
        public void SplitAtZero_DoesNotMatchAttackAtZero()
        {
            ObjectiveProgress progress = CreateProgress(Objective("Attack", AttackAt(0), 1));

            RuneDeliveryResult result = progress.Deliver(SplitAt(0));

            Assert.That(result, Is.EqualTo(RuneDeliveryResult.Incorrect));
            Assert.That(progress.CurrentCount, Is.Zero);
        }

        [Test]
        public void PartialElementAssignment_DoesNotMatchCompleteElementTarget()
        {
            ObjectiveProgress progress = CreateProgress(Objective(
                "Fire Air",
                ElementRune(
                    new ElementZoneAssignment(ElementZone.Left, RuneElement.Fire),
                    new ElementZoneAssignment(ElementZone.Right, RuneElement.Air)),
                1));

            RuneDeliveryResult result = progress.Deliver(ElementRune(
                new ElementZoneAssignment(ElementZone.Left, RuneElement.Fire)));

            Assert.That(result, Is.EqualTo(RuneDeliveryResult.Incorrect));
            Assert.That(progress.CurrentCount, Is.Zero);
        }

        [Test]
        public void CompleteElementAssignment_MatchesCorrespondingTarget()
        {
            RuneData target = ElementRune(
                new ElementZoneAssignment(ElementZone.Left, RuneElement.Fire),
                new ElementZoneAssignment(ElementZone.Right, RuneElement.Air));
            ObjectiveProgress progress = CreateProgress(Objective("Fire Air", target, 1));

            RuneDeliveryResult result = progress.Deliver(target.Copy());

            Assert.That(result, Is.EqualTo(RuneDeliveryResult.CorrectAndObjectiveCompleted));
        }

        [Test]
        public void SplitAtNinety_MatchesSplitAtNinetyTarget()
        {
            ObjectiveProgress progress = CreateProgress(Objective("Split 90", SplitAt(90), 2));

            RuneDeliveryResult result = progress.Deliver(SplitAt(90));

            Assert.That(result, Is.EqualTo(RuneDeliveryResult.Correct));
            Assert.That(progress.CurrentCount, Is.EqualTo(1));
        }

        [Test]
        public void RequiredCount_CompletesObjective()
        {
            ObjectiveProgress progress = CreateProgress(Objective("Circle", EmptyCircle(), 2));
            progress.Deliver(EmptyCircle());

            RuneDeliveryResult result = progress.Deliver(EmptyCircle());

            Assert.That(result, Is.EqualTo(RuneDeliveryResult.CorrectAndObjectiveCompleted));
            Assert.That(progress.AreAllObjectivesComplete, Is.True);
        }

        [Test]
        public void ObjectiveCompletion_AdvancesToNextObjective()
        {
            ObjectiveProgress progress = CreateProgress(
                Objective("Circle", EmptyCircle(), 1),
                Objective("Attack", AttackAt(0), 1));

            progress.Deliver(EmptyCircle());

            Assert.That(progress.CurrentObjectiveIndex, Is.EqualTo(1));
            Assert.That(progress.CurrentObjective.DisplayName, Is.EqualTo("Attack"));
        }

        [Test]
        public void NewObjective_BeginsWithZeroProgress()
        {
            ObjectiveProgress progress = CreateProgress(
                Objective("Circle", EmptyCircle(), 1),
                Objective("Attack", AttackAt(0), 2));

            progress.Deliver(EmptyCircle());

            Assert.That(progress.CurrentCount, Is.Zero);
        }

        [Test]
        public void CompletingFinalObjective_EntersCompletedState()
        {
            ObjectiveProgress progress = CreateProgress(Objective("Circle", EmptyCircle(), 1));

            progress.Deliver(EmptyCircle());

            Assert.That(progress.AreAllObjectivesComplete, Is.True);
            Assert.That(progress.CurrentObjective, Is.Null);
        }

        [Test]
        public void ExtraDelivery_DoesNotCorruptCompletedState()
        {
            ObjectiveProgress progress = CreateProgress(Objective("Circle", EmptyCircle(), 1));
            progress.Deliver(EmptyCircle());

            RuneDeliveryResult result = progress.Deliver(EmptyCircle());

            Assert.That(result, Is.EqualTo(RuneDeliveryResult.AllObjectivesAlreadyComplete));
            Assert.That(progress.AreAllObjectivesComplete, Is.True);
            Assert.That(progress.CurrentCount, Is.Zero);
        }

        [Test]
        public void Delivery_DoesNotMutateTargetRune()
        {
            RuneData target = AttackAt(0);
            ObjectiveProgress progress = CreateProgress(Objective("Attack", target, 2));

            progress.Deliver(AttackAt(0));

            Assert.That(progress.CurrentObjective.TargetRune, Is.EqualTo(target));
            Assert.That(progress.CurrentObjective.TargetRune.Glyphs, Has.Count.EqualTo(1));
        }

        [Test]
        public void ObjectiveDefinition_CopiesTargetRuneData()
        {
            RuneData source = SplitAt(90);

            ObjectiveDefinition definition = Objective("Split 90", source, 1);
            ObjectiveProgress progress = CreateProgress(definition);

            Assert.That(definition.TargetRune, Is.Not.SameAs(source));
            Assert.That(progress.CurrentObjective.TargetRune, Is.Not.SameAs(definition.TargetRune));
            Assert.That(progress.CurrentObjective.TargetRune, Is.EqualTo(source));
        }

        [Test]
        public void ObjectiveAsset_CreatesIndependentRuntimeDefinition()
        {
            ObjectiveDefinitionAsset asset = ScriptableObject.CreateInstance<ObjectiveDefinitionAsset>();
            RuneData target = SplitAt(90);
            asset.Configure("Editable Split", target, 7);

            ObjectiveDefinition first = asset.CreateRuntimeDefinition();
            ObjectiveDefinition second = asset.CreateRuntimeDefinition();

            Assert.That(first.DisplayName, Is.EqualTo("Editable Split"));
            Assert.That(first.RequiredCount, Is.EqualTo(7));
            Assert.That(first.TargetRune, Is.EqualTo(target));
            Assert.That(first.TargetRune, Is.Not.SameAs(target));
            Assert.That(second.TargetRune, Is.Not.SameAs(first.TargetRune));

            Object.DestroyImmediate(asset);
        }

        [Test]
        public void SerializedTargetUpdate_IsUsedWhenCreatingRuntimeDefinition()
        {
            RuneData serializedTarget = EmptyCircle();
            _ = serializedTarget.Glyphs;
            JsonUtility.FromJsonOverwrite(
                "{\"baseShape\":0,\"glyphs\":[{\"type\":0,\"rotation\":0}],\"elementZones\":[]}",
                serializedTarget);
            ObjectiveDefinitionAsset asset = CreateObjectiveAsset(
                "Serialized Attack",
                serializedTarget,
                1);
            ObjectiveProgress progress = CreateProgress(asset.CreateRuntimeDefinition());

            RuneDeliveryResult incorrectResult = progress.Deliver(EmptyCircle());
            RuneDeliveryResult correctResult = progress.Deliver(AttackAt(0));

            Assert.That(incorrectResult, Is.EqualTo(RuneDeliveryResult.Incorrect));
            Assert.That(correctResult,
                Is.EqualTo(RuneDeliveryResult.CorrectAndObjectiveCompleted));
            Assert.That(asset.TargetRune, Is.EqualTo(AttackAt(0)));

            Object.DestroyImmediate(asset);
        }

        [Test]
        public void ObjectiveAssetOrder_DeterminesSequentialProgression()
        {
            ObjectiveDefinitionAsset attack = CreateObjectiveAsset("Attack", AttackAt(0), 1);
            ObjectiveDefinitionAsset circle = CreateObjectiveAsset("Circle", EmptyCircle(), 1);
            ObjectiveProgress progress = CreateProgress(
                attack.CreateRuntimeDefinition(),
                circle.CreateRuntimeDefinition());

            Assert.That(progress.Deliver(EmptyCircle()), Is.EqualTo(RuneDeliveryResult.Incorrect));
            Assert.That(progress.Deliver(AttackAt(0)),
                Is.EqualTo(RuneDeliveryResult.CorrectAndObjectiveCompleted));
            Assert.That(progress.CurrentObjective.DisplayName, Is.EqualTo("Circle"));

            Object.DestroyImmediate(attack);
            Object.DestroyImmediate(circle);
        }

        [Test]
        public void FinalThroughputObjective_RemainsEditableThroughObjectiveAsset()
        {
            ObjectiveDefinitionAsset objective = CreateObjectiveAsset(
                "Final Throughput: Attack Rune",
                AttackAt(0),
                500);

            ObjectiveDefinition initialDefinition = objective.CreateRuntimeDefinition();
            objective.Configure("Tuned Final Throughput", AttackAt(0), 750);
            ObjectiveDefinition tunedDefinition = objective.CreateRuntimeDefinition();

            Assert.That(initialDefinition.TargetRune, Is.EqualTo(AttackAt(0)));
            Assert.That(initialDefinition.RequiredCount, Is.EqualTo(500));
            Assert.That(tunedDefinition.DisplayName, Is.EqualTo("Tuned Final Throughput"));
            Assert.That(tunedDefinition.RequiredCount, Is.EqualTo(750));

            Object.DestroyImmediate(objective);
        }

        [Test]
        public void FinalThroughputObjective_CompletesAtEndOfExistingSequence()
        {
            ObjectiveProgress progress = CreateProgress(
                Objective("Existing Objective", EmptyCircle(), 1),
                Objective("Final Throughput: Attack Rune", AttackAt(0), 3));

            progress.Deliver(EmptyCircle());
            RuneDeliveryResult incorrectResult = progress.Deliver(SplitAt(0));
            RuneDeliveryResult firstCorrectResult = progress.Deliver(AttackAt(0));
            progress.Deliver(AttackAt(0));
            RuneDeliveryResult finalResult = progress.Deliver(AttackAt(0));

            Assert.That(progress.CurrentObjectiveIndex, Is.EqualTo(2));
            Assert.That(incorrectResult, Is.EqualTo(RuneDeliveryResult.Incorrect));
            Assert.That(firstCorrectResult, Is.EqualTo(RuneDeliveryResult.Correct));
            Assert.That(finalResult,
                Is.EqualTo(RuneDeliveryResult.CorrectAndObjectiveCompleted));
            Assert.That(progress.AreAllObjectivesComplete, Is.True);
            Assert.That(progress.CurrentObjective, Is.Null);
        }

        [Test]
        public void EngraverUpgrade_ReducesTimeForFinalThroughputTargetWithoutBeingRequired()
        {
            var baselineEngraver = new EngraverProcess(
                Vector2Int.zero,
                GridDirection.East,
                GlyphType.Attack,
                4f,
                2f);
            var upgradedEngraver = new EngraverProcess(
                Vector2Int.zero,
                GridDirection.East,
                GlyphType.Attack,
                4f,
                2f);
            upgradedEngraver.TryInstallAccelerationUpgrade();
            baselineEngraver.TryAcceptInput(EmptyCircle(), GridDirection.East);
            upgradedEngraver.TryAcceptInput(EmptyCircle(), GridDirection.East);

            bool baselineCompletedAtTwoSeconds = baselineEngraver.Advance(2f);
            bool upgradedCompletedAtTwoSeconds = upgradedEngraver.Advance(2f);
            bool baselineCompletedAtFourSeconds = baselineEngraver.Advance(2f);

            ObjectiveProgress baselineProgress = CreateProgress(
                Objective("Final Throughput", AttackAt(0), 1));
            ObjectiveProgress upgradedProgress = CreateProgress(
                Objective("Final Throughput", AttackAt(0), 1));
            RuneDeliveryResult baselineResult = baselineProgress.Deliver(
                baselineEngraver.HeldRune);
            RuneDeliveryResult upgradedResult = upgradedProgress.Deliver(
                upgradedEngraver.HeldRune);

            Assert.That(baselineCompletedAtTwoSeconds, Is.False);
            Assert.That(upgradedCompletedAtTwoSeconds, Is.True);
            Assert.That(baselineCompletedAtFourSeconds, Is.True);
            Assert.That(baselineEngraver.HeldRune, Is.EqualTo(AttackAt(0)));
            Assert.That(upgradedEngraver.HeldRune, Is.EqualTo(AttackAt(0)));
            Assert.That(baselineResult,
                Is.EqualTo(RuneDeliveryResult.CorrectAndObjectiveCompleted));
            Assert.That(upgradedResult,
                Is.EqualTo(RuneDeliveryResult.CorrectAndObjectiveCompleted));
        }

        [Test]
        public void SpiritRune_CanBeEngravedInfusedAndDelivered()
        {
            var engraver = new EngraverProcess(
                Vector2Int.zero,
                GridDirection.East,
                RuneSigil.Spirit,
                1f);
            var infuser = new ElementInfuserProcess(
                Vector2Int.right,
                GridDirection.East,
                RuneElement.Fire,
                1f);
            ObjectiveProgress progress = CreateProgress(Objective(
                "Fire Spirit Rune",
                new RuneData(
                    RuneBaseShape.Circle,
                    RuneSigil.Spirit,
                    RuneElement.Fire),
                1));

            engraver.TryAcceptInput(EmptyCircle(), GridDirection.East);
            engraver.Advance(1f);
            engraver.TryTakeOutput(out RuneData spiritRune);
            infuser.TryAcceptInput(spiritRune, GridDirection.East);
            infuser.Advance(1f);

            RuneDeliveryResult result = progress.Deliver(infuser.HeldRune);

            Assert.That(spiritRune.ToString(), Is.EqualTo("Circle | Spirit"));
            Assert.That(infuser.HeldRune.ToString(), Is.EqualTo("Circle | Spirit | Fire"));
            Assert.That(result,
                Is.EqualTo(RuneDeliveryResult.CorrectAndObjectiveCompleted));
        }

        [Test]
        public void SpiritObjective_DoesNotMatchAccelerationRune()
        {
            ObjectiveProgress progress = CreateProgress(Objective(
                "Spirit Rune",
                new RuneData(RuneBaseShape.Circle, RuneSigil.Spirit),
                1));

            RuneDeliveryResult result = progress.Deliver(
                new RuneData(RuneBaseShape.Circle, RuneSigil.Acceleration));

            Assert.That(result, Is.EqualTo(RuneDeliveryResult.Incorrect));
            Assert.That(progress.CurrentCount, Is.Zero);
        }

        [Test]
        public void DeliveredAccelerationRune_AddsInventoryIndependentlyOfObjectiveProgress()
        {
            ObjectiveProgress progress = CreateProgress(Objective(
                "Spirit Rune",
                new RuneData(RuneBaseShape.Circle, RuneSigil.Spirit),
                1));
            var inventory = new AccelerationRuneInventory();
            var receiver = new HubReceiver(
                Vector2Int.zero,
                GridDirection.East,
                progress,
                inventory);

            bool accepted = receiver.TryAcceptInput(
                new RuneData(RuneBaseShape.Circle, RuneSigil.Acceleration),
                GridDirection.East);

            Assert.That(accepted, Is.True);
            Assert.That(inventory.Count, Is.EqualTo(1));
            Assert.That(progress.CurrentCount, Is.Zero);
            Assert.That(receiver.LastDeliveryResult, Is.EqualTo(RuneDeliveryResult.Incorrect));
        }

        [Test]
        public void ConsumingAccelerationInventory_DoesNotDecreaseCumulativeObjectiveProgress()
        {
            RuneData accelerationRune = new(
                RuneBaseShape.Circle,
                RuneSigil.Acceleration);
            ObjectiveProgress progress = CreateProgress(Objective(
                "Acceleration Rune",
                accelerationRune,
                2));
            var inventory = new AccelerationRuneInventory();
            var receiver = new HubReceiver(
                Vector2Int.zero,
                GridDirection.East,
                progress,
                inventory);
            receiver.TryAcceptInput(accelerationRune, GridDirection.East);

            bool consumed = inventory.TryConsume();

            Assert.That(consumed, Is.True);
            Assert.That(inventory.Count, Is.Zero);
            Assert.That(progress.CurrentCount, Is.EqualTo(1));
            Assert.That(progress.CurrentObjectiveIndex, Is.Zero);
        }

        [Test]
        public void EmptyAccelerationInventory_CannotBeConsumedOrBecomeNegative()
        {
            var inventory = new AccelerationRuneInventory();

            bool consumed = inventory.TryConsume();

            Assert.That(consumed, Is.False);
            Assert.That(inventory.Count, Is.Zero);
        }

        [Test]
        public void IncorrectDelivery_IsConsumedWithoutProgress()
        {
            ObjectiveProgress progress = CreateProgress(Objective("Attack", AttackAt(0), 1));
            var inventory = new AccelerationRuneInventory();
            var receiver = new HubReceiver(
                Vector2Int.zero,
                GridDirection.East,
                progress,
                inventory);
            var transport = new BeltTransportSystem(1f);
            transport.RegisterInputReceiver(receiver);
            BeltCell inputBelt = transport.AddBelt(Vector2Int.left, GridDirection.East);
            RuneData incorrectRune = SplitAt(0);
            inputBelt.TryAccept(incorrectRune, GridDirection.East);

            transport.Advance(1f);

            Assert.That(inputBelt.HasItem, Is.False);
            Assert.That(receiver.LastDeliveryResult, Is.EqualTo(RuneDeliveryResult.Incorrect));
            Assert.That(progress.CurrentCount, Is.Zero);
        }

        [Test]
        public void BeltFromInvalidSide_CannotFeedHub()
        {
            ObjectiveProgress progress = CreateProgress(Objective("Circle", EmptyCircle(), 1));
            var inventory = new AccelerationRuneInventory();
            var receiver = new HubReceiver(
                Vector2Int.zero,
                GridDirection.East,
                progress,
                inventory);
            var transport = new BeltTransportSystem(1f);
            transport.RegisterInputReceiver(receiver);
            BeltCell invalidBelt = transport.AddBelt(Vector2Int.down, GridDirection.North);
            invalidBelt.TryAccept(EmptyCircle(), GridDirection.North);

            transport.Advance(1f);

            Assert.That(invalidBelt.HasItem, Is.True);
            Assert.That(progress.CurrentCount, Is.Zero);
        }

        private static ObjectiveProgress CreateProgress(params ObjectiveDefinition[] objectives)
        {
            return new ObjectiveProgress(objectives);
        }

        private static ObjectiveDefinition Objective(
            string name,
            RuneData target,
            int requiredCount)
        {
            return new ObjectiveDefinition(name, target, requiredCount);
        }

        private static ObjectiveDefinitionAsset CreateObjectiveAsset(
            string name,
            RuneData target,
            int requiredCount)
        {
            ObjectiveDefinitionAsset asset = ScriptableObject.CreateInstance<ObjectiveDefinitionAsset>();
            asset.Configure(name, target, requiredCount);
            return asset;
        }

        private static RuneData EmptyCircle()
        {
            return new RuneData(RuneBaseShape.Circle);
        }

        private static RuneData AttackAt(int rotation)
        {
            return RuneWithGlyph(GlyphType.Attack, (GlyphRotation)rotation);
        }

        private static RuneData SplitAt(int rotation)
        {
            return RuneWithGlyph(GlyphType.Split, (GlyphRotation)rotation);
        }

        private static RuneData AccelerationAt(int rotation)
        {
            return RuneWithGlyph(GlyphType.Acceleration, (GlyphRotation)rotation);
        }

        private static RuneData RuneWithGlyph(GlyphType type, GlyphRotation rotation)
        {
            return new RuneData(
                RuneBaseShape.Circle,
                new[] { new GlyphData(type, rotation) },
                null);
        }

        private static RuneData ElementRune(params ElementZoneAssignment[] assignments)
        {
            return new RuneData(RuneBaseShape.Circle, null, assignments);
        }
    }
}
