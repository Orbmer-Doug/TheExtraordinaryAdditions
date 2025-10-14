using Terraria.Achievements;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain;

namespace TheExtraordinaryAdditions.Content.Achievements;

public class DefeatedStygain : ModAchievement
{
    public override string TextureName => AssetRegistry.GetTexturePath(AdditionsTexture.DefeatedStygain);
    public override void SetStaticDefaults()
    {
        Achievement.SetCategory(AchievementCategory.Slayer);
        AddNPCKilledCondition(ModContent.NPCType<StygainHeart>());
    }
    public override Position GetDefaultPosition() => new Before("OBSESSIVE_DEVOTION");
}