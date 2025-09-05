using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Debuff;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Buffs.Buff;

public class EternalRest : ModBuff
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.EternalRest);

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.pvpBuff[Type] = true;
        Main.buffNoSave[Type] = false;
        BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        player.Additions().EternalRested = true;
        player.AddBuff(ModContent.BuffType<EternalRestCooldown>(), SecondsToFrames(30));
    }
}
