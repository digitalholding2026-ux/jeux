using System.Collections;
using System.Linq;
using ColorFlow.Game.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ColorFlow.Tests.PlayMode
{
    /// <summary>
    /// Smoke tests de la scène Game : le drag &amp; drop depuis le tapis pose un
    /// blob, trois blobs alignés peignent le motif, et le redémarrage remet la
    /// grille à zéro.
    /// </summary>
    public sealed class GameSceneSmokeTests
    {
        [UnityTest]
        public IEnumerator GameScene_Loads_AndDragFillsACell()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;

            var controller = Object.FindFirstObjectByType<GameSceneController>();
            Assert.IsNotNull(controller, "GameSceneController absent de la scène Game.");

            controller.SpawnCreatureForTest();
            yield return null;

            var button = controller.GetCreatureButton(0);
            Assert.IsNotNull(button, "Aucune créature visible sur le tapis roulant.");

            var placed = controller.DragFirstCreatureToValidCellForTest();
            yield return null;

            Assert.IsTrue(placed, "Le glisser-déposer n'a pas posé de blob.");
            Assert.GreaterOrEqual(controller.Game.Grid.OccupiedCount, 1, "La grille ne contient aucun blob.");
        }

        [UnityTest]
        public IEnumerator GameScene_ThreeAlignedBlobs_PaintTheMotif()
        {
            var cross = LevelCatalog.All().FirstOrDefault(level => level.LevelId == "croix");
            Assert.IsNotNull(cross, "Niveau « croix » introuvable.");
            LevelSelection.Current = cross;

            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;

            var controller = Object.FindFirstObjectByType<GameSceneController>();
            Assert.IsNotNull(controller);

            for (var i = 0; i < 3; i++)
            {
                controller.SpawnCreatureForTest();
                yield return null;
                Assert.IsTrue(controller.DragFirstCreatureToValidCellForTest(), "Drag refusé.");
            }

            yield return null;

            Assert.GreaterOrEqual(controller.PaintedPixels, 3, "Trois blobs alignés doivent peindre trois cases.");
        }

        [UnityTest]
        public IEnumerator GameScene_Restart_ResetsTheGrid()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;

            var controller = Object.FindFirstObjectByType<GameSceneController>();
            Assert.IsNotNull(controller);

            controller.SpawnCreatureForTest();
            yield return null;
            Assert.IsTrue(controller.DragFirstCreatureToValidCellForTest());
            yield return null;
            Assert.GreaterOrEqual(controller.Game.Grid.OccupiedCount, 1);

            controller.RestartGame();
            yield return null;

            Assert.AreEqual(0, controller.PaintedPixels);
            Assert.AreEqual(0, controller.Game.Grid.OccupiedCount);
            Assert.AreEqual(0, controller.Game.Conveyor.Count);
        }
    }
}
