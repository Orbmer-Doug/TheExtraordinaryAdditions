using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.Buffs.Buff;

public class DesertsBlessing : ModBuff
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.DesertsBlessing);

    public override void SetStaticDefaults()
    {
        Main.buffNoSave[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        player.Additions().AridFlask = true;
    }
}
