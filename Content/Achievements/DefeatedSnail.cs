using Terraria;
using Terraria.Achievements;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Misc;

namespace TheExtraordinaryAdditions.Content.Achievements;

public class DefeatedSnail : ModAchievement
{
    public override string TextureName => AssetRegistry.GetTexturePath(AdditionsTexture.DefeatedSnail);
    public override void SetStaticDefaults()
    {
        Achievement.SetCategory(AchievementCategory.Slayer);
        AddNPCKilledCondition(ModContent.NPCType<TheGiantSnailFromAncientTimes>());
    }
    public override Position GetDefaultPosition() => new After("ICE_SCREAM");
    public override void OnCompleted(Achievement achievement)
    {
        for (int i = 0; i < 500; i++)
        {
            Vector2 topLeft = Main.screenPosition;
            Vector2 bottomRight = Main.screenPosition + Main.ScreenSize.ToVector2();
            Vector2 pos = Main.rand.Next(4) switch
            {
                0 => Vector2.Lerp(topLeft, topLeft + Vector2.UnitX * Main.screenWidth, Main.rand.NextFloat()),
                1 => Vector2.Lerp(topLeft, topLeft + Vector2.UnitY * Main.screenWidth, Main.rand.NextFloat()),
                2 => Vector2.Lerp(bottomRight, bottomRight - Vector2.UnitX * Main.screenWidth, Main.rand.NextFloat()),
                3 => Vector2.Lerp(bottomRight, bottomRight - Vector2.UnitY * Main.screenWidth, Main.rand.NextFloat()),
                _ => Main.LocalPlayer.Center
            };
            ParticleRegistry.SpawnCartoonAngerParticle(pos, Main.rand.Next(120, 240), Main.rand.NextFloat(.7f, 1.5f), RandomRotation(), Color.DarkRed, Color.Red);
        }
    }
}