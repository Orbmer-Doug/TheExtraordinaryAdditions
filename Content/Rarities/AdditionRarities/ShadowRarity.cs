using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Rarities.Sparkles;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
public class ShadowRarity : ModRarity
{
    public override Color RarityColor => new(69, 32, 68);

    // mist list
    internal static List<Behavior<RarityParticleInfo>> MistList = [];
    public static void DrawCustomTooltipLine(DrawableTooltipLine tooltipLine)
    {
        CustomRaritySystem.GetTextDimensions(tooltipLine, out Vector2 size, out Rectangle rect);

        if (Main.rand.NextBool(2))
        {
            Vector2 pos = rect.RandomRectangle();
            Vector2 start = pos + Main.rand.NextVector2CircularEdge(60f, 60f);
            Vector2 vel = Main.rand.NextVector2CircularLimited(6f, 6f, .4f, 1.2f);
            int life = Main.rand.Next(50, 90);
            float scale = Main.rand.NextFloat(.6f, .9f);

            CustomRaritySystem.Spawn(ref MistList, new RarityPixel(start, vel, life, scale, Color.BlueViolet, Color.DarkViolet, pos, 3));
        }

        if (Main.rand.NextBool(2))
        {
            CustomRaritySystem.Spawn(ref MistList, new RaritySmoke(rect.RandomRectangle(), Vector2.UnitX * Main.rand.NextFloat(-2f, 2f),
                Main.rand.Next(30, 50), Main.rand.NextFloat(.5f, .8f), Color.Violet));
        }

        CustomRaritySystem.UpdateAndDrawParticles(tooltipLine, ref MistList);

        // Draw the base tooltip text and glow.
        CustomRaritySystem.DrawTextWithGlow(tooltipLine, Color.BlueViolet, Color.Lerp(new Color(103, 47, 90), new Color(69, 32, 68), (float)MathF.Sin(Main.GlobalTimeWrappedHourly * .9f)), Color.Black);
    }
}
