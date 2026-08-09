using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;
using ParticleRegistry = TheExtraordinaryAdditions.Common.Particles.Particle.ParticleRegistry;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;

public class HailfireHoldout : BaseIdleHoldoutProjectile
{
    public override int AssociatedItemID => ModContent.ItemType<Hailfire>();

    public override int IntendedProjectileType => ModContent.ProjectileType<HailfireHoldout>();

    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Hailfire);

    public const int WaitTime = 32;
    public ref float Wait => ref Projectile.ai[0];
    public ref float Time => ref Projectile.ai[1];
    public ref float Recoil => ref Projectile.ai[2];

    public override void Defaults()
    {
        Projectile.width = Projectile.height = 50;
        Projectile.DamageType = DamageClass.Ranged;
    }

    public Vector2 Tip => Projectile.Center + PolarVector(60f, Projectile.rotation) +
                          PolarVector(6f * Dir * Owner.gravDir, Projectile.rotation - MathHelper.PiOver2);

    public int Dir => Projectile.velocity.X.NonZeroSign();

    public override void SafeAI()
    {
        Projectile.Opacity = InverseLerp(0f, 14f, Time);

        if (this.RunLocal())
        {
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Center.SafeDirectionTo(Modded.MouseWorld), .2f);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }

        Owner.ChangeDir((Projectile.velocity.X > 0f).ToDirectionInt());
        Projectile.rotation = Projectile.velocity.ToRotation();
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation);
        Owner.SetBackHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation);

        Projectile.Center = Center + PolarVector(22f - Recoil, Projectile.rotation);

        int shell = ModContent.ProjectileType<HailfireShell>();
        if ((this.RunLocal() && Modded.SafeMouseLeft.Current) && Wait <= 0f &&
            TryUseAmmo(out _, out _, out _, out _, out _))
        {
            SoundID.Item61.Play(Tip, 1.1f, -.1f, .1f);
            Vector2 vel = Projectile.velocity.SafeNormalize(Vector2.Zero);

            for (int i = 0; i < 10; i++)
                ParticleRegistry.SpawnGlowParticle(Tip, Vector2.Zero, 10, Main.rand.NextFloat(20f, 60f),
                    Color.OrangeRed, 1.3f);
            for (int i = 0; i < 12; i++)
                ParticleRegistry.SpawnMistParticle(Tip, vel.RotatedByRandom(.5f) * Main.rand.NextFloat(2f, 6f),
                    Main.rand.NextFloat(.4f, .8f), Color.OrangeRed, Color.DarkGray, Main.rand.NextFloat(190f, 244f));

            if (this.RunLocal())
                Projectile.NewProj(Tip, vel * 22f, shell, Projectile.damage, Projectile.knockBack, Owner.whoAmI);

            Recoil = 10f;
            Wait = WaitTime;
            this.Sync();
        }

        Recoil = MathHelper.Clamp(Animators.MakePoly(3f).OutFunction.Evaluate(Recoil, -.25f, .03f), 0f, 40f);

        if (Wait > 0f)
            Wait--;

        if (prediction == null || prediction.Disposed)
            prediction = new(ratio => 5f, (coord, position) => Color.White * .2f, null, 120);
        predictionPoints ??= new(120);
        predictionPoints.SetPoints(GetBallisticPath(Tip, Projectile.velocity.SafeNormalize(Vector2.Zero) * 22f, .5f,
            120,
            16f));

        Time++;
    }

    public static List<Vector2> GetBallisticPath(
        Vector2 startPos,
        Vector2 startVelocity,
        float gravity = 0.1f,
        int steps = 120,
        float maxFallSpeed = 16f)
    {
        var points = new List<Vector2>(steps);
        Vector2 pos = startPos;
        Vector2 vel = startVelocity;

        for (int i = 0; i < steps; i++)
        {
            points.Add(pos);

            vel.Y += gravity;
            if (vel.Y > maxFallSpeed)
                vel.Y = maxFallSpeed;

            pos += vel;

            if (Collision.SolidCollision(pos, 1, 1))
                break;
        }

        return points;
    }

    private OptimizedPrimitiveTrail prediction;
    private TrailPoints predictionPoints;

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        float rotation = Projectile.rotation;
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Vector2 origin = texture.Size() * .5f;
        Main.spriteBatch.Draw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), rotation, origin,
            Projectile.scale, FixedDirection(), 0f);

        if (this.RunLocal())
        {
            void draw()
            {
                if (prediction == null || predictionPoints == null || prediction.Disposed)
                    return;

                ManagedShader shader = ShaderRegistry.SideStreakTrail;
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.Pixel), 1, SamplerState.LinearWrap);
                prediction.DrawTrail(shader, predictionPoints.Points, -1, true);
            }

            PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.HeldProjectiles);
        }

        return false;
    }
}
