using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Summoner.Late;
using TheExtraordinaryAdditions.Content.Projectiles.Summoner.Late.Avia;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.Buffs.Summon;

public class LittleStar : ModBuff
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.LittleStar);

    public override void SetStaticDefaults()
    {
        Main.buffNoSave[Type] = true;
        Main.buffNoTimeDisplay[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        GlobalPlayer modded = player.GetModPlayer<GlobalPlayer>();
        if (player.ownedProjectileCounts[ModContent.ProjectileType<LivingStarFlareMinion>()] > 0)
            modded.Minion[GlobalPlayer.AdditionsMinion.Flare] = true;
        if (!modded.Minion[GlobalPlayer.AdditionsMinion.Flare])
        {
            player.DelBuff(buffIndex);
            buffIndex--;
        }
        else
            player.buffTime[buffIndex] = 18000;
    }
}
