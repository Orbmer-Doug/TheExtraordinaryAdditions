using Terraria.Achievements;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

namespace TheExtraordinaryAdditions.Content.Achievements;

public class DefeatedAsterlin : ModAchievement
{
    public override string TextureName => AssetRegistry.GetTexturePath(AdditionsTexture.DefeatedAsterlin);
    public override void SetStaticDefaults()
    {
        Achievement.SetCategory(AchievementCategory.Slayer);
        AddNPCKilledCondition(ModContent.NPCType<Asterlin>());
    }
}