using ColorFlow.Game.Data;
using UnityEngine;

namespace ColorFlow.Game.Presentation
{
    /// <summary>
    /// Charge les niveaux placeholders depuis Resources/Levels.
    /// Sprint 0 : évite tout câblage manuel de références dans les scènes.
    /// </summary>
    public static class LevelCatalog
    {
        private static LevelData[] _levels;

        public static LevelData[] All()
        {
            if (_levels == null || _levels.Length == 0)
            {
                var loaded = Resources.LoadAll<LevelData>("Levels");
                System.Array.Sort(loaded, (a, b) => string.CompareOrdinal(a.name, b.name));
                _levels = loaded;
            }

            return _levels;
        }

        public static LevelData First()
        {
            var all = All();
            return all.Length > 0 ? all[0] : null;
        }
    }
}
