using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Summoner.Late.Avia;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.Buffs.Summon;

public class AvragenPresence : ModBuff
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.AvragenPresence);

    public override void SetStaticDefaults()
    {
        Main.buffNoTimeDisplay[Type] = true;
        Main.buffNoSave[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        GlobalPlayer modded = player.GetModPlayer<GlobalPlayer>();
        if (player.ownedProjectileCounts[ModContent.ProjectileType<AvragenMinion>()] > 0)
            modded.Avragen = true;
        if (!modded.Avragen)
        {
            player.DelBuff(buffIndex);
            buffIndex--;
        }
        else
            player.buffTime[buffIndex] = 18000;
    }
}
