using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Rarities.Sparkles;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;

public class LaserClassRarity : ModRarity
{
    public override Color RarityColor => Color.OrangeRed;

    internal static List<Behavior<RarityParticleInfo>> SmokeList = [];

    public static void DrawCustomTooltipLine(DrawableTooltipLine tooltipLine)
    {
        CustomRaritySystem.GetTextDimensions(tooltipLine, out Vector2 size, out Rectangle rect);

        if (Main.rand.NextBool())
        {
            CustomRaritySystem.Spawn(ref SmokeList, new RaritySmoke(rect.RandomRectangle(), Main.rand.NextVector2CircularEdge(1f, 1f),
                Main.rand.Next(80, 120), Main.rand.NextFloat(.4f, .68f), Color.OrangeRed));
        }

        CustomRaritySystem.UpdateAndDrawParticles(tooltipLine, ref SmokeList);

        // Draw the base tooltip text and glow.
        CustomRaritySystem.DrawTextWithGlow(tooltipLine, Color.Red, Color.OrangeRed);
    }
}
