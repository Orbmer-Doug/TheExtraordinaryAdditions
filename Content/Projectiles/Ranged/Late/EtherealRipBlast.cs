using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;

public class EtherealRipBlast : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public ref float Time => ref Projectile.ai[0];
    public int Timeleft = 180;
    
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 1000;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 68;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.timeLeft = Timeleft;
        Projectile.alpha = 255;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 10;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.penetrate = -1;
        Projectile.extraUpdates = 5;
    }

    public override void AI()
    {
        if (trail == null || trail._disposed)
            trail = new(WidthFunction, ColorFunction, null, 100);

        cache.Update(Projectile.Center + Projectile.velocity);

        if (Projectile.FinalExtraUpdate())
            ParticleRegistry.SpawnTechyHolosquareParticle(Projectile.Center + Main.rand.NextVector2Circular(30f * Projectile.scale, 30f * Projectile.scale),
                -Projectile.velocity * Main.rand.NextFloat(.2f, .4f), Main.rand.Next(20, 30), Main.rand.NextFloat(.4f, .9f), Color.Cyan);

        Projectile.scale = Projectile.Opacity = GetLerpBump(0f, 30f, Timeleft, Timeleft - 15f, Time);
        Projectile.rotation = Projectile.velocity.ToRotation();

        Lighting.AddLight(Projectile.Center, Color.Cyan.ToVector3() * 2f * Projectile.scale);
        Time++;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Projectile.damage > 1000)
            Projectile.damage = (int)(Projectile.damage * .95f);

        if (this.RunLocal())
            Projectile.NewProj(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<LightripBlast>(), (int)(Projectile.damage * .5f), Projectile.knockBack, Projectile.owner);

        for (int i = 0; i < 20; i++)
        {
            ParticleRegistry.SpawnSparkParticle(Projectile.Center, -Projectile.velocity.RotatedByRandom(.1f) * i * .1f, 30, .6f, Color.White, true);
        }
    }

    public override void OnKill(int timeLeft)
    {
        for (int i = 0; i < cache.Points.Length; i++)
        {
            for (int a = 0; a < 2; a++)
            {
                ParticleRegistry.SpawnGlowParticle(cache.Points[i], Projectile.velocity.RotatedByRandom(.1f) * .2f,
                    Main.rand.Next(20, 30), Main.rand.NextFloat(.4f, .8f), Color.DeepSkyBlue);
            }
        }
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        modifiers.ScalingArmorPenetration += 1f;
        modifiers.DefenseEffectiveness *= 0f;

        if (target.IsThanatos())
        {
            modifiers.FinalDamage *= .85f;
        }
    }

    public Color ColorFunction(SystemVector2 completionRatio, Vector2 position)
    {
        float fadeOpacity = Math.Min(Projectile.timeLeft / (float)cache.Points.Length, 1f);
        return Color.Lerp(Color.Cyan, Color.DeepSkyBlue, .4f) * fadeOpacity * MathHelper.SmoothStep(1f, 0f, completionRatio.X);
    }

    public float WidthFunction(float c)
    {
        return Projectile.width * 1.6f * Animators.MakePoly(4f).OutFunction(c);
    }

    public OptimizedPrimitiveTrail trail;
    public TrailPoints cache = new(100);
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || cache == null)
                return;

            ManagedShader shader = ShaderRegistry.BaseLaserShader;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.CrackedNoise), 1);
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.Streak), 2);
            shader.TrySetParameter("globalTime", Main.GlobalTimeWrappedHourly * 12f);
            trail.DrawTrail(shader, cache.Points, 80);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        return false;
    }
}