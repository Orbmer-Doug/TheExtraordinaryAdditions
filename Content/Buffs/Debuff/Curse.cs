using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.Buffs.Debuff;

public class Curse : ModBuff
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Curse);

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = true;
        Main.buffNoTimeDisplay[Type] = true;
        BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;

        Main.pvpBuff[Type] = true; // This buff can be applied by other players in Pvp, so we need this to be true.
    }

    public override void Update(NPC npc, ref int buffIndex)
    {
        if (npc.GetGlobalNPC<AdditionsGlobalNPC>().Cursed < npc.buffTime[buffIndex])
        {
            npc.GetGlobalNPC<AdditionsGlobalNPC>().Cursed = npc.buffTime[buffIndex];
        }
        npc.DelBuff(buffIndex);
        buffIndex--;
    }
}