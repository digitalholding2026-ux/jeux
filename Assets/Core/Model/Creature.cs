using System;

namespace ColorFlow.Core
{
    /// <summary>Créature présente sur le tapis roulant. Elle porte une couleur unique à peindre.</summary>
    public sealed class Creature
    {
        public Creature(int id, RgbaColor color)
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
        }

        public int Id { get; }

        public RgbaColor Color { get; }

        public override string ToString()
        {
            return "Creature#" + Id + " " + Color.ToHex();
        }
    }
}
