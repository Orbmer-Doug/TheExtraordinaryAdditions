using Daybreak.Common.Features.Rarities;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Rarities;

public class BloodWroughtRarity : ModRarity, IRarityTextRenderer
{
    public override Color RarityColor => Color.DarkRed;

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
            RarityParticles.SpawnDroplet(ref particles, ref presence, rect.RandomRectangle(),
                Vector2.UnitY * Main.rand.NextFloat(.5f, 1.1f),
                Main.rand.Next(70, 130), Main.rand.NextFloat(.3f, .5f), Color.DarkRed);

        RarityParticles.UpdateAndDrawParticles(drawContext, font, text, position, ref particles, ref presence);

        Texture2D glowTexture = AssetRegistry.GennedTextures.GlowParticleSmall;
        Color glowColor = Color.DarkRed;
        glowColor.A = 0;
        Main.spriteBatch.DrawBetterRect(glowTexture,
            ToScreenTarget(
                position + (drawContext.DrawKind == RarityDrawContext.Kind.PopupText ? Vector2.Zero : textSize / 2f),
                textSize * 2.2f * scale), null,
            glowColor * .35f, 0f, glowTexture.Size() / 2f);

        float anim = Main.GlobalTimeWrappedHourly % 4f / 4f;
        float interpol = MakePoly(2f).OutFunction.Evaluate(0f, 1f, anim);

        for (int x = -1; x <= 1; x += 2)
        {
            Vector2 pos = position + new Vector2(10f * interpol * x, 0f);
            ChatManager.DrawColorCodedString(Main.spriteBatch, font, text,
                pos, Color.Crimson * (1f - interpol) * .7f, rotation, origin, scale);
        }

        for (int y = -1; y <= 1; y += 2)
        {
            Vector2 pos = position + new Vector2(0f, 10f * interpol * y);
            ChatManager.DrawColorCodedString(Main.spriteBatch, font, text,
                pos, Color.Crimson * (1f - interpol) * .7f, rotation, origin, scale);
        }

        ChatManager.DrawColorCodedString(Main.spriteBatch, font, text,
            position + new Vector2(-1f), Color.Red * .5f, rotation, origin, scale);
        ChatManager.DrawColorCodedString(Main.spriteBatch, font, text,
            position + new Vector2(1f), Color.Red, rotation, origin, scale);
        ChatManager.DrawColorCodedString(Main.spriteBatch, font, text,
            position, Color.Black, rotation, origin, scale);
    }
}
