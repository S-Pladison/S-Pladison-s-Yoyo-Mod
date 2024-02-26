using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace SPYoyoMod
{
    public static class ModAssets
    {
        public const string Path = $"{nameof(SPYoyoMod)}/Assets/";

        public const string EffectsPath = Path + "Effects/";
        public const string TexturesPath = Path + "Textures/";
        public const string SoundsPath = Path + "Sounds/";

        public const string ItemsPath = TexturesPath + "Items/";
        public const string ProjectilesPath = TexturesPath + "Projectiles/";
        public const string DustsPath = TexturesPath + "Dusts/";
        public const string TilesPath = TexturesPath + "Tiles/";
        public const string MiscPath = TexturesPath + "Misc/";

        public static Asset<Effect> RequestEffect(string name, AssetRequestMode mode = AssetRequestMode.ImmediateLoad)
        {
            return ModContent.Request<Effect>(EffectsPath + name, mode);
        }
    }
}