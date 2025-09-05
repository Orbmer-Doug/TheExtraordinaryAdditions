using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;

public class SillyPinkSwing : BaseSwordSwing
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.SillyPinkHammer);
    public enum SillyState
    {
        Reel,
        Swing,
        Recoil
    }

    public SillyState State
    {
        get => (SillyState)Projectile.Additions().ExtraAI[7];
        set => Projectile.Additions().ExtraAI[7] = (int)value;
    }

    public int ReelTime => (int)(80f / MeleeSpeed);
    public const int RecoilTime = 30;
    public override int SwingTime => 40;
    public override float SwingAngle => (3 * MathHelper.Pi / 2) / 2;
    public float ReelCompletion => InverseLerp(0f, ReelTime, Time);

    public RotatedRectangle HeadRect()
    {
        return new(32 * Projectile.scale, Projectile.Center + PolarVector(68f * Projectile.scale, Projectile.rotation - SwordRotation),
            Projectile.Center + PolarVector(91f * Projectile.scale, Projectile.rotation - SwordRotation));
    }

    public override float SwingOffset()
    {
        if (State == SillyState.Reel)
            return SwordRotation + InitialMouseAngle + (SwingAngle * new PiecewiseCurve()
                .Add(0f, .2f, .4f, MakePoly(2f).InFunction)
                .Add(.2f, 1f, 1f, MakePoly(4f).OutFunction)
                .Evaluate(ReelCompletion)) * -Direction;

        return base.SwingOffset();
    }

    public override bool? CanDamage() => State == SillyState.Swing ? null : false;

    public override void SafeInitialize()
    {
        after ??= new(8, () => Projectile.Center);
        after.afterimages = null;
        State = SillyState.Reel;
    }

    public override void SafeAI()
    {
        // Owner values
        if (State != SillyState.Recoil)
            Projectile.rotation = SwingOffset();
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation - SwordRotation);
        Projectile.Center = Owner.GetFrontHandPositionImproved();
        Owner.heldProj = Projectile.whoAmI;
        Owner.SetDummyItemTime(2);
        Owner.ChangeDir(Direction);
        Owner.itemRotation = WrapAngle(Projectile.rotation);
        Projectile.scale = MeleeScale;

        switch (State)
        {
            case SillyState.Reel:
                if (this.RunLocal())
                {
                    Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Center.SafeDirectionTo(Modded.mouseWorld), .3f);
                    if (Projectile.velocity != Projectile.oldVelocity)
                        this.Sync();
                }
                InitialMouseAngle = Projectile.velocity.ToRotation();
                Direction = Projectile.velocity.X.NonZeroSign();

                if (ReelCompletion >= 1f && !Modded.MouseLeft.Current && this.RunLocal())
                {
                    Time = 0f;
                    State = SillyState.Swing;
                    Projectile.rotation = SwingOffset();
                    this.Sync();
                }
                else if (ReelCompletion < 1f && !Modded.MouseLeft.Current && this.RunLocal())
                    KillEffect();

                Projectile.Opacity = InverseLerp(0f, 20f, Time);
                break;
            case SillyState.Swing:
                after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One * Projectile.scale, Projectile.Opacity, Projectile.rotation, Effects, 100, 0, 0f));

                if (Animation() >= .16f & !PlayedSound)
                {
                    SoundID.DD2_GhastlyGlaivePierce.Play(Projectile.Center, 1.1f, -.1f);
                    PlayedSound = true;
                }

                if (HeadRect().SolidCollision())
                    StartRecoil();
                if (Time > MaxTime)
                    KillEffect();
                Projectile.Opacity = InverseLerp(MaxTime, MaxTime - 30f, Time);
                break;
            case SillyState.Recoil:
                after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One * Projectile.scale, Projectile.Opacity, Projectile.rotation, Effects, 100, 0, 0f));

                Projectile.rotation -= .09f * InverseLerp(RecoilTime, 0f, Time) * Direction;
                if (Time > RecoilTime)
                    KillEffect();
                Projectile.Opacity = InverseLerp(RecoilTime, RecoilTime - 30f, Time);
                break;
        }

    }

    public override void NPCHitEffects(in Vector2 start, in Vector2 end, NPC npc, NPC.HitInfo hit)
    {
        StartRecoil();
    }

    public void StartRecoil()
    {
        Vector2 pos = HeadRect().Right;

        SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Pitch = .2f, Volume = .9f }, Projectile.Center);
        ScreenShakeSystem.New(new(.4f * Projectile.scale, .3f), pos);
        if (this.RunLocal())
        {
            for (int i = 0; i < 2; i++)
                Projectile.NewProj(pos, -SwordDir.RotatedByRandom(.4f) * Main.rand.NextFloat(5f, 12f), ProjectileID.PartyGirlGrenade, Projectile.damage / 4, 0f, Owner.whoAmI);
        }
        for (int i = 0; i < 40; i++)
        {
            Dust.NewDustPerfect(pos, Main.rand.Next(12) switch
            {
                0 => DustID.FireworkFountain_Blue,
                1 => DustID.FireworkFountain_Green,
                2 => DustID.FireworkFountain_Pink,
                3 => DustID.FireworkFountain_Red,
                4 => DustID.FireworkFountain_Yellow,
                5 => DustID.Confetti_Blue,
                6 => DustID.Confetti_Green,
                7 => DustID.Confetti_Pink,
                8 => DustID.Confetti_Yellow,
                _ => DustID.Confetti
            }, -SwordDir.RotatedByRandom(.6f) * Main.rand.NextFloat(10f, 15f), 0, default, Main.rand.NextFloat(.8f, 1.2f));
        }
        ParticleRegistry.SpawnDetailedBlastParticle(pos, Vector2.Zero, Vector2.One * 120f * Projectile.scale, Vector2.Zero, 40, Color.HotPink, null, Color.DeepPink, true);
        if (this.RunLocal())
            Projectile.CreateFriendlyExplosion(pos, Vector2.One * 120f * Projectile.scale, Projectile.damage / 2, Projectile.knockBack / 2f, 2, 9);

        Time = 0f;
        State = SillyState.Recoil;
        this.Sync();
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return HeadRect().Intersects(targetHitbox);
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
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

        after?.DrawFancySwordAfterimages(Tex, Projectile.Center, [Color.Pink * .8f * Brightness], origin, Effects, RotationOffset, Projectile.Opacity, Projectile.scale);

        Main.spriteBatch.Draw(Tex, Projectile.Center - Main.screenPosition, null, lightColor * Projectile.Opacity,
            Projectile.rotation + RotationOffset, origin, Projectile.scale, Effects, 0f);
        return false;
    }
}