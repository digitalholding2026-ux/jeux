namespace ColorFlow.Game.Data
{
    /// <summary>Identifiant des skins de blobs (accessoires dessinés par le code).</summary>
    public enum SkinId
    {
        Classic = 0,
        Glasses = 1,
        Hat = 2,
        Crown = 3
    }

    /// <summary>Description d'un skin : nom affiché et prix en pièces.</summary>
    public sealed class SkinInfo
    {
        public SkinInfo(SkinId id, string displayName, int price, string description)
        {
            Id = id;
            DisplayName = displayName;
            Price = price;
            Description = description;
        }

        public SkinId Id { get; }

        public string DisplayName { get; }

        public int Price { get; }

        public string Description { get; }
    }

    /// <summary>Catalogue statique de la boutique (aucun asset).</summary>
    public static class SkinCatalog
    {
        public static readonly SkinInfo[] All =
        {
            new SkinInfo(SkinId.Classic, "Classique", 0, "Le blob d'origine"),
            new SkinInfo(SkinId.Glasses, "Lunettes", 120, "Un look de génie"),
            new SkinInfo(SkinId.Hat, "Chapeau", 200, "Prêt pour l'aventure"),
            new SkinInfo(SkinId.Crown, "Couronne", 320, "Le roi des blobs")
        };

        public static SkinInfo Get(SkinId id)
        {
            for (var i = 0; i < All.Length; i++)
            {
                if (All[i].Id == id)
                {
                    return All[i];
                }
            }

            return null;
        }
    }
}
