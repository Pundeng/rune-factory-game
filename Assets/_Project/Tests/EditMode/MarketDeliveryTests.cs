using FantasyShapez.Food;
using FantasyShapez.Logistics;
using FantasyShapez.Runes;
using NUnit.Framework;
using UnityEngine;

namespace FantasyShapez.Tests.EditMode
{
    public sealed class MarketDeliveryTests
    {
        [Test]
        public void Inventory_TracksFoodByIdAndKind()
        {
            var inventory = new MarketInventory();
            var rawApple = new FoodItemData("apple", FoodItemKind.RawIngredient);
            var processedApple = new FoodItemData("apple", FoodItemKind.ProcessedFood);

            Assert.That(inventory.RecordDelivery(rawApple), Is.EqualTo(1));
            Assert.That(inventory.RecordDelivery(new FoodItemData("apple",
                FoodItemKind.RawIngredient)), Is.EqualTo(2));
            Assert.That(inventory.RecordDelivery(processedApple), Is.EqualTo(1));
            Assert.That(inventory.RecordDelivery(new FoodItemData("pear",
                FoodItemKind.RawIngredient)), Is.EqualTo(1));
            Assert.That(inventory.GetDeliveredCount(rawApple), Is.EqualTo(2));
            Assert.That(inventory.GetDeliveredCount(processedApple), Is.EqualTo(1));
            Assert.That(inventory.TotalDelivered, Is.EqualTo(4));
            Assert.That(inventory.Currency, Is.EqualTo(4));
        }

        [Test]
        public void SellValues_AwardCurrencyPerDeliveryWithoutChangingFoodCounts()
        {
            var inventory = new MarketInventory();
            var rawApple = new FoodItemData("apple", FoodItemKind.RawIngredient, 2);
            var driedApple = new FoodItemData("Dried Apple", FoodItemKind.ProcessedFood, 5);

            Assert.That(inventory.RecordDelivery(rawApple), Is.EqualTo(1));
            Assert.That(inventory.RecordDelivery(new FoodItemData("apple",
                FoodItemKind.RawIngredient, 2)), Is.EqualTo(2));
            Assert.That(inventory.RecordDelivery(driedApple), Is.EqualTo(1));
            Assert.That(inventory.GetDeliveredCount(rawApple), Is.EqualTo(2));
            Assert.That(inventory.GetDeliveredCount(driedApple), Is.EqualTo(1));
            Assert.That(inventory.TotalDelivered, Is.EqualTo(3));
            Assert.That(inventory.Currency, Is.EqualTo(9));
            Assert.That(rawApple.SellValue, Is.EqualTo(2));
            Assert.That(driedApple.SellValue, Is.EqualTo(5));
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                new FoodItemData("unsellable", FoodItemKind.RawIngredient, 0));
        }

        [Test]
        public void Receiver_AcceptsFoodAndReportsSuccessfulDelivery()
        {
            var inventory = new MarketInventory();
            var receiver = new MarketReceiver(Vector2Int.zero, inventory);
            var apple = new FoodItemData("apple", FoodItemKind.RawIngredient);
            FoodItemData delivered = null;
            int deliveredCount = 0;
            receiver.FoodDelivered += (food, count) =>
            {
                delivered = food;
                deliveredCount = count;
            };

            Assert.That(receiver.TryAcceptItem(apple, GridDirection.East), Is.True);
            Assert.That(delivered, Is.SameAs(apple));
            Assert.That(deliveredCount, Is.EqualTo(1));
            Assert.That(inventory.TotalDelivered, Is.EqualTo(1));
            Assert.That(inventory.Currency, Is.EqualTo(1));
        }

        [Test]
        public void Receiver_RejectsRunesAndInvalidDirectionsWithoutCounting()
        {
            var inventory = new MarketInventory();
            var receiver = new MarketReceiver(Vector2Int.zero, inventory);
            var apple = new FoodItemData("apple", FoodItemKind.RawIngredient);

            Assert.That(receiver.TryAcceptItem(new RuneData(RuneBaseShape.Circle),
                GridDirection.East), Is.False);
            Assert.That(receiver.TryAcceptItem(apple, (GridDirection)99), Is.False);
            Assert.That(inventory.TotalDelivered, Is.Zero);
            Assert.That(inventory.Currency, Is.Zero);

            var transport = new BeltTransportSystem(1f);
            BeltCell belt = transport.AddBelt(Vector2Int.left, GridDirection.East);
            belt.TryAccept(new RuneData(RuneBaseShape.Circle), GridDirection.East);
            transport.RegisterInputReceiver(receiver);
            transport.Advance(1f);
            Assert.That(belt.HasItem, Is.True);
            Assert.That(inventory.TotalDelivered, Is.Zero);
            Assert.That(inventory.Currency, Is.Zero);
        }

        [Test]
        public void TwoBelts_DeliverFoodToMarketInSameStep()
        {
            var inventory = new MarketInventory();
            var receiver = new MarketReceiver(Vector2Int.zero, inventory);
            var transport = new BeltTransportSystem(1f);
            BeltCell west = transport.AddBelt(Vector2Int.left, GridDirection.East);
            BeltCell east = transport.AddBelt(Vector2Int.right, GridDirection.West);
            west.TryAccept(new FoodItemData("apple", FoodItemKind.RawIngredient),
                GridDirection.East);
            east.TryAccept(new FoodItemData("apple", FoodItemKind.RawIngredient),
                GridDirection.West);
            transport.RegisterInputReceiver(receiver);

            transport.Advance(1f);

            Assert.That(receiver.AllowsConcurrentInput, Is.True);
            Assert.That(inventory.GetDeliveredCount(new FoodItemData("apple",
                FoodItemKind.RawIngredient)), Is.EqualTo(2));
            Assert.That(west.HasItem, Is.False);
            Assert.That(east.HasItem, Is.False);
            Assert.That(inventory.Currency, Is.EqualTo(2));
        }

        [Test]
        public void PlotThroughHarvesterAndBelt_DeliversAppleToMarket()
        {
            var apple = new CropDefinition("Apple",
                new FoodItemData("apple", FoodItemKind.RawIngredient, 3), 2f);
            var plot = new FarmPlotProcess(1);
            plot.SelectCrop(apple);
            plot.Advance(2f);
            var harvester = new HarvesterProcess(1f, 1);
            Assert.That(harvester.Advance(1f, plot), Is.EqualTo(1));

            var inventory = new MarketInventory();
            var receiver = new MarketReceiver(Vector2Int.right, inventory);
            var transport = new BeltTransportSystem(1f);
            BeltCell belt = transport.AddBelt(Vector2Int.zero, GridDirection.East);
            transport.RegisterOutputSource(new HarvesterSource(harvester));
            transport.RegisterInputReceiver(receiver);

            transport.Advance(0f);
            Assert.That(belt.HasItem, Is.True);
            transport.Advance(1f);

            Assert.That(belt.HasItem, Is.False);
            Assert.That(inventory.GetDeliveredCount(apple.Output), Is.EqualTo(1));
            Assert.That(inventory.TotalDelivered, Is.EqualTo(1));
            Assert.That(inventory.Currency, Is.EqualTo(3));
        }

        private sealed class HarvesterSource : IItemOutputSource
        {
            private readonly HarvesterProcess process;

            public HarvesterSource(HarvesterProcess process)
            {
                this.process = process;
            }

            public Vector2Int OutputCell => Vector2Int.zero;
            public GridDirection OutputDirection => GridDirection.East;
            public bool HasOutput => process.HasOutput;
            public ITransportItem PeekOutput() => process.PeekOutput();

            public bool TryTakeOutput(out ITransportItem item)
            {
                bool taken = process.TryTakeOutput(out FoodItemData food);
                item = food;
                return taken;
            }
        }
    }
}
