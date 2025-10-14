using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Early;

public class MeteorSwing : BaseSwordSwing
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.MeteorKatana);

    public int HitCounter
    {
        get => (int)Projectile.AdditionsInfo().ExtraAI[7];
        set => Projectile.AdditionsInfo().ExtraAI[7] = value;
    }

    public override int SwingTime => 30;

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
        after ??= new(4, () => Projectile.Center);
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
                after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One * Projectile.scale, Projectile.Opacity, Projectile.rotation, Effects, 70, 2, 0f));
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
            Vector2 pos = Vector2.Lerp(Rect().Bottom + PolarVector(22f, Projectile.rotation - SwordRotation), Rect().Top, Main.rand.NextFloat());
            Vector2 vel = -SwordDir * Main.rand.NextFloat(4f, 8f);
            int life = Main.rand.Next(19, 25);
            float scale = Main.rand.NextFloat(.4f, .8f);
            Color color = Color.Lerp(Color.OrangeRed, Color.Red, Main.rand.NextFloat(.2f, .6f));

            ParticleRegistry.SpawnSquishyPixelParticle(pos, vel, life * 3, scale * 2f, color, Color.White, 4);
            ParticleRegistry.SpawnGlowParticle(Vector2.Lerp(Rect().Bottom + PolarVector(22f, Projectile.rotation - SwordRotation), Rect().Top, Main.rand.NextFloat()), vel, life, scale * 60f, color, .8f);
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
            int life = Main.rand.Next(100, 125);
            float scale = Main.rand.NextFloat(1.2f, 1.9f);
            Color color = Color.OrangeRed.Lerp(Color.Orange, Main.rand.NextFloat(.2f, .5f));
            ParticleRegistry.SpawnSquishyPixelParticle(start + Main.rand.NextVector2Circular(14f, 14f), vel * 2f, life / 2, scale * 1.5f, color, Color.White, 5);
            ParticleRegistry.SpawnHeavySmokeParticle(start, vel, (int)(life * .5f), scale * .3f, color, .8f);
        }
        npc.velocity += SwordDir * Item.knockBack * npc.knockBackResist;

        int type = ModContent.ProjectileType<MeteorSpawn>();
        if (this.RunLocal() && Owner.CountOwnerProjectiles(type) < 3 && Projectile.numHits <= 0)
        {
            HitCounter++;
            if (HitCounter % 3 == 0)
                Projectile.NewProj(start, Vector2.Zero, type, Projectile.damage, Projectile.knockBack / 3, Projectile.owner);
        }

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
        after?.DrawFancySwordAfterimages(Tex, Projectile.Center, [Color.OrangeRed * 1.8f], origin, Effects, RotationOffset, Projectile.Opacity, Projectile.scale);

        Main.spriteBatch.Draw(Tex, Projectile.Center - Main.screenPosition, null, lightColor,
            Projectile.rotation + RotationOffset, origin, Projectile.scale, Effects, 0f);

        return false;
    }
}