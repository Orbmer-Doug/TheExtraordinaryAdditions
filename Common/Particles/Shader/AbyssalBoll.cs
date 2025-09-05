using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Late;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Common.Particles.Shader;

public class AbyssalBoll : ShaderParticle
{
    public override bool Active => particles.Count != 0;
    public override ShaderParticleDrawLayer DrawContext => ShaderParticleDrawLayer.AfterProjectiles;
    public override IEnumerable<Texture2D> Layers => [AssetRegistry.GetTexture(AdditionsTexture.WavyBlotchNoise),
        AssetRegistry.GetTexture(AdditionsTexture.WaterNoise)];
    public override Color EdgeColor => AbyssalCurrents.BrackishPalette[1];
    public override Vector2 CalculateManualOffsetForLayer(int layerIndex)
    {
        switch (layerIndex)
        {
            case 0:
                return Vector2.UnitX * Main.GlobalTimeWrappedHourly * 0.03f;

            case 1:
                Vector2 offset = Vector2.One * (float)Math.Cos(Main.GlobalTimeWrappedHourly * 0.041f) * 2f;
                offset = offset.RotatedBy((float)Math.Cos(Main.GlobalTimeWrappedHourly * 0.08f) * 0.97f);
                return offset;
        }

        return Vector2.Zero;
    }

    public override void PrepareSpriteBatch(SpriteBatch spriteBatch)
    {
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, Main.Rasterizer, null, Matrix.Identity);
    }

    public static void Spawn(Vector2 pos, Vector2 vel, float scale) => ModContent.GetInstance<AbyssalBoll>().SpawnParticle(new(pos, vel, 60, Vector2.One * scale, Color.White, 1f));

    public override void UpdateParticle(ref ParticleData particle, int index)
    {
        particle.Scale *= .97f;
        particle.Position += particle.Velocity;
        particle.Velocity *= .98f;
    }
    public override bool ShouldKill(ParticleData particle) => particle.Scale.Length() <= 1.4f;
    public override void DrawInstances()
    {
        Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.BasicCircularCircle);

        foreach (ParticleData particle in particles)
        {
            Vector2 origin = tex.Size() * 0.5f;
            Main.spriteBatch.PixelDraw(tex, particle.Position, null, AbyssalCurrents.BrackishPalette[2], 0f, origin, particle.Scale / tex.Size(), SpriteEffects.None);
        }
    }
}