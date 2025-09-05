using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Summoner.Early;

namespace TheExtraordinaryAdditions.Content.Buffs.Summon;

public class FlockOfShields : ModBuff
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.FlockOfShields);

    public override void SetStaticDefaults()
    {
        Main.buffNoSave[Type] = true;
        Main.buffNoTimeDisplay[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        if (player.ownedProjectileCounts[ModContent.ProjectileType<EnchantedShield>()] > 0)
        {
            player.buffTime[buffIndex] = 18000;
        }
        else
        {
            player.DelBuff(buffIndex);
            buffIndex--;
        }

    }
}
