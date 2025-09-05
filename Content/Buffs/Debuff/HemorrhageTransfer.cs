using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.Buffs.Debuff;

public class HemorrhageTransfer : ModBuff
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.HemorrhageTransfer);

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = true;
        Main.buffNoTimeDisplay[Type] = true;
        BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        Main.pvpBuff[Type] = false;
    }
}