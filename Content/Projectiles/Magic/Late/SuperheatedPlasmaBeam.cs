using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles.Shader;
using TheExtraordinaryAdditions.Content.Buffs.Debuff;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Late;

public class SuperheatedPlasmaBeam : ModProjectile, ILocalizedModType, IModType
{
    public Player Owner => Main.player[Projectile.owner];

    public Projectile ProjOwner
    {
        get
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].identity == Projectile.ai[0] && Main.projectile[i].active && Main.projectile[i].owner == Projectile.owner)
                {
                    return Main.projectile[i];
                }
            }
            return null;
        }
    }

    public ref float LaserLength => ref Projectile.ai[1];
    public ref float Time => ref Projectile.ai[2];

    public override string Texture => AssetRegistry.Invis;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1200;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 32;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 5;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.hide = true;
        Projectile.scale = 0.05f;
        Projectile.ContinuouslyUpdateDamageStats = true;
    }

    public override void AI()
    {
        if (!Owner.channel || Owner.Available() == false || ProjOwner == null)
        {
            Projectile.Kill();
            return;
        }
        else
            Projectile.timeLeft = 2;

        if (trail == null || trail._disposed)
            trail = new(WidthFunction, ColorFunction, null, 100);

        Projectile.scale = MakePoly(3f).OutFunction.Evaluate(Time, 0f, 20f, 0f, 2f);
        if (this.RunLocal())
        {
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        Projectile.Center = ProjOwner.Center + Vector2.UnitY.RotatedBy(ProjOwner.rotation) * -7f + ProjOwner.velocity.SafeNormalize(Vector2.UnitY) * ProjOwner.width * .5f;

        if (this.RunLocal())
        {
            Vector2 newAimDirection = ProjOwner.velocity.SafeNormalize(Vector2.UnitY);
            Projectile.velocity = newAimDirection;
        }

        Vector2 start = Projectile.Center;
        Vector2 expected = start + Projectile.velocity * 2100f;
        Vector2 end = LaserCollision(start, expected, CollisionTarget.Tiles, out CollisionTarget hit, WidthFunction(.5f));
        cache.SetPoints(start.GetLaserControlPoints(end, 100));
        if (end != expected && hit == CollisionTarget.Tiles)
        {
            Vector2 endOfLaser = cache.Points[^1];
            Vector2 vel = endOfLaser.SafeDirectionTo(ProjOwner.Center).RotatedByRandom(.55f) * Main.rand.NextFloat(5f, 14f);
            if (Main.rand.NextBool())
            {
                ParticleRegistry.SpawnCloudParticle(endOfLaser, vel, Color.OrangeRed, Color.DarkOrange, 120, 1f, .8f);
            }

            Vector2 pos = endOfLaser + Utils.NextVector2Circular(Main.rand, 10f, 10f);
            MoltenBall.Spawn(pos, WidthFunction(1f - Main.rand.NextFloat(.1f)) + 20f);
        }

        Time++;
    }

    private float WidthFunction(float completionRatio)
    {
        float width = Projectile.scale * 20f;
        float completion = InverseLerp(0.015f, 0.15f, completionRatio);
        float maxSize = width + completionRatio * width * 1.5f;
        return MakePoly(2).OutFunction.Evaluate(16f, maxSize, completion);
    }

    private Color ColorFunction(SystemVector2 completionRatio, Vector2 position)
    {
        return MulticolorLerp(completionRatio.X, Color.White, Color.Chocolate, Color.OrangeRed, Color.OrangeRed * 1.2f) * InverseLerp(0f, .01f, completionRatio.X);
    }

    public ManualTrailPoints cache = new(100);
    public OptimizedPrimitiveTrail trail;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || cache == null)
                return;

            ManagedShader shader = ShaderRegistry.FlameTrail;
            shader.TrySetParameter("heatInterpolant", .2f + Utils.Turn01ToCyclic010(Main.GlobalTimeWrappedHourly * .5f));
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.FlameMap2), 1);
            trail.DrawTrail(shader, cache.Points, 250);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        return false;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.LineCollision(cache.Points[0], cache.Points[^1], WidthFunction(1f));
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
    {
        overWiresUI.Add(index);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Vector2 endOfLaser = Projectile.Center + Projectile.velocity * LaserLength;
        for (int i = 0; i < 6; i++)
        {
            Vector2 vel = endOfLaser.SafeDirectionTo(ProjOwner.Center) * Main.rand.NextFloat(2f, 5f);
            ParticleRegistry.SpawnGlowParticle(target.RandAreaInEntity(), vel, Main.rand.Next(18, 26), Main.rand.NextFloat(.5f, .8f), Color.Lerp(Color.Red, Color.OrangeRed, Main.rand.NextFloat(.4f, .9f)));
        }

        target.AddBuff(ModContent.BuffType<PlasmaIncineration>(), 150, false);
    }

    public override bool ShouldUpdatePosition() => false;
}