using Terraria.Achievements;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Hostile.Aurora;

namespace TheExtraordinaryAdditions.Content.Achievements;

public class DefeatedAuroraGuard : ModAchievement
{
    public override string TextureName => AssetRegistry.GetTexturePath(AdditionsTexture.DefeatedAuroraGuard);
    public override void SetStaticDefaults()
    {
        Achievement.SetCategory(AchievementCategory.Slayer);
        AddNPCKilledCondition(ModContent.NPCType<AuroraGuard>());
    }
    public override Position GetDefaultPosition() => new After("STILL_HUNGRY");
}