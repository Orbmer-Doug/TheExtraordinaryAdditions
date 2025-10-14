using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Late;

public class StratagemMark : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Eagle500kgBomb);
    public override void SetDefaults()
    {
        Projectile.DamageType = DamageClass.Generic;
        Projectile.Size = Vector2.One * 36f;
        Projectile.timeLeft = 200;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = true;
    }

    public bool HitGround
    {
        get => Projectile.ai[0] == 1f;
        set => Projectile.ai[0] = value.ToInt();
    }
    public ref float Time => ref Projectile.ai[1];
    public ref float GroundTime => ref Projectile.ai[2];

    public const int ThrowTime = 40;
    public int Dir => Projectile.velocity.X.NonZeroSign();
    public float Completion => InverseLerp(0f, ThrowTime, Time);
    public float ThrowDisplacement()
    {
        return Projectile.velocity.ToRotation() + (MathHelper.PiOver2 * new PiecewiseCurve()
            .Add(0f, -1f, .4f, Sine.OutFunction)
            .Add(-1f, -.1f, 1f, MakePoly(4).InFunction)
            .Evaluate(Completion) * Dir);
    }

    public Player Owner => Main.player[Projectile.owner];
    public static readonly float CallInTime = SecondsToFrames(3.45f);
    public override void AI()
    {
        if (trail == null || trail.Disposed)
            trail = new(c => InverseLerp(0.015f, 0.09f, c) * 20f * InverseLerp(0f, 20f, GroundTime), (c, pos) => Color.Red * Fade, null, 40);

        if (Time < ThrowTime)
        {
            Projectile.tileCollide = false;
            float rot = ThrowDisplacement();
            Projectile.rotation = rot;
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rot - MathHelper.PiOver2);
            Projectile.Center = Owner.GetFrontHandPositionImproved() + PolarVector(Projectile.width / 2, rot);
        }
        if (Time == ThrowTime)
        {
            Projectile.tileCollide = true;
            Projectile.velocity *= 15f;
        }
        if (Time > ThrowTime)
        {
            Projectile.VelocityBasedRotation();
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + .1f, -20f, 20f);
        }

        if (HitGround)
        {
            GroundTime++;
            cache.SetPoints(Projectile.Center.GetLaserControlPoints(Projectile.Center - Vector2.UnitY * 1000f, 40));
        }
        else
            Projectile.timeLeft = (int)CallInTime + 5;

        Time++;
    }

    public float Fade => InverseLerp(0f, 15f, Projectile.timeLeft);
    public override bool ShouldUpdatePosition() => Time >= ThrowTime;
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (!HitGround)
        {
            SoundEngine.PlaySound(SoundID.NPCHit4 with { Pitch = -.1f, Volume = 1.1f }, Projectile.Center);
            HitGround = true;
        }

        if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > float.Epsilon)
            Projectile.velocity.X = -oldVelocity.X * .3f;
        if (Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > float.Epsilon)
            Projectile.velocity.Y = -oldVelocity.Y * .3f;
        Projectile.velocity *= .8f;

        return false;
    }

    public override void OnKill(int timeLeft)
    {
        if (this.RunLocal())
        {
            Vector2 pos = Projectile.Center - Vector2.UnitY.RotatedByRandom(.18f) * 1000f;
            Vector2 vel = pos.SafeDirectionTo(Projectile.Center) * 10f;
            Projectile.NewProj(pos, vel, ModContent.ProjectileType<_500kg>(), Projectile.damage, 55f, Projectile.owner);
        }
    }

    public OptimizedPrimitiveTrail trail;
    public TrailPoints cache = new(40);
    public override bool PreDraw(ref Color lightColor)
    {
        if (GroundTime > 0f)
        {
            void draw()
            {
                if (trail == null || trail.Disposed || cache == null)
                    return;

                ManagedShader shader = ShaderRegistry.SpecialLightningTrail;
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.DendriticNoise), 1);
                trail.DrawTrail(shader, cache.Points, 80);
            }
            PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        }

        Projectile.DrawBaseProjectile(lightColor * Fade);
        return false;
    }
}
