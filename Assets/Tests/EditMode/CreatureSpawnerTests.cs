using System.Collections.Generic;
using ColorFlow.Core;
using NUnit.Framework;

namespace ColorFlow.Tests.EditMode
{
    public sealed class CreatureSpawnerTests
    {
        private static LevelDefinition Level()
        {
            return LevelBuilder.FromRows(
                "test",
                "Test",
                new[]
                {
                    "11",
                    "22"
                },
                new[] { "#FF0000", "#00FF00" },
                0.5f);
        }

        [Test]
        public void SpawnNow_UsesAColorFromTheRemainingPattern()
        {
            var grid = new GridModel(Level());
            var spawner = new CreatureSpawner(0.5f);
            var random = new RandomSource(42);

            var creature = spawner.SpawnNow(grid, random);

            Assert.IsNotNull(creature);
            Assert.IsTrue(grid.HasTargetAt(0, 0));
            Assert.IsTrue(creature.Color.Equals(grid.TargetColorAt(0, 0)) ||
                          creature.Color.Equals(grid.TargetColorAt(0, 1)));
        }

        [Test]
        public void SpawnNow_IdsAreIncreasing()
        {
            var grid = new GridModel(Level());
            var spawner = new CreatureSpawner(0.5f);
            var random = new RandomSource(1);

            var first = spawner.SpawnNow(grid, random);
            var second = spawner.SpawnNow(grid, random);

            Assert.AreEqual(first.Id + 1, second.Id);
        }

        [Test]
        public void Tick_SpawnsAccordingToInterval()
        {
            var grid = new GridModel(Level());
            var spawner = new CreatureSpawner(0.5f);
            var spawned = new List<Creature>();
            spawner.Spawned += c => spawned.Add(c);

            spawner.Tick(0.6f, grid, new RandomSource(7));
            Assert.AreEqual(1, spawned.Count);

            spawner.Tick(0.5f, grid, new RandomSource(7));
            Assert.AreEqual(2, spawned.Count);

            spawner.Tick(0.1f, grid, new RandomSource(7));
            Assert.AreEqual(2, spawned.Count);
        }

        [Test]
        public void Tick_WhenDisabled_DoesNotSpawn()
        {
            var grid = new GridModel(Level());
            var spawner = new CreatureSpawner(0.5f) { Enabled = false };
            var spawned = new List<Creature>();
            spawner.Spawned += c => spawned.Add(c);

            spawner.Tick(5f, grid, new RandomSource(3));

            Assert.AreEqual(0, spawned.Count);
        }

        [Test]
        public void SpawnNow_ReturnsNullWhenPatternIsComplete()
        {
            var level = Level();
            var grid = new GridModel(level);
            for (var y = 0; y < grid.Height; y++)
            {
                for (var x = 0; x < grid.Width; x++)
                {
                    grid.TryPaint(x, y, grid.TargetColorAt(x, y));
                }
            }

            var spawner = new CreatureSpawner(0.5f);

            Assert.IsNull(spawner.SpawnNow(grid, new RandomSource(9)));
        }

        [Test]
        public void Interval_IsClampedToPositiveValue()
        {
            var spawner = new CreatureSpawner(-1f);

            Assert.Greater(spawner.Interval, 0f);
        }
    }
}
