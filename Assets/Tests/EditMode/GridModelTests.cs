using ColorFlow.Core;
using NUnit.Framework;

namespace ColorFlow.Tests.EditMode
{
    public sealed class GridModelTests
    {
        private static LevelDefinition CrossLevel()
        {
            return LevelBuilder.FromRows(
                "croix",
                "Croix",
                new[]
                {
                    ".1.",
                    "111",
                    ".1."
                },
                new[] { "#FF7A59" },
                1f);
        }

        private static LevelDefinition TwoColorsLevel()
        {
            return LevelBuilder.FromRows(
                "deux",
                "Deux",
                new[]
                {
                    "11.",
                    ".22"
                },
                new[] { "#FF0000", "#00FF00" },
                1f);
        }

        [Test]
        public void Constructor_FromLevel_ExposesTargetCount()
        {
            var grid = new GridModel(CrossLevel());

            Assert.AreEqual(3, grid.Width);
            Assert.AreEqual(3, grid.Height);
            Assert.AreEqual(5, grid.TargetCount);
            Assert.AreEqual(0, grid.PaintedCount);
            Assert.AreEqual(5, grid.RemainingCount);
            Assert.IsFalse(grid.IsComplete);
        }

        [Test]
        public void TryPaint_ValidCell_FillsAndRaisesEvent()
        {
            var grid = new GridModel(CrossLevel());
            var paintedX = -1;
            var paintedY = -1;
            grid.PixelPainted += (x, y, c) =>
            {
                paintedX = x;
                paintedY = y;
            };

            var result = grid.TryPaint(1, 0, RgbaColor.FromHex("#FF7A59"));

            Assert.IsTrue(result);
            Assert.IsTrue(grid.IsPainted(1, 0));
            Assert.AreEqual(1, grid.PaintedCount);
            Assert.AreEqual(1, paintedX);
            Assert.AreEqual(0, paintedY);
        }

        [Test]
        public void TryPaint_WrongColor_IsRejected()
        {
            var grid = new GridModel(CrossLevel());

            var result = grid.TryPaint(1, 0, RgbaColor.FromHex("#00FF00"));

            Assert.IsFalse(result);
            Assert.IsFalse(grid.IsPainted(1, 0));
            Assert.AreEqual(0, grid.PaintedCount);
        }

        [Test]
        public void TryPaint_EmptyCellOrOutOfBounds_IsRejected()
        {
            var grid = new GridModel(CrossLevel());

            Assert.IsFalse(grid.TryPaint(0, 0, RgbaColor.FromHex("#FF7A59")));
            Assert.IsFalse(grid.TryPaint(-1, 0, RgbaColor.FromHex("#FF7A59")));
            Assert.IsFalse(grid.TryPaint(3, 0, RgbaColor.FromHex("#FF7A59")));
        }

        [Test]
        public void TryPaint_AlreadyPaintedCell_IsRejected()
        {
            var grid = new GridModel(CrossLevel());
            var color = RgbaColor.FromHex("#FF7A59");

            Assert.IsTrue(grid.TryPaint(1, 0, color));
            Assert.IsFalse(grid.TryPaint(1, 0, color));
            Assert.AreEqual(1, grid.PaintedCount);
        }

        [Test]
        public void TryFindNextPixel_ReturnsFirstMatchInRowMajorOrder()
        {
            var grid = new GridModel(TwoColorsLevel());
            var red = RgbaColor.FromHex("#FF0000");

            Assert.IsTrue(grid.TryFindNextPixel(red, out var x, out var y));
            Assert.AreEqual(0, x);
            Assert.AreEqual(0, y);

            grid.TryPaint(0, 0, red);
            grid.TryPaint(1, 0, red);

            Assert.IsFalse(grid.TryFindNextPixel(red, out _, out _));
        }

        [Test]
        public void TryFindNextPixel_NoCompatiblePixel_ReturnsFalse()
        {
            var grid = new GridModel(TwoColorsLevel());

            var found = grid.TryFindNextPixel(RgbaColor.FromHex("#0000FF"), out var x, out var y);

            Assert.IsFalse(found);
            Assert.AreEqual(-1, x);
            Assert.AreEqual(-1, y);
        }

        [Test]
        public void TryPaintNext_FillsThePatternInOrder()
        {
            var grid = new GridModel(TwoColorsLevel());
            var green = RgbaColor.FromHex("#00FF00");

            Assert.IsTrue(grid.TryPaintNext(green, out var x, out var y));
            Assert.AreEqual(1, x);
            Assert.AreEqual(1, y);
            Assert.IsTrue(grid.IsPainted(1, 1));
        }

        [Test]
        public void IsComplete_TrueWhenAllTargetsPainted()
        {
            var grid = new GridModel(CrossLevel());
            var color = RgbaColor.FromHex("#FF7A59");

            while (grid.TryPaintNext(color, out _, out _))
            {
            }

            Assert.AreEqual(5, grid.PaintedCount);
            Assert.AreEqual(0, grid.RemainingCount);
            Assert.IsTrue(grid.IsComplete);
        }

        [Test]
        public void TargetColorAt_OutOfBounds_ReturnsTransparent()
        {
            var grid = new GridModel(CrossLevel());

            Assert.IsTrue(grid.TargetColorAt(9, 9).IsTransparent);
            Assert.IsFalse(grid.HasTargetAt(0, 0));
            Assert.IsTrue(grid.HasTargetAt(1, 0));
        }
    }
}
