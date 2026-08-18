using System;
using Daybreak.Common.Features.Rarities;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Rarities;

public class UniqueRarity : ModRarity, IRarityTextRenderer
{
    public override Color RarityColor => Color.BlueViolet;

    internal static RarityParticles.ParticleInfo[] particles =
        new RarityParticles.ParticleInfo[RarityParticles.MaxParticles];

    internal static ulong[] presence = BitmaskUtils.CreateMask(RarityParticles.MaxParticles);

    public void RenderText(SpriteBatch sb, DynamicSpriteFont font, string text, Vector2 position, Color color,
        float rotation,
        Vector2 origin, Vector2 scale, SpriteEffects effects, RarityDrawContext drawContext, float maxWidth = -1,
        float spread = 2)
    {
        CustomRaritySystem.GetTextDimensions(font, text, Vector2.Zero, out Vector2 textSize, out Rectangle rect);

        if (Main.rand.NextBool(3))
        {
            Color col = Color.SlateBlue.Lerp(Color.MediumSlateBlue, Main.rand.NextFloat(.3f, .7f));
            Vector2 pos = Vector2.Lerp(rect.BottomLeft(), rect.BottomRight(), Main.rand.NextFloat());
            Vector2 vel = -Vector2.UnitY * Main.rand.NextFloat(1f, 3f);
            float size = Main.rand.NextFloat(.2f, .5f);
            int life = Main.rand.Next(50, 120);
            RarityParticles.SpawnSparkle(ref particles, ref presence, pos, vel, life, size, col);
        }

        RarityParticles.UpdateAndDrawParticles(drawContext, font, text, position, ref particles, ref presence);

        Color outerColor = ColorSwap(Color.SlateBlue, Color.MediumSlateBlue, 2f);
        Color textInnerColor = Color.Black;

        Texture2D glowTexture = AssetRegistry.GennedTextures.GlowParticleSmall;
        Color glowColor = Color.SlateBlue;
        glowColor.A = 0;
        Main.spriteBatch.DrawBetterRect(glowTexture,
            ToScreenTarget(
                position + (drawContext.DrawKind == RarityDrawContext.Kind.PopupText ? Vector2.Zero : textSize / 2f),
                textSize * 2.2f * scale), null,
            glowColor * .35f, 0f, glowTexture.Size() / 2f);

        float sine = (float) ((1 + Math.Sin(Main.GlobalTimeWrappedHourly * 2.5f)) / 2);
        float sineOffset = MathHelper.Lerp(0.5f, 1f, sine);
        for (int i = 0; i < 12; i++)
        {
            Vector2 afterimageOffset = (MathHelper.TwoPi * i / 12f + Main.GlobalTimeWrappedHourly).ToRotationVector2() *
                                       (4f * sineOffset);

            ChatManager.DrawColorCodedString(Main.spriteBatch, font, text,
                position + afterimageOffset.RotatedBy(MathHelper.TwoPi * (i / 12f)),
                outerColor * 0.9f, rotation, origin, scale);
        }

        Color mainTextColor = Color.Lerp(glowColor, textInnerColor, 0.9f);
        ChatManager.DrawColorCodedString(Main.spriteBatch, font, text,
            position, mainTextColor, rotation, origin, scale);
    }
}
