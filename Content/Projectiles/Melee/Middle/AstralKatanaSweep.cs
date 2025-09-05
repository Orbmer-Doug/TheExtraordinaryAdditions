using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;

public class AstralKatanaSweep : BaseSwordSwing
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.ImpureAstralKatanas);
    public static readonly Color[] AstralOrangePalette =
    [
        new(255, 164, 94),
        new(237, 93, 83),
        new(189, 66, 84),
    ];

    public static readonly Color[] AstralBluePalette =
    [
        new(109, 242, 196),
        new(66, 189, 181),
        new(46, 146, 153),
        new(32, 111, 133)
    ];

    public bool Orange
    {
        get => Projectile.Additions().ExtraAI[7] == 1f;
        set => Projectile.Additions().ExtraAI[7] = value.ToInt();
    }

    public bool NoReset
    {
        get => Projectile.Additions().ExtraAI[8] == 1f;
        set => Projectile.Additions().ExtraAI[8] = value.ToInt();
    }

    public override int SwingTime => 32;
    public override int StopTimeFrames => 0;

    public override float Animation()
    {
        float anim = new PiecewiseCurve()
            .Add(-1f, -.8f, .4f, MakePoly(3f).InFunction)
            .Add(-.8f, 1f, 1f, MakePoly(4f).OutFunction)
            .Evaluate(InverseLerp(0f, MaxTime, Time));

        if (Orange)
            return -anim;
        return anim;
    }

    public override float SwingOffset()
    {
        if (Orange)
            return base.SwingOffset();

        return base.SwingOffset();
    }

    public override void Defaults()
    {
        // Check for tiles
        Projectile.ownerHitCheck = true;
    }

    public override void SafeInitialize()
    {
        old.Clear();
    }

    public override void SafeAI()
    {
        // Owner values
        Projectile.Center = Orange ? Owner.GetBackHandPositionImproved() : Owner.GetFrontHandPositionImproved();
        Owner.heldProj = Projectile.whoAmI;
        Owner.SetDummyItemTime(2);
        Owner.ChangeDir(Direction);

        if (Orange)
            Owner.SetBackHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation - SwordRotation);
        else
            Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation - SwordRotation);
        Owner.itemRotation = WrapAngle(Projectile.rotation);

        Projectile.rotation = SwingOffset();

        // swoosh
        if (SwingCompletion >= .5f && !PlayedSound)
        {
            AdditionsSound.MediumSwing.Play(Projectile.Center, .6f, 0f, .2f, 20, Name);
            PlayedSound = true;
        }

        if (trail == null || trail._disposed)
            trail = new(WidthFunct, ColorFunct, (c) => Center.ToNumerics(), 25);

        // Update trails
        if (TimeStop <= 0f)
        {
            old.Update(Rect().Bottom + PolarVector(64f, Projectile.rotation - SwordRotation) + Owner.velocity - Center);
        }

        float scaleUp = MeleeScale * 1.38f;
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
            if (Modded.SafeMouseLeft.Current && VanishTime <= 0 && NoReset == false)
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
        // If too slow or at the start of a swing, dont even bother
        if (AngularVelocity < .03f || Time < 5f || Time % 2 == 1)
            return;

        for (int i = 0; i < 2; i++)
        {
            Vector2 pos = Vector2.Lerp(Rect().Bottom + PolarVector(16f, Projectile.rotation - SwordRotation), Rect().Top, Main.rand.NextFloat());
            Vector2 vel = -SwordDir * Main.rand.NextFloat(4f, 8f);
            int life = Main.rand.Next(19, 25);
            float scale = Main.rand.NextFloat(.4f, .8f);
            Color color = ColorFunct(new(0f, Main.rand.NextFloat()), Vector2.Zero);

            ParticleRegistry.SpawnSquishyPixelParticle(pos, vel, life * 3, scale * 2f, color, Color.White, 4);
        }

        // Account for flask
        Projectile.EmitEnchantmentVisualsAt(Rect().RandomPoint(), 1, 1);
    }

    // Create hitlag and pretty sparkles on hit with enemies
    public override void NPCHitEffects(in Vector2 start, in Vector2 end, NPC npc, NPC.HitInfo hit)
    {
        for (int i = 0; i < 20; i++)
        {
            Vector2 vel = SwordDir.RotatedByRandom(.21f) * Main.rand.NextFloat(4f, 8f);
            if (Orange)
                vel = -vel;

            float scale = Main.rand.NextFloat(1.2f, 1.9f);
            Color color = ColorFunct(new(0f, Main.rand.NextFloat()), Vector2.Zero);
            ParticleRegistry.SpawnCloudParticle(start, vel.RotatedByRandom(.35f) * .8f, color, color * .3f, Main.rand.Next(30, 40), Main.rand.NextFloat(40f, 80f), .7f, 1);
            ParticleRegistry.SpawnGlowParticle(start, vel.RotatedByRandom(.25f), Main.rand.Next(20, 30), Main.rand.NextFloat(.2f, .5f), color, .5f);
            ParticleRegistry.SpawnSparkleParticle(start, vel * Main.rand.NextFloat(1.9f, 2.8f), Main.rand.Next(20, 30), Main.rand.NextFloat(.4f, .7f), color, color * 2f, 1.4f);
        }
        npc.velocity += SwordDir * Item.knockBack * npc.knockBackResist;

        ScreenShakeSystem.New(new(.1f, .1f), start);
        AdditionsSound.RoySpecial2.Play(start, .6f, 0f, .3f);
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

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        // Make it a crit if the strike was with the very tip
        if (new RotatedRectangle(30f, Rect().Top - PolarVector(10f, Projectile.rotation - SwordRotation),
            Rect().Top + PolarVector(10f, Projectile.rotation - SwordRotation)).Intersects(target.Hitbox))
        {
            modifiers.SetCrit();
        }
    }

    public OptimizedPrimitiveTrail trail;
    public TrailPoints old = new(25);

    public float WidthFunct(float c)
    {
        return SmoothStep(1f, 0f, c) * 74f * Projectile.scale;
    }

    public Color ColorFunct(SystemVector2 c, Vector2 position)
    {
        // Requires a little tweaking but is better than oddly specific completion times
        float opacity = InverseLerp(0.011f, 0.07f, AngularVelocity);

        Color[] col = AstralBluePalette;
        if (Orange)
            col = AstralOrangePalette;
        return MulticolorLerp(c.X, col) * opacity;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        // Determine the effects for drawing. These must be done here otherwise silly things WILL happen.

        void draw()
        {
            if (trail == null || old == null || SwingCompletion < .4f)
                return;

            ManagedShader shader = ShaderRegistry.SideStreakTrail;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.Cosmos), 1);
            trail.DrawTrail(shader, old.Points);
        }


        void sword()
        {
            Vector2 origin;
            bool flip = SwingDir != SwingDirection.Up;
            if (Direction == -1)
                flip = SwingDir == SwingDirection.Up;
            if (Orange)
                flip = !flip;

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

            Main.spriteBatch.Draw(Tex, Projectile.Center - Main.screenPosition, null, Color.White * Brightness,
                        Projectile.rotation + RotationOffset, origin, Projectile.scale, Effects, 0f);
        }

        LayeredDrawSystem.QueueDrawAction(sword, Orange ? PixelationLayer.UnderPlayers : PixelationLayer.OverPlayers);
        PixelationSystem.QueuePrimitiveRenderAction(draw, Orange ? PixelationLayer.UnderPlayers : PixelationLayer.OverPlayers);

        return false;
    }
}

public class AstralKatanaThrow : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.ImpureAstralKatanas);
    private const int Lifetime = 400;
    private const int NonLungeWait = 3;
    private const int LungeWait = 5;
    public override void SetDefaults()
    {
        Projectile.width = 10;
        Projectile.height = 81;

        Projectile.friendly = true;
        Projectile.tileCollide = true;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.timeLeft = Lifetime;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 1;
        Projectile.penetrate = -1;
        Projectile.hide = true;
    }

    public enum CurrentState
    {
        Throwing,
        Thrown,
        HitEnemy,
        HitGround,
        Lunging,
    }
    public CurrentState State
    {
        get => (CurrentState)Projectile.ai[0];
        set => Projectile.ai[0] = (float)value;
    }
    public ref float AccumulatedVel => ref Projectile.ai[1];
    public ref float EnemyID => ref Projectile.ai[2];
    public ref float Time => ref Projectile.Additions().ExtraAI[0];
    public int Dir
    {
        get => (int)Projectile.Additions().ExtraAI[1];
        set => Projectile.Additions().ExtraAI[1] = value;
    }
    public int InitDir
    {
        get => (int)Projectile.Additions().ExtraAI[2];
        set => Projectile.Additions().ExtraAI[2] = value;
    }
    public ref float LungeTime => ref Projectile.Additions().ExtraAI[3];
    public ref float SavedAngle => ref Projectile.Additions().ExtraAI[4];

    public const int ThrowTime = 30;
    public float Completion => InverseLerp(0f, ThrowTime, Time);
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();
    public float ThrowDisplacement()
    {
        return Projectile.velocity.ToRotation() + (2.1f * new PiecewiseCurve()
            .Add(0f, -1f, .3f, Sine.OutFunction)
            .AddStall(-1f, .5f)
            .Add(-1f, 0f, 1f, MakePoly(4).InFunction)
            .Evaluate(Completion) * InitDir);
    }

    public override void OnSpawn(IEntitySource source)
    {
        State = CurrentState.Throwing;
        this.Sync();
    }

    public RotatedRectangle Rect()
    {
        Point point = (Projectile.position + Projectile.velocity).ToPoint();
        return new Rectangle(point.X, point.Y, Projectile.width, Projectile.height).ToRotated(Projectile.rotation + PiOver4);
    }

    public override void AI()
    {
        const int Slow = 30;
        Owner.ChangeDir(State == CurrentState.Throwing ? InitDir : Dir);
        Owner.heldProj = Projectile.whoAmI;
        Owner.SetDummyItemTime(2);
        Vector2 center = Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);

        if (State == CurrentState.Throwing)
        {
            Projectile.timeLeft = Lifetime;
            if (this.RunLocal())
            {
                Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, center.SafeDirectionTo(Owner.Additions().mouseWorld), .3f);
                if (Projectile.velocity != Projectile.oldVelocity)
                    this.Sync();
            }
            if (Time == 0)
                InitDir = Projectile.velocity.X.NonZeroSign();
            float rot = ThrowDisplacement();
            Projectile.rotation = rot + PiOver4;
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rot - PiOver2);
            Projectile.Center = Owner.GetFrontHandPositionImproved();

            Time++;
            if (Time == ThrowTime + 2)
            {
                AdditionsSound.etherealReleaseA.Play(Projectile.Center, .6f, .1f);
                State = CurrentState.Thrown;
                Time = 0; 
                if (this.RunLocal())
                    Projectile.velocity = center.SafeDirectionTo(Owner.Additions().mouseWorld) * 6f;
                this.Sync();
            }
            return;
        }

        Vector2 dest = Rect().Bottom;
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, (LungeTime <= 0 ? center.AngleTo(dest) : SavedAngle) );
        if (LungeTime <= 0)
        {
            Dir = (Projectile.Center.X > Owner.Center.X).ToDirectionInt();
        }

        if (State != CurrentState.HitEnemy)
        {
            if (Rect().SolidCollision())
            {
                OnTileCollide(Projectile.oldVelocity);
            }
        }

        if (trail == null || trail._disposed)
            trail = new(tip, WidthFunct, ColorFunct, null, 60);

        Projectile.extraUpdates = 10;
        cache ??= new(60);
        cache.Update(Projectile.Center + PolarVector(40f, Projectile.rotation - PiOver4) + Projectile.velocity);

        if (State == CurrentState.Thrown)
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver4;

            if (Time > (Lifetime - Slow))
            {
                AccumulatedVel -= .6f;
                Projectile.Opacity = Projectile.scale = 1f - InverseLerp(Lifetime - Slow, Lifetime, Time, true);
                Projectile.velocity *= .6f;
                Projectile.extraUpdates = 1;
            }
            else
            {
                AccumulatedVel += Projectile.velocity.Length() * 0.05f;
                Projectile.velocity *= .995f;
            }

            Time++;
            return;
        }

        Projectile.velocity *= 0.91f;

        if (State != CurrentState.Lunging)
        {
            if (this.RunLocal() && Modded.MouseRight.Current)
            {
                Projectile.timeLeft = Lifetime;
                this.Sync();
            }

            if (this.RunLocal() && Modded.MouseRight.JustReleased)
            {
                AdditionsSound.BraveDashStart.Play(center, 1f, .1f);
                Owner.GetModPlayer<AstralKatanaPlayer>().Cooldown = SecondsToFrames(LungeWait);
                SavedAngle = center.AngleTo(dest);
                State = CurrentState.Lunging;
                Time = 0;
                this.Sync();
            }
        }

        if (State == CurrentState.Lunging)
        {
            int timeLunging = 32 * Projectile.extraUpdates;
            float comp = InverseLerp(0f, timeLunging, LungeTime);
            if (comp >= 1f)
            {
                Projectile.Opacity = InverseLerp(20f * Projectile.MaxUpdates, 0f, Time);
                if (Projectile.Opacity <= 0f)
                    Projectile.Kill();

                Time++;
            }
            else
            {
                if (LungeTime == 0 && this.RunLocal())
                {
                    AstralKatanaSweep sweep = Main.projectile[Projectile.NewProj(Projectile.Center, center.SafeDirectionTo(dest), ModContent.ProjectileType<AstralKatanaSweep>(),
                        Projectile.damage * 2, Projectile.knockBack, Owner.whoAmI)].As<AstralKatanaSweep>();
                    sweep.Orange = true;
                    sweep.NoReset = true;
                }

                float anim = new PiecewiseCurve()
                    .Add(0f, .3f, .4f, MakePoly(2f).InFunction)
                    .Add(.3f, 1f, 1f, MakePoly(3f).OutFunction)
                    .Evaluate(comp);
                Owner.Center = Vector2.Lerp(Owner.Center, dest, anim);
                if (Owner.RotHitbox().SolidCollision())
                {
                    Owner.velocity -= center.SafeDirectionTo(dest) * 8f;
                    LungeTime = timeLunging;
                }

                if (Projectile.numUpdates.BetweenNum(0, 4) && MathF.Abs((Owner.position - Owner.oldPosition).Length()) > 3f)
                {
                    Owner.GiveIFrames(2);
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 pos = Owner.RandAreaInEntity();
                        Vector2 vel = -dest.SafeDirectionTo(Owner.Center) * Main.rand.NextFloat(4f, 8f);
                        int life = Main.rand.Next(19, 25);
                        float scale = Main.rand.NextFloat(.4f, .8f);
                        Color color = MulticolorLerp(Main.rand.NextFloat(), Utility.FastUnion(AstralKatanaSweep.AstralBluePalette, AstralKatanaSweep.AstralOrangePalette));

                        ParticleRegistry.SpawnSquishyPixelParticle(pos, vel, life * 3, scale * 2f, color, Color.White, 4);
                    }
                }
            }

            Projectile.timeLeft = Lifetime;
            LungeTime++;
            return;
        }

        Projectile.Opacity = InverseLerp(0f, 10f * Projectile.MaxUpdates, Projectile.timeLeft, true);

        if (State == CurrentState.HitEnemy)
        {
            // Stick to the target
            NPC target = Main.npc[(int)EnemyID];

            if (!target.active)
            {
                if (Projectile.timeLeft > 5)
                    Projectile.timeLeft = 5;

                Projectile.velocity = Vector2.Zero;
            }
            else
            {
                Projectile.position = target.position + offset;
                if (Projectile.position != Projectile.oldPosition)
                    this.Sync();
            }
            AccumulatedVel -= 0.6f;
        }

        if (State == CurrentState.HitGround)
        {
            AccumulatedVel -= 0.6f;
        }
    }

    public override void OnKill(int timeLeft)
    {
        if (State != CurrentState.Lunging)
            Owner.GetModPlayer<AstralKatanaPlayer>().Cooldown = SecondsToFrames(NonLungeWait);
    }

    private void SetCollided(bool stick)
    {
        Projectile.extraUpdates = 1;
        State = stick ? CurrentState.HitGround : CurrentState.HitEnemy;
        Projectile.timeLeft = stick ? 150 : 120;
        if (stick)
        {
            Projectile.tileCollide = false;
            AdditionsSound.IkeSpecial2.Play(Projectile.Center, .2f, .2f);
        }

        this.Sync();
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (State == CurrentState.Thrown)
        {
            SetCollided(true);
        }

        Projectile.velocity *= 0.01f;
        Projectile.Center += oldVelocity * 3f;
        return false;
    }

    private Vector2 offset;
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.WriteVector2(offset);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        offset = reader.ReadVector2();
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        // Create some on hit particles
        Vector2 pos = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * Projectile.height * .5f;
        ParticleRegistry.SpawnSparkleParticle(pos, Vector2.Zero, Main.rand.Next(12, 15), 3f, Color.White, Color.Purple, 1.4f);
        for (int i = 0; i < 14; i++)
        {
            Vector2 vel = -Projectile.velocity.RotatedByRandom(.12f) * Main.rand.NextFloat(2f, 5f);
            ParticleRegistry.SpawnSparkParticle(pos, vel, Main.rand.Next(20, 30), Main.rand.NextFloat(.4f, .5f), Color.Purple.Lerp(Color.White, Main.rand.NextFloat(.4f, .6f)));
        }

        // Set the sticking variables
        if (target.life > 0)
        {
            Projectile.tileCollide = false;
            EnemyID = target.whoAmI;
            offset = Projectile.position - target.position;
            offset -= Projectile.velocity;

            SetCollided(false);
            this.Sync();
        }
    }

    public override bool? CanDamage()
    {
        return State == CurrentState.Thrown;
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
    {
        if (State == CurrentState.HitEnemy)
        {
            behindNPCsAndTiles.Add(index);
        }
        else
        {
            Projectile.hide = false;
        }
    }

    internal float WidthFunct(float c)
    {
        return SmoothStep(40f, 0f, c);
    }

    internal Color ColorFunct(SystemVector2 c, Vector2 position)
    {
        return MulticolorLerp(c.X, AstralKatanaSweep.AstralBluePalette) * InverseLerp(0f, 60f, AccumulatedVel) * Projectile.Opacity;
    }

    public TrailPoints cache;
    public OptimizedPrimitiveTrail trail;
    public static readonly ITrailTip tip = new RoundedTip(20);
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (AccumulatedVel > 2f && trail != null && cache != null)
            {
                ManagedShader shader = ShaderRegistry.SideStreakTrail;
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.Cosmos), 1);
                trail.DrawTippedTrail(shader, cache.Points, tip, true);
            }
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        Texture2D texture = Projectile.ThisProjectileTexture();
        Vector2 pos = Projectile.Center;

        Vector2 origin;
        SpriteEffects fx;
        float off;
        bool flip = InitDir == 1;

        if (flip)
        {
            origin = new Vector2(0, texture.Height);

            off = 0;
            fx = SpriteEffects.None;
        }
        else
        {
            origin = new Vector2(texture.Width, texture.Height);

            off = PiOver2;
            fx = SpriteEffects.FlipHorizontally;
        }
        if (State != CurrentState.Throwing)
            origin = texture.Size() / 2;

        Main.spriteBatch.DrawBetter(texture, pos, null, lightColor * Projectile.Opacity, Projectile.rotation + off, origin, Projectile.scale, fx);
        return false;
    }
}

public class AstralKatanaPlayer : ModPlayer
{
    public int Cooldown;
    public override void PostUpdateMiscEffects()
    {
        if (Cooldown > 0)
        {
            if (Main.rand.NextBool(4))
            {
                Vector2 center = Player.RotatedRelativePoint(Player.MountedCenter, true, true);
                Vector2 pos = center + Main.rand.NextVector2CircularLimited(200f, 200f, .7f, 1f);
                Vector2 vel = RandomVelocity(1f, 1f, 4f);
                int life = Main.rand.Next(40, 100);
                float scale = Main.rand.NextFloat(.3f, .5f);
                Color col = MulticolorLerp(Sin01(Main.GlobalTimeWrappedHourly * 1.4f), AstralKatanaSweep.AstralBluePalette);
                Color col2 = MulticolorLerp(Cos01(Main.GlobalTimeWrappedHourly * 1.4f), AstralKatanaSweep.AstralOrangePalette);
                Color color = Main.rand.NextBool() ? col : col2;
                ParticleRegistry.SpawnBloomPixelParticle(pos, vel, life, scale, color, color, center, 1.1f);
            }
            Cooldown--;
        }
    }
}