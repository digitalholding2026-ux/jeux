using UnityEngine;

namespace ColorFlow.Game.Data
{
    /// <summary>
    /// Méta-progression persistée avec PlayerPrefs : pièces d'or, skins possédés
    /// et skin équipé. Aucune sauvegarde de partie, uniquement l'économie.
    /// </summary>
    public static class MetaProgress
    {
        private const string CoinsKey = "colorflow.coins";
        private const string SelectedSkinKey = "colorflow.skin.selected";
        private const string OwnedPrefix = "colorflow.skin.owned.";

        /// <summary>Solde courant en pièces d'or.</summary>
        public static int Coins => PlayerPrefs.GetInt(CoinsKey, 0);

        public static void AddCoins(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            PlayerPrefs.SetInt(CoinsKey, Coins + amount);
            PlayerPrefs.Save();
        }

        public static bool Owns(SkinId skin)
        {
            if (skin == SkinId.Classic)
            {
                return true;
            }

            return PlayerPrefs.GetInt(OwnedPrefix + (int)skin, 0) == 1;
        }

        /// <summary>Tente d'acheter un skin. Retourne false si déjà possédé ou trop cher.</summary>
        public static bool TryBuy(SkinId skin)
        {
            if (Owns(skin))
            {
                return false;
            }

            var info = SkinCatalog.Get(skin);
            if (info == null || info.Price <= 0 || Coins < info.Price)
            {
                return false;
            }

            PlayerPrefs.SetInt(OwnedPrefix + (int)skin, 1);
            PlayerPrefs.SetInt(CoinsKey, Coins - info.Price);
            PlayerPrefs.Save();
            return true;
        }

        public static SkinId SelectedSkin
        {
            get
            {
                var value = PlayerPrefs.GetInt(SelectedSkinKey, (int)SkinId.Classic);
                var skin = (SkinId)value;
                return Owns(skin) ? skin : SkinId.Classic;
            }
            set
            {
                var skin = Owns(value) ? value : SkinId.Classic;
                PlayerPrefs.SetInt(SelectedSkinKey, (int)skin);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Utilisé par les tests PlayMode pour repartir d'un état propre.</summary>
        public static void ResetAll()
        {
            PlayerPrefs.DeleteKey(CoinsKey);
            PlayerPrefs.DeleteKey(SelectedSkinKey);
            for (var i = 0; i < SkinCatalog.All.Length; i++)
            {
                PlayerPrefs.DeleteKey(OwnedPrefix + (int)SkinCatalog.All[i].Id);
            }

            PlayerPrefs.Save();
        }
    }
}
