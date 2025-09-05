using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;

public class Birch : BaseSwordSwing
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.BirchTree);

    public override float Animation()
    {
        if (SwingDir == SwingDirection.Down)
        {
            return Exp(2.3f).InOutFunction.Evaluate(-1f, 3.67f, SwingCompletion);
        }

        return new PiecewiseCurve()
            .Add(-1f, -.8f, .2f, MakePoly(2.4f).InFunction)
            .Add(-.8f, 1f, 1f, Exp(1.8f).OutFunction)
            .Evaluate(SwingCompletion);
    }

    public override float SwingAngle => 3 * MathHelper.Pi / 4;
    public override int SwingTime => SwingDir == SwingDirection.Down ? 85 : 60;

    public RotatedRectangle BushRect()
    {
        return new(40f * Projectile.scale, Projectile.Center + PolarVector(31f * Projectile.scale, Projectile.rotation - SwordRotation), Projectile.Center + PolarVector(82f * Projectile.scale, Projectile.rotation - SwordRotation));
    }

    public override void SafeInitialize()
    {
        after ??= new(10, () => Projectile.Center);
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
        if (Animation() >= .26f && !PlayedSound && !Main.dedServ)
        {
            AdditionsSound.Trees.Play(Projectile.Center, 1.3f, 0f, 1f, 400, Name);
            PlayedSound = true;
        }

        if (SwingDir == SwingDirection.Down && MathF.Round(SwingCompletion, 2) == .5f)
            Projectile.ResetLocalNPCHitImmunity();

        if (Time % 2 == 1)
        {
            after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One * Projectile.scale, Projectile.Opacity, Projectile.rotation, Effects, 100));
        }

        float scaleUp = MeleeScale * 3.5f;
        if (VanishTime <= 0)
        {
            Projectile.scale = MakePoly(3f).InOutFunction(InverseLerp(0f, 15f * MaxUpdates, OverallTime)) * scaleUp;
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

        if (Main.dedServ || AngularVelocity < .03f)
            return;

        for (int i = 0; i < 2; i++)
        {
            Dust d = Dust.NewDustPerfect(BushRect().RandomPoint(), 196, -SwordDir * 4f, 0, default, Main.rand.NextFloat(1.1f, 1.9f));
            d.noGravity = true;
        } 
    }

    public override void NPCHitEffects(in Vector2 start, in Vector2 end, NPC npc, NPC.HitInfo hit)
    {
        ParticleRegistry.SpawnPulseRingParticle(start, Vector2.Zero, 10, 0f, Vector2.One, 0f, 300f, Color.Brown);
        AdditionsSound.etherealSmash.Play(start, 1.2f, -.1f, .2f);
        for (int i = 0; i < 20; i++)
        {
            Dust.NewDustPerfect(start + Main.rand.NextVector2Circular(10f, 10f), DustID.WoodFurniture,
                SwordDir.RotatedByRandom(.4f) * Main.rand.NextFloat(8f, 15f), 0, default, Main.rand.NextFloat(1.2f, 1.7f));
        }
        ScreenShakeSystem.New(new(.4f, .3f), start);
    }

    public override void PlayerHitEffects(in Vector2 start, in Vector2 end, Player player, Player.HurtInfo info)
    {
        ParticleRegistry.SpawnPulseRingParticle(start, Vector2.Zero, 10, 0f, Vector2.One, 0f, 300f, Color.Brown);
        AdditionsSound.etherealSmash.Play(start, 1.2f, -.1f, .2f);
        for (int i = 0; i < 20; i++)
        {
            Dust.NewDustPerfect(start + Main.rand.NextVector2Circular(10f, 10f), DustID.WoodFurniture,
                SwordDir.RotatedByRandom(.4f) * Main.rand.NextFloat(8f, 15f), 0, default, Main.rand.NextFloat(1.2f, 1.7f));
        }
        ScreenShakeSystem.New(new(.4f, .3f), start);
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (SwingDir == SwingDirection.Down)
        {
            modifiers.Knockback *= 10f;
        }
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
        after?.DrawFancySwordAfterimages(Tex, Projectile.Center, [Color.SaddleBrown * .8f * Brightness], origin, Effects, RotationOffset, Projectile.Opacity, Projectile.scale);

        Main.spriteBatch.Draw(Tex, Projectile.Center - Main.screenPosition, null, lightColor,
            Projectile.rotation + RotationOffset, origin, Projectile.scale, Effects, 0f);

        return false;
    }
}