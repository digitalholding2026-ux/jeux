using System;

namespace ColorFlow.Core
{
    /// <summary>
    /// Construit un <see cref="LevelDefinition"/> à partir d'une description texte.
    /// Format : une ligne de texte par rangée, '.' = pas de pixel, '1'..'9' = index
    /// dans la palette (1 = palette[0]).
    /// </summary>
    public static class LevelBuilder
    {
        public static LevelDefinition FromRows(
            string id,
            string displayName,
            string[] rows,
            string[] paletteHex,
            float spawnInterval)
        {
            if (!TryFromRows(id, displayName, rows, paletteHex, spawnInterval, out var level, out var error))
            {
                throw new ArgumentException(error);
            }

            return level;
        }

        public static bool TryFromRows(
            string id,
            string displayName,
            string[] rows,
            string[] paletteHex,
            float spawnInterval,
            out LevelDefinition level,
            out string error)
        {
            level = null;

            if (rows == null || rows.Length == 0)
            {
                error = "Le niveau ne contient aucune ligne.";
                return false;
            }

            var height = rows.Length;
            var width = rows[0] != null ? rows[0].Length : 0;
            if (width == 0)
            {
                error = "La première ligne est vide.";
                return false;
            }

            for (var y = 0; y < height; y++)
            {
                if (rows[y] == null || rows[y].Length != width)
                {
                    error = "La ligne " + y + " n'a pas la même largeur que la première (" + width + ").";
                    return false;
                }
            }

            var palette = new RgbaColor[paletteHex != null ? paletteHex.Length : 0];
            for (var i = 0; i < palette.Length; i++)
            {
                if (!RgbaColor.TryFromHex(paletteHex[i], out palette[i]) || palette[i].IsTransparent)
                {
                    error = "Couleur de palette invalide à l'index " + i + " : '" + paletteHex[i] + "'.";
                    return false;
                }
            }

            var cells = new RgbaColor[width * height];
            var targets = 0;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var c = rows[y][x];
                    if (c == '.')
                    {
                        continue;
                    }

                    if (c < '1' || c > '9')
                    {
                        error = "Caractère inattendu '" + c + "' en (" + x + ", " + y + "). Utilisez '.' ou '1'..'9'.";
                        return false;
                    }

                    var paletteIndex = c - '1';
                    if (paletteIndex >= palette.Length)
                    {
                        error = "Le chiffre '" + c + "' renvoie à une couleur absente de la palette.";
                        return false;
                    }

                    cells[y * width + x] = palette[paletteIndex];
                    targets++;
                }
            }

            if (targets == 0)
            {
                error = "Le niveau ne contient aucun pixel cible.";
                return false;
            }

            if (spawnInterval <= 0f)
            {
                error = "L'intervalle de spawn doit être strictement positif.";
                return false;
            }

            level = new LevelDefinition(id, displayName, width, height, cells, palette, spawnInterval);
            error = null;
            return true;
        }
    }
}
