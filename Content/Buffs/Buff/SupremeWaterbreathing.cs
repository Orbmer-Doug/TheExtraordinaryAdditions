using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.Buffs.Buff;

public class SupremeWaterbreathing : ModBuff
{
    public override string Texture => AssetRegistry.GennedTextures.SupremeWaterbreathing.Path;

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = false;
        Main.buffNoSave[Type] = false;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        player.AdditionsBuffs().BigOxygen = true;
        player.breath = player.breathMax + 91;
    }
}
