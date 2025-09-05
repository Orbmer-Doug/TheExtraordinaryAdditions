using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Rarities.Sparkles;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;

public class BloodWroughtRarity : ModRarity
{
    public override Color RarityColor => Color.DarkRed;

    internal static List<Behavior<RarityParticleInfo>> BloodList = [];
    public static void DrawCustomTooltipLine(DrawableTooltipLine tooltipLine)
    {
        CustomRaritySystem.GetTextDimensions(tooltipLine, out Vector2 size, out Rectangle rect);

        if (Main.rand.NextBool(4))
            CustomRaritySystem.Spawn(ref BloodList, new RarityDroplet(rect.RandomRectangle(), Vector2.UnitY * Main.rand.NextFloat(.5f, 1.1f),
                Main.rand.Next(70, 130), Main.rand.NextFloat(.3f, .5f), Color.DarkRed));

        // Draw the base tooltip text and glow.
        CustomRaritySystem.DrawTextWithGlow(tooltipLine, Color.Black, Color.DarkRed, Color.IndianRed);

        CustomRaritySystem.UpdateAndDrawParticles(tooltipLine, ref BloodList);
    }
}
