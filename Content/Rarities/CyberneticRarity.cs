using Daybreak.Common.Features.Rarities;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Rarities;

public class CyberneticRarity : ModRarity, IRarityTextRenderer
{
    public override Color RarityColor => Color.DeepSkyBlue;

    internal static RarityParticles.ParticleInfo[] particles =
        new RarityParticles.ParticleInfo[RarityParticles.MaxParticles];

    internal static ulong[] presence = BitmaskUtils.CreateMask(RarityParticles.MaxParticles);

    public void RenderText(SpriteBatch sb, DynamicSpriteFont font, string text, Vector2 position, Color color,
        float rotation,
        Vector2 origin, Vector2 scale, SpriteEffects effects, RarityDrawContext drawContext, float maxWidth = -1,
        float spread = 2)
    {
        CustomRaritySystem.GetTextDimensions(font, text, Vector2.Zero, out Vector2 textSize, out Rectangle rect);

        if (Main.rand.NextBool(5))
            RarityParticles.SpawnHolosquare(ref particles, ref presence, rect.RandomRectangle(),
                -Vector2.UnitX * Main.rand.NextFloat(1f, 3f), Main.rand.Next(30, 60),
                Main.rand.NextFloat(.5f, .9f), Color.Lerp(Color.Cyan, Color.DeepSkyBlue, Main.rand.NextFloat()), 1f,
                Main.rand.NextFloat(1.8f, 3f));


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
            position + new Vector2(-4f, 0f), Color.Black, rotation, origin, scale);
        ChatManager.DrawColorCodedString(Main.spriteBatch, font, text,
            position + new Vector2(-2f, 0f), Color.DarkCyan, rotation, origin, scale);
        ChatManager.DrawColorCodedString(Main.spriteBatch, font, text,
            position, Color.Cyan, rotation, origin, scale);
    }
}
