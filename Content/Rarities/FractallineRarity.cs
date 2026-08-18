using Daybreak.Common.Features.Rarities;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics.Resources;

namespace TheExtraordinaryAdditions.Content.Rarities;

public class FractallineRarity : ModRarity, IRarityTextRenderer
{
    public override Color RarityColor => new Color(64, 0, 138);

    public void RenderText(SpriteBatch sb, DynamicSpriteFont font, string text, Vector2 position, Color color,
        float rotation,
        Vector2 origin, Vector2 scale, SpriteEffects effects, RarityDrawContext drawContext, float maxWidth = -1,
        float spread = 2)
    {
        ManagedShader displace = AssetRegistry.GennedShaders.GlitchDisplacement;
        displace.SetTexture(AssetRegistry.GennedTextures.PerlinCloud, 0);
        displace.TrySetParameter("globalTime", Main.GlobalTimeWrappedHourly);

        CustomRaritySystem.DrawTextWithShader(font, text, position, Color.Purple, rotation, origin, scale,
            displace.Shader.Value);
    }
}
