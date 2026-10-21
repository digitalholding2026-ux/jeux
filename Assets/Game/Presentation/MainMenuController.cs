using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ColorFlow.Game.Presentation
{
    /// <summary>Menu principal : choix parmi les niveaux placeholder.</summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        private void Awake()
        {
            UiFactory.EnsureEventSystem();

            var canvas = UiFactory.CreateCanvas("Canvas_Menu");
            UiFactory.CreateImage("Background", canvas.transform, UiFactory.Background);

            var title = UiFactory.CreateText("Title", canvas.transform, "COLOR FLOW", 132, UiFactory.TextPrimary);
            UiFactory.SetRect(
                title.rectTransform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -240f),
                new Vector2(980f, 180f));

            var subtitle = UiFactory.CreateText("Subtitle", canvas.transform, "Choisis un motif", 54, UiFactory.TextMuted);
            UiFactory.SetRect(
                subtitle.rectTransform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -420f),
                new Vector2(900f, 80f));

            BuildLevelButtons(canvas.transform);

            var footer = UiFactory.CreateText(
                "Footer",
                canvas.transform,
                "Prototype Sprint 0 — données placeholder",
                38,
                UiFactory.TextMuted);
            UiFactory.SetRect(
                footer.rectTransform,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 70f),
                new Vector2(980f, 60f));
        }

        private static void BuildLevelButtons(Transform parent)
        {
            var levels = LevelCatalog.All();

            if (levels.Length == 0)
            {
                var error = UiFactory.CreateText(
                    "NoLevels",
                    parent,
                    "Aucun niveau dans Resources/Levels",
                    44,
                    UiFactory.Accent);
                UiFactory.SetRect(
                    error.rectTransform,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    Vector2.zero,
                    new Vector2(900f, 120f));
                return;
            }

            const float height = 150f;
            const float gap = 40f;
            var total = levels.Length * height + (levels.Length - 1) * gap;
            var start = total * 0.5f - height * 0.5f;

            for (var i = 0; i < levels.Length; i++)
            {
                var level = levels[i];
                var button = UiFactory.CreateButton(
                    "Level_" + level.name,
                    parent,
                    level.DisplayName,
                    UiFactory.Panel,
                    UiFactory.TextPrimary,
                    56);

                UiFactory.SetRect(
                    button.GetComponent<RectTransform>(),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0f, start - i * (height + gap) + 60f),
                    new Vector2(760f, height));

                var captured = level;
                button.onClick.AddListener(() =>
                {
                    LevelSelection.Current = captured;
                    SceneManager.LoadScene("Game");
                });
            }
        }
    }
}
