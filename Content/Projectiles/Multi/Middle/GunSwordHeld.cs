using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Cooldowns;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Multi.Middle;

public class GunSwordSword : BaseSwordSwing
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.BoneGunsword);

    public override int SwingTime => 40;

    public override float Animation()
    {
        return new PiecewiseCurve()
            .Add(-1f, -1.2f, .4f, MakePoly(3f).InFunction)
            .Add(-1.2f, 1f, 1f, MakePoly(3f).OutFunction)
            .Evaluate(InverseLerp(0f, MaxTime, Time));
    }

    public override void Defaults()
    {
        // Check for tiles
        Projectile.ownerHitCheck = true;
    }

    public override void SafeInitialize()
    {
        after ??= new(7, () => Projectile.Center);
        after.afterimages = null;
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

        // swoosh
        if (Animation() >= .26f && !PlayedSound)
        {
            AdditionsSound.BreakerSwing.Play(Projectile.Center, .6f, .1f, .1f);
            PlayedSound = true;
        }

        // Update trails
        if (TimeStop <= 0f)
        {
            after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One * Projectile.scale, Projectile.Opacity, Projectile.rotation, Effects, 205, 0, 0f));
        }

        float scaleUp = MeleeScale * 1.15f;
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
                Initialized = false;
                this.Sync();
            }
            else
            {
                VanishTime++;
                this.Sync();
            }
        }

        CreateSparkles();
    }

    public void CreateSparkles()
    {
        if (AngularVelocity < .03f || Time < 5f)
            return;

        Projectile.EmitEnchantmentVisualsAt(Rect().RandomPoint(), 1, 1);
    }

    // Create hitlag and pretty sparkles on hit with enemies
    public override void NPCHitEffects(in Vector2 start, in Vector2 end, NPC npc, NPC.HitInfo hit)
    {
        for (int i = 0; i < 20; i++)
        {
            Vector2 vel = SwordDir.RotatedByRandom(.21f) * Main.rand.NextFloat(4f, 8f);
            float scale = Main.rand.NextFloat(.7f, .9f);
            Dust.NewDustPerfect(start, DustID.Bone, vel, 0, default, scale);
        }

        if (this.RunLocal())
        {
            Vector2 close = npc.RotHitbox().GetClosestPoint(Rect().Center, true);
            for (int i = 0; i < 4; i++)
            {
                Projectile.NewProj(close, SwordDir.RotatedByRandom(.6f) * Main.rand.NextFloat(7f, 10f), ModContent.ProjectileType<SplinteredBone>(), Projectile.damage / 4, 0f, Owner.whoAmI);
            }
        }

        npc.velocity += SwordDir * Item.knockBack * npc.knockBackResist;
        AdditionsSound.etherealSwordSwingB.Play(start, 1f, .8f);
        ScreenShakeSystem.New(new(.1f, .1f), start);
        TimeStop = StopTime;
    }

    // Do the same for players (if it ever happened)
    public override void PlayerHitEffects(in Vector2 start, in Vector2 end, Player player, Player.HurtInfo info)
    {
        for (int i = 0; i < 20; i++)
        {
            Vector2 vel = SwordDir * Main.rand.NextFloat(4f, 8f);
            int life = Main.rand.Next(100, 125);
            float scale = Main.rand.NextFloat(.9f, 1.5f);
            Color color = Color.BlueViolet;
            ParticleRegistry.SpawnSquishyPixelParticle(start + Main.rand.NextVector2Circular(10f, 10f), vel, life, scale, color, Color.Violet);
        }

        ScreenShakeSystem.New(new(.1f, .1f), start);
        AdditionsSound.RoySpecial2.Play(start, .6f, 0f, .3f);
        TimeStop = StopTime;
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

        // Not manually setting the rotation offset and sprite effects here caused a latency between frames where rarely a artifact would occur
        after?.DrawFancySwordAfterimages(Tex, Projectile.Center, [Color.DarkGray * 1.1f * Brightness], origin, Effects, RotationOffset, Projectile.Opacity, Projectile.scale);

        Main.spriteBatch.Draw(Tex, Projectile.Center - Main.screenPosition, null, lightColor,
            Projectile.rotation + RotationOffset, origin, Projectile.scale, Effects, 0f);

        return false;
    }
}

public class GunGunSword : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.BoneGunsword);

    public override void SetDefaults()
    {
        Projectile.width = 72;
        Projectile.height = 74;
        Projectile.hostile = false;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;

        Projectile.timeLeft = 10000;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.friendly = true;
        Projectile.hostile = false;

        Projectile.penetrate = -1;

        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;

        Projectile.ContinuouslyUpdateDamageStats = true;

        Projectile.noEnchantmentVisuals = true;
        Projectile.netImportant = true;
    }

    public const int StabTime = 30;
    public const int WaitFrame = 15;
    public const int FadeTime = 20;
    public ref float Time => ref Projectile.ai[0];
    public bool Engage
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }
    public ref float EngagedTime => ref Projectile.ai[2];
    public ref float Fade => ref Projectile.Additions().ExtraAI[0];
    public ref float Wait => ref Projectile.Additions().ExtraAI[1];
    public ref float Dist => ref Projectile.Additions().ExtraAI[2];
    public ref float Recoil => ref Projectile.Additions().ExtraAI[3];
    public int NPCIndex
    {
        get => (int)Projectile.Additions().ExtraAI[4];
        set => Projectile.Additions().ExtraAI[4] = value;
    }
    public NPC Target;
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();
    public Vector2 Center => Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);
    public int Direction => Projectile.velocity.X.NonZeroSign();
    public Item Item => Owner.HeldItem;
    public Vector2 Offset;
    public Vector2 IntersectionPoint;
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.WriteVector2(Offset);
        writer.WriteVector2(IntersectionPoint);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Offset = reader.ReadVector2();
        IntersectionPoint = reader.ReadVector2();
    }

    public override void AI()
    {
        if (!Owner.Available() || Fade >= FadeTime)
        {
            Projectile.Kill();
            return;
        }

        if (Time == 0f)
        {
            if (this.RunLocal())
            {
                Projectile.velocity = Center.SafeDirectionTo(Modded.mouseWorld);
                this.Sync();
            }
                AdditionsSound.SnakeRocketOut.Play(Projectile.Center, .6f);
        }

        Dist = MakePoly(20f).OutFunction.Evaluate(Time, 0f, StabTime, 0f, 60f);
        if (Time < StabTime)
        {
            if (Engage)
            {
                NPC target = Main.npc[NPCIndex] ?? null;
                if (EngagedTime < WaitFrame)
                {
                    if (target != null && target.active && !Collision.SolidCollision(Owner.position, Owner.width, Owner.height) && target.velocity.Length() < 80f)
                    {
                        IntersectionPoint = target.RotHitbox().GetClosestPoint(IntersectionPoint);
                        Owner.Center = target.RotHitbox().GetClosestPoint(IntersectionPoint) + Offset;
                        Owner.velocity = target.velocity;
                        this.Sync();
                    }
                    else
                    {
                        // Target is dead or invalid, end the sticking phase
                        Engage = false;
                        Fade++;
                    }
                }
                if (EngagedTime == WaitFrame)
                {
                    Vector2 pos = target.RotHitbox().GetClosestPoint(Projectile.Center);
                    for (int i = 0; i < 30; i++)
                    {
                        Vector2 vel = Main.rand.NextVector2Circular(10f, 10f);
                        int life = Main.rand.Next(40, 60);
                        float scale = Main.rand.NextFloat(.8f, 1.5f);
                        Color col = Color.OrangeRed.Lerp(Color.Red, Main.rand.NextFloat(.3f, .8f));
                        ParticleRegistry.SpawnBloomPixelParticle(pos, vel, life, scale, col, Color.White, null, 1.5f, 5);
                        ParticleRegistry.SpawnMistParticle(pos, vel, scale, col, Color.DarkGray, Main.rand.NextFloat(190f, 240f));
                        ParticleRegistry.SpawnGlowParticle(pos, vel * .8f, life / 3, scale * 92f, col, 1.7f);
                    }
                    ParticleRegistry.SpawnDetailedBlastParticle(pos, Vector2.Zero, Vector2.One * 120f, Vector2.Zero, 30, Color.OrangeRed, null, Color.Red, true);
                    ParticleRegistry.SpawnBlurParticle(pos, 60, .4f, 120f, .7f);
                    Projectile.CreateFriendlyExplosion(pos, Vector2.One * 120f, (int)(Projectile.damage * 1.5f), 8f, 8, 5);

                    Owner.velocity -= Projectile.velocity * Utils.Remap(Owner.Distance(pos), 0f, 500f, 10f, 0f);
                    Modded.LungingDown = true;

                    CalUtils.AddCooldown(Owner, SkullKaboomCooldown.ID, SecondsToFrames(3));
                    AdditionsSound.SnakeRocket.Play(Projectile.Center, 1.4f);
                    Projectile.MaxUpdates = 2;
                    this.Sync();
                }
                else if (EngagedTime > (WaitFrame + 20))
                    Fade++;

                EngagedTime++;
            }
        }
        else
        {
            if (!Modded.SafeMouseRight.Current && this.RunLocal())
            {
                Fade++;
            }
            else if (Fade > 0f)
                Fade--;
            else
            {
                if (this.RunLocal())
                {
                    Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Center.SafeDirectionTo(Modded.mouseWorld), .4f);
                    if (Projectile.velocity != Projectile.oldVelocity)
                        this.Sync();
                }

                if (Wait == 0f && Owner.HasAmmo(Item) && Modded.SafeMouseRight.Current && this.RunLocal())
                {
                    Owner.PickAmmo(Item, out int type, out float speed, out int dmg, out float kb, out int ammoID, Owner.IsAmmoFreeThisShot(Item, Owner.ChooseAmmo(Item), Owner.ChooseAmmo(Item).type));
                    Vector2 pos = Projectile.Center + PolarVector(16f, Projectile.rotation - PiOver4 + (PiOver2 * (Direction == 1 ? -1 : 1)));
                    Vector2 vel = (Projectile.rotation - PiOver4).ToRotationVector2() * Clamp(speed, Item.shootSpeed, Item.shootSpeed * 2);

                    for (int i = 0; i < 4; i++)
                    {
                        vel *= Main.rand.NextFloat(.8f, 1.2f);
                        Projectile.NewProj(pos, vel.RotatedByRandom(.2f), ModContent.ProjectileType<SkeleShot>(), dmg / 2, kb / 2, Projectile.owner);
                    }

                    for (int i = 0; i < 25; i++)
                    {
                        ParticleRegistry.SpawnSparkParticle(pos, vel.RotatedByRandom(.2f) * Main.rand.NextFloat(1.2f, 1.9f), Main.rand.Next(20, 30), Main.rand.NextFloat(.4f, .5f), Color.Chocolate.Lerp(Color.OrangeRed, Main.rand.NextFloat(.4f, .5f)), false, true);
                    }

                    AdditionsSound.banditShot1B.Play(pos, .8f, .1f, .1f);
                    Wait = Item.useTime;
                    Recoil = 15f;
                    this.Sync();
                }
            }

            Dist = MakePoly(3f).InFunction.Evaluate(Time, StabTime, StabTime + 20f, 60f, 34f);
        }

        if (Wait > 0f)
            Wait--;

        if (Recoil > 0f)
            Recoil = MakePoly(3f).OutFunction.Evaluate(Recoil, -1.25f, .05f);

        float rot = Bump(0f, .4f, 1f, .6f).Evaluate(Item.useTime - Wait, 0f, Item.useTime / 2, 0f, -.35f);
        Projectile.rotation = Projectile.velocity.ToRotation() + PiOver4 + (rot * Direction);
        if (Time < StabTime)
            Dist *= Projectile.Opacity;
        Projectile.Center = Center + PolarVector(Dist - Recoil, Projectile.rotation - PiOver4);
        Projectile.Opacity = InverseLerp(FadeTime, 0f, Fade);

        Owner.heldProj = Projectile.whoAmI;
        Owner.SetDummyItemTime(2);
        Owner.ChangeDir(Direction);
        Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - PiOver4 - PiOver2);
        Owner.itemRotation = WrapAngle(Projectile.rotation);

        Time += Engage ? InverseLerp(6f, 0f, EngagedTime) : 1f;
    }

    public override bool PreKill(int timeLeft)
    {
        Modded.LungingDown = false;
        return base.PreKill(timeLeft);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Time < StabTime && !Engage)
        {
            NPCIndex = target.whoAmI;
            Engage = true;

            IntersectionPoint = target.RotHitbox().GetClosestPoint(IntersectionPoint);
            Offset = Owner.Center - IntersectionPoint;
            this.Sync();
        }
    }

    public override bool? CanDamage() => Time < StabTime ? null : false;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.LineCollision(Projectile.BaseRotHitbox().BottomLeft, Projectile.BaseRotHitbox().TopRight, 16f);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();

        float rotation = Projectile.rotation;

        float rotOff;
        SpriteEffects fx;

        if (MathF.Cos(Projectile.rotation - PiOver4) < 0f)
        {
            rotOff = 0;
            fx = SpriteEffects.None;
        }
        else
        {
            rotOff = PiOver2;
            fx = SpriteEffects.FlipHorizontally;
        }

        Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, lightColor * Projectile.Opacity, rotation + rotOff, tex.Size() / 2, Projectile.scale, fx, 0f);

        return false;
    }
}