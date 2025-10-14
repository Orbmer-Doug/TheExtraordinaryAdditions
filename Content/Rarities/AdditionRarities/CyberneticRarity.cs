using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Rarities.Sparkles;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;

public class CyberneticRarity : ModRarity
{
    public override Color RarityColor => Color.DeepSkyBlue;

    internal static List<Behavior<RarityParticleInfo>> TechyList = [];

    public static void DrawCustomTooltipLine(DrawableTooltipLine line)
    {
        CustomRaritySystem.GetTextDimensions(line, out Vector2 size, out Rectangle rect);

        if (Main.rand.NextBool())
            CustomRaritySystem.Spawn(ref TechyList, new RarityHolosquare(rect.RandomRectangle(),
                -Vector2.UnitY * Main.rand.NextFloat(2f, 4f), Main.rand.Next(30, 60),
                Main.rand.NextFloat(.5f, .9f), Color.Lerp(Color.Cyan, Color.DeepSkyBlue, Main.rand.NextFloat()), 1f, Main.rand.NextFloat(1.8f, 3f)));

        if (Main.rand.NextBool(14))
            CustomRaritySystem.Spawn(ref TechyList, new RarityPixel(rect.RandomRectangle(), Main.rand.NextVector2CircularLimited(10f, 10f, .3f, 1f),
                Main.rand.Next(20, 30), Main.rand.NextFloat(.3f, .6f), Color.Cyan, Color.DeepSkyBlue, null, 4));

        CustomRaritySystem.DrawTextWithGlow(line, Color.DeepSkyBlue, Color.Cyan, Color.Black);
        CustomRaritySystem.UpdateAndDrawParticles(line, ref TechyList);
    }
}
