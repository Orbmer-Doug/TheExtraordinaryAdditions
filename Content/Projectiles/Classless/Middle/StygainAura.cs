using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Middle;

public class StygainAura : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 1;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 15;
        Projectile.penetrate = -1;
        Projectile.DamageType = DamageClass.Generic;
    }

    public Player Owner => Main.player[Projectile.owner];
    public ref float Radius => ref Projectile.ai[0];
    public ref float Time => ref Projectile.ai[1];
    public ref float StoredDamage => ref Projectile.ai[2];
    private static readonly float TotalTime = SecondsToFrames(3);
    public static readonly int CooldownTime = SecondsToFrames(10);
    public float Completion => InverseLerp(0f, 30f, Time) * InverseLerp(TotalTime, TotalTime - 20f, Time);
    public override void AI()
    {
        if (Time > TotalTime)
        {
            Projectile.Kill();
            return;
        }
        if (trail == null || trail.Disposed)
            trail = new(c => 6f + StoredDamage * .002f, (c, pos) => Color.Lerp(Color.White, Color.Red * 1.4f, Completion + .3f), null, 40);

        Time++;

        Projectile.Center = Owner.Center;
        Radius = 100f * Completion;

        for (int i = 0; i < 40; i++)
            points.SetPoint(i, Projectile.Center + Vector2.One.RotatedBy(i / 19f * MathHelper.TwoPi) * (Radius + 20));
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (StoredDamage < 10000)
        {
            if (target.boss && target.realLife <= 0)
            {
                StoredDamage += damageDone * 3f;
            }
            else
                StoredDamage += damageDone;
        }

        for (int i = 0; i < 28; i++)
        {
            Vector2 pos = target.RandAreaInEntity();
            Vector2 vel = pos.SafeDirectionTo(Owner.Center).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(4f, 12f);
            if (i.BetweenNum(0, 4))
            {
                ParticleRegistry.SpawnBloodStreakParticle(pos, vel, 40, Main.rand.NextFloat(.2f, .4f), Color.DarkRed);
            }
            ParticleRegistry.SpawnGlowParticle(pos, vel, 40, Main.rand.NextFloat(20f, 30f), Color.DarkRed);
        }
    }

    public override void OnKill(int timeLeft)
    {
        if (!(Owner.lifeSteal <= 0f))
            Owner.Heal((int)(StoredDamage * .002f));

        if (StoredDamage >= 10000f)
        {
            Vector2 pos = Owner.Center + new Vector2(-4f * Owner.direction, -20f);
            Vector2 vel = pos.SafeDirectionTo(Owner.Additions().mouseWorld).SafeNormalize(Vector2.Zero);
            for (float i = 1f; i > 0f; i -= .2f)
                ParticleRegistry.SpawnPulseRingParticle(Owner.Center, vel * (14f * i), 40, vel.ToRotation(), new(.5f, 1f), 0f, i * 320f, Color.Crimson);
            if (this.RunLocal())
                Projectile.NewProj(pos, vel, ModContent.ProjectileType<SanguineRay>(), Projectile.damage * 2, 0f, Owner.whoAmI);
            
            AdditionsSound.VirtueAttack.Play(pos, .8f, -.3f);
        }
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => CircularHitboxCollision(Projectile.Center, Radius + 75, targetHitbox);

    public OptimizedPrimitiveTrail trail;
    public TrailPoints points = new(40);
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || points == null)
                return;

            ManagedShader prim = ShaderRegistry.EnlightenedBeam;
            prim.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.ShadowTrail), 1, SamplerState.LinearWrap);
            prim.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.WavyBlotchNoise), 2, SamplerState.LinearWrap);
            prim.TrySetParameter("time", Main.GlobalTimeWrappedHourly * 2f);
            prim.TrySetParameter("repeats", 10f);
            trail.DrawTrail(prim, points.Points, 100, true);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        return false;
    }
}
