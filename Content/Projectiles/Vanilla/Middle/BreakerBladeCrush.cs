using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets.Audio;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;
using static System.MathF;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;

public class BreakerBladeCrush : BaseSwordSwing
{
    public override string Texture => ItemID.BreakerBlade.GetTerrariaItem();

    public enum BladeState
    {
        Swinging,
        Charging,
    }

    public BladeState State
    {
        get => (BladeState)Projectile.AdditionsInfo().ExtraAI[7];
        set => Projectile.AdditionsInfo().ExtraAI[7] = (int)value;
    }

    public bool SpecialAttack => Modded.AtMaxLimit;
    public bool Beam
    {
        get => Projectile.AdditionsInfo().ExtraAI[9] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[9] = value.ToInt();
    }

    public override int SwingTime => Beam ? SecondsToFrames(.8f) : SpecialAttack ? SecondsToFrames(.4f) : SecondsToFrames(.6f);
    public const float BladeLength = 122f;
    public Color Brightest => Color.White.Lerp(Color.Violet, .2f);
    public Color Bright => SpecialAttack ? new(163, 222, 250) : new(85, 237, 71);
    public Color Mid => SpecialAttack ? new(24, 136, 217) : new(70, 186, 60);
    public Color Dark => SpecialAttack ? new(23, 94, 144) : new(41, 122, 34);
    public LoopedSoundInstance charge;

    public override void SafeInitialize()
    {
        points.Clear();
    }

    public override void SafeAI()
    {
        if (Beam)
            InitialMouseAngle = Direction == 1 ? 0f : MathHelper.Pi;

        Projectile.Center = Owner.GetFrontHandPositionImproved();
        Owner.heldProj = Projectile.whoAmI;
        Owner.SetDummyItemTime(2);
        Owner.ChangeDir(Direction);
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation - SwordRotation);
        Owner.itemRotation = WrapAngle(Projectile.rotation);

        switch (State)
        {
            case BladeState.Swinging:
                if (trail == null || trail.Disposed)
                    trail = new(WidthFunct, ColorFunct, (c) => Center.ToNumerics(), 20);
                points?.Update(Rect().Center + PolarVector(20f, Projectile.rotation - SwordRotation) - Center);

                if (Animation() >= .26f && !PlayedSound && !Main.dedServ)
                {
                    SoundStyle val = Beam ? AssetRegistry.GetSound(AdditionsSound.BreakerBeam) :
                    SpecialAttack ? AssetRegistry.GetSound(AdditionsSound.BreakerSwingSpecial) : AssetRegistry.GetSound(AdditionsSound.BreakerSwing);
                    val.Play(Projectile.Center, .6f * Projectile.scale, -.15f, .15f, null, 10, Name);

                    if (Beam && this.RunLocal())
                    {
                        Projectile.NewProj(Projectile.Center, Vector2.UnitX * 15 * Direction, ModContent.ProjectileType<BreakerBeam>(),
                            Projectile.damage, Projectile.knockBack * .25f, Projectile.owner, 0f, Owner.CheckSolidGround().ToInt(), 0f, SpecialAttack.ToInt());

                        if (Modded.AtMaxLimit)
                        {
                            Modded.BreakerLimit = 0;
                        }
                    }

                    PlayedSound = true;
                }
                Projectile.rotation = SwingOffset();

                if (VanishTime <= 0)
                {
                    Projectile.scale = MakePoly(3f).OutFunction(InverseLerp(0f, 10f * MaxUpdates, OverallTime)) * MeleeScale;
                }
                else
                {
                    Projectile.scale = MakePoly(4f).OutFunction.Evaluate(VanishTime, 0f, 18f * MaxUpdates, MeleeScale, 0f);
                    if (Projectile.scale <= 0f)
                        KillEffect();
                    VanishTime++;
                }

                if (this.RunLocal() && SwingCompletion >= 1f)
                {
                    if ((Beam ? Modded.SafeMouseRight.Current : Modded.SafeMouseLeft.Current) && VanishTime <= 0)
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

                break;
            case BladeState.Charging:
                if (Time == 0f)
                {
                    Projectile.rotation = MathHelper.Pi * 3f / 2f;
                    this.Sync();
                }

                Owner.ChangeDir(Direction);

                charge ??= LoopedSoundManager.CreateNew(
                    new AdditionsLoopedSound(AdditionsSound.BreakerChargeFull, () => .85f),
                    new AdditionsLoopedSound(AdditionsSound.BreakerCharge, () => 1.2f),
                    () => AdditionsLoopedSound.ProjectileNotActive(Projectile));
                charge.Update(Projectile.Center);

                Projectile.rotation = Projectile.rotation.AngleLerp(Direction == 1 ? SwordRotation : MathHelper.Pi + SwordRotation, .2f);
                Owner.velocity.X = 0f;
                Projectile.scale = Projectile.Opacity = Exp().OutFunction(InverseLerp(0f, 30f, Time));
                Modded.BreakerLimit += .02f;
                Projectile.timeLeft = 40;

                Projectile.Center = Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);

                if ((Modded.SafeMouseLeft.Current == false && this.RunLocal()) || Modded.AtMaxLimit)
                    Projectile.Kill();

                if (Time % 2f == 1f)
                {
                    Color yellow = MulticolorLerp(Main.rand.NextFloat(), new Color(251, 250, 228), new Color(236, 239, 183), new Color(217, 218, 166));
                    Color blue = MulticolorLerp(Main.rand.NextFloat(), new Color(189, 248, 249), new Color(157, 223, 224), new Color(125, 201, 196));
                    Color col = Main.rand.NextBool() ? yellow : blue;

                    Vector2 pos = Owner.RandAreaInEntity();
                    int life = Main.rand.Next(12, 25);
                    float scale = Main.rand.NextFloat(.4f, .7f);
                    Vector2 vel = -Vector2.UnitY * Main.rand.NextFloat(1f, 5f);

                    ParticleRegistry.SpawnSparkParticle(pos, vel, life, scale, col);
                }
                break;
        }
    }

    public override bool? CanDamage() => State == BladeState.Charging ? false : base.CanDamage();

    public override void NPCHitEffects(in Vector2 start, in Vector2 end, NPC npc, NPC.HitInfo hit)
    {
        bool firstStrike = hit.Damage >= Item.damage * 2.5f;

        ScreenShakeSystem.New(new((Beam ? .1f : .15f) * (firstStrike ? 1.5f : 1f), .2f), start);

        if (SpecialAttack)
        {
            AdditionsSound.BreakerUpHit.Play(Projectile.Center, .8f, 0f, .15f);
        }
        else
        {
            Modded.BreakerLimit += hit.Damage * .01f;
            SoundStyle val = firstStrike ? AssetRegistry.GetSound(AdditionsSound.BreakerHit2) :
                AssetRegistry.GetSound(AdditionsSound.BreakerHit1);
            val.Play(Projectile.Center, .3f, -.15f, .15f);
        }

        ParticleRegistry.SpawnTwinkleParticle(start, Vector2.Zero, 20, new(Main.rand.NextFloat(.9f, 1.4f)), Bright, 4, default, RandomRotation());
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (target.life > (target.lifeMax * .9f))
        {
            modifiers.Knockback += .4f;
            modifiers.FinalDamage *= 2.5f;
        }
    }

    public float WidthFunct(float c) => 80f;
    public Color ColorFunct(SystemVector2 c, Vector2 pos)
    {
        float opacity = InverseLerp(0.018f, 0.07f, AngularVelocity);
        return Color.White * opacity;
    }

    public OptimizedPrimitiveTrail trail;
    public TrailPoints points = new(20);
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

        // Prepare the zany trail
        void draw()
        {
            if (trail == null || points == null)
                return;

            ManagedShader shader = ShaderRegistry.SwingShaderIntense;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.FlameMap2), 0);
            shader.TrySetParameter("firstColor", Bright);
            shader.TrySetParameter("secondaryColor", Mid);
            shader.TrySetParameter("tertiaryColor", Dark);
            shader.TrySetParameter("flip", flip);
            trail.DrawTrail(shader, points.Points);
        }

        Main.spriteBatch.Draw(Tex, Projectile.Center - Main.screenPosition, null, lightColor,
            Projectile.rotation + RotationOffset, origin, Projectile.scale, Effects, 0f);

        // Queue the trail for drawing
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.OverProjectiles);
        return false;
    }
}