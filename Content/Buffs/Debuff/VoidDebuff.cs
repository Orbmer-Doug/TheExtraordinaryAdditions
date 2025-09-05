using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.Buffs.Debuff;

public class VoidDebuff : ModBuff
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.VoidDebuff);
    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = true;
        Main.buffNoTimeDisplay[Type] = true;
        BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
    }

    public override void Update(NPC npc, ref int buffIndex)
    {
        if (npc.GetGlobalNPC<AdditionsGlobalNPC>().VoidEnergy < npc.buffTime[buffIndex])
        {
            npc.GetGlobalNPC<AdditionsGlobalNPC>().VoidEnergy = npc.buffTime[buffIndex];
        }
        npc.DelBuff(buffIndex);
        buffIndex--;
    }
}