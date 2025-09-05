using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;

namespace TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
public class PrimordialRarity : ModRarity
{
    public override Color RarityColor => new Color(64, 0, 138);

    internal static List<RarityParticleInfo> PrimordialSparkleList = [];
    public static void DrawCustomTooltipLine(DrawableTooltipLine tooltipLine)
    {
        ManagedShader displace = ShaderRegistry.GlitchDisplacement;
        displace.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.Perlin), 0);
        displace.TrySetParameter("globalTime", Main.GlobalTimeWrappedHourly);

        CustomRaritySystem.DrawTextWithShader(tooltipLine, displace.Shader.Value, Color.Purple);
    }
}
