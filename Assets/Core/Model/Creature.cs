using System;

namespace ColorFlow.Core
{
    /// <summary>
    /// Créature présente sur le tapis roulant.
    ///
    /// <see cref="Color"/> est la couleur « métier » : elle pilote la règle
    /// d'équité (une créature n'apparaît que si des pixels de cette couleur
    /// restent à peindre). Pour une créature spéciale, cette couleur reste
    /// cachée : le joueur voit <see cref="DisplayColor"/>, qui identifie le
    /// pouvoir (bombe, arc-en-ciel, ligne).
    /// </summary>
    public sealed class Creature
    {
        /// <summary>Gris ardoise de la bombe.</summary>
        public static readonly RgbaColor BombDisplayColor = new RgbaColor(74, 81, 99);

        /// <summary>Rose vif de l'arc-en-ciel (base du combo).</summary>
        public static readonly RgbaColor RainbowDisplayColor = new RgbaColor(255, 79, 163);

        /// <summary>Bleu profond de la créature « ligne ».</summary>
        public static readonly RgbaColor LineDisplayColor = new RgbaColor(31, 78, 156);

        public Creature(int id, RgbaColor color)
            : this(id, color, CreatureKind.Normal)
        {
        }

        public Creature(int id, RgbaColor color, CreatureKind kind)
        {
            if (id <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id), "L'identifiant doit être strictement positif.");
            }

            if (color.IsTransparent)
            {
                throw new ArgumentException("Une créature ne peut pas être transparente.", nameof(color));
            }

            Id = id;
            Color = color;
            Kind = kind;
        }

        public int Id { get; }

        /// <summary>Couleur métier, utilisée par la règle d'équité.</summary>
        public RgbaColor Color { get; }

        public CreatureKind Kind { get; }

        public bool IsSpecial => Kind != CreatureKind.Normal;

        /// <summary>Couleur réellement affichée sur le tapis.</summary>
        public RgbaColor DisplayColor
        {
            get
            {
                switch (Kind)
                {
                    case CreatureKind.Bomb:
                        return BombDisplayColor;
                    case CreatureKind.Rainbow:
                        return RainbowDisplayColor;
                    case CreatureKind.Line:
                        return LineDisplayColor;
                    default:
                        return Color;
                }
            }
        }

        /// <summary>
        /// Identité de combo : deux créatures de la même couleur affichée
        /// enchaînent un combo (deux bombes combotent donc entre elles).
        /// </summary>
        public RgbaColor ComboColor => DisplayColor;

        public override string ToString()
        {
            return "Creature#" + Id + " " + Kind + " " + Color.ToHex();
        }
    }
}
