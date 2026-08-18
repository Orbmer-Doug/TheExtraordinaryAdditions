using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Summoner.Late;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Globals.PlayerGlobal;

namespace TheExtraordinaryAdditions.Content.Buffs.Summon;

public class LittleStar : ModBuff
{
    public override string Texture => AssetRegistry.GennedTextures.LittleStar.Path;

    public override void SetStaticDefaults()
    {
        Main.buffNoSave[Type] = true;
        Main.buffNoTimeDisplay[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        PlayerMinion modded = player.AdditionsMinion();
        if (player.ownedProjectileCounts[ModContent.ProjectileType<LivingStarFlareMinion>()] > 0)
            modded.Flare = true;
        if (!modded.Flare)
        {
            player.DelBuff(buffIndex);
            buffIndex--;
        }
        else
            player.buffTime[buffIndex] = 18000;
    }
}
