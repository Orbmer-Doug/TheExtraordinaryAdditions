using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Summoner.Middle;

namespace TheExtraordinaryAdditions.Content.Buffs.Summon;

public class FlockOfRazorShields : ModBuff
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.FlockOfRazorShields);

    public override void SetStaticDefaults()
    {
        Main.buffNoSave[Type] = true;
        Main.buffNoTimeDisplay[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        if (player.ownedProjectileCounts[ModContent.ProjectileType<WitheredShredderShield>()] > 0)
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
