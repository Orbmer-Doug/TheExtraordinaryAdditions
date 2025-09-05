using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;

public class InfluxWaverSwing : BaseSwordSwing
{
    public override string Texture => ItemID.InfluxWaver.GetTerrariaItem();
    public override int SwingTime => Item.useAnimation;

    public override void SafeInitialize()
    {
        after ??= new(8, () => Projectile.Center);

        // Reset arrays
        after.afterimages = null;
    }

    public override void SafeAI()
    {
        Projectile.Center = Owner.GetFrontHandPositionImproved();
        Owner.heldProj = Projectile.whoAmI;
        Owner.SetDummyItemTime(2);
        Owner.ChangeDir(Direction);
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation - SwordRotation);
        Owner.itemRotation = WrapAngle(Projectile.rotation);
        Projectile.rotation = SwingOffset();

        if (Animation() >= .26f && !PlayedSound)
        {
            if (this.RunLocal())
                Main.projectile[Projectile.NewProj(Center, Projectile.velocity * 7f, ModContent.ProjectileType<InfluxWaverProj>(),
                    Projectile.damage, Projectile.knockBack, Projectile.owner)].As<InfluxWaverProj>().Copy = false;

            SoundID.Item1.Play(Projectile.Center, 1f, 0f, .2f);
            PlayedSound = true;
        }

        after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One * Projectile.scale, Projectile.Opacity, Projectile.rotation, Effects, 90));

        if (VanishTime <= 0)
        {
            Projectile.scale = MakePoly(2f).OutFunction(InverseLerp(0f, 10f * MaxUpdates, OverallTime)) * MeleeScale;
        }
        else
        {
            Projectile.scale = MakePoly(3f).OutFunction.Evaluate(VanishTime, 0f, 18f * MaxUpdates, MeleeScale, 0f);
            if (Projectile.scale <= 0f)
                KillEffect();
            VanishTime++;
        }

        if (this.RunLocal() && SwingCompletion >= 1f)
        {
            if (Modded.SafeMouseLeft.Current && VanishTime <= 0)
            {
                SwingDir = SwingDir == SwingDirection.Up ? SwingDirection.Down : SwingDirection.Up;
                Initialized = false;
                this.Sync();
            }
            else
            {
                VanishTime++;
                this.Sync();
            }
        }
    }

    public override void NPCHitEffects(in Vector2 start, in Vector2 end, NPC npc, NPC.HitInfo hit)
    {
        for (int i = 0; i < 18; i++)
            Dust.NewDustPerfect(start + Main.rand.NextVector2Circular(10f, 10f), DustID.t_Martian, SwordDir.RotatedByRandom(.4f) * Main.rand.NextFloat(3f, 11f), 0, default, Main.rand.NextFloat(.5f, .9f)).noGravity = true;
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        // Determine the effects for drawing. These must be done here otherwise silly things WILL happen.
        Vector2 origin;
        bool flip = SwingDir != SwingDirection.Up;
        if (Direction == -1)
            flip = SwingDir == SwingDirection.Up;

        if (flip)
        {
            origin = new Vector2(0, Tex.Height);

            RotationOffset = 0;
            Effects = SpriteEffects.None;
        }
        else
        {
            origin = new Vector2(Tex.Width, Tex.Height);

            RotationOffset = PiOver2;
            Effects = SpriteEffects.FlipHorizontally;
        }

        after?.DrawFancySwordAfterimages(Tex, Projectile.Center, [new(210, 255, 250), new(113, 251, 255), new(115, 204, 219), new(34, 128, 203)], origin, Effects, RotationOffset, Projectile.Opacity, Projectile.scale);

        Main.spriteBatch.Draw(Tex, Projectile.Center - Main.screenPosition, null, lightColor,
            Projectile.rotation + RotationOffset, origin, Projectile.scale, Effects, 0f);
        return false;
    }
}

public class InfluxWaverCreator : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 1;
        Projectile.friendly = Projectile.hostile = Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.timeLeft = 50;
    }
    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;
    public int Time
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }

    public NPC target;
    public override void AI()
    {
        if (target == null || !target.active || !target.CanHomeInto())
        {
            Projectile.Kill();
            return;
        }

        if (Time % 25 == 24 && this.RunLocal())
        {
            Vector2 pos = target.RandAreaInEntity();
            InfluxWaverProj slash = Main.projectile[Projectile.NewProj(pos, Vector2.Zero, ModContent.ProjectileType<InfluxWaverProj>(),
                Projectile.damage, Projectile.knockBack, Projectile.owner)].As<InfluxWaverProj>();

            float dist = Main.rand.NextFloat(100f, 200f);
            float angle = RandomRotation();
            float offset = Main.rand.NextFloat(.3f, .9f);

            slash.Start = pos + PolarVector(dist, angle + offset);
            slash.Center = pos;
            slash.End = pos + PolarVector(dist, angle - offset);
            slash.Copy = true;
        }
        Time++;
    }
}

public class InfluxWaverProj : ModProjectile
{
    public override string Texture => ProjectileID.InfluxWaver.GetTerrariaProj();
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 48;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.MaxUpdates = 2;
        Projectile.hostile = false;
        Projectile.friendly = true;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.penetrate = 1;
        Projectile.stopsDealingDamageAfterPenetrateHits = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.Opacity = 0f;
        Projectile.noEnchantmentVisuals = true;
    }

    public const int SliceTime = 80;
    public const int FadeTime = 30;
    public const int Lifetime = SliceTime + FadeTime;

    public Vector2 Start
    {
        get;
        set;
    }
    public Vector2 Center
    {
        get;
        set;
    }
    public Vector2 End
    {
        get;
        set;
    }

    public ref float Time => ref Projectile.ai[0];
    public bool Copy
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }
    public Vector2[] Path = new Vector2[100];
    public override void AI()
    {
        if (Time == 0f)
        {
            if (Copy)
            {
                for (int i = 0; i < Path.Length; i++)
                {
                    Vector2 controlPoint = 2 * Center - 0.5f * Start - 0.5f * End;

                    float t = InverseLerp(0f, Path.Length, i);
                    float u = 1 - t;
                    float tt = t * t;
                    float uu = u * u;
                    float ut2 = 2 * u * t;
                    Vector2 point = (uu * Start) + (ut2 * controlPoint) + (tt * End);
                    Path[i] = point;
                }

                Projectile.Center = Path[0];
                Projectile.timeLeft = Lifetime;
            }
            else
            {
                Projectile.timeLeft = 400;
            }
        }
        Projectile.Opacity = InverseLerp(0f, 10f * Projectile.MaxUpdates, Time) * InverseLerp(0f, FadeTime, Projectile.timeLeft);

        if (Copy)
        {
            float t = Animators.CubicBezier(.27f, .79f, 0f, 1.01f)(InverseLerp(0f, SliceTime, Time));
            Projectile.Center = MultiLerp(t, Path);
            if (t < .99f)
                Projectile.rotation = Projectile.Center.AngleTo(MultiLerp(t + .01f, Path)) + MathHelper.PiOver4;
        }
        else
        {
            Projectile.scale = MathF.Sin(Time * .15f) * .1f + .9f;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            if (Time % 2 == 1)
                ParticleRegistry.SpawnTechyHolosquareParticle(Projectile.RotHitbox().TopRight, -Projectile.velocity * Main.rand.NextFloat(.1f, .2f),
                    Main.rand.Next(30, 50), Main.rand.NextFloat(.6f, .9f), new(34, 128, 203));

            if (Collision.SolidCollision(Projectile.Center, 4, 4))
            {
                if (Projectile.timeLeft > FadeTime + 10)
                    Projectile.timeLeft = FadeTime + 10;
            }

            if (Projectile.timeLeft < FadeTime + 10)
            {
                Projectile.friendly = Projectile.hostile = false;
                Projectile.velocity *= .9f;
            }
        }

        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.LineCollision(Projectile.BaseRotHitbox().BottomLeft, Projectile.BaseRotHitbox().TopRight, 22f);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (!Copy && Projectile.numHits <= 0)
        {
            if (Projectile.timeLeft > FadeTime)
                Projectile.timeLeft = FadeTime;

            if (this.RunLocal())
            {
                InfluxWaverCreator creator = Main.projectile[Projectile.NewProj(target.Center, Vector2.Zero,
                    ModContent.ProjectileType<InfluxWaverCreator>(), Projectile.damage, Projectile.knockBack, Projectile.owner)].As<InfluxWaverCreator>();
                creator.target = target;
            }
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        if (Copy)
        {
            Utility.DrawChromaticAberration(Projectile.rotation.ToRotationVector2(), 3f, (Vector2 offset, Color colorMod) =>
            {
                Main.spriteBatch.DrawBetter(Projectile.ThisProjectileTexture(), Projectile.Center + offset, null, Color.White.MultiplyRGBA(colorMod) * Projectile.Opacity,
                    Projectile.rotation, Projectile.ThisProjectileTexture().Size() / 2, Projectile.scale);
            });
        }
        Projectile.DrawBaseProjectile((Color.White * Projectile.Opacity * (Copy ? .8f : 1f)) with { A = (byte)(Copy ? 10 : 255) });
        return false;
    }
}