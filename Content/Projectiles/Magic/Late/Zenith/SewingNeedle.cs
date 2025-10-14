using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Late.Zenith;

public class SewingNeedle : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.SewingNeedle);
    public ref float Time => ref Projectile.ai[0];
    public Player Owner => Main.player[Projectile.owner];
    public override void SetDefaults()
    {
        Projectile.width = 46;
        Projectile.height = 286;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 300;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.tileCollide = false;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 15;
    }

    public override void AI()
    {
        if (trail == null || trail.Disposed)
            trail = new(WidthFunct, ColorFunct, null, 14);

        Projectile.Opacity = GetLerpBump(0f, 5f, 300f, 280f, Time);
        Lighting.AddLight(Projectile.Center, Color.Fuchsia.ToVector3() * 2.5f * Projectile.Opacity);
        Projectile.FacingUp();

        cache ??= new(20);
        cache.Update(Projectile.RotHitbox().Bottom);

        Time++;
    }

    public float WidthFunct(float completionRatio)
    {
        float expansionCompletion = (float)Math.Pow(1f - completionRatio, 2.0);
        return MathHelper.Lerp(30f, 44f * Projectile.scale, expansionCompletion);
    }

    public static Color ColorFunct(SystemVector2 completionRatio, Vector2 position)
    {
        float trailOpacity = GetLerpBump(0f, .05f, 1f, .65f, completionRatio.X);
        Color startingColor = Color.Lerp(Color.Magenta, Color.White, 0.4f);
        Color middleColor = Color.Lerp(Color.Cyan, Color.Red, 0.2f);
        Color endColor = Color.Lerp(Color.DarkBlue, Color.Red, 0.67f);
        return MulticolorLerp(completionRatio.X, startingColor, middleColor, endColor) * trailOpacity;
    }

    public TrailPoints cache;
    public OptimizedPrimitiveTrail trail;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail != null)
            {
                ManagedShader shader = ShaderRegistry.FadedStreak;
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.Streak2), 1);
                trail.DrawTrail(shader, cache.Points, 120);
            }
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        Texture2D texture = Projectile.ThisProjectileTexture();
        Main.spriteBatch.DrawBetter(texture, Projectile.Center, null, Color.Lerp(lightColor, Color.White, 0.5f) * Projectile.Opacity, Projectile.rotation, texture.Size() / 2f, Projectile.scale, 0);

        void glows()
        {
            Vector2 beeg = Projectile.Center - PolarVector(94f, Projectile.rotation - MathHelper.PiOver2);
            Vector2 smol = Projectile.Center - PolarVector(128f, Projectile.rotation - MathHelper.PiOver2);
            Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.GlowHarsh);
            Vector2 orig = tex.Size() / 2;
            float wave = MathF.Sin(Main.GlobalTimeWrappedHourly * 1.5f) * .5f + 1.5f;

            for (float i = .9f; i < 1.1f; i += .1f)
            {
                Main.spriteBatch.DrawBetterRect(tex, ToTarget(beeg, new Vector2(90 * wave * 1.5f)), null, Color.White * Projectile.Opacity * i, 0f, orig);
                Main.spriteBatch.DrawBetterRect(tex, ToTarget(smol, new Vector2(45 * wave)), null, Color.White * Projectile.Opacity * i, 0f, orig);
            }
        }
        PixelationSystem.QueueTextureRenderAction(glows, PixelationLayer.OverProjectiles, BlendState.Additive);

        return false;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.LineCollision(Projectile.BaseRotHitbox().Bottom, Projectile.BaseRotHitbox().Top, Projectile.width * Projectile.scale);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Vector2 smokeDirection = Projectile.velocity / 6;
        Vector2 pos = Projectile.BaseRotHitbox().Top;
        for (int i = 0; i < 20; i++)
        {
            Color col = Color.Lerp(Color.Crimson, Color.BlueViolet, Main.rand.NextFloat() + Main.GlobalTimeWrappedHourly);

            Vector2 vel = smokeDirection.RotatedByRandom(0.6f) * Main.rand.NextFloat(.6f, 1.2f);
            vel.Y -= 3f * (i / 20);

            int life = Main.rand.Next(84, 100);
            float scale = Main.rand.NextFloat(1f, 1.8f);

            ParticleRegistry.SpawnHeavySmokeParticle(pos, vel, life, scale, col, scale, true);
            ParticleRegistry.SpawnSparkleParticle(pos, vel * 2, life, scale / 2, col, Color.White, 1.7f);

            if (i % 2 == 1)
                ParticleRegistry.SpawnCloudParticle(pos, vel, col, Color.Lerp(Color.DarkMagenta, Color.Crimson, Main.rand.NextFloat(.2f, .8f)), life, scale * 90.8f, Main.rand.NextFloat(.5f, .7f), 1);
        }

        int slashCreatorID = ModContent.ProjectileType<SeamsCreator>();
        if (Owner.ownedProjectileCounts[slashCreatorID] < 20 && this.RunLocal())
        {
            Projectile.NewProj(target.Center, Vector2.Zero, slashCreatorID, Projectile.damage, Projectile.knockBack, Projectile.owner, target.whoAmI, RandomRotation(), 0f);
        }

        Projectile.damage = (int)(Projectile.damage * .9f);
    }
}
