using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace SPYoyoMod.Common.Configs
{
    public class ServerSideConfig : ModConfig
    {
        public override ConfigScope Mode { get => ConfigScope.ServerSide; }

        [DefaultValue(true)]
        [ReloadRequired]
        public bool ReworkedVanillaYoyos { get; set; }

        [DefaultValue(true)]
        [ReloadRequired]
        public bool ReworkedYoyoUseStyle { get; set; }
    }
}