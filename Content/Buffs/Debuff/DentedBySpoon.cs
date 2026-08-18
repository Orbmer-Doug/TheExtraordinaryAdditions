using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Globals.NPCGlobal;

namespace TheExtraordinaryAdditions.Content.Buffs.Debuff;

public class DentedBySpoon : ModBuff
{
    public override string Texture => AssetRegistry.GennedTextures.DentedBySpoon.Path;

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
        player.AdditionsBuffs().DentedBySpoon = true;
        player.statDefense *= .75f;
    }
}
