using FantasyShapez.Production;
using FantasyShapez.Resources;
using FantasyShapez.Runes;
using NUnit.Framework;

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
    }
}
