using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Common.Particles.Shader;

public class EpidemicPlague : ShaderParticle
{
    public override bool Active => particles.Count != 0;
    public override IEnumerable<Texture2D> Layers => [AssetRegistry.GetTexture(AdditionsTexture.WavyBlotchNoise)];
    public override Color EdgeColor => Color.DarkOliveGreen;
    public override ShaderParticleDrawLayer DrawContext => ShaderParticleDrawLayer.AfterProjectiles;
    public override Vector2 CalculateManualOffsetForLayer(int layerIndex)
    {
        Vector2 offset = Vector2.One * (float)Math.Cos(Main.GlobalTimeWrappedHourly * 0.041f) * 2f;
        offset = offset.RotatedBy((float)Math.Cos(Main.GlobalTimeWrappedHourly * 0.08f) * 0.97f);
        return offset;
    }

    public override void PrepareSpriteBatch(SpriteBatch spriteBatch)
    {
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, Main.Rasterizer, null, Matrix.Identity);
    }

    public static void Spawn(Vector2 pos, Vector2 vel, float scale) => ModContent.GetInstance<EpidemicPlague>().SpawnParticle(new(pos, vel, 60, new Vector2(Main.rand.NextFloat(.75f, 1f), Main.rand.NextFloat(.75f, 1f)) * scale, Color.White, RandomRotation()));

    public override void UpdateParticle(ref ParticleData particle, int index)
    {
        particle.Position += particle.Velocity;
        particle.Scale = Vector2.Clamp(particle.Scale - new Vector2(0.31f), Vector2.Zero, Vector2.One * 200f) * 0.9956f;
        if (particle.Scale.Length() < 15f)
            particle.Scale *= 0.95f - 0.9f;
        particle.Velocity *= .97f;
        particle.Rotation += particle.Velocity.Length() * .1f;
    }
    public override bool ShouldKill(ParticleData particle) => particle.Scale.Length() < 1f;
    public override void DrawInstances()
    {
        Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.BasicCircle);

        // Draw all particles.
        foreach (ParticleData particle in particles)
        {
            Vector2 drawPosition = particle.Position;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 scale = particle.Scale / tex.Size();

            Main.spriteBatch.PixelDraw(tex, drawPosition, null, Color.DarkOliveGreen, particle.Rotation, origin, scale, 0);
        }
    }
}