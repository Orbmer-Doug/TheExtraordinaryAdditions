using Terraria;
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
            modded.LaserDrones = true;
        if (!modded.LaserDrones)
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
