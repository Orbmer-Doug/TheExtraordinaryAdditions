using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.Buffs.Debuff;
public class FulminationCooldown : ModBuff
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.FulminationCooldown);

    public override void SetStaticDefaults()
    {
        Main.buffNoTimeDisplay[Type] = false;
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = false;
        BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
    }
}
