using UnityEngine;

namespace ColorFlow.Game.Presentation
{
    /// <summary>
    /// Direction artistique du Sprint 1 : thème clair, joyeux, contrasté.
    /// Aucune couleur sombre (ni noir, ni gris foncé, ni marron) hors texte.
    /// </summary>
    public static class JuicePalette
    {
        // --- Décor ---
        public static readonly Color SkyTop = Hex("5FC4F0");
        public static readonly Color SkyBottom = Hex("E8F9FF");
        public static readonly Color Cream = Hex("FFF6E3");
        public static readonly Color Paper = Hex("FFFFFF");

        // --- Texte ---
        public static readonly Color Ink = Hex("2E3A59");
        public static readonly Color InkSoft = Hex("7C8AA6");

        // --- Accents ---
        public static readonly Color Accent = Hex("FF7A45");
        public static readonly Color AccentDeep = Hex("E15B2B");
        public static readonly Color Pink = Hex("FF6FA5");
        public static readonly Color PinkDeep = Hex("E24B84");
        public static readonly Color Sun = Hex("FFC93C");
        public static readonly Color Mint = Hex("3FD0B0");
        public static readonly Color MintDeep = Hex("25AE92");
        public static readonly Color Violet = Hex("9B7BFF");
        public static readonly Color Sky = Hex("4FB8FF");

        // --- Éléments de jeu ---
        public static readonly Color Slot = new Color(0.26f, 0.55f, 0.70f, 0.30f);
        public static readonly Color SlotRim = new Color(1f, 1f, 1f, 0.22f);
        public static readonly Color Belt = new Color(1f, 0.99f, 0.96f, 0.96f);
        public static readonly Color BeltStripe = new Color(0.68f, 0.89f, 0.97f, 0.55f);
        public static readonly Color BeltTray = new Color(0.30f, 0.60f, 0.76f, 0.34f);
        public static readonly Color Gloss = new Color(1f, 1f, 1f, 0.40f);
        public static readonly Color Shadow = new Color(0.08f, 0.20f, 0.34f, 0.18f);
        public static readonly Color ShadowSoft = new Color(0.08f, 0.20f, 0.34f, 0.12f);

        /// <summary>Sept couleurs de blobs, vives et bien séparées.</summary>
        public static readonly Color[] Creatures =
        {
            Hex("FF6B6B"), // corail
            Hex("FF9F45"), // mandarine
            Hex("FFD93D"), // soleil
            Hex("3FD0B0"), // menthe
            Hex("4FB8FF"), // ciel
            Hex("9B7BFF"), // violette
            Hex("FF6FA5")  // bubblegum
        };

        public static Color Hex(string hex)
        {
            if (ColorUtility.TryParseHtmlString("#" + hex, out var color))
            {
                return color;
            }

            return Color.magenta;
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        public static Color Lighten(Color color, float amount)
        {
            return Color.Lerp(color, Color.white, Mathf.Clamp01(amount));
        }

        public static Color Darken(Color color, float amount)
        {
            return Color.Lerp(color, Color.black, Mathf.Clamp01(amount));
        }

        /// <summary>Choix déterministe d'une couleur de blob à partir d'un index.</summary>
        public static Color CreatureColor(int index)
        {
            return Creatures[Mathf.Abs(index) % Creatures.Length];
        }
    }
}
