using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Rarities.Sparkles;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;

public class CrosscodeRarity : ModRarity
{
    public override Color RarityColor => new(255, 72, 0);

    internal static List<Behavior<RarityParticleInfo>> PixelList = [];
    public static void DrawCustomTooltipLine(DrawableTooltipLine tooltipLine)
    {
        CustomRaritySystem.GetTextDimensions(tooltipLine, out Vector2 size, out Rectangle rect);

        if (Main.rand.NextBool())
        {
            Vector2 vel = Vector2.UnitY.RotatedByRandom(.8f) * Main.rand.NextFloat(-4f, 4f);
            int life = Main.rand.Next(40, 60);
            float scale = Main.rand.NextFloat(.4f, .9f);
            CustomRaritySystem.Spawn(ref PixelList, new RarityPixel(rect.RandomRectangle(), vel, life, scale, Color.Cyan, Color.LightSkyBlue));
        }

        if (Main.rand.NextBool(12))
        {
            CustomRaritySystem.Spawn(ref PixelList, new RaritySparkle(rect.RandomRectangle(), Vector2.Zero,
                Main.rand.Next(40, 60), Main.rand.NextFloat(1.6f, 1.8f), Color.SkyBlue));
        }

        // Draw the base tooltip text and glow.
        CustomRaritySystem.DrawTextWithGlow(tooltipLine, Color.LightBlue, Color.Cyan, Color.DarkBlue);

        CustomRaritySystem.UpdateAndDrawParticles(tooltipLine, ref PixelList);
    }
}
