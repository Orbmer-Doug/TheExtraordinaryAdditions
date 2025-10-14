using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Debuff;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Late;

/// <summary>
/// A past weapon to be proud of.
/// </summary>
public class SpoonSwing : BaseSwordSwing
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TheSpoon);

    public enum SpoonState
    {
        Poke,
        Scoop,
        Smash
    }

    public SpoonState CurrentState
    {
        get => (SpoonState)Projectile.AdditionsInfo().ExtraAI[7];
        set => Projectile.AdditionsInfo().ExtraAI[7] = (float)value;
    }

    public int GetSwingTime
    {
        get
        {
            return CurrentState switch
            {
                SpoonState.Poke => 20,
                SpoonState.Scoop => 30,
                SpoonState.Smash => 40,
                _ => 0,
            };
        }
    }

    public override int SwingTime => GetSwingTime;
    public override int MaxUpdates => 10;
    public override bool? CanDamage() => SwingCompletion.BetweenNum(.2f, .94f, true) ? null : false;
    public float IdealSize
    {
        get
        {
            return CurrentState switch
            {
                SpoonState.Poke => 2.4f,
                SpoonState.Scoop => 2.5f,
                SpoonState.Smash => 3.3f,
                _ => 0,
            };
        }
    }

    private const float snapPoint = 0.45f;
    private const float retractionPoint = 0.6f;
    internal float PokeCurve() => new PiecewiseCurve()
        .Add(0f, 1f, Time * snapPoint, Circ.OutFunction)
        .Add(0f, 0f, Time * retractionPoint, MakePoly(1).InFunction)
        .Add(1f, -1f, 1f, MakePoly(4).InOutFunction).Evaluate(SwingCompletion);

    public const float ScoopAngle = -(Pi * 7f / 13f);
    internal float ScoopCurve() => new PiecewiseCurve()
        .Add(-.3f, -1f, .3f, MakePoly(2).OutFunction)
        .Add(-1f, .7f, .75f, MakePoly(3).InFunction)
        .Add(.7f, 1.1f, 1f, MakePoly(2).OutFunction).Evaluate(SwingCompletion);

    internal float SmashCurve() => new PiecewiseCurve()
        .Add(-1.1f, -1.3f, .3f, MakePoly(3).OutFunction)
        .AddStall(-1.3f, .45f)
        .Add(-1.3f, .85f, .85f, MakePoly(3).InFunction)
        .Add(.85f, 1f, 1f, MakePoly(2).OutFunction).Evaluate(SwingCompletion);

    public override float SwingAngle
    {
        get
        {
            return CurrentState switch
            {
                SpoonState.Poke => 0f,
                SpoonState.Scoop => ScoopAngle,
                SpoonState.Smash => -ScoopAngle,
                _ => 0,
            };
        }
    }

    public override float Animation()
    {
        return CurrentState switch
        {
            SpoonState.Poke => ScoopCurve(),
            SpoonState.Scoop => ScoopCurve(),
            SpoonState.Smash => SmashCurve(),
            _ => 0f,
        };
    }

    public override void SafeInitialize()
    {
        after ??= new(8, () => Projectile.Center);
        after.Clear();
    }

    public override void SafeAI()
    {
        // Owner values
        Projectile.Center = Owner.GetFrontHandPositionImproved() - PolarVector(10f, Projectile.rotation - SwordRotation);
        Owner.heldProj = Projectile.whoAmI;
        Owner.SetDummyItemTime(2);
        Owner.ChangeDir(Direction);
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation - SwordRotation);
        Owner.itemRotation = WrapAngle(Projectile.rotation);

        Projectile.rotation = SwingOffset();

        // swoosh
        if (Animation() >= .26f && !PlayedSound)
        {
            switch (CurrentState)
            {
                case SpoonState.Poke:
                    AdditionsSound.BraveSwingLarge.Play(Projectile.Center, 1.4f, -.2f);
                    break;
                case SpoonState.Scoop:
                    AdditionsSound.BraveSwingLarge.Play(Projectile.Center, 1.4f, -.2f);
                    break;
                case SpoonState.Smash:
                    AdditionsSound.BraveSwingLarge.Play(Projectile.Center, 2f, -.45f);
                    break;
            }

            PlayedSound = true;
        }

        // Update trails
        if (Time % 2 == 1)
        {
            after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One * Projectile.scale, Projectile.Opacity, Projectile.rotation, Effects, 200, 3, Animation() * 3f));
        }

        float scaleUp = MeleeScale * IdealSize;
        if (CurrentState != SpoonState.Poke)
        {
            if (VanishTime <= 0)
            {
                Projectile.scale = Lerp(Projectile.scale, MakePoly(3f).OutFunction(InverseLerp(0f, 10f * MaxUpdates, OverallTime)) * scaleUp, .1f);
            }
            else
            {
                Projectile.scale = MakePoly(4f).OutFunction.Evaluate(VanishTime, 0f, 18f * MaxUpdates, scaleUp, 0f);
                if (Projectile.scale <= 0f)
                    KillEffect();
                VanishTime++;
            }
        }
        else
        {
            Projectile.scale = Convert01To010(SwingCompletion) * scaleUp;

            if (VanishTime > 0f)
            {
                if (Projectile.scale <= 0f)
                    KillEffect();
                VanishTime++;
            }
        }

        // Reset if still holding left, otherwise fade
        if (this.RunLocal() && SwingCompletion >= 1f)
        {
            if (Modded.SafeMouseLeft.Current && VanishTime <= 0)
            {
                CurrentState = CurrentState == SpoonState.Poke ? SpoonState.Scoop : CurrentState == SpoonState.Scoop ? SpoonState.Smash : SpoonState.Poke;
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

        float opacity = InverseLerp(0.016f, 0.07f, AngularVelocity);
        after?.DrawFancySwordAfterimages(Tex, Projectile.Center, [Color.White * Brightness], origin, Effects, RotationOffset, Projectile.Opacity * opacity, Projectile.scale);

        Main.spriteBatch.Draw(Tex, Projectile.Center - Main.screenPosition, null, lightColor,
            Projectile.rotation + RotationOffset, origin, Projectile.scale, Effects, 0f);

        return false;
    }

    public override void NPCHitEffects(in Vector2 start, in Vector2 end, NPC npc, NPC.HitInfo hit)
    {
        npc.AddBuff(ModContent.BuffType<DentedBySpoon>(), SecondsToFrames(4));

        switch (CurrentState)
        {
            case SpoonState.Poke:
                AdditionsSound.BeegBell.Play(Projectile.Center, .3f, 0f, .2f);
                ScreenShakeSystem.New(new(.6f, .15f), Projectile.Center);

                break;
            case SpoonState.Scoop:
                AdditionsSound.BeegBell.Play(Projectile.Center, .58f, 0f, .3f);
                ScreenShakeSystem.New(new(2f, .5f), Projectile.Center);

                break;
            case SpoonState.Smash:
                int type = ModContent.ProjectileType<SpoonShockwave>();
                if (this.RunLocal() && Owner.ownedProjectileCounts[type] <= 0 && Utility.CountOwnerProjectiles(Owner, type) <= 0)
                {
                    ParticleRegistry.SpawnBlurParticle(Projectile.Center, 40, .4f, 800f);

                    int dmg = (int)Owner.GetTotalDamage<MeleeDamageClass>().ApplyTo(1000);
                    Projectile.NewProj(npc.Bottom, Vector2.Zero, type, dmg, 0f, Owner.whoAmI);
                }

                AdditionsSound.metalSlam.Play(Projectile.Center, 2.2f);
                ScreenShakeSystem.New(new(3f, 1f), Projectile.Center);

                break;
        }
    }
}