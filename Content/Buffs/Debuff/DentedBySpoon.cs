using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.Buffs.Debuff;

public class DentedBySpoon : ModBuff
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.DentedBySpoon);

    public override void SetStaticDefaults()
    {
        Main.pvpBuff[Type] = true;
    }

    public override void Update(NPC npc, ref int buffIndex)
    {
        npc.GetGlobalNPC<AdditionsGlobalNPC>().DentedBySpoon = true;
    }
    public override void Update(Player player, ref int buffIndex)
    {
        player.GetModPlayer<GlobalPlayer>().DentedBySpoon = true;
        player.statDefense *= .75f;
    }
}