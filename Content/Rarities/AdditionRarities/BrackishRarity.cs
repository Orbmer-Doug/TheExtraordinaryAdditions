using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Late;
using TheExtraordinaryAdditions.Content.Rarities.Sparkles;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;

public class BrackishRarity : ModRarity
{
    public override Color RarityColor => Color.Aqua;

    internal static List<Behavior<RarityParticleInfo>> WatorList = [];
    public static void DrawCustomTooltipLine(DrawableTooltipLine tooltipLine)
    {
        CustomRaritySystem.GetTextDimensions(tooltipLine, out Vector2 size, out Rectangle rect);

        if (Main.rand.NextBool(6))
            CustomRaritySystem.Spawn(ref WatorList, new RarityDroplet(rect.RandomRectangle(), Vector2.UnitY * Main.rand.NextFloat(.1f, 1.4f),
                Main.rand.Next(90, 150), Main.rand.NextFloat(.4f, .9f), AbyssalCurrents.BrackishPalette[2]));

        // Draw the base tooltip text and glow.
        CustomRaritySystem.DrawTextWithGlow(tooltipLine, AbyssalCurrents.BrackishPalette[0], AbyssalCurrents.BrackishPalette[1], Color.Black);

        CustomRaritySystem.UpdateAndDrawParticles(tooltipLine, ref WatorList);
    }
}