using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.Buffs.Debuff;
public class MyceliumCooldown : ModBuff
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.MyceliumCooldown);

    public override void SetStaticDefaults()
    {
        Main.buffNoTimeDisplay[Type] = false;
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = false;
        BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
    }
}
