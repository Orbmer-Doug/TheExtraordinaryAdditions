using System;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;

public class SolarBrandSparks : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 32;
        Projectile.timeLeft = 220;
        Projectile.penetrate = 2;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = true;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 20;
        Projectile.extraUpdates = 1;
        Projectile.active = true;
        Projectile.noEnchantmentVisuals = true;
        Projectile.reflected = true;
        Projectile.scale = 1f;
    }

    internal Color ColorFunction(SystemVector2 c, Vector2 position)
    {
        float fade = InverseLerp(0f, 100f, Projectile.timeLeft);
        return Color.Lerp(Color.DarkOrange, Color.OrangeRed * (MathF.Sin((Projectile.identity * 2 % 30) + Main.GlobalTimeWrappedHourly * 3f) * .5f + 1.5f), fade) * fade;
    }

    internal float WidthFunction(float c)
    {
        return OptimizedPrimitiveTrail.HemisphereWidthFunct(c, .5f * Projectile.width * MathHelper.SmoothStep(1f, 0f, c));
    }


    public override bool? CanHitNPC(NPC target) => Projectile.numHits <= 0;
    public override void AI()
    {
        if (trail == null || trail.Disposed)
            trail = new(WidthFunction, ColorFunction, null, 20);

        if (Projectile.numHits > 0)
        {
            Projectile.velocity *= .94f;
            if (Projectile.timeLeft > 100)
                Projectile.timeLeft = 100;
        }
        else
        {
            if (NPCTargeting.TryGetClosestNPC(new(Projectile.Center, 500, true, true), out NPC target))
                Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Projectile.SafeDirectionTo(target.Center) * 30f, .1f);
        }

        cache ??= new(20);
        cache.Update(Projectile.Center);

        Lighting.AddLight(Projectile.Center, Color.OrangeRed.ToVector3() * .6f);

        if (Projectile.velocity.Length() > 5f)
            Projectile.velocity *= .9f;
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
        Projectile.Kill();
        return false;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        for (int i = 0; i < 10; i++)
        {
            ParticleRegistry.SpawnHeavySmokeParticle(Projectile.Center + Main.rand.NextVector2Circular(2, 2),
                -Projectile.velocity * Main.rand.NextFloat(.1f, .2f), 20, Main.rand.NextFloat(.3f, .4f), Color.OrangeRed, Main.rand.NextFloat(.6f, .8f), true);
            ParticleRegistry.SpawnSparkParticle(Projectile.Center, -Projectile.velocity.RotatedByRandom(.3f) * Main.rand.NextFloat(.2f, .5f), 30, Main.rand.NextFloat(.2f, .5f), Color.OrangeRed);
        }
    }
    
    public OptimizedPrimitiveTrail trail;
    public TrailPoints cache;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || cache == null)
                return;
            ManagedShader shader = ShaderRegistry.FadedStreak;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.StreakMagma), 1);
            trail.DrawTrail(shader, cache.Points, 60, true);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        return false;
    }
}