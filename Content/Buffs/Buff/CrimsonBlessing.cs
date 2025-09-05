using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Debuff;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Buffs.Buff;

public class CrimsonBlessing : ModBuff
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CrimsonBlessing);

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.pvpBuff[Type] = true;
        Main.buffNoSave[Type] = false;
        BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
    }
    public override void Update(Player player, ref int buffIndex)
    {
        player.moveSpeed += .2f;
        player.Additions().CrimsonBlessing = true;
        player.AddBuff(ModContent.BuffType<CrimsonBlessingCooldown>(), SecondsToFrames(50));
    }
}
