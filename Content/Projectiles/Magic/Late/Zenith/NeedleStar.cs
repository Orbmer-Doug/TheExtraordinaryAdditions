using System;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics.Meshes;
using TheExtraordinaryAdditions.Core.Graphics.Resources;
using TheExtraordinaryAdditions.Core.Graphics.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Late.Zenith;

public class NeedleStar : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GennedTextures.Invisible.Path;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 25;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = 2;
        Projectile.extraUpdates = 5;
        Projectile.timeLeft = 120;
        Projectile.localNPCHitCooldown = 20;
        Projectile.usesLocalNPCImmunity = true;
    }

    public ref float Time => ref Projectile.ai[0];

    public override void AI()
    {
        if (trail == null || trail.Disposed)
            trail = new(WidthFunction, ColorFunction, null, 30);

        cache ??= new(20);
        cache.Update(Projectile.Center);

        if (Projectile.numHits > 0 || Projectile.timeLeft < 20)
        {
            Projectile.velocity *= .96f;
            Projectile.timeLeft = 20;
            if (cache.Points.AllPointsEqual())
                Projectile.Kill();
        }

        Projectile.Opacity = InverseLerp(0f, 5f * Projectile.MaxUpdates, Time) *
                             InverseLerp(0f, 2f, Projectile.velocity.Length());
        Time++;
    }

    internal Color ColorFunction(SystemVector2 completionRatio, Vector2 position)
    {
        float fadeToEnd = MathHelper.Lerp(0.65f, 1f, Cos01((0f - Main.GlobalTimeWrappedHourly) * 3f));
        float fadeOpacity = Utils.GetLerpValue(1f, 0.64f, completionRatio.X, true) * Projectile.Opacity;
        Color endColor = Color.Lerp(Color.Cyan, Color.Magenta,
            Sin01(completionRatio.X * (float) Math.PI * 1.6f - Main.GlobalTimeWrappedHourly * 4f));
        return Color.Lerp(Color.White, endColor, fadeToEnd) * fadeOpacity;
    }

    internal float WidthFunction(float c)
    {
        return Trail.HemisphereWidthFunct(c, MathHelper.SmoothStep(Projectile.height * .75f, 0f, c));
    }

    public TrailPoints cache;
    public Trail trail;
    public override bool? CanHitNPC(NPC target) => Projectile.numHits <= 0 ? null : false;

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        SoundID.DD2_WitherBeastCrystalImpact.Play(Projectile.Center, .7f, 0f, .1f);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail != null)
            {
                ManagedShader shader = AssetRegistry.GennedShaders.FadedStreak;
                shader.SetTexture(AssetRegistry.GennedTextures.StreakMagma, 1);
                shader.SetTexture(AssetRegistry.GennedTextures.WavyBlotchNoise, 2);
                trail.DrawTrail(shader, cache.Points, 100);
            }
        }

        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        Texture2D starTexture = AssetRegistry.GennedTextures.CritSpark;
        Texture2D bloomTexture = AssetRegistry.GennedTextures.GlowParticleSmall;
        Color color = ColorFunction(SystemVector2.Zero, Vector2.Zero);
        float rotation = Main.GlobalTimeWrappedHourly * 8f;

        SpriteBatch.DrawRectPixelated(PixelationLayer.UnderProjectiles, BlendState.Additive, bloomTexture,
            ToTarget(Projectile.Center, new Vector2(50)), null,
            color * .6f, 0f, bloomTexture.Size() / 2);
        SpriteBatch.DrawRectPixelated(PixelationLayer.UnderProjectiles, BlendState.Additive, bloomTexture,
            ToTarget(Projectile.Center, new Vector2(90)), null,
            color * .4f, 0f, bloomTexture.Size() / 2);
        SpriteBatch.DrawAltPixelated(PixelationLayer.UnderProjectiles, BlendState.Additive, starTexture,
            Projectile.Center, null, Color.White * Projectile.Opacity,
            rotation, starTexture.Size() / 2, Projectile.scale * 2.3f);
        SpriteBatch.DrawAltPixelated(PixelationLayer.UnderProjectiles, BlendState.Additive, starTexture,
            Projectile.Center, null, Color.White * Projectile.Opacity,
            -rotation + MathHelper.PiOver4, starTexture.Size() / 2, Projectile.scale * 1.6f);

        return false;
    }
}
