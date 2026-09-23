using System.Reflection;
using FantasyShapez.Buildings;
using FantasyShapez.Grid;
using FantasyShapez.Logistics;
using FantasyShapez.Production;
using FantasyShapez.Resources;
using FantasyShapez.Runes;
using NUnit.Framework;
using UnityEngine;

namespace FantasyShapez.Tests.EditMode
{
    public sealed class RuneExtractorTests
    {
        [Test]
        public void CircleResource_ExposesCircleAndExtractsEmptyCircleRune()
        {
            var resource = new RuneStoneResource(RuneBaseShape.Circle);

            RuneData rune = resource.Extract();

            Assert.That(resource.BaseShape, Is.EqualTo(RuneBaseShape.Circle));
            Assert.That(rune.BaseShape, Is.EqualTo(RuneBaseShape.Circle));
            Assert.That(rune.Glyphs, Is.Empty);
            Assert.That(rune.ElementZones, Is.Empty);
        }

        [Test]
        public void SeparateExtractions_CreateIndependentRuneInstances()
        {
            var resource = new RuneStoneResource(RuneBaseShape.Circle);

            RuneData first = resource.Extract();
            RuneData second = resource.Extract();

            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first, Is.EqualTo(second));
        }

        [Test]
        public void OutputBuffer_RejectsOutputBeyondCapacity()
        {
            var buffer = new RuneOutputBuffer(2);

            Assert.That(buffer.TryAdd(new RuneData(RuneBaseShape.Circle)), Is.True);
            Assert.That(buffer.TryAdd(new RuneData(RuneBaseShape.Circle)), Is.True);
            Assert.That(buffer.TryAdd(new RuneData(RuneBaseShape.Circle)), Is.False);
            Assert.That(buffer.Count, Is.EqualTo(2));
        }

        [Test]
        public void Extractor_StopsAtCapacityAndResumesAfterOutputIsTaken()
        {
            var process = new RuneExtractorProcess(
                new RuneStoneResource(RuneBaseShape.Circle),
                1f,
                2);

            int initiallyProduced = process.Advance(3f);
            int producedWhileFull = process.Advance(10f);
            bool tookOutput = process.OutputBuffer.TryTakeOutput(out RuneData takenRune);
            RuneData waitingRune = process.OutputBuffer.PeekOutput();
            int producedWithoutElapsedTime = process.Advance(0f);
            int producedAfterSpaceOpened = process.Advance(1f);

            Assert.That(initiallyProduced, Is.EqualTo(2));
            Assert.That(producedWhileFull, Is.Zero);
            Assert.That(tookOutput, Is.True);
            Assert.That(takenRune.BaseShape, Is.EqualTo(RuneBaseShape.Circle));
            Assert.That(waitingRune, Is.Not.SameAs(takenRune));
            Assert.That(producedWithoutElapsedTime, Is.Zero);
            Assert.That(producedAfterSpaceOpened, Is.EqualTo(1));
            Assert.That(process.OutputBuffer.Count, Is.EqualTo(2));
        }

        [Test]
        public void ExtractorWithoutResource_CannotProduce()
        {
            var process = new RuneExtractorProcess(null, 1f, 2);

            int produced = process.Advance(10f);

            Assert.That(process.HasValidResource, Is.False);
            Assert.That(produced, Is.Zero);
            Assert.That(process.OutputBuffer.HasOutput, Is.False);
        }

        [Test]
        public void DiscardContents_ClearsBufferedOutput()
        {
            var process = new RuneExtractorProcess(
                new RuneStoneResource(RuneBaseShape.Circle),
                1f,
                2);
            process.Advance(2f);

            process.DiscardContents();

            Assert.That(process.OutputBuffer.Count, Is.Zero);
            Assert.That(process.OutputBuffer.HasOutput, Is.False);
        }

        [Test]
        public void MoveState_PreservesBufferedRuneAndPartialCycle()
        {
            var process = new RuneExtractorProcess(
                new RuneStoneResource(RuneBaseShape.Circle), 1f, 2);
            process.Advance(1.5f);

            RuneExtractorProcess moved = process.CopyForMove(
                new RuneStoneResource(RuneBaseShape.Circle));

            Assert.That(process.OutputBuffer.Count, Is.EqualTo(1));
            Assert.That(moved.OutputBuffer.Count, Is.EqualTo(1));
            Assert.That(moved.OutputBuffer.PeekOutput().BaseShape,
                Is.EqualTo(RuneBaseShape.Circle));
            Assert.That(moved.OutputBuffer.PeekOutput(),
                Is.Not.SameAs(process.OutputBuffer.PeekOutput()));

            moved.Advance(0.5f);
            moved.OutputBuffer.TryTakeOutput(out RuneData bufferedRune);
            moved.OutputBuffer.TryTakeOutput(out RuneData newlyExtractedRune);
            Assert.That(bufferedRune.BaseShape, Is.EqualTo(RuneBaseShape.Circle));
            Assert.That(newlyExtractedRune.BaseShape, Is.EqualTo(RuneBaseShape.Circle));

            Assert.That(process.OutputBuffer.Count, Is.EqualTo(1),
                "The original remains intact until the group move commits.");
        }

        [Test]
        public void MoveState_PreservesFullOutputBuffer()
        {
            var process = new RuneExtractorProcess(
                new RuneStoneResource(RuneBaseShape.Circle), 1f, 2);
            process.Advance(2f);

            RuneExtractorProcess moved = process.CopyForMove(
                new RuneStoneResource(RuneBaseShape.Circle));

            Assert.That(moved.OutputBuffer.Count, Is.EqualTo(2));
            Assert.That(moved.OutputBuffer.CanAcceptOutput, Is.False);
        }

        [Test]
        public void MoveState_UsesDestinationResourceForFutureExtraction()
        {
            var source = new RuneExtractorProcess(null, 1f, 2);
            source.OutputBuffer.TryAdd(new RuneData(RuneBaseShape.Circle));

            RuneExtractorProcess moved = source.CopyForMove(
                new RuneStoneResource(RuneBaseShape.Circle));

            Assert.That(source.HasValidResource, Is.False);
            Assert.That(moved.Advance(1f), Is.EqualTo(1));
            Assert.That(moved.OutputBuffer.Count, Is.EqualTo(2));
        }

        [Test]
        public void ActiveExtractorWithBufferedOutput_CanEnterMoveMode()
        {
            var resourceObject = new GameObject("Resource");
            var extractorObject = new GameObject("Extractor");
            try
            {
                RuneStoneResourceNode resourceNode =
                    resourceObject.AddComponent<RuneStoneResourceNode>();
                RuneExtractor extractor = extractorObject.AddComponent<RuneExtractor>();
                extractor.Initialize(resourceNode, Vector2Int.zero,
                    GridDirection.North, null);
                RuneExtractorProcess process =
                    (RuneExtractorProcess)typeof(RuneExtractor)
                        .GetField("process", BindingFlags.Instance | BindingFlags.NonPublic)
                        .GetValue(extractor);
                process.Advance(1.5f);

                Assert.That(extractor.OutputCount, Is.EqualTo(1));
                Assert.That(extractor.CanMove, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(extractorObject);
                Object.DestroyImmediate(resourceObject);
            }
        }

        [Test]
        public void SceneResourceNotInSerializedList_AllowsExtractorPlacement()
        {
            var gridObject = new GameObject("Grid");
            var resourceMapObject = new GameObject("Resource Map");
            var placementObject = new GameObject("Extractor Placement");
            var resourceObject = new GameObject("Manually Added Resource");

            try
            {
                GridSystem gridSystem = gridObject.AddComponent<GridSystem>();
                RuneResourceMap resourceMap =
                    resourceMapObject.AddComponent<RuneResourceMap>();
                RuneExtractorPlacementBehavior placementBehavior =
                    placementObject.AddComponent<RuneExtractorPlacementBehavior>();
                RuneStoneResourceNode resourceNode =
                    resourceObject.AddComponent<RuneStoneResourceNode>();
                var resourceCell = new Vector2Int(17, -9);
                resourceObject.transform.position = gridSystem.GridToWorld(resourceCell);

                SetPrivateField(resourceMap, "gridSystem", gridSystem);
                SetPrivateField(placementBehavior, "resourceMap", resourceMap);

                bool canPlaceOnResource = placementBehavior.CanPlace(
                    resourceCell,
                    Vector2Int.one,
                    BuildingRotation.Degrees0);
                bool canPlaceOnEmptyCell = placementBehavior.CanPlace(
                    resourceCell + Vector2Int.right,
                    Vector2Int.one,
                    BuildingRotation.Degrees0);

                Assert.That(resourceNode, Is.Not.Null);
                Assert.That(canPlaceOnResource, Is.True);
                Assert.That(canPlaceOnEmptyCell, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(resourceObject);
                Object.DestroyImmediate(placementObject);
                Object.DestroyImmediate(resourceMapObject);
                Object.DestroyImmediate(gridObject);
            }
        }

        private static void SetPrivateField<TTarget, TValue>(
            TTarget target,
            string fieldName,
            TValue value)
        {
            FieldInfo field = typeof(TTarget).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }
    }
}
