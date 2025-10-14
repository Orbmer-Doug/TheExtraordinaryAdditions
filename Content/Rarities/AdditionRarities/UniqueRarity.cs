using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Rarities.Sparkles;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;

public class UniqueRarity : ModRarity
{
    public override Color RarityColor => Color.BlueViolet;
    internal static List<Behavior<RarityParticleInfo>> SparkleList = [];
    public static void DrawCustomTooltipLine(DrawableTooltipLine line)
    {
        CustomRaritySystem.GetTextDimensions(in line, out Vector2 textSize, out Rectangle textRect);

        if (Main.rand.NextBool(7))
        {
            Color color = Color.SlateBlue.Lerp(Color.MediumSlateBlue, Main.rand.NextFloat(.3f, .7f));
            Vector2 pos = textRect.RandomRectangle();
            Vector2 vel = Main.rand.NextVector2Circular(3, 3);
            float scale = Main.rand.NextFloat(.2f, 1.7f);
            int life = Main.rand.Next(50, 120);
            CustomRaritySystem.Spawn(ref SparkleList, new RaritySparkle(pos, vel, life, scale, color));
        }

        CustomRaritySystem.UpdateAndDrawParticles(line, ref SparkleList);

        Color outerColor = ColorSwap(Color.SlateBlue, Color.MediumSlateBlue, 2f);

        // Draw the base tooltip text and glow.
        CustomRaritySystem.DrawTextWithGlow(line, Color.DeepSkyBlue, outerColor);
    }
}
