using System.Collections;
using ColorFlow.Game.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ColorFlow.Tests.PlayMode
{
    /// <summary>Smoke test : la scène Game se charge et un tap remplit un pixel.</summary>
    public sealed class GameSceneSmokeTests
    {
        [UnityTest]
        public IEnumerator GameScene_Loads_AndTapFillsAPixel()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;

            var controller = Object.FindFirstObjectByType<GameSceneController>();
            Assert.IsNotNull(controller, "GameSceneController absent de la scène Game.");

            controller.SpawnCreatureForTest();
            yield return null;

            var before = controller.PaintedPixels;
            var button = controller.GetCreatureButton(0);
            Assert.IsNotNull(button, "Aucune créature visible sur le tapis roulant.");

            var pointer = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            yield return null;

            Assert.AreEqual(before + 1, controller.PaintedPixels, "Le tap n'a pas rempli de pixel.");
            Assert.AreEqual(0, controller.Game.Conveyor.Count, "La créature n'a pas quitté le tapis.");
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
            controller.TapFirstCreatureForTest();
            yield return null;
            Assert.Greater(controller.PaintedPixels, 0);

            controller.RestartGame();
            yield return null;

            Assert.AreEqual(0, controller.PaintedPixels);
            Assert.AreEqual(0, controller.Game.Conveyor.Count);
        }
    }
}
