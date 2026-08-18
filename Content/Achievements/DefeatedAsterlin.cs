using Terraria.Achievements;
using Terraria.ModLoader;
using Asterlin = TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Asterlin;

namespace TheExtraordinaryAdditions.Content.Achievements;

public class DefeatedAsterlin : ModAchievement
{
    public override string TextureName => AssetRegistry.GennedTextures.DefeatedAsterlin.Path;

    public override void SetStaticDefaults()
    {
        Achievement.SetCategory(AchievementCategory.Slayer);
        AddNPCKilledCondition(ModContent.NPCType<Asterlin>());
    }
}
