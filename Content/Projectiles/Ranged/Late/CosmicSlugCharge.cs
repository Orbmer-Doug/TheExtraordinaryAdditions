using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;

public class CosmicSlugCharge : ModProjectile, ILocalizedModType, IModType
{
    internal static readonly int UpdateCount = 8;
    internal static readonly int Lifetime = UpdateCount * SecondsToFrames(2);
    public override string Texture => AssetRegistry.Invis;

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 2000;
    }

    public override void SetDefaults()
    {
        Projectile.width =
        Projectile.height = 12;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.friendly = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = Lifetime;
        Projectile.MaxUpdates = UpdateCount;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 40;
    }
    public override bool? CanDamage() => null;
    public override void AI()
    {
        Projectile.Opacity = Projectile.scale = GetLerpBump(0f, .01f, 1f, .9f, InverseLerp(0f, Lifetime, Projectile.timeLeft));
        Lighting.AddLight(Projectile.Center, Color.BlueViolet.ToVector3() * 2f);

        cache ??= new(35);
        cache.Update(Projectile.Center);
    }

    public override bool OnTileCollide(Vector2 oldVelocity) => false;
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Projectile.velocity *= .9f;
        Projectile.damage = (int)(Projectile.damage * .9f);
        for (int i = 0; i < 20; i++)
        {
            ParticleRegistry.SpawnSparkParticle(Projectile.Center, -Projectile.velocity.RotatedByRandom(.1f) * i * .1f, 30, .5f, Color.White);
            if (i % 2f == 0f)
            {
                ParticleRegistry.SpawnSparkleParticle(Projectile.Center, -Projectile.velocity.RotatedByRandom(.2f) * Main.rand.NextFloat(2f, 8f),
                    38, Main.rand.NextFloat(.3f, .6f), Color.Cyan, Color.BlueViolet, 1.7f);
            }
        }

    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        modifiers.ScalingArmorPenetration += 1f;
        modifiers.DefenseEffectiveness *= 0f;
    }

    internal Color ColorFunction(SystemVector2 completionRatio, Vector2 position)
    {
        float fadeOpacity = Math.Min(Projectile.timeLeft / (float)cache.Points.Length, 1f);
        return Color.Lerp(Color.Violet, Color.BlueViolet, completionRatio.X) * fadeOpacity * Projectile.Opacity;
    }

    internal float WidthFunction(float completionRatio)
    {
        float width = Math.Min(Projectile.timeLeft / (float)cache.Points.Length, 1f);
        return (1f - completionRatio) * Projectile.width * width * Projectile.scale;
    }

    public TrailPoints cache;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            ManagedShader shader = ShaderRegistry.SmoothFlame;
            shader.TrySetParameter("heatInterpolant", 1f);
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.StreakLightning), 1);
            OptimizedPrimitiveTrail trail = new(WidthFunction, ColorFunction, null, 35);
            trail.DrawTrail(shader, cache.Points, 90);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        void flare()
        {
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Texture2D star = AssetRegistry.GetTexture(AdditionsTexture.LensStar);
            Main.EntitySpriteDraw(star, pos, null, ColorFunction(SystemVector2.One / 2, Vector2.Zero), Projectile.velocity.ToRotation(), star.Size() * .5f, Projectile.Opacity * .5f, 0, 0f);
            Main.EntitySpriteDraw(star, pos, null, Color.White, Projectile.velocity.ToRotation(), star.Size() * .5f, Projectile.Opacity * .25f, 0, 0f);

            Texture2D bloom = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);
            Main.EntitySpriteDraw(bloom, pos, null, ColorFunction(SystemVector2.One / 2, Vector2.Zero) * .5f, Projectile.velocity.ToRotation(), bloom.Size() * .5f, Projectile.Opacity * .5f, 0, 0f);
        }
        PixelationSystem.QueueTextureRenderAction(flare, PixelationLayer.UnderProjectiles, BlendState.Additive);
        
        return false;
    }
}
