using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;

public class HeavyLaser : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    private const int MaxUpdates = 5;
    private const int Lifetime = 35 * MaxUpdates;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = MaxLaserLength;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 38;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.MaxUpdates = MaxUpdates;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.penetrate = -1;
        Projectile.scale = 1f;
        Projectile.timeLeft = Lifetime;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 15;
    }
    private const int MaxLaserLength = 3000;
    public ref float Time => ref Projectile.ai[0];
    public ref float LaserLength => ref Projectile.ai[1];
    public bool HitSomething
    {
        get => Projectile.ai[2] == 1f;
        set => Projectile.ai[2] = value.ToInt();
    }
    public ref float LaserExpansion => ref Projectile.Additions().ExtraAI[0];
    public Vector2 Start => Projectile.Center;
    public Vector2 End;
    public override void AI()
    {
        if (trail == null || trail._disposed)
            trail = new(WidthFunct, ColorFunct, null, 90);

        Projectile.scale = Convert01To010(Utils.GetLerpValue(0f, Lifetime, Time, true));
        Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.Zero);

            Vector2 expected = Start + Projectile.velocity * LaserLength;

        if (!HitSomething)
        {
            LaserExpansion++;
            LaserLength = Utils.Remap(LaserExpansion, 0f, Lifetime, 0f, MaxLaserLength);
            End = LaserCollision(Start, expected, CollisionTarget.Tiles | CollisionTarget.NPCs, WidthFunct(.5f));
        }

        if (End != expected && !HitSomething)
        {
            for (int i = 0; i < 38; i++)
            {
                float completion = InverseLerp(0f, 38f, i);
                if (i % 2 == 1)
                {
                    ParticleRegistry.SpawnDetailedBlastParticle(End, Vector2.Zero, Vector2.One * 150f * completion,
                        Vector2.Zero, 40 - MultiLerp(completion, 30, 20, 10, 0), Color.OrangeRed);
                }

                ParticleRegistry.SpawnSparkleParticle(End, Main.rand.NextVector2Unit() * Main.rand.NextFloat(4f, 10f), Main.rand.Next(30, 50), Main.rand.NextFloat(.4f, .8f), Color.OrangeRed, Color.Chocolate, Main.rand.NextFloat(1f, 1.4f), .15f);
                ParticleRegistry.SpawnSparkParticle(End, Main.rand.NextVector2CircularLimited(20f, 20f, .6f, 1f),
                    Main.rand.Next(50, 90), Main.rand.NextFloat(.8f, 1.1f), Color.OrangeRed.Lerp(Color.Chocolate, .4f), true);
            }

            ScreenShakeSystem.New(new(.4f, .2f), End);
            AdditionsSound.etherealSlam.Play(End, .8f, -.35f, .1f, 8);
            Projectile.CreateFriendlyExplosion(End, new(150), Projectile.damage, Projectile.knockBack, 10, 10);

            HitSomething = true;
        }

        trailPoints.SetPoints(Start.GetLaserControlPoints(End, 90));

        Time++;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => HitSomething ? false : null;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, End + Projectile.velocity * 30f);
    }

    public OptimizedPrimitiveTrail trail;
    public ManualTrailPoints trailPoints = new(90);
    public float WidthFunct(float c)
    {
        float tipInterpolant = MathF.Sqrt(1f - Animators.MakePoly(4f).InFunction(InverseLerp(0.2f, 0f, 1f - c)));
        return tipInterpolant * Projectile.scale * Projectile.width;
    }
    public Color ColorFunct(SystemVector2 c, Vector2 position)
    {
        return Color.Lerp(Color.OrangeRed.Lerp(Color.Chocolate, .32f), Color.Red, 0.5f) * InverseLerp(0f, .09f, c.X);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || trailPoints == null)
                return;

            ManagedShader shader = ShaderRegistry.CrunchyLaserShader;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.FlameMap1), 1);
            trail.DrawTrail(shader, trailPoints.Points);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        return false;
    }
}