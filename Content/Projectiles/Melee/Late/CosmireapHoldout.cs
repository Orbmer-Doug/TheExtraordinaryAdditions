using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using CalamityMod;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles.Shader;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;
using static Microsoft.Xna.Framework.MathHelper;
using ParticleRegistry = TheExtraordinaryAdditions.Common.Particles.Particle.ParticleRegistry;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Late;

public sealed class CosmireapSweep : BaseSwordSwing
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Cosmireaper);

    public override void StaticDefaults()
    {
        Main.projFrames[Type] = 4;
    }

    public enum ReaperState
    {
        Acceleration,
        Impact,
        Pinch,
    }

    public int SwingCounter
    {
        get => (int)Projectile.AdditionsInfo().ExtraAI[7];
        set => Projectile.AdditionsInfo().ExtraAI[7] = value;
    }

    public ReaperState CurrentState
    {
        get => (ReaperState)Projectile.AdditionsInfo().ExtraAI[8];
        set => Projectile.AdditionsInfo().ExtraAI[8] = (int)value;
    }

    public override int SwingTime
    {
        get
        {
            switch (CurrentState)
            {
                case ReaperState.Acceleration:
                    return 22;
                case ReaperState.Impact:
                    return 40;
                case ReaperState.Pinch:
                default:
                    return 1;
            }
        }
    }

    public override float SwingAngle
    {
        get
        {
            switch (CurrentState)
            {
                case ReaperState.Acceleration:
                    return PiOver2;
                case ReaperState.Impact:
                    return PiOver2 * 5;
                case ReaperState.Pinch:
                default:
                    return 1;
            }
        }
    }

    public override float Animation()
    {
        float sign = .25f * (SwingCounter % 3 == 0).ToDirectionInt();
        return CurrentState == ReaperState.Acceleration
            ? base.Animation()
            : new PiecewiseCurve()
                .Add(sign, sign, .4f, MakePoly(3f).InFunction)
                .Add(sign, 1f, 1f, MakePoly(5f).OutFunction)
                .Evaluate(InverseLerp(0f, MaxTime, Time));
    }

    public override void SafeInitialize()
    {
        after ??= new(9, () => Projectile.Center);
        Projectile.numHits = 0;
        after.Clear();
    }

    public override void SafeAI()
    {
        // Owner values
        Projectile.Center = Owner.GetFrontHandPositionImproved();
        Owner.heldProj = Projectile.whoAmI;
        Owner.SetDummyItemTime(2);
        Owner.ChangeDir(Direction);
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation - SwordRotation);
        Owner.itemRotation = WrapAngle(Projectile.rotation);

        Projectile.rotation = SwingOffset();

        if (this.RunLocal())
        {
            Projectile.velocity =
                Vector2.SmoothStep(Projectile.velocity, Center.SafeDirectionTo(Modded.MouseWorld), .09f);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
            Direction = Projectile.velocity.X.NonZeroSign();
            InitialMouseAngle = Projectile.velocity.ToRotation();
        }

        // swoosh
        if (Animation() >= .26f && !PlayedSound)
        {
            AdditionsSound.MediumSwing.Play(Projectile.Center, .6f, 0f, .2f);
            PlayedSound = true;
        }

        // Update trails
        if (TimeStop <= 0f)
        {
            if (Time % 2 == 1)
            {
                Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.Cosmireaper_Proj);
                Rectangle frame = tex.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
                after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One * Projectile.scale, Projectile.Opacity,
                    Projectile.rotation, Effects, 70, 2, 2f, frame));
            }
        }

        if (CurrentState == ReaperState.Impact)
        {
            if (Time == MaxTime / 2)
                Projectile.ResetLocalNPCHitImmunity();

            if (this.RunLocal() && Time % (MaxTime / 4) == (MaxTime / 4 - 1))
            {
                Projectile.NewProj(Rect().Top, Vector2.Zero, ModContent.ProjectileType<NebulaicBolt>(),
                    (int)(Projectile.damage / 4f), Projectile.knockBack, Projectile.owner);
            }
        }

        float scaleUp = MeleeScale * 1.25f;
        if (VanishTime <= 0)
        {
            Projectile.scale = MakePoly(3f).OutFunction(InverseLerp(0f, 10f * MaxUpdates, OverallTime)) * scaleUp;
        }
        else
        {
            Projectile.scale = MakePoly(4f).OutFunction.Evaluate(VanishTime, 0f, 18f * MaxUpdates, scaleUp, 0f);
            if (Projectile.scale <= 0f)
                KillEffect();
            VanishTime++;
        }

        // Reset if still holding left, otherwise fade
        if (this.RunLocal() && SwingCompletion >= 1f)
        {
            if (Modded.SafeMouseLeft.Current && VanishTime <= 0)
            {
                SwingDir = SwingDir == SwingDirection.Up ? SwingDirection.Down : SwingDirection.Up;
                CurrentState = SwingCounter % 3 == 0 ? ReaperState.Impact : ReaperState.Acceleration;
                Initialized = false;
            }
            else
            {
                VanishTime++;
            }

            this.Sync();
            SwingCounter++;
        }

        switch (CurrentState)
        {
            case ReaperState.Acceleration:
                Projectile.frame = 1;
                break;
            case ReaperState.Impact:
                Projectile.frame = 2;
                break;
        }
    }

    public override void NPCHitEffects(in Vector2 start, in Vector2 end, NPC npc, NPC.HitInfo hit)
    {
        AdditionsSound.etherealSmallHit.Play(Projectile.Center, 1.7f, 0f, .14f);

        if (Main.netMode != NetmodeID.Server)
        {
            for (int i = 0; i < 50; i++)
            {
                Vector2 pos = npc.Center + Main.rand.NextVector2Circular(20f, 20f);
                Vector2 vel = SwordDir * Main.rand.NextFloat(4f, 8f) + Main.rand.NextVector2Circular(3f, 3f);
                float scale = Main.rand.NextFloat(20f, 30f);
                ShaderParticleRegistry.SpawnCosmicParticle(pos, vel, scale);
            }
        }

        npc.velocity += SwordDir * Item.knockBack * npc.knockBackResist;

        ScreenShakeSystem.New(new(CurrentState == ReaperState.Impact ? .24f : .1f, .1f), start);
    }

    public override void PlayerHitEffects(in Vector2 start, in Vector2 end, Player player, Player.HurtInfo info)
    {
        for (int i = 0; i < 20; i++)
        {
            Vector2 vel = SwordDir * Main.rand.NextFloat(4f, 8f);
            int life = Main.rand.Next(100, 125);
            float scale = Main.rand.NextFloat(.9f, 1.5f);
            Color color = Color.BlueViolet;
            ParticleRegistry.SpawnSquishyPixelParticle(start + Main.rand.NextVector2Circular(10f, 10f), vel, life,
                scale, color, Color.Violet);
        }

        ScreenShakeSystem.New(new(.1f, .1f), start);
        AdditionsSound.RoySpecial2.Play(start, .6f, 0f, .3f);
        TimeStop = StopTime;
    }

    public FancyAfterimages after;

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.Cosmireaper_Proj);
        Rectangle frame = tex.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);

        Vector2 origin;
        bool flip = SwingDir != SwingDirection.Up;
        if (Direction == -1)
            flip = SwingDir == SwingDirection.Up;

        if (flip)
        {
            origin = new Vector2(0, frame.Height);

            RotationOffset = 0;
            Effects = SpriteEffects.None;
        }
        else
        {
            origin = new Vector2(frame.Width, frame.Height);

            RotationOffset = PiOver2;
            Effects = SpriteEffects.FlipHorizontally;
        }

        after?.DrawFancySwordAfterimages(tex, Projectile.Center, [Color.DarkViolet, Color.BlueViolet, Color.Violet],
            origin, Effects, RotationOffset, Projectile.Opacity, Projectile.scale, 0f, frame);

        Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, frame, lightColor,
            Projectile.rotation + RotationOffset, origin, Projectile.scale, Effects, 0f);

        return false;
    }
}

public class CosmireapThrow : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Cosmireaper_Proj);
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 4;
    }

    public override void SetDefaults()
    {
        Projectile.width = 92;
        Projectile.height = 118;
        Projectile.penetrate = -1;
        Projectile.Opacity = 0f;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 10;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.timeLeft = 99999;
        Projectile.noEnchantmentVisuals = true;
        Projectile.friendly = true;
        Projectile.hostile = false;
    }


    public int ReelTimer
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }

    public int ThrowTimer
    {
        get => (int)Projectile.ai[1];
        set => Projectile.ai[1] = value;
    }

    public bool Released
    {
        get => (int)Projectile.ai[2] == 1;
        set => Projectile.ai[2] = value.ToInt();
    }

    public bool Created
    {
        get => (int)Projectile.AdditionsInfo().ExtraAI[0] == 1;
        set => Projectile.AdditionsInfo().ExtraAI[0] = value.ToInt();
    }

    public static readonly int ReelTime = CalUtils.SecondsToFrames(.45f);
    public static readonly int ThrowOutTime = 120;
    public float ChargeProgress => InverseLerp(0f, ReelTime, ReelTimer);
    private const float ArmAmt = PiOver2 + .4f;
    public float ArmAnticipationMovement => MakePoly(3).OutFunction(1f - ChargeProgress) * -ArmAmt;

    public float Correction => .79f * Projectile.velocity.X.NonZeroSign();

    public const float ThrowOutDistance = 920f;
    public float ThrowCompletion => InverseLerp(0f, ThrowOutTime, ThrowTimer);
    internal float ThrowCurve() => new PiecewiseCurve()
        .Add(0.1f, 1f, .45f, Circ.OutFunction)
        .Add(1f, 1f, .6f, MakePoly(1).InOutFunction)
        .Add(1f, 0f, 1f, MakePoly(4.5f).OutFunction).Evaluate(ThrowCompletion);

    public FancyAfterimages after;
    
    public override void AI()
    {
        Projectile.frame = 3;
        Projectile.timeLeft = 2;
        Owner.heldProj = Projectile.whoAmI;
        Projectile.Opacity = InverseLerp(0f, 10f, ReelTimer);
        float armRotation = Projectile.velocity.ToRotation() +
                            ArmAnticipationMovement * Projectile.velocity.X.NonZeroSign();
        Vector2 center = Owner.RotatedRelativePoint(Owner.MountedCenter);

        if (!Released)
        {
            if (this.RunLocal())
            {
                Projectile.velocity = center.SafeDirectionTo(Modded.MouseWorld);
                if (Projectile.velocity != Projectile.oldVelocity)
                    this.Sync();
            }

            Projectile.spriteDirection = (int)(Projectile.velocity.X.NonZeroSign() == -1
                ? SpriteEffects.FlipVertically
                : SpriteEffects.None);

            float num = armRotation * Owner.gravDir;
            Projectile.Center = center + PolarVector(80f, num) * Owner.gravDir;

            Projectile.rotation = armRotation + Correction * Owner.gravDir;
            Owner.SetDummyItemTime(2);
            Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
            Owner.SetFrontHandBetter(0, Projectile.rotation);

            ReelTimer++;

            if (ChargeProgress >= 1f)
            {
                Released = true;
                this.Sync();
            }

            return;
        }
        
        if (ThrowTimer == 0f)
        {
            after ??= new(12, () => Projectile.Center);
            if (this.RunLocal())
                Projectile.velocity = Owner.SafeDirectionTo(Owner.Additions().MouseWorld);
            Projectile.timeLeft = ThrowOutTime + 1;

            AdditionsSound.etherealReleaseA.Play(Projectile.Center, 1.4f, 0f, .2f);
            this.Sync();
        }

        Projectile.Center = Owner.Center + Projectile.velocity * Projectile.scale * 10 + Projectile.velocity * ThrowOutDistance * ThrowCurve();
        after?.UpdateFancyAfterimages(new(Projectile.Center, Projectile.scale * Vector2.One, Projectile.Opacity, Projectile.rotation,
            (SpriteEffects)Projectile.spriteDirection, 0, 3, 3f, Projectile.ThisProjectileTexture().Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame), false, -.1f));

        // Hold hand out to projectile
        Owner.SetFrontHandBetter(0, Owner.AngleTo(Projectile.Center));
        Owner.SetDummyItemTime(2);

        // Face the projectile
        Owner.ChangeDir(Owner.direction = !(Projectile.Center.X < Owner.Center.X) ? 1 : -1);

        if (ThrowCompletion >= .5f)
        {
            int type = ModContent.ProjectileType<LaceratedSpace>();
            if (!Created)
            {
                if (this.RunLocal())
                    Projectile.NewProj(Projectile.Center, Owner.Center.SafeDirectionTo(Projectile.Center), type, Projectile.damage / 2, 0f, Owner.whoAmI);
                AdditionsSound.etherealSplit.Play(Projectile.Center, 2f, -.1f);
                Created = true;
                this.Sync();
            }

            if (!FindProjectile(out Projectile laceration, type, Owner.whoAmI))
                return;
            laceration.ai[0] = 1f;
            laceration.As<LaceratedSpace>().Start = Owner.Center + Projectile.velocity * ThrowOutDistance;
            laceration.As<LaceratedSpace>().End = Projectile.Center;
            laceration.netUpdate = true;

            Rectangle hitbox = Projectile.Hitbox;
            if (hitbox.Intersects(Owner.Hitbox))
            {
                laceration.ai[0] = 0f;
                laceration.netUpdate = true;
                Projectile.Kill();
            }

            Projectile.rotation = Projectile.AngleTo(Owner.Center) - Correction - (.85f * Projectile.velocity.X.NonZeroSign());
            Projectile.extraUpdates = 2;
            this.Sync();
        }
        else
        {
            Projectile.rotation += .25f * Projectile.velocity.X.NonZeroSign();
        }

        ThrowTimer++;
    }

    public override bool ShouldUpdatePosition() => Released;

    private static readonly Texture2D chainTexture = AssetRegistry.GetTexture(AdditionsTexture.ReaperChain);
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Vector2 origin = frame.Size() * 0.5f;

        if (ThrowTimer > 0f)
        {
            float opacity = GetLerpBump(0f, .1f, 1f, .9f, ThrowCompletion);
            Vector2 shake = Vector2.One.RotatedByRandom(TwoPi) * InverseLerp(.45f, 0f, ThrowCompletion) * 6f;

            int dist = (int)Vector2.Distance(Owner.Center, Projectile.Center) / 16;
            Vector2[] points = new Vector2[dist + 1];
            points[0] = Owner.Center;
            points[dist] = Projectile.Center;

            for (int i = 1; i < dist + 1; i++)
            {
                Rectangle chainFrame = new(0, 0 + 18 * (i % 2), 12, 18);
                Vector2 positionAlongLine = Vector2.Lerp(Owner.Center, Projectile.Center, i / (float)dist);
                points[i] = positionAlongLine + shake * (float)Math.Sin(i / (float)dist * MathHelper.Pi);

                float rotation = (points[i] - points[i - 1]).ToRotation() - MathHelper.PiOver2;
                float yScale = Vector2.Distance(points[i], points[i - 1]) / chainFrame.Height;
                Vector2 scale = new(1, yScale);

                Color chainLightColor = Lighting.GetColor((int)points[i].X / 16, (int)points[i].Y / 16);

                Vector2 chainOrigin = new(chainFrame.Width / 2f, chainFrame.Height);
                Main.EntitySpriteDraw(chainTexture, points[i] - Main.screenPosition, chainFrame,
                    chainLightColor * opacity * 0.7f, rotation, chainOrigin, scale, SpriteEffects.None, 0);
            }
        }

        after?.DrawFancyAfterimages(texture, [Color.DarkViolet, Color.BlueViolet, Color.Violet], Projectile.Opacity);
        Main.EntitySpriteDraw(texture, drawPosition, frame, Projectile.GetAlpha(Color.White), Projectile.rotation,
            origin, Projectile.scale, (SpriteEffects)Projectile.spriteDirection, 0f);
        return false;
    }
}

public class NebulaicBolt : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();

    public override void SetDefaults()
    {
        Projectile.Size = new(12f);
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 50;
        Projectile.hostile = false;
        Projectile.timeLeft = 400;
        Projectile.penetrate = 2;
    }

    public int Timer
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }

    public ref float RotAmt => ref Projectile.ai[1];

    public bool Hit
    {
        get => (int)Projectile.ai[2] == 1;
        set => Projectile.ai[2] = value.ToInt();
    }

    public static readonly int Wait = CalUtils.SecondsToFrames(.2f);

    public override void AI()
    {
        if (Timer < Wait)
        {
            Projectile.Opacity = InverseLerp(0f, Wait, Timer);
        }

        if (Timer == Wait)
        {
            if (this.RunLocal())
            {
                Projectile.velocity = Projectile.SafeDirectionTo(Modded.MouseWorld) * 5f;
                Projectile.rotation = Projectile.velocity.ToRotation();
                Projectile.MaxUpdates = 5;
                this.Sync();
            }

            AdditionsSound.etherealRelease.Play(Projectile.Center, .7f, -.2f, .2f, 30);

            for (int i = 0; i < 3; i++)
            {
                float rot = Utils.Remap(i, 0, 3, -.4f, .4f);
                float end = i is 0 or 2 ? 50f : 80f;
                float speed = i is 0 or 2 ? .25f : .1f;
                Vector2 vel = Projectile.velocity.RotatedBy(rot) * speed;
                ParticleRegistry.SpawnPulseRingParticle(Projectile.Center, vel, 40,
                    vel.ToRotation(), new(.5f, 1f), 0f, end, Color.Violet, true);
            }
        }

        if (Timer >= Wait)
        {
            for (int i = 0; i < 4; i++)
            {
                float rot = Utils.Remap(i, 0, 4, 0f, TwoPi) + Projectile.rotation;
                Vector2 pos = Projectile.Center + PolarVector(20f, rot);
                Vector2 vel =
                    pos.SafeDirectionTo(Projectile.Center + PolarVector(40f, Projectile.velocity.ToRotation())) * 12f;
                ParticleRegistry.SpawnSparkParticle(pos, vel, 40, .5f, Color.BlueViolet);
            }

            Projectile.rotation += .03f;

            if (Hit && NPCTargeting.TryGetClosestNPC(new(Projectile.Center, 2000, false, true), out NPC target))
            {
                Projectile.velocity += Projectile.Center.SafeDirectionTo(target.Center) * .8f;
            }
        }

        Timer++;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Hit)
            return;

        Projectile.velocity *= 4f;
        RotAmt = Main.rand.NextFloat(-.2f, .2f);
        Hit = true;
        this.Sync();
    }

    public override bool? CanDamage() => Timer >= Wait ? null : false;

    public override bool PreDraw(ref Color lightColor)
    {
        return false;
    }
}

public class LaceratedSpace : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;

    public override void SetDefaults()
    {
        Projectile.width = 52;
        Projectile.height = 52;
        Projectile.friendly = true;
        Projectile.ignoreWater = false;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = CalUtils.SecondsToFrames(4);
        Projectile.DamageType = DamageClass.Generic;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 10;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Projectile.damage = (int)(Projectile.damage * 0.945);
    }

    public bool MakingPoints
    {
        get => (int)Projectile.ai[0] == 1;
        set => Projectile.ai[0] = value.ToInt();
    }

    public Vector2 Start { get; set; }
    public Vector2 End { get; set; }
    public List<Vector2> Points = [];

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.WriteVector2(Start);
        writer.WriteVector2(End);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Start = reader.ReadVector2();
        End = reader.ReadVector2();
    }

    public override void AI()
    {
        Lighting.AddLight(Projectile.Center, Color.BlueViolet.ToVector3() * 2f);
        float interpolant = Utils.GetLerpValue(0f, 30f, Projectile.timeLeft, true) * 3f;

        if (MakingPoints)
        {
            Points = Start.GetLaserControlPoints(End, 50);
        }

        if (Points != null && Points.Count > 0)
        {
            for (int i = 0; i < Points.Count; i++)
            {
                float completion = Convert01To010(InverseLerp(0, Points.Count, i));

                Vector2 vel = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(.367f * completion) *
                              Main.rand.NextFloat(15.5f, 20f) * completion;
                ShaderParticleRegistry.SpawnCosmicParticle(Points[i], vel, interpolant * 20f * completion);
            }
        }
    }

    public override bool ShouldUpdatePosition() => false;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.CollisionFromPoints(Points, Projectile.width);
    }
}
