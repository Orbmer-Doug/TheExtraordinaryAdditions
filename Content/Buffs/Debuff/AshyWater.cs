using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.Buffs.Debuff;

public class AshyWater : ModBuff
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.AshyWater);

    public override void SetStaticDefaults()
    {
        Main.buffNoTimeDisplay[Type] = true;
        Main.pvpBuff[Type] = true;
        Main.debuff[Type] = true;
        BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
    }

    public override void Update(NPC npc, ref int buffIndex)
    {
        npc.GetGlobalNPC<AdditionsGlobalNPC>().ashy = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        player.GetModPlayer<GlobalPlayer>().ashy = true;
    }
}