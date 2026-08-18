using Daybreak.Common.Features.Rarities;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Rarities;

public class CrosscodeRarity : ModRarity, IRarityTextRenderer
{
    public override Color RarityColor => Color.LightCyan;

    internal static RarityParticles.ParticleInfo[] particles =
        new RarityParticles.ParticleInfo[RarityParticles.MaxParticles];

    internal static ulong[] presence = BitmaskUtils.CreateMask(RarityParticles.MaxParticles);

    public void RenderText(SpriteBatch sb, DynamicSpriteFont font, string text, Vector2 position, Color color,
        float rotation,
        Vector2 origin, Vector2 scale, SpriteEffects effects, RarityDrawContext drawContext, float maxWidth = -1,
        float spread = 2)
    {
        CustomRaritySystem.GetTextDimensions(font, text, Vector2.Zero, out Vector2 textSize, out Rectangle rect);

        if (Main.rand.NextBool(6))
        {
            Vector2 vel = Vector2.UnitY.RotatedByRandom(.8f) * Main.rand.NextFloat(-2f, 2f);
            int life = Main.rand.Next(40, 60);
            float size = Main.rand.NextFloat(.4f, .9f);
            RarityParticles.SpawnPixel(ref particles, ref presence, rect.RandomRectangle(), vel, life, size, Color.Cyan,
                Color.LightSkyBlue);
        }

        if (Main.rand.NextBool(16))
        {
            RarityParticles.SpawnStar(ref particles, ref presence, rect.RandomRectangle(), Vector2.Zero,
                Main.rand.Next(40, 60), Main.rand.NextFloat(1.6f, 1.8f), Color.SkyBlue);
        }

        RarityParticles.UpdateAndDrawParticles(drawContext, font, text, position, ref particles, ref presence);

        Texture2D glowTexture = AssetRegistry.GennedTextures.GlowParticleSmall;
        Color glowColor = Color.DarkCyan;
        glowColor.A = 0;
        Main.spriteBatch.DrawBetterRect(glowTexture,
            ToScreenTarget(
                position + (drawContext.DrawKind == RarityDrawContext.Kind.PopupText ? Vector2.Zero : textSize / 2f),
                textSize * 2.2f * scale), null,
            glowColor * .35f, 0f, glowTexture.Size() / 2f);

        ChatManager.DrawColorCodedString(Main.spriteBatch, font, text,
            position + new Vector2(0f, 1f), Color.Cyan, rotation, origin, scale);
        ChatManager.DrawColorCodedString(Main.spriteBatch, font, text,
            position + new Vector2(0f, -1f), Color.Cyan, rotation, origin, scale);
        ChatManager.DrawColorCodedString(Main.spriteBatch, font, text,
            position, Color.White, rotation, origin, scale);
    }
}
