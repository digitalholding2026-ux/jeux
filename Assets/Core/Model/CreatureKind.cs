namespace ColorFlow.Core
{
    /// <summary>
    /// Type d'une créature. Sprint 2 : trois créatures spéciales s'ajoutent aux
    /// créatures normales, avec des effets de peinture distincts.
    /// </summary>
    public enum CreatureKind
    {
        /// <summary>Créature classique : peint le prochain pixel de sa couleur.</summary>
        Normal = 0,

        /// <summary>Bombe : peint un carré de 3×3 pixels autour du prochain pixel ciblé.</summary>
        Bomb = 1,

        /// <summary>Arc-en-ciel : peint le prochain pixel cible, quelle que soit sa couleur.</summary>
        Rainbow = 2,

        /// <summary>Ligne : peint toute une ligne ou toute une colonne.</summary>
        Line = 3
    }
}
