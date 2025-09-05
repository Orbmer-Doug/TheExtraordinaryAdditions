using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.Buffs.Buff;

public class WinterHeart : ModBuff
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.WinterHeart);

    public override void SetStaticDefaults()
    {
        Main.buffNoSave[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        player.GetModPlayer<GlobalPlayer>().frigidTonic = true;
        player.buffImmune[BuffID.Chilled] = true;
        player.endurance += 0.05f;
    }
}