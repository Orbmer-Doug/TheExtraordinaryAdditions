using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Debuff;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.Buffs.Buff;

public class EternalRest : ModBuff
{
    public override string Texture => AssetRegistry.GennedTextures.EternalRest.Path;

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.pvpBuff[Type] = true;
        Main.buffNoSave[Type] = false;
        BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        player.AdditionsBuffs().EternalRested = true;
        player.AddBuff(ModContent.BuffType<EternalRestCooldown>(), SecondsToFrames(30));
    }
}
