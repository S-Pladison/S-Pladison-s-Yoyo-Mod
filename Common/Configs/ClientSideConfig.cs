using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace SPYoyoMod.Common.Configs
{
    public class ClientSideConfig : ModConfig
    {
        public override ConfigScope Mode { get => ConfigScope.ClientSide; }

        [DefaultValue(true)]
        public bool ReworkedYoyoUseStyle { get; set; }
    }
}