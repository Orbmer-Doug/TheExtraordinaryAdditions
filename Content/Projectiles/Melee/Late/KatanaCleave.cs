using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
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

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Late;

public class KatanaCleave : BaseSwordSwing
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.KatanaCleave);

    public override int SwingTime => 30;
    public override int StopTimeFrames => 0;
    public override float SwingAngle => TwoPi / (Swing switch { Power.Main => 4.5f, Power.Second => 4f, Power.Third => 3f, _ => 1f });
    public enum Power
    {
        Main,
        Second,
        Third,
    }

    public Power Swing
    {
        get => (Power)Projectile.Additions().ExtraAI[7];
        set => Projectile.Additions().ExtraAI[7] = (int)value;
    }

    public bool MadeBlade
    {
        get => Projectile.Additions().ExtraAI[8] == 1f;
        set => Projectile.Additions().ExtraAI[8] = value.ToInt();
    }

    public override float Animation()
    {
        float anim = new PiecewiseCurve()
            .Add(-1.1f, -.9f, .4f, MakePoly(2f).InFunction)
            .Add(-.9f, 1.1f, 1f, MakePoly(6f).OutFunction)
            .Evaluate(InverseLerp(0f, MaxTime, Time));

        return anim;
    }

    public override void Defaults()
    {
        // Check for tiles
        Projectile.ownerHitCheck = true;
    }

    public override void SafeInitialize()
    {
        MadeBlade = false;
        points.Clear();
    }

    public override void SafeAI()
    {
        // Owner values
        Vector2 off = Swing switch
        {
            Power.Main => Vector2.Zero,
            Power.Second => PolarVector(70f, Projectile.rotation - SwordRotation),
            Power.Third => PolarVector(120f, Projectile.rotation - SwordRotation),
            _ => Vector2.Zero
        };
        Projectile.Center = Owner.GetFrontHandPositionImproved() + off;

        if (Swing == Power.Main)
        {
            Owner.heldProj = Projectile.whoAmI;
            Owner.SetDummyItemTime(2);
            Owner.ChangeDir(Direction);
            Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation - SwordRotation);
            Owner.itemRotation = WrapAngle(Projectile.rotation);
        }

        Projectile.rotation = SwingOffset();

        // swoosh
        if (Animation() >= .26f && !PlayedSound && !Main.dedServ)
        {
            AdditionsSound.MediumSwing2.Play(Projectile.Center, .6f, Swing switch { Power.Main => 0f, Power.Second => -.3f, Power.Third => -.5f, _ => -10f }, .2f, 0, Name);
            PlayedSound = true;
        }

        if (trail == null || trail._disposed)
            trail = new(WidthFunct, ColorFunct, (c) => Center.ToNumerics(), 40);

        // Update trails
        if (TimeStop <= 0f)
        {
            for (int i = 0; i < 2; i++)
                points.Update(Projectile.Center + PolarVector(78f * Projectile.scale, Projectile.rotation - SwordRotation) + Owner.velocity - Center);
        }

        if (SwingCompletion == .3f && !MadeBlade && Swing != Power.Third && this.RunLocal())
        {
            KatanaCleave cleave = Main.projectile[Projectile.NewProj(Projectile.Center, Projectile.velocity, Type, Projectile.damage, Projectile.knockBack, Owner.whoAmI)].As<KatanaCleave>();
            cleave.SwingDir = SwingDir == SwingDirection.Up ? SwingDirection.Down : SwingDirection.Up;
            switch (Swing)
            {
                case Power.Main:
                    cleave.Swing = Power.Second;
                    break;
                case Power.Second:
                    cleave.Swing = Power.Third;
                    break;
                case Power.Third:
                    break;
            }
            MadeBlade = true;

            Projectile.netUpdate = true;
            Projectile.netSpam = 0;
        }

        float scaleUp = MeleeScale * (Swing switch
        {
            Power.Main => 1.1f,
            Power.Second => 1.3f,
            Power.Third => 1.5f,
            _ => 0f,
        });

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
            if (Modded.SafeMouseLeft.Current && VanishTime <= 0 && Swing == Power.Main)
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
        if (AngularVelocity < .03f || Time < 5f || Time % 2 == 1 || Main.dedServ)
            return;

        // Account for flask
        Projectile.EmitEnchantmentVisualsAt(Rect().RandomPoint(), 1, 1);
    }

    // Create hitlag and pretty sparkles on hit with enemies
    public override void NPCHitEffects(in Vector2 start, in Vector2 end, NPC npc, NPC.HitInfo hit)
    {
        for (int i = 0; i < 30; i++)
        {
            float completion = InverseLerp(0f, 30, i);
            ParticleRegistry.SpawnGlowParticle(start, Vector2.Zero, 50, 60f * completion, MulticolorLerp(completion, Color.DarkGray, Color.SlateGray, Color.LightSlateGray));

            Vector2 vel = SwordDir * Lerp(-20f, 20f, completion);
            if (vel == Vector2.Zero)
                vel = SwordDir * 2f;
            int life = (int)Lerp(10, 30, Convert01To010(completion));
            float scale = (int)Lerp(.5f, 2f, Convert01To010(completion));
            ParticleRegistry.SpawnSparkParticle(start, vel, life, scale * 2f, Color.LightCyan);
        }

        npc.velocity += SwordDir * Item.knockBack * npc.knockBackResist;

        ScreenShakeSystem.New(new(.1f, .1f), start);
        AdditionsSound.RoySpecial2.Play(start, .6f, 0f, .3f);
    }

    // Do the same for players (if it ever happened)
    public override void PlayerHitEffects(in Vector2 start, in Vector2 end, Player player, Player.HurtInfo info)
    {
        for (int i = 0; i < 30; i++)
        {
            float completion = InverseLerp(0f, 30, i);
            ParticleRegistry.SpawnGlowParticle(start, Vector2.Zero, 50, 60f * completion, MulticolorLerp(completion, Color.DarkGray, Color.SlateGray, Color.LightSlateGray));

            Vector2 vel = SwordDir * Lerp(-20f, 20f, completion);
            if (vel == Vector2.Zero)
                vel = SwordDir * 2f;
            int life = (int)Lerp(10, 30, Convert01To010(completion));
            float scale = (int)Lerp(.5f, 2f, Convert01To010(completion));
            ParticleRegistry.SpawnSparkParticle(start, vel, life, scale * 2f, Color.LightCyan);
        }

        ScreenShakeSystem.New(new(.1f, .1f), start);
        AdditionsSound.RoySpecial2.Play(start, .6f, 0f, .3f);
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        // Make it a crit if the strike was with the very tip
        if (new RotatedRectangle(30f, Rect().Top - PolarVector(10f, Projectile.rotation - SwordRotation),
            Rect().Top + PolarVector(10f, Projectile.rotation - SwordRotation)).Intersects(target.Hitbox))
        {
            modifiers.SetCrit();
        }

        switch (Swing)
        {
            case Power.Second:
                modifiers.DisableCrit();
                modifiers.FinalDamage *= .8f;
                break;
            case Power.Third:
                modifiers.DisableCrit();
                modifiers.FinalDamage *= .4f;
                break;
        }
    }

    public float WidthFunct(float c)
    {
        return 77f * Projectile.scale;
    }

    public Color ColorFunct(SystemVector2 c, Vector2 pos)
    {
        float opacity = InverseLerp(0.018f, 0.05f, AngularVelocity);

        return MulticolorLerp(c.X, Color.White, Color.Gray, Color.DarkGray) * opacity;
    }

    public OptimizedPrimitiveTrail trail;
    public TrailPoints points = new(40);
    public override bool PreDraw(ref Color lightColor)
    {
        // Determine the effects for drawing. These must be done here otherwise silly things WILL happen.
        Vector2 origin;
        bool flip = SwingDir != SwingDirection.Up;
        if (Direction == -1)
            flip = SwingDir == SwingDirection.Up;

        if (!flip)
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

        void draw()
        {
            if (trail == null || points == null)
                return;

            ManagedShader shader = AssetRegistry.GetShader("KatanaTrail");//ShaderRegistry.SwingShader;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.WavyBlotchNoise), 0);
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.DarkTurbulentNoise), 1);
            shader.TrySetParameter("flip", flip);
            shader.TrySetParameter("globalTime", Main.GlobalTimeWrappedHourly);

            trail.DrawTrail(shader, points.Points);
        }


        if (Swing == Power.Main)
        {
            Main.spriteBatch.Draw(Tex, Projectile.Center - Main.screenPosition, null, lightColor,
                Projectile.rotation + RotationOffset, origin, Projectile.scale, Effects, 0f);
        }
        else
        {
            const int amount = 10;
            for (int i = 0; i < amount; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / amount).ToRotationVector2() * Convert01To010(SwingCompletion) * 3f;
                Main.spriteBatch.Draw(Tex, Projectile.Center + drawOffset - Main.screenPosition, null, Color.White with { A = 0 } * .9f,
                    Projectile.rotation + RotationOffset, origin, Projectile.scale, Effects, 0f);
            }
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        return false;
    }
}

public class KatanaSweep : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.KatanaCleave);
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 1;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.hostile = false;
        Projectile.friendly = true;
    }

    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();
    public int Time
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }

    public const float Dist = 600f;
    public const int SliceTime = 4;
    public const int HoldTime = 30;
    public const int WhirlwindTime = 50;

    public enum SweepStates
    {
        Slice,
        Hold,
        Whirlwind
    }

    public SweepStates State
    {
        get => (SweepStates)Projectile.ai[1];
        set => Projectile.ai[1] = (int)value;
    }

    public bool Init
    {
        get => Projectile.ai[2] == 1f;
        set => Projectile.ai[2] = value.ToInt();
    }

    public ref float TotalTime => ref Projectile.Additions().ExtraAI[0];
    public Vector2 Center => Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);
    public int Dir => Projectile.velocity.X.NonZeroSign();
    public Vector2 InitialStart;
    public Vector2 End;

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.WriteVector2(InitialStart);
        writer.WriteVector2(End);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        InitialStart = reader.ReadVector2();
        End = reader.ReadVector2();
    }

    public override void AI()
    {
        if (!Init)
        {
            if (this.RunLocal())
                Projectile.velocity = Center.SafeDirectionTo(Modded.mouseWorld);
            Projectile.rotation = Center.AngleTo(Modded.mouseScreen) + PiOver4;

            InitialStart = Owner.Center;

            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.velocity * Dist;
            Vector2? tile = RaytraceTiles(start, end);
            if (tile.HasValue)
                End = tile.Value - PolarVector(Owner.Size.Length() / 2, Projectile.velocity.ToRotation());
            else
                End = end;
            AdditionsSound.HeavySwordSwing.Play(Owner.Center, 1.5f, .2f);

            Init = true;
            this.Sync();
        }

        Owner.heldProj = Owner.whoAmI;
        Projectile.Center = Owner.GetFrontHandPositionImproved();
        Owner.SetDummyItemTime(2);
        Owner.ChangeDir(Dir);
        Projectile.timeLeft = 100;

        switch (State)
        {
            case SweepStates.Slice:
                Owner.velocity *= 0;
                Owner.Center = Vector2.Lerp(Owner.Center, End, MakePoly(2f).InOutFunction(InverseLerp(0f, SliceTime, Time)));
                Projectile.rotation = Projectile.rotation.SmoothAngleLerp(Projectile.velocity.ToRotation() + PiOver4, .2f, .4f);

                if (Time > SliceTime)
                {
                    State = SweepStates.Hold;
                    Time = 0;
                }
                break;
            case SweepStates.Hold:
                Owner.velocity *= 0;
                Owner.Center = End;

                float rot = Dir == 1 ? -Pi : PiOver2;
                Projectile.rotation = Projectile.rotation.SmoothAngleLerp(rot, .2f, .4f);

                if (Time > HoldTime)
                {
                    State = SweepStates.Whirlwind;
                    Time = 0;
                }
                break;
            case SweepStates.Whirlwind:

                if (Time > WhirlwindTime)
                {
                    Projectile.Opacity = InverseLerp(WhirlwindTime + 30, WhirlwindTime, Time);
                    if (Projectile.Opacity <= 0f)
                        Projectile.Kill();
                }
                else
                {
                    if (this.RunLocal())
                    {
                        Vector2 pos = Vector2.Lerp(InitialStart, End, Main.rand.NextFloat()) + Main.rand.NextVector2Circular(150f, 150f);
                        Vector2 vel = Main.rand.NextVector2Circular(1f, 1f);
                        Projectile.NewProj(pos, vel, ModContent.ProjectileType<KatanaSlice>(), Projectile.damage / 10, 0f, Owner.whoAmI);

                        AdditionsSound.SwordSliceShort.Play(pos, .4f, .1f, 0f, 0, Name);
                    }
                }

                break;
        }

        float armRot = InitialStart.AngleTo(End);
        if (State == SweepStates.Hold || State == SweepStates.Whirlwind)
            armRot = Projectile.rotation - PiOver4 - (Dir == -1 ? -PiOver2 : PiOver2);
        Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRot - PiOver2);

        Time++;
        TotalTime++;
    }

    public override bool? CanDamage()
    {
        if (State == SweepStates.Slice)
            return null;

        return false;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.LineCollision(InitialStart, End, Projectile.ThisProjectileTexture().Height);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Vector2 origin;
        SpriteEffects fx;
        float off;
        bool flip = Dir == 1;

        if (!flip)
        {
            origin = new Vector2(0, tex.Height);

            off = 0;
            fx = SpriteEffects.None;
        }
        else
        {
            origin = new Vector2(tex.Width, tex.Height);

            off = PiOver2;
            fx = SpriteEffects.FlipHorizontally;
        }

        Main.spriteBatch.DrawBetter(tex, Projectile.Center, null, lightColor * Projectile.Opacity, Projectile.rotation + off, origin, Projectile.scale, fx);

        if (Init)
        {
            void slice()
            {
                Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.BloomLine);

                Vector2 origin = tex.Size() * 0.5f;
                float opac = Animators.MakePoly(2f).InFunction(InverseLerp(20f, 0f, TotalTime)) * 3f;
                Color col = Color.Lerp(Color.SlateGray, Color.Gray, Projectile.identity / 7f % 1f) * opac;

                float width = InitialStart.Distance(End) * Animators.MakePoly(6f).OutFunction(InverseLerp(0f, SliceTime * 2, TotalTime));
                float height = Projectile.ThisProjectileTexture().Height / 3;
                float rot = Projectile.velocity.ToRotation();
                Vector2 size = new(width, height);
                Vector2 pos = (InitialStart + End) / 2f;

                for (float i = .5f; i < 1f; i += .05f)
                {
                    Main.spriteBatch.DrawBetterRect(tex, ToTarget(pos, size * i * .4f * opac), null, Color.White * opac, rot, origin);
                    Main.spriteBatch.DrawBetterRect(tex, ToTarget(pos, size * i), null, col, rot, origin);
                    Main.spriteBatch.DrawBetterRect(tex, ToTarget(pos, size * i * 1.3f), null, Color.DarkSlateBlue * opac * .4f, rot, origin);
                }
            }
            PixelationSystem.QueueTextureRenderAction(slice, PixelationLayer.Dusts, BlendState.Additive);
        }

        return false;
    }
}

public class KatanaSlice : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.SeamStrike);

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 1;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.penetrate = 3;
        Projectile.Opacity = 1f;
        Projectile.timeLeft = MaxTime;
        Projectile.MaxUpdates = 2;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = Projectile.MaxUpdates * 12;
        Projectile.noEnchantmentVisuals = true;
        Projectile.stopsDealingDamageAfterPenetrateHits = true;
    }

    public ref float Time => ref Projectile.ai[0];
    public const int MaxTime = 28;
    public const int MaxWidth = 1400;
    public float Interpolant => InverseLerp(0f, MaxTime, Time);
    public Point Size;
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(Size.X);
        writer.Write(Size.Y);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Size.X = reader.ReadInt32();
        Size.Y = reader.ReadInt32();
    }

    public override void AI()
    {
        Projectile.rotation = Projectile.velocity.ToRotation();

        int width = (int)Animators.MakePoly(3f).OutFunction.Evaluate(70f, MaxWidth, Interpolant);
        int height = (int)Animators.MakePoly(3f).OutFunction.Evaluate(100f, 10f, Interpolant);
        Size = new(width, height);
        Projectile.Opacity = Animators.MakePoly(2f).InFunction(InverseLerp(0f, 5f * Projectile.MaxUpdates, Projectile.timeLeft));

        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 size = new(Size.X / 2, 10);
        return new RotatedRectangle(Projectile.Center - size / 2, size, Projectile.rotation).Intersects(targetHitbox);
    }

    public override bool ShouldUpdatePosition() => false;

    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            float progress = InverseLerp(0f, Time, MaxTime);
            Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);

            Vector2 origin = tex.Size() * 0.5f;
            Color col = Color.Lerp(Color.SlateGray, Color.Gray, Projectile.identity / 7f % 1f) * Projectile.Opacity;

            for (float i = .5f; i < 1f; i += .1f)
            {
                Main.spriteBatch.DrawBetterRect(tex, ToTarget(Projectile.Center, Size.ToVector2() * i * .4f * Projectile.Opacity), null, Color.White * Projectile.Opacity, Projectile.rotation, origin);
                Main.spriteBatch.DrawBetterRect(tex, ToTarget(Projectile.Center, Size.ToVector2() * i), null, col, Projectile.rotation, origin);
                Main.spriteBatch.DrawBetterRect(tex, ToTarget(Projectile.Center, Size.ToVector2() * i * 1.3f), null, Color.DarkSlateBlue * Projectile.Opacity * .4f, Projectile.rotation, origin);

            }
        }
        PixelationSystem.QueueTextureRenderAction(draw, PixelationLayer.Dusts, BlendState.Additive);

        return false;
    }
}