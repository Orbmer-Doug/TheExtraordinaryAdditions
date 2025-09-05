using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Late.Zenith;

public class NeedleStar : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.Invis;
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
        if (trail == null || trail._disposed)
            trail = new(tip, WidthFunction, ColorFunction, null, 30);

        cache ??= new(20);
        cache.Update(Projectile.Center);

        if (Projectile.numHits > 0 || Projectile.timeLeft < 20)
        {
            Projectile.velocity *= .96f;
            Projectile.timeLeft = 20;
            if (cache.Points.AllPointsEqual())
                Projectile.Kill();
        }

        Projectile.Opacity = InverseLerp(0f, 5f * Projectile.MaxUpdates, Time) * InverseLerp(0f, 2f, Projectile.velocity.Length());
        Time++;
    }

    internal Color ColorFunction(SystemVector2 completionRatio, Vector2 position)
    {
        float fadeToEnd = MathHelper.Lerp(0.65f, 1f, (float)Cos01((0f - Main.GlobalTimeWrappedHourly) * 3f));
        float fadeOpacity = Utils.GetLerpValue(1f, 0.64f, completionRatio.X, true) * Projectile.Opacity;
        Color endColor = Color.Lerp(Color.Cyan, Color.Magenta, (float)Sin01(completionRatio.X * (float)Math.PI * 1.6f - Main.GlobalTimeWrappedHourly * 4f));
        return Color.Lerp(Color.White, endColor, fadeToEnd) * fadeOpacity;
    }

    internal float WidthFunction(float completionRatio)
    {
        return MathHelper.SmoothStep(Projectile.height * .75f, 0f, completionRatio);
    }

    public TrailPoints cache;
    public OptimizedPrimitiveTrail trail;
    public static readonly ITrailTip tip = new RoundedTip(12);
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
                ManagedShader shader = ShaderRegistry.FadedStreak;
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.StreakMagma), 1);
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.WavyBlotchNoise), 2);
                trail.DrawTippedTrail(shader, cache.Points, tip, true, 100);
            }
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        void star()
        {
            Texture2D starTexture = AssetRegistry.GetTexture(AdditionsTexture.CritSpark);
            Texture2D bloomTexture = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);
            Color color = ColorFunction(SystemVector2.Zero, Vector2.Zero);
            float rotation = Main.GlobalTimeWrappedHourly * 8f;

            Main.spriteBatch.DrawBetterRect(bloomTexture, ToTarget(Projectile.Center, new Vector2(50)), null, color * .6f, 0f, bloomTexture.Size() / 2);
            Main.spriteBatch.DrawBetterRect(bloomTexture, ToTarget(Projectile.Center, new Vector2(90)), null, color * .4f, 0f, bloomTexture.Size() / 2);
            Main.spriteBatch.DrawBetter(starTexture, Projectile.Center, null, Color.White * Projectile.Opacity, rotation, starTexture.Size() / 2, Projectile.scale * 2.3f);
            Main.spriteBatch.DrawBetter(starTexture, Projectile.Center, null, Color.White * Projectile.Opacity, -rotation + MathHelper.PiOver4, starTexture.Size() / 2, Projectile.scale * 1.6f);
        }
        PixelationSystem.QueueTextureRenderAction(star, PixelationLayer.UnderProjectiles, BlendState.Additive);

        return false;
    }
}