using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain.Projectiles;

public class BloodRay : ProjOwnedByNPC<StygainHeart>
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.BloodRay);
    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 4;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 24;
        Projectile.hostile = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = Lifetime;
        Projectile.Opacity = 0f;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }

    public ref float Time => ref Projectile.Additions().ExtraAI[0];

    private const int Lifetime = 400;
    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        StygainHeart.ApplyLifesteal(this, target, info.Damage);
    }

    public override void SafeAI()
    {
        if (trail == null || trail._disposed)
            trail = new(WidthFunction, ColorFunction, null, 10);
        Lighting.AddLight(Projectile.Center, Color.DarkRed.ToVector3() * 1.2f * Projectile.Opacity);
        float bump = GetLerpBump(0f, 20f, Lifetime, Lifetime - 20f, Time);

        Projectile.Opacity = Projectile.scale = bump;

        float rotAmt = MathHelper.ToRadians(.67f);
        Projectile.velocity = Projectile.velocity.RotatedBy(rotAmt);

        if (FindProjectile(out Projectile barrier, ModContent.ProjectileType<HemoglobBarrier>()))
        {
            if (Vector2.Distance(Projectile.Center, barrier.Center) >= StygainHeart.BarrierSize)
            {
                if (this.RunServer())
                {
                    barrier.ai[0] += .1f;
                    barrier.ai[2] = 1;
                }
                Projectile.Kill();
            }
        }

        if (Projectile.velocity.Length() < 16f)
            Projectile.velocity *= 1.035f;

        cache ??= new(10);
        cache.Update(Projectile.RotHitbox().Top);

        Projectile.SetAnimation(4, 8);
        Projectile.FacingUp();
        Time++;
    }

    public override void OnKill(int timeLeft)
    {
        for (int i = 0; i < 12; i++)
        {
            if (i % 4f == 3f)
            {
                ParticleRegistry.SpawnBloodStreakParticle(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.Zero),
                    Main.rand.Next(16, 30), Main.rand.NextFloat(.2f, .5f), Color.DarkRed);
            }

            Vector2 pos = Projectile.RandAreaInEntity();
            Vector2 vel = Projectile.velocity.RotatedByRandom(.25f) * Main.rand.NextFloat(.3f, .9f);
            int time = Main.rand.Next(12, 20);
            float scale = Main.rand.NextFloat(.2f, .5f);
            Color col = Color.Crimson;
            ParticleRegistry.SpawnGlowParticle(pos, vel, time, scale * 1.25f, col);
            ParticleRegistry.SpawnCloudParticle(pos, vel, col, Color.DarkRed, time, Projectile.width * scale * 2f, Main.rand.NextFloat(.5f, 1f));
        }
    }

    internal Color ColorFunction(SystemVector2 completionRatio, Vector2 position)
    {
        float fadeOpacity = (float)Math.Sqrt(Projectile.timeLeft / 100f);
        return Color.Red * fadeOpacity;
    }

    internal float WidthFunction(float completionRatio)
    {
        return MathHelper.SmoothStep(Projectile.height * .5f, 0f, completionRatio) * Projectile.scale;
    }

    public TrailPoints cache;
    public OptimizedPrimitiveTrail trail;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || trail._disposed || cache == null)
                return;
            ManagedShader shader = ShaderRegistry.FadedStreak;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.StreakMagma), 1);
            trail.DrawTrail(shader, cache.Points, 30);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        SpriteEffects direction = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        Projectile.DrawBaseProjectile(Projectile.GetAlpha(Color.White), direction);
        return false;
    }
}
