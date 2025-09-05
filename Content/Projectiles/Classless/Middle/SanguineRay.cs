using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;


namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Middle;

public class SanguineRay : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    private const int Lifetime = 45;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 20;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.extraUpdates = 1;
        Projectile.DamageType = DamageClass.Generic;
        Projectile.penetrate = -1;
        Projectile.scale = 1f;
        Projectile.timeLeft = Lifetime;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 15;
    }

    public const float MaxLaserLength = 3330f;
    public Player Owner => Main.player[Projectile.owner];
    public ref float Time => ref Projectile.ai[0];
    public ref float LaserLength => ref Projectile.ai[1];

    private const int TotalTime = 40;

    public override void AI()
    {
        if (trail == null || trail._disposed)
            trail = new(WidthFunct, ColorFunct, null, 40);

        Time++;
        float lifeCompletion = InverseLerp(0f, TotalTime, Time, true);
        Projectile.scale = Convert01To010(lifeCompletion) * 2f;
        Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.Zero);

        Color DarkOrange = Color.Red;
        DelegateMethods.v3_1 = DarkOrange.ToVector3() * Projectile.scale * 0.4f;

        cache ??= new(40);
        Vector2 expected = Projectile.Center + Projectile.velocity * 4000f;
        Vector2 end = LaserCollision(Projectile.Center, expected, CollisionTarget.Tiles);
        cache?.SetPoints(Projectile.Center.GetLaserControlPoints(end, 40));
        if (end != expected && cache != null)
        {
            ParticleRegistry.SpawnGlowParticle(end, cache.Points[^1].SafeDirectionTo(cache.Points[0]).RotatedByRandom(.3f) * Main.rand.NextFloat(3f, 14f),
                20, 40f, Color.Lerp(Color.Red, Color.DarkRed, Main.rand.NextFloat(.2f, .9f)));
        }
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Utility.CheckLinearCollision(cache.Points[0], cache.Points[^1], target.Hitbox, out Vector2 pos, out _))
        {
            for (int i = 0; i < 3; i++)
            {
                Vector2 vel = -Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(.46f) * Main.rand.NextFloat(5f, 15f);
                Color color = Color.Lerp(Color.Red, Color.DarkRed, Main.rand.NextFloat(.4f, .9f));
                ParticleRegistry.SpawnGlowParticle(pos, vel, Main.rand.Next(18, 26), Main.rand.NextFloat(30f, 50f), color * 2f, 1f, true);
                ParticleRegistry.SpawnBloomLineParticle(pos, vel * 2f, 50, .5f, color);
                ParticleRegistry.SpawnBloodParticle(pos, vel * Main.rand.NextFloat(1.1f, 1.5f),
                    Main.rand.Next(30, 50), Main.rand.NextFloat(.8f, 1.3f), Color.Lerp(Color.DarkRed, Color.Crimson, Main.rand.NextFloat(.2f, .9f)) * 1.4f);
            }
        }
    }

    private const int Points = 50;
    public override bool ShouldUpdatePosition() => false;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (cache == null)
            return false;

        return targetHitbox.LineCollision(cache.Points[0], cache.Points[^1], WidthFunct(.5f));
    }

    private float WidthFunct(float completionRatio)
    {
        float width = Projectile.scale * 20f;
        float frontExpansionInterpolant = InverseLerp(0.015f, 0.15f, completionRatio);
        float maxSize = width + completionRatio * width * 1.5f;
        return MakePoly(2).OutFunction.Evaluate(11f, maxSize, frontExpansionInterpolant);
    }

    public Color ColorFunct(SystemVector2 c, Vector2 position)
    {
        Color val = Color.Lerp(Color.DarkRed, Color.Red, (float)Math.Cos(Main.GlobalTimeWrappedHourly * 0.67f - c.X / LaserLength * 29f) * 0.5f + 0.5f);

        float trailOpacity = GetLerpBump(0f, 0.067f, 1f, .56f, c.X) * 0.9f;
        val.A = (byte)(trailOpacity * 255);

        return Color.Lerp(val, Color.Red, 0.5f) * trailOpacity;
    }

    public ManualTrailPoints cache;
    public OptimizedPrimitiveTrail trail;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || cache == null)
                return;

            ManagedShader shader = ShaderRegistry.PierceTrailShader;
            shader.TrySetParameter("globalTime", Main.GlobalTimeWrappedHourly * 4f);
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.ShadowTrail), 1, SamplerState.LinearWrap);
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.HarshNoise), 2, SamplerState.LinearWrap);
            trail.DrawTrail(shader, cache.Points, 80);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        return false;
    }
}