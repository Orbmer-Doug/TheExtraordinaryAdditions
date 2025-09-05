using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Summoner.Middle;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.Buffs.Summon;

public class LaserDrones : ModBuff
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.LaserDrones);

    public override void SetStaticDefaults()
    {
        Main.buffNoTimeDisplay[Type] = true;
        Main.buffNoSave[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        GlobalPlayer modded = player.GetModPlayer<GlobalPlayer>();
        if (player.ownedProjectileCounts[ModContent.ProjectileType<LazerDrone>()] > 0)
            modded.Minion[GlobalPlayer.AdditionsMinion.LaserDrones] = true;
        if (!modded.Minion[GlobalPlayer.AdditionsMinion.LaserDrones])
        {
            player.DelBuff(buffIndex);
            buffIndex--;
        }
        else
        {
            player.buffTime[buffIndex] = 18000;
        }
    }
}
