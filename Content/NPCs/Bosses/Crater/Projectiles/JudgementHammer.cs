using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

public class JudgementHammer : ProjOwnedByNPC<Asterlin>
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.JudgementHammer);

    public override void SetDefaults()
    {
        Projectile.width = 112;
        Projectile.height = 128;
        Projectile.hostile = true;
        Projectile.friendly = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }
    public AdditionsGlobalProjectile ModdedProj => Projectile.Additions();
    public Texture2D Tex => Projectile.ThisProjectileTexture();

    public int Time
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }
    public ref float RotationOffset => ref ModdedProj.ExtraAI[1];
    public SpriteEffects Effects
    {
        get => (SpriteEffects)Projectile.spriteDirection;
        set => Projectile.spriteDirection = (int)value;
    }
    public int Direction
    {
        get => (int)ModdedProj.ExtraAI[2];
        set => ModdedProj.ExtraAI[2] = value;
    }
    public static readonly float SwingAngle = TwoPi / 3f;
    public float ChargeCompletion => InverseLerp(0f, Asterlin.Judgement_HammerReelTime, Time);

    public override void SendAI(BinaryWriter writer)
    {
        writer.Write((float)Projectile.rotation);
        writer.Write((sbyte)Projectile.spriteDirection);
    }
    public override void ReceiveAI(BinaryReader reader)
    {
        Projectile.rotation = (float)reader.ReadSingle();
        Projectile.spriteDirection = (sbyte)reader.ReadSByte();
    }

    public float Rotation()
    {
        float maxRot = MathHelper.Pi * .8f;
        return Projectile.velocity.ToRotation() + new PiecewiseCurve()
        .Add(0f, -maxRot, .4f, Sine.OutFunction)
        .Add(-maxRot, 0f, 1f, Exp(2.1f).InFunction)
        .Evaluate(ChargeCompletion) * Direction;
    }

    public override bool? CanDamage() => ChargeCompletion >= 1f;
    public override bool ShouldUpdatePosition() => ChargeCompletion >= 1f;

    public RotatedRectangle Rect()
    {
        return new(100f, Projectile.Center, Projectile.Center + (Projectile.rotation - MathHelper.PiOver4).ToRotationVector2() * 148f);
    }

    public override void SafeAI()
    {
        Vector2 home = Target.Center + Target.velocity * 20f;
        Vector2 target = Owner.Center.SafeDirectionTo(home);

        if (ChargeCompletion < 1f)
        {
            if (Time == 0)
                Projectile.velocity = target;
            else
                Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, target, (1f - ChargeCompletion) * .5f);

            Direction = Projectile.velocity.X.NonZeroSign();
            Boss.SetDirection(-Direction);
            float rotation = Rotation();
            Projectile.rotation = rotation + MathHelper.PiOver4;
            Boss.SetRightHandTarget(Boss.rightArm.RootPosition + PolarVector(400f, rotation));
            Projectile.Center = Boss.RightHandPosition;
            Time++;
            return;
        }
        after ??= new(20, () => Rect().Center);
        after.UpdateFancyAfterimages(new(Rect().Center, Vector2.One, Projectile.Opacity * InverseLerp(0f, 10f, Time), Projectile.rotation + RotationOffset, Effects, 0, 0, 0f, null, false));

        if (Time == Asterlin.Judgement_HammerReelTime)
        {
            Projectile.velocity = target * 23f;

            // Apparently real high exponents in in-easing functions can cause them to not fully go through, so fix the rotation
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

            AdditionsSound.BraveDashStart.Play(Projectile.Center, 1.2f, -.2f, .1f);
            Projectile.MaxUpdates = 5;
        }

        ParticleRegistry.SpawnTechyHolosquareParticle(Rect().RandomPoint(), -Projectile.velocity * Main.rand.NextFloat(.1f, .4f), Main.rand.Next(24, 38), Main.rand.NextFloat(.6f, .9f), Color.Gold);
        ParticleRegistry.SpawnGlowParticle(Rect().RandomPoint(), -Projectile.velocity * Main.rand.NextFloat(.1f, .2f), Main.rand.Next(24, 38), Main.rand.NextFloat(50f, 100f), Color.Gold, .4f);

        if (Rect().SolidCollision())
        {
            Vector2 ground = RaytraceTiles(Projectile.Center - Vector2.UnitY * 1500f, Projectile.Center + Vector2.UnitY * 200f) ?? Projectile.Center;

            ScreenShakeSystem.New(new(1f, 1.4f, 4000f), ground);
            ParticleRegistry.SpawnBlurParticle(ground, 150, .1f, 4900f);
            ParticleRegistry.SpawnFlash(ground, 40, .9f, 600f);
            AdditionsSound.MeteorImpact.Play(ground, 1.4f, -.3f, .1f);

            Projectile.velocity *= 0f;
            int waves = 5;
            int initial = 5;
            int build = 3;
            float speed = Main.rand.NextFloat(12f, 14f);
            float maxAngle = 1.8f / 2f;

            for (int j = 0; j < waves; j++)
            {
                for (int i = 0; i < initial; i++)
                {
                    float angle = MathHelper.Lerp(-maxAngle, maxAngle, i / (float)(initial - 1f));
                    angle += Main.rand.NextFloatDirection() * 0.12f;
                    Vector2 rockVelocity = -Vector2.UnitY.RotatedBy(angle) * speed;
                    SpawnProjectile(ground, rockVelocity, ModContent.ProjectileType<SeethingRockball>(), Asterlin.LightAttackDamage, 0f);
                }
                speed += Main.rand.NextFloat(5f, 7f);
                initial += build;
            }

            float maxRad = MathHelper.Pi;
            float initDir = -MathHelper.PiOver2;
            float angleOffset = 0f;
            speed = 3f;
            for (int i = 0; i < Asterlin.Swings_DartWaves; i++)
            {
                for (int j = 0; j < Asterlin.Swings_DartAmount; j++)
                {
                    float completion = InverseLerp(0f, Asterlin.Swings_DartAmount - 1, j);
                    float angle = initDir + MathHelper.Lerp(-maxRad / 2, maxRad / 2, completion) + angleOffset;
                    Vector2 vel = PolarVector(10f, angle);
                    SpawnProjectile(ground, vel * speed, ModContent.ProjectileType<OverloadedLightDart>(), Asterlin.LightAttackDamage, 0f);
                }
                angleOffset = maxRad / (2 * (Asterlin.Swings_DartAmount - 1));
                speed /= 2;
            }

            SpawnProjectile(ground, -Vector2.UnitY, ModContent.ProjectileType<LightPillar>(), Asterlin.HeavyAttackDamage, 0f);

            Projectile.Kill();
        }
        else
            Direction = Projectile.velocity.X.NonZeroSign();
        Time++;
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Vector2 origin;

        if (Direction == 1)
        {
            origin = new Vector2(0, Tex.Height);

            RotationOffset = 0f;
            Effects = SpriteEffects.None;
        }
        else
        {
            origin = new Vector2(Tex.Width, Tex.Height);

            RotationOffset = PiOver2;
            Effects = SpriteEffects.FlipHorizontally;
        }

        after?.DrawFancyAfterimages(tex, [Color.LightGoldenrodYellow, Color.Gold, Color.DarkGoldenrod]);
        for (int j = 0; j < 3; j++)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = ((MathHelper.TwoPi * InverseLerp(0f, 8, i) + Main.GlobalTimeWrappedHourly * Utils.Remap(j, 0, 3, 4f, 1.8f)).ToRotationVector2() * Utils.Remap(j, 0, 3, 5f, 25f));
                Color color = MulticolorLerp(InverseLerp(0f, 3, j), Color.PaleGoldenrod, Color.Gold, Color.DarkGoldenrod) with { A = 0 } * ChargeCompletion * Utils.Remap(j, 0, 3, .9f, .3f);
                Main.spriteBatch.Draw(Tex, Projectile.Center + offset - Main.screenPosition, null, color,
                Projectile.rotation + RotationOffset, origin, Projectile.scale, Effects, 0f);
            }
        }
        Main.spriteBatch.Draw(Tex, Projectile.Center - Main.screenPosition, null, Color.White,
                    Projectile.rotation + RotationOffset, origin, Projectile.scale, Effects, 0f);
        return false;
    }
}