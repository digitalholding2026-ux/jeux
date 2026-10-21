using ColorFlow.Core;
using NUnit.Framework;

namespace ColorFlow.Tests.EditMode
{
    public sealed class LevelBuilderTests
    {
        [Test]
        public void FromRows_ParsesDimensionsAndTargets()
        {
            var level = LevelBuilder.FromRows(
                "coeur",
                "Cœur",
                new[]
                {
                    ".1.",
                    "111",
                    ".1."
                },
                new[] { "#FF4E88" },
                1.5f);

            Assert.AreEqual(3, level.Width);
            Assert.AreEqual(3, level.Height);
            Assert.AreEqual(5, level.TargetPixelCount);
            Assert.AreEqual(1.5f, level.SpawnInterval);
            Assert.AreEqual("Cœur", level.DisplayName);
            Assert.IsTrue(level.HasPixelAt(1, 1));
            Assert.IsFalse(level.HasPixelAt(0, 0));
            Assert.AreEqual("#FF4E88", level.CellAt(1, 1).ToHex());
        }

        [Test]
        public void FromRows_UsesPaletteIndexes()
        {
            var level = LevelBuilder.FromRows(
                "deux",
                "Deux",
                new[] { "12", "21" },
                new[] { "#112233", "#445566" },
                1f);

            Assert.AreEqual("#112233", level.CellAt(0, 0).ToHex());
            Assert.AreEqual("#445566", level.CellAt(1, 0).ToHex());
            Assert.AreEqual("#445566", level.CellAt(0, 1).ToHex());
        }

        [Test]
        public void TryFromRows_RejectsRaggedRows()
        {
            var built = LevelBuilder.TryFromRows(
                "ko",
                "KO",
                new[] { "11", "1" },
                new[] { "#FFFFFF" },
                1f,
                out _,
                out var error);

            Assert.IsFalse(built);
            Assert.IsNotNull(error);
        }

        [Test]
        public void TryFromRows_RejectsUnknownCharacter()
        {
            var built = LevelBuilder.TryFromRows(
                "ko",
                "KO",
                new[] { "1X" },
                new[] { "#FFFFFF" },
                1f,
                out _,
                out var error);

            Assert.IsFalse(built);
            StringAssert.Contains("X", error);
        }

        [Test]
        public void TryFromRows_RejectsPaletteIndexOutOfRange()
        {
            var built = LevelBuilder.TryFromRows(
                "ko",
                "KO",
                new[] { "12" },
                new[] { "#FFFFFF" },
                1f,
                out _,
                out _);

            Assert.IsFalse(built);
        }

        [Test]
        public void TryFromRows_RejectsEmptyPattern()
        {
            var built = LevelBuilder.TryFromRows(
                "ko",
                "KO",
                new[] { "..", ".." },
                new[] { "#FFFFFF" },
                1f,
                out _,
                out _);

            Assert.IsFalse(built);
        }

        [Test]
        public void TryFromRows_RejectsInvalidHexColor()
        {
            var built = LevelBuilder.TryFromRows(
                "ko",
                "KO",
                new[] { "1" },
                new[] { "pas-une-couleur" },
                1f,
                out _,
                out var error);

            Assert.IsFalse(built);
            StringAssert.Contains("palette", error);
        }

        [Test]
        public void FromRows_ThrowsOnInvalidLevel()
        {
            Assert.Throws<System.ArgumentException>(() =>
                LevelBuilder.FromRows("ko", "KO", new[] { "" }, new[] { "#FFFFFF" }, 1f));
        }

        [Test]
        public void TryFromRows_RejectsNonPositiveInterval()
        {
            var built = LevelBuilder.TryFromRows(
                "ko",
                "KO",
                new[] { "1" },
                new[] { "#FFFFFF" },
                0f,
                out _,
                out _);

            Assert.IsFalse(built);
        }
    }
}
