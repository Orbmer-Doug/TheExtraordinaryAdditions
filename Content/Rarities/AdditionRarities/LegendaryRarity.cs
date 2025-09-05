using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Rarities.Sparkles;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;

public class LegendaryRarity : ModRarity
{
    public override Color RarityColor => new(255, 72, 0);

    internal static List<Behavior<RarityParticleInfo>> SparkleList = [];

    public static void DrawCustomTooltipLine(DrawableTooltipLine tooltipLine)
    {
        CustomRaritySystem.GetTextDimensions(tooltipLine, out Vector2 size, out Rectangle rect);

        if (Main.rand.NextBool())
        {
            Vector2 pos = rect.RandomRectangle();
            Vector2 vel = -pos.SafeDirectionTo(rect.Bottom()) * Main.rand.NextFloat(3f, 5f);
            int life = Main.rand.Next(30, 40);
            float scale = Main.rand.NextFloat(.2f, .5f);
            CustomRaritySystem.Spawn(ref SparkleList, new RaritySpark(pos, vel, life, scale, Color.Gold));
        }

        if (Main.rand.NextBool(5))
        {
            Vector2 pos = rect.RandomRectangle();
            Vector2 vel = -pos.SafeDirectionTo(rect.Bottom()) * Main.rand.NextFloat(4f, 7f);
            CustomRaritySystem.Spawn(ref SparkleList, new RarityPixel(pos, vel, Main.rand.Next(60, 150),
                Main.rand.NextFloat(.4f, .8f), Color.Gold, Color.PaleGoldenrod, null, 4));
        }

        if (Main.rand.NextBool(20))
        {
            CustomRaritySystem.Spawn(ref SparkleList, new RaritySparkle(rect.RandomRectangle(), Vector2.Zero,
                Main.rand.Next(50, 70), Main.rand.NextFloat(1.4f, 1.8f), Color.Gold));
        }

        CustomRaritySystem.DrawTextWithGlow(tooltipLine, Color.Goldenrod, Color.PaleGoldenrod);

        CustomRaritySystem.UpdateAndDrawParticles(tooltipLine, ref SparkleList);
    }
}
