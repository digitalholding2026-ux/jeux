using System.Collections;
using ColorFlow.Game.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ColorFlow.Tests.PlayMode
{
    /// <summary>
    /// Smoke test du menu : l'écran se construit par le code, un niveau est
    /// sélectionné et le bouton JOUER lance bien la scène Game (avec fondu).
    /// </summary>
    public sealed class MainMenuSmokeTests
    {
        [UnityTest]
        public IEnumerator MainMenu_Loads_AndStartsGame()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;

            var menu = Object.FindFirstObjectByType<MainMenuController>();
            Assert.IsNotNull(menu, "MainMenuController absent de la scène MainMenu.");
            Assert.IsNotNull(LevelSelection.Current, "Aucun niveau sélectionné au démarrage.");

            var play = GameObject.Find("Play");
            Assert.IsNotNull(play, "Bouton JOUER introuvable.");
            var button = play.GetComponent<Button>();
            Assert.IsNotNull(button, "Le bouton JOUER n'est pas un Button.");

            button.onClick.Invoke();

            var timeout = 5f;
            while (SceneManager.GetActiveScene().name != "Game" && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.AreEqual("Game", SceneManager.GetActiveScene().name, "Le bouton JOUER n'a pas ouvert la scène Game.");
        }
    }
}
