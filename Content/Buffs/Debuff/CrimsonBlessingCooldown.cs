using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.Buffs.Debuff;

public class CrimsonBlessingCooldown : ModBuff
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CrimsonBlessingCooldown);

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.pvpBuff[Type] = true;
        Main.buffNoSave[Type] = false;
        BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
    }
}
