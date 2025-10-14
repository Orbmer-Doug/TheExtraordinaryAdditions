using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Late.Zenith;

public class ConcentratedEnergy : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.ConcentratedEnergy);

    public override void SetDefaults()
    {
        Projectile.width = 74;
        Projectile.height = 30;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 200;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.tileCollide = false;
        Projectile.localNPCHitCooldown = 10;
        Projectile.usesLocalNPCImmunity = true;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.LineCollision(Projectile.BaseRotHitbox().Left, Projectile.BaseRotHitbox().Right, Projectile.height);
    }

    public override bool? CanHitNPC(NPC target) => HasHitTarget ? false : null;
    public bool HasHitTarget
    {
        get => Projectile.ai[0] == 1f;
        set => Projectile.ai[0] = value.ToInt();
    }

    public ref float Time => ref Projectile.ai[1];
    public override void AI()
    {
        if (trail == null || trail.Disposed)
            trail = new(WidthFunction, ColorFunction, null, 10);

        Lighting.AddLight(Projectile.Center, Color.Fuchsia.ToVector3() * 1.2f * Projectile.scale);

        if (Time > SecondsToFrames(.5f) && HasHitTarget == false)
        {
            if (NPCTargeting.TryGetClosestNPC(new(Projectile.Center, 800, false, true), out NPC target))
                Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Projectile.SafeDirectionTo(target.Center) * 30f, .22f);
        }

        if (HasHitTarget == true)
        {
            int type = (Projectile.identity % 2f == 0f).ToDirectionInt();
            Projectile.velocity = Projectile.velocity.RotatedBy(MathHelper.Pi / 120f * type) * 0.9f;
            Projectile.Center += Main.rand.NextVector2CircularEdge(3f, 3f);
            Projectile.Opacity = InverseLerp(0f, 40f, Projectile.timeLeft);
        }
        else
        {
            Projectile.Opacity = Projectile.scale = InverseLerp(0f, 20f, Projectile.timeLeft);
        }
        Projectile.FacingRight();

        cache ??= new(10);
        cache.Update(Projectile.RotHitbox().Left);

        Time++;
    }

    internal Color ColorFunction(SystemVector2 completionRatio, Vector2 position)
    {
        float fadeToEnd = MathHelper.Lerp(0.65f, 1f, (float)Cos01((0f - Main.GlobalTimeWrappedHourly) * 3f));
        float fadeOpacity = Utils.GetLerpValue(1f, 0.64f, completionRatio.X, true) * InverseLerp(0f, 8f, Time) * Projectile.Opacity;
        Color endColor = Color.Lerp(Color.DarkMagenta, Color.Cyan, (float)Sin01(completionRatio.X * (float)Math.PI * 1.6f - Main.GlobalTimeWrappedHourly * 4f));
        return Color.Lerp(Color.White, endColor, fadeToEnd) * fadeOpacity;
    }

    internal float WidthFunction(float completionRatio)
    {
        return Projectile.width * 0.4f * MathHelper.SmoothStep(0.2f, 1f, Utils.GetLerpValue(0f, 0.3f, completionRatio, true)) * Projectile.Opacity;
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
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.ShadowTrail), 1);
                trail.DrawTrail(shader, cache.Points, 40, true);
            }
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        Texture2D texture = Projectile.ThisProjectileTexture();
        Main.spriteBatch.DrawBetter(texture, Projectile.Center, null, Color.Lerp(lightColor, Color.White, 0.5f) * Projectile.Opacity,
            Projectile.rotation, texture.Size() / 2f, Projectile.scale);

        return false;
    }

    public override void OnKill(int timeLeft)
    {
        SoundID.DD2_WitherBeastDeath.Play(Projectile.Center, 1.1f, 0f, .1f);

        if (NPCTargeting.TryGetClosestNPC(new(Projectile.Center, 900, false, true), out NPC target))
        {
            Vector2 targ = target.RandAreaInEntity();
            Vector2 pos = Projectile.RotHitbox().Left + Projectile.velocity.SafeNormalize(Vector2.Zero) * 22f;
            if (target.CanHomeInto() && this.RunLocal())
            {
                Vector2 vel = Projectile.SafeDirectionTo(targ) * Main.rand.NextFloat(9f, 14f);
                Projectile.NewProj(pos, vel, ModContent.ProjectileType<NeedleStar>(), Projectile.damage / 2, Projectile.knockBack, Projectile.owner);
                SoundEngine.PlaySound(SoundID.Item105 with { Volume = .8f, MaxInstances = 20, Pitch = .4f }, Projectile.Center);
                ParticleRegistry.SpawnDetailedBlastParticle(pos, Vector2.Zero, new Vector2(.45f, 1f) * 60f, Vector2.Zero, 30, Color.Magenta, vel.ToRotation(), null, true);
                ParticleRegistry.SpawnDetailedBlastParticle(pos, Vector2.Zero, new Vector2(.45f, 1f) * 90f, Vector2.Zero, 30, Color.Magenta, vel.ToRotation(), null, true);
            }

            const int amount = 30;
            for (int i = 0; i < amount; i++)
            {
                Vector2 vel = NextVector2Ellipse(5f, 10f, Projectile.AngleTo(targ));
                float scale = Main.rand.NextFloat(.3f, .6f);
                ParticleRegistry.SpawnSparkParticle(pos, vel, 40, scale, Color.DarkViolet.Lerp(Color.Violet, Main.rand.NextFloat(.2f, .9f)));
            }
        }
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (!HasHitTarget)
        {
            Projectile.velocity *= 2f;
            Projectile.timeLeft = 40;
            HasHitTarget = true;
        }

        Vector2 pos = Projectile.BaseRotHitbox().Right;
        for (int i = 0; i < 12; i++)
        {
            Vector2 vel = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(.2f) * -Main.rand.NextFloat(2f, 12f);
            int life = Main.rand.Next(20, 40);
            float scale = Main.rand.NextFloat(.5f, .78f);
            Color col = Color.Magenta.Lerp(Color.DodgerBlue, Main.rand.NextFloat());
            ParticleRegistry.SpawnBloomPixelParticle(pos, vel, life, scale * 1.2f, col, Color.White, null, 1.1f);
            ParticleRegistry.SpawnSparkleParticle(pos, vel, life, scale * 1.3f, col, Color.White, 1.4f, .14f);
        }
    }
}
