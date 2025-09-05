using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Common.Particles.Shader;

public class MoltenBall : ShaderParticle
{
    public override bool Active => particles.Count != 0;

    public override ShaderParticleDrawLayer DrawContext => ShaderParticleDrawLayer.OverPlayers;

    public override IEnumerable<Texture2D> Layers
    {
        get
        {
            yield return ModContent.Request<Texture2D>("TheExtraordinaryAdditions/Assets/Textures/Grayscale/Invisible").Value;
        }
    }

    public override Color EdgeColor => new(255, 56, 3);

    public override void ClearInstances() => particles.Clear();

    public override void PrepareShaderForTarget(int layerIndex)
    {
        ManagedShader shader = ShaderRegistry.AdditiveFusableParticleEdgeShader;
        Vector2 renderTargetSize = Main.ScreenSize.ToVector2() / 2f;

        shader.TrySetParameter("screenArea", renderTargetSize);
        shader.TrySetParameter("layerOffset", Vector2.Zero);
        shader.TrySetParameter("singleFrameScreenOffset", Vector2.Zero);
        shader.Render();
    }

    public static void Spawn(Vector2 pos, float scale) => ModContent.GetInstance<MoltenBall>().SpawnParticle(new(pos, Vector2.Zero, 0, Vector2.One * scale, Color.White));

    public override void UpdateParticle(ref ParticleData particle, int index)
    {
        particle.Scale = Vector2.Clamp(particle.Scale - new Vector2(0.24f), Vector2.Zero, Vector2.One * 200f) * 0.9956f;
        if (particle.Scale.Length() < 15f)
            particle.Scale *= 0.95f - 0.9f;

        particle.Color = EdgeColor * 1.2f;
        particle.Color.B = (byte)(particle.Color.B + (int)(MathF.Cos(particle.Position.Y * 0.015f + Main.GlobalTimeWrappedHourly * 0.1f) * 3f));
        float brightnessInterpolant = Utils.GetLerpValue(10f, 2f, particle.Time, true) * 0.67f;
        particle.Color = Color.Lerp(particle.Color, Color.Wheat, brightnessInterpolant);
    }

    public override bool ShouldKill(ParticleData particle) => particle.Scale.Length() <= 3;

    public override void PrepareSpriteBatch(SpriteBatch spriteBatch)
    {
        // Draw with additive blending.
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.Default, Main.Rasterizer, null, Matrix.Identity); // No transformations
    }

    public override void DrawInstances()
    {
        Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.BrightLight);

        foreach (ParticleData particle in particles)
        {
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 scale = Vector2.One * (particle.Scale / tex.Size());
            Color lavaColor = particle.Color;

            Main.spriteBatch.PixelDraw(tex, particle.Position, null, lavaColor, 0f, origin, scale, SpriteEffects.None);
        }
    }
}