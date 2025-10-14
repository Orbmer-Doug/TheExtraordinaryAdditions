using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.Buffs.Debuff;

public class EternalRestCooldown : ModBuff
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.EternalRestCooldown);

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.pvpBuff[Type] = true;
        Main.buffNoSave[Type] = false;
        BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
    }
}
