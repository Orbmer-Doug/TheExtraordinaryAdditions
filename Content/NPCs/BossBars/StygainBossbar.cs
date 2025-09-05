using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;

namespace TheExtraordinaryAdditions.Content.NPCs.BossesBars;

public class StygainBossbar : ModBossBar
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.StygainBossbar);
    public override Asset<Texture2D> GetIconTexture(ref Rectangle? iconFrame)
    {
        return ModContent.Request<Texture2D>(AssetRegistry.GetTexturePath(AdditionsTexture.StygainHeart_Head_Boss));
    }

    public override bool PreDraw(SpriteBatch sb, NPC npc, ref BossBarDrawParams drawParams)
    {
        float lifePercent = drawParams.Life / drawParams.LifeMax;

        return base.PreDraw(sb, npc, ref drawParams);
    }
}
