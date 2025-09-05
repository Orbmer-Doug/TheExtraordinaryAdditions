using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;

public class HolyDart : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.penetrate = 2;
        Projectile.timeLeft = 240;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.tileCollide = false;
    }

    public override void OnSpawn(IEntitySource source)
    {
        for (int i = 0; i < 15; i++)
        {
            Vector2 vel = Projectile.velocity.RotatedByRandom(.28f) * Main.rand.NextFloat(.8f, 2f);
            int life = Main.rand.Next(20, 30);
            float scale = Main.rand.NextFloat(.6f, .9f);
            Color col = Color.Gold.Lerp(Color.LightGoldenrodYellow, Main.rand.NextFloat(.3f, .6f));
            ParticleRegistry.SpawnSparkleParticle(Projectile.Center + Main.rand.NextVector2Circular(8f, 8f), vel, life, scale, col, col * 2f);
            ParticleRegistry.SpawnHeavySmokeParticle(Projectile.Center + Main.rand.NextVector2Circular(9f, 9f), vel, life, scale, col);
        }

        Projectile.netUpdate = true;
    }

    public ref float Time => ref Projectile.ai[0];
    public override void AI()
    {
        cache ??= new(30);
        cache.Update(Projectile.Center);

        if (Projectile.numHits > 0 || Projectile.timeLeft < 40)
        {
            Projectile.velocity *= .6f;
            Projectile.Opacity *= .85f;
            if (Projectile.timeLeft > 40)
                Projectile.timeLeft = 40;
        }

        Lighting.AddLight(Projectile.Center, Color.Goldenrod.ToVector3() * Projectile.scale);

        Projectile.scale = Animators.MakePoly(4f).InOutFunction(InverseLerp(0f, 30f, Time));
        if (Projectile.scale >= 1f && Projectile.numHits <= 0)
        {
            if (NPCTargeting.TryGetClosestNPC(new(Projectile.Center, 1400), out NPC npc) && npc.GetGlobalNPC<HolyGlobalNPC>().MarkedTime > 0)
                Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Projectile.SafeDirectionTo(npc.Center) * MathF.Min(Projectile.Distance(npc.Center), 20f), .4f);
        }

        Projectile.rotation = Projectile.velocity.ToRotation();
        Time++;
    }

    internal Color ColorFunction(SystemVector2 completionRatio, Vector2 position)
    {
        float fadeToEnd = MathHelper.Lerp(0.65f, 1f, Cos01((0f - Main.GlobalTimeWrappedHourly) * 3f));
        float fadeOpacity = Utils.GetLerpValue(1f, 0.64f, completionRatio.X, true) * Projectile.Opacity;
        Color endColor = Color.Lerp(Color.Goldenrod, Color.PaleGoldenrod, Sin01(completionRatio.X * (float)Math.PI * 1.6f - Main.GlobalTimeWrappedHourly * 4f));
        return Color.Lerp(Color.Yellow, endColor, fadeToEnd) * fadeOpacity;
    }

    internal float WidthFunction(float completionRatio)
    {
        return Projectile.height / 2 * MathHelper.SmoothStep(1f, 0f, completionRatio);
    }

    public TrailPoints cache;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            ManagedShader shader = ShaderRegistry.SmoothFlame;
            shader.TrySetParameter("heatInterpolant", 2f);
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.CrackedNoise), 1);
            OptimizedPrimitiveTrail trail = new(WidthFunction, ColorFunction, null, 30);
            trail.DrawTrail(shader, cache.Points, 120);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        void flare()
        {
            for (float i = .1f; i < .4f; i += .1f)
            {
                Texture2D bloomTexture = AssetRegistry.GetTexture(AdditionsTexture.LensStar);
                Color color = Main.hslToRgb(Main.GlobalTimeWrappedHourly * 0.6f % 1f, 1f, 0.85f, byte.MaxValue);
                Vector2 bloomCenter = cache.Points[0] - Main.screenPosition;
                Main.EntitySpriteDraw(bloomTexture, bloomCenter, null, color * 0.6f * Projectile.Opacity, Projectile.rotation, bloomTexture.Size() / 2f, i * Projectile.scale, 0, 0f);
            }
        }
        PixelationSystem.QueueTextureRenderAction(flare, PixelationLayer.UnderProjectiles, BlendState.Additive);

        return false;
    }
    public override bool? CanHitNPC(NPC target) => Projectile.numHits <= 0;
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        SoundEngine.PlaySound(SoundID.DD2_WitherBeastDeath with { Volume = Main.rand.NextFloat(1.5f, 2.2f), PitchVariance = .3f, MaxInstances = 20 }, Projectile.Center, null);
    }
}
