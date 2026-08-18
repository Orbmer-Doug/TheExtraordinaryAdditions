using Daybreak.Common.Features.Rarities;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Rarities;

public class LegendaryRarity : ModRarity, IRarityTextRenderer
{
    public override Color RarityColor => Color.Gold;

    internal static RarityParticles.ParticleInfo[] particles =
        new RarityParticles.ParticleInfo[RarityParticles.MaxParticles];

    internal static ulong[] presence = BitmaskUtils.CreateMask(RarityParticles.MaxParticles);

    public void RenderText(SpriteBatch sb, DynamicSpriteFont font, string text, Vector2 position, Color color,
        float rotation,
        Vector2 origin, Vector2 scale, SpriteEffects effects, RarityDrawContext drawContext, float maxWidth = -1,
        float spread = 2)
    {
        CustomRaritySystem.GetTextDimensions(font, text, Vector2.Zero, out Vector2 textSize, out Rectangle rect);

        if (Main.rand.NextBool(4))
        {
            Vector2 pos = rect.RandomRectangle();
            Vector2 vel = -pos.SafeDirectionTo(rect.Bottom()) * Main.rand.NextFloat(1f, 3f);
            int life = Main.rand.Next(30, 40);
            float size = Main.rand.NextFloat(.3f, .4f);
            RarityParticles.SpawnPixel(ref particles, ref presence, pos, vel, life, size, Color.Gold, Color.Goldenrod,
                null, 6);
        }

        ChatManager.DrawColorCodedString(Main.spriteBatch, font, text,
            position - new Vector2(3f), Color.DarkGoldenrod * .4f, rotation, origin, scale);
        ChatManager.DrawColorCodedString(Main.spriteBatch, font, text,
            position - new Vector2(-1f) + Main.rand.NextVector2Circular(1f, 1f), Color.Gold, rotation, origin, scale);
        ChatManager.DrawColorCodedString(Main.spriteBatch, font, text,
            position + new Vector2(3f), Color.DarkGoldenrod * .4f, rotation, origin, scale);
        ChatManager.DrawColorCodedString(Main.spriteBatch, font, text,
            position + new Vector2(-1f) + Main.rand.NextVector2Circular(1f, 1f), Color.Gold, rotation, origin, scale);
        ChatManager.DrawColorCodedString(Main.spriteBatch, font, text,
            position, Color.White, rotation, origin, scale);
    }
}
