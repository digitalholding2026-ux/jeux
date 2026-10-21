using ColorFlow.Core;
using NUnit.Framework;

namespace ColorFlow.Tests.EditMode
{
    public sealed class PaintResolverTests
    {
        private static readonly RgbaColor Red = RgbaColor.FromHex("#FF0000");
        private static readonly RgbaColor Green = RgbaColor.FromHex("#00FF00");

        private static LevelDefinition TwoPixels()
        {
            return LevelBuilder.FromRows(
                "test",
                "Test",
                new[]
                {
                    "12"
                },
                new[] { "#FF0000", "#00FF00" },
                1f);
        }

        [Test]
        public void Resolve_ValidTap_PaintsMatchingPixel()
        {
            var grid = new GridModel(TwoPixels());
            var resolver = new PaintResolver();
            var creature = new Creature(1, Red);

            var result = resolver.Resolve(creature, grid);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(0, result.X);
            Assert.AreEqual(0, result.Y);
            Assert.IsTrue(grid.IsPainted(0, 0));
            Assert.IsFalse(result.LevelCompleted);
        }

        [Test]
        public void Resolve_TapWithoutCompatiblePixel_IsRejected()
        {
            var grid = new GridModel(TwoPixels());
            var resolver = new PaintResolver();
            var creature = new Creature(1, Green);
            grid.TryPaint(1, 0, Green);

            var result = resolver.Resolve(creature, grid);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(PaintFailureReason.NoCompatiblePixel, result.Failure);
            Assert.AreEqual(1, grid.PaintedCount);
            Assert.IsTrue(grid.IsPainted(1, 0));
        }

        [Test]
        public void Resolve_TapOnUnknownColor_ReturnsNoCompatiblePixel()
        {
            var grid = new GridModel(TwoPixels());
            var resolver = new PaintResolver();
            var creature = new Creature(1, RgbaColor.FromHex("#0000FF"));

            var result = resolver.Resolve(creature, grid);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(PaintFailureReason.NoCompatiblePixel, result.Failure);
            Assert.AreEqual(0, grid.PaintedCount);
        }

        [Test]
        public void Resolve_NullCreature_ReturnsNoCreature()
        {
            var grid = new GridModel(TwoPixels());

            var result = new PaintResolver().Resolve(null, grid);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(PaintFailureReason.NoCreature, result.Failure);
        }

        [Test]
        public void Resolve_LastPixel_ReportsVictory()
        {
            var grid = new GridModel(TwoPixels());
            var resolver = new PaintResolver();

            Assert.IsTrue(resolver.Resolve(new Creature(1, Red), grid).IsSuccess);
            var last = resolver.Resolve(new Creature(2, Green), grid);

            Assert.IsTrue(last.IsSuccess);
            Assert.IsTrue(last.LevelCompleted);
            Assert.IsTrue(grid.IsComplete);
        }

        [Test]
        public void Resolve_OnCompletedGrid_ReturnsLevelComplete()
        {
            var grid = new GridModel(TwoPixels());
            var resolver = new PaintResolver();
            resolver.Resolve(new Creature(1, Red), grid);
            resolver.Resolve(new Creature(2, Green), grid);

            var result = resolver.Resolve(new Creature(3, Red), grid);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(PaintFailureReason.LevelComplete, result.Failure);
        }
    }
}
