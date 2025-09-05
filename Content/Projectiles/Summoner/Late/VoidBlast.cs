using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Summoner.Late;

public class VoidBlast : ModProjectile
{
    public OptimizedPrimitiveTrail circle;
    private readonly ManualTrailPoints points = new(40);

    private const int Lifetime = 35;
    public float Completion => InverseLerp(0f, Lifetime, Time);
    public float Radius => MakePoly(4).OutFunction.Evaluate(0f, 120f, Completion);
    public ref float Time => ref Projectile.ai[0];
    public override string Texture => AssetRegistry.Invis;

    public override void SetDefaults()
    {
        Projectile.DamageType = DamageClass.Summon;
        Projectile.friendly = true;
        Projectile.width = Projectile.height = 2;
        Projectile.tileCollide = false;
        Projectile.timeLeft = Lifetime;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 15;
    }

    public override void AI()
    {
        if (Time == 0f)
        {
            ParticleRegistry.SpawnChromaticAberration(Projectile.Center, Lifetime, 1.24f, 150f);
            for (int i = 0; i < 25; i++)
                ParticleRegistry.SpawnMistParticle(Projectile.Center, Main.rand.NextVector2Circular(20f, 20f),
                    Main.rand.NextFloat(.7f, 1.4f), Color.Violet, Color.DarkViolet, Main.rand.NextFloat(150f, 220f), Main.rand.NextFloat(-.1f, .1f));
        }

        if (circle == null || circle._disposed)
            circle = new(WidthFunct, ColorFunct, null, points.Count);

        for (int i = 0; i < points.Count; i++)
            points.SetPoint(i, Projectile.Center + ((MathHelper.TwoPi + (MathF.Sqrt(2f) / 2f)) * InverseLerp(0f, points.Count, i)).ToRotationVector2() * Radius);

        Vector2 pos = points.Points[Main.rand.Next(0, points.Count - 1)];
        int life = Main.rand.Next(30, 40);
        float scale = Main.rand.NextFloat(.4f, .8f);
        Color col = ColorFunct(new(0f, Main.rand.NextFloat()), Vector2.Zero);
        Vector2 vel = Projectile.Center.SafeDirectionTo(pos) * 11f * Completion;

        ParticleRegistry.SpawnSquishyPixelParticle(pos, vel, life * 3, scale * 3f, col, Color.White, 8);

        for (int i = 0; i < 2; i++)
            ParticleRegistry.SpawnMistParticle(Projectile.Center, Vector2.Zero,
                Main.rand.NextFloat(.65f, 1f), Color.Violet, Color.DarkViolet, Main.rand.NextFloat(150f, 220f), Main.rand.NextFloat(-.1f, .1f));

        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.CollisionFromPoints(points.Points, WidthFunct);
    }

    public Color ColorFunct(SystemVector2 c, Vector2 pos)
    {
        Color col = MulticolorLerp(Completion, Color.White.Lerp(Color.Violet, .5f), Color.Violet, Color.DarkViolet, Color.Black);
        return col * MathHelper.Lerp(2f, .5f, Completion);
    }

    public float WidthFunct(float c) => 30f * (1f - Completion);

    public override bool PreDraw(ref Color lightColor)
    {
        void blast()
        {
            if (circle != null)
            {
                ManagedShader shader = ShaderRegistry.RealityTearShader;
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.ManifoldNoise), 0, SamplerState.LinearWrap);
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.NeuronNoise), 1, SamplerState.LinearWrap);
                circle.DrawTrail(shader, points.Points, 300, true);
            }
        }
        PixelationSystem.QueuePrimitiveRenderAction(blast, PixelationLayer.OverPlayers);

        return false;
    }
}