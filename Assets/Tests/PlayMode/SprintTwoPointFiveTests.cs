using System.Collections;
using System.Linq;
using ColorFlow.Game.Data;
using ColorFlow.Game.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ColorFlow.Tests.PlayMode
{
    /// <summary>
    /// Vérifications PlayMode du hotfix Sprint 2.5 : aire sûre du menu, feedback
    /// de boutique, persistance du skin et révélation de l'œuf de Pâques.
    /// </summary>
    public sealed class SprintTwoPointFiveTests
    {
        [UnityTest]
        public IEnumerator Menu_Buttons_AreInsideTheSafeArea()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;

            var safe = GameObject.Find("SafeArea")?.GetComponent<RectTransform>();
            Assert.IsNotNull(safe, "Aucune aire sûre dans le menu.");

            AssertInside(safe, GameObject.Find("Title"), "titre");
            AssertInside(safe, GameObject.Find("Play"), "bouton JOUER");
            AssertInside(safe, GameObject.Find("Shop"), "bouton BOUTIQUE");
            AssertInside(safe, GameObject.Find("Hint"), "carte d'aide");
            AssertInside(safe, GameObject.Find("Wallet"), "portefeuille");
        }

        [UnityTest]
        public IEnumerator Shop_WithoutEnoughCoins_ShowsRedFeedbackAndShakes()
        {
            MetaProgress.ResetAll();
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;

            GameObject.Find("Shop").GetComponent<Button>().onClick.Invoke();
            yield return null;

            var row = GameObject.Find("Row_Glasses");
            Assert.IsNotNull(row, "Ligne du skin Lunettes introuvable.");
            var action = row.transform.Find("Action").GetComponent<Button>();
            Assert.IsTrue(action.interactable, "Le bouton doit rester cliquable pour afficher le feedback.");

            action.onClick.Invoke();
            yield return null;

            var feedback = GameObject.Find("Feedback")?.GetComponent<Text>();
            Assert.IsNotNull(feedback, "Texte de feedback absent de la boutique.");
            Assert.AreEqual("Pas assez de pièces !", feedback.text);
            Assert.AreEqual(new Color(0.886f, 0.294f, 0.518f, 1f).r, feedback.color.r, 0.02f);
            Assert.Greater(feedback.GetComponent<CanvasGroup>().alpha, 0.5f);
            Assert.IsFalse(MetaProgress.Owns(SkinId.Glasses));
        }

        [UnityTest]
        public IEnumerator Shop_Purchase_DeductsCoinsAndEquipsTheSkin()
        {
            MetaProgress.ResetAll();
            MetaProgress.AddCoins(1000);
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;

            GameObject.Find("Shop").GetComponent<Button>().onClick.Invoke();
            yield return null;

            var row = GameObject.Find("Row_Hat");
            Assert.IsNotNull(row);
            row.transform.Find("Action").GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.IsTrue(MetaProgress.Owns(SkinId.Hat));
            Assert.AreEqual(SkinId.Hat, MetaProgress.SelectedSkin);
            Assert.AreEqual(1000 - SkinCatalog.Get(SkinId.Hat).Price, MetaProgress.Coins);

            var feedback = GameObject.Find("Feedback")?.GetComponent<Text>();
            Assert.IsNotNull(feedback);
            StringAssert.Contains("débloqué", feedback.text);
        }

        [UnityTest]
        public IEnumerator EquippedSkin_IsRestoredOnTheNextGameLaunch()
        {
            MetaProgress.ResetAll();
            MetaProgress.AddCoins(1000);
            Assert.IsTrue(MetaProgress.TryBuy(SkinId.Crown));
            MetaProgress.SelectedSkin = SkinId.Crown;

            LevelSelection.Current = LevelCatalog.All().First(level => level.LevelId == "damier");
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;

            var controller = Object.FindFirstObjectByType<GameSceneController>();
            Assert.IsNotNull(controller);

            for (var i = 0; i < 5; i++)
            {
                controller.SpawnCreatureForTest();
                yield return null;
            }

            var anyAccessory = false;
            for (var i = 0; i < 5; i++)
            {
                var accessory = controller.GetCreatureAccessory(i);
                if (accessory != null && accessory.enabled)
                {
                    anyAccessory = true;
                }
            }

            Assert.AreEqual(SkinId.Crown, MetaProgress.SelectedSkin);
            Assert.IsTrue(anyAccessory, "Le skin équipé n'apparaît pas sur les blobs au relancement.");
        }

        [UnityTest]
        public IEnumerator EasterEgg_StaysHidden_UntilThreeWins()
        {
            MetaProgress.ResetAll();
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;

            Assert.AreEqual(6, CountChips(), "L'œuf de Pâques ne doit pas être visible au départ.");

            MetaProgress.RecordWin("croix");
            MetaProgress.RecordWin("damier");
            MetaProgress.RecordWin("coeur");
            Assert.AreEqual(3, MetaProgress.TotalWins);

            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;

            Assert.AreEqual(7, CountChips(), "L'œuf de Pâques doit apparaître après 3 victoires.");
        }

        private static int CountChips()
        {
            var safe = GameObject.Find("SafeArea");
            Assert.IsNotNull(safe);
            return safe.GetComponentsInChildren<RectTransform>(false)
                .Count(rect => rect.name.StartsWith("Chip_"));
        }

        private static void AssertInside(RectTransform area, GameObject element, string label)
        {
            Assert.IsNotNull(element, "Élément introuvable : " + label);
            var rect = element.GetComponent<RectTransform>();
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);

            for (var i = 0; i < corners.Length; i++)
            {
                var local = area.InverseTransformPoint(corners[i]);
                Assert.IsTrue(
                    area.rect.Contains(local),
                    "« " + label + " » dépasse de l'aire sûre (coin " + i + ").");
            }
        }
    }
}
