using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Hostile.Aurora.TEST;
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

public class BergcrusherSwing : BaseSwordSwing
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Bergcrusher);

    public bool HitGlacier
    {
        get => Projectile.Additions().ExtraAI[7] == 1f;
        set => Projectile.Additions().ExtraAI[7] = value.ToInt();
    }

    public bool Right
    {
        get => Projectile.Additions().ExtraAI[8] == 1f;
        set => Projectile.Additions().ExtraAI[8] = value.ToInt();
    }

    public bool Released
    {
        get => Projectile.Additions().ExtraAI[9] == 1f;
        set => Projectile.Additions().ExtraAI[9] = value.ToInt();
    }

    public override int SwingTime => Released ? 25 : 70;

    public override float Animation()
    {
        return new PiecewiseCurve()
            .Add(-1f, -1.2f, .55f, MakePoly(3f).InFunction)
            .Add(-1.2f, 1f, 1f, MakePoly(5.3f).OutFunction)
            .Evaluate(InverseLerp(0f, MaxTime, Time));
    }

    public const int ReelTime = 80;
    public override float SwingAngle => Right ? 2.125f : base.SwingAngle;
    public override float SwingOffset()
    {
        if (Right)
        {
            if (!Released)
            {
                return SwordRotation + InitialMouseAngle + SwingAngle * MakePoly(4f).InFunction.Evaluate(0f, -1f, InverseLerp(0f, ReelTime * MaxUpdates / MeleeSpeed, Time)) * Direction;
            }
            return SwordRotation + InitialMouseAngle + SwingAngle * MakePoly(5f).OutFunction.Evaluate(-1f, 1f, SwingCompletion) * Direction;
        }

        return base.SwingOffset();
    }

    public override void Defaults()
    {
        // Check for tiles
        Projectile.ownerHitCheck = true;
    }

    public override void SafeInitialize()
    {
        HitGlacier = false;
        points.Clear();
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

        if (trail == null || trail._disposed)
            trail = new(WidthFunct, ColorFunct, c => Center.ToNumerics(), 20);

        // swoosh
        bool sound = Animation() >= .2f && !PlayedSound;
        if (Right && !Released)
            sound = false;
        if (Right && Released)
            sound = !PlayedSound;
        if (sound && !Main.dedServ)
        {
            AdditionsSound.MediumSwing2.Play(Projectile.Center, 1.1f, -.3f, .15f);
            PlayedSound = true;
        }

        if (Right && this.RunLocal())
        {
            if (!Released)
            {
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Center.SafeDirectionTo(Modded.mouseWorld), .05f);
                InitialMouseAngle = Projectile.velocity.ToRotation();

                if (Modded.MouseRight.JustReleased && Time >= ReelTime)
                {
                    Time = 0f;
                    Released = true;
                    Projectile.netUpdate = true;
                }
                else if (Modded.MouseRight.JustReleased && Time < ReelTime)
                    VanishTime++;
            }
        }

        if (TimeStop <= 0f)
        {
            points.Update(Rect().Center.Lerp(Rect().Top, .5f) - Center);
        }

        float scaleUp = MeleeScale * 1.55f;
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
        bool end = SwingCompletion >= 1f;
        if (Right)
            end = Released && SwingCompletion >= 1f;
        if (this.RunLocal() && end)
        {
            if (Modded.SafeMouseLeft.Current && VanishTime <= 0)
            {
                SwingDir = SwingDir == SwingDirection.Up ? SwingDirection.Down : SwingDirection.Up;
                Initialized = false;
            }
            else
            {
                VanishTime++;
            }
        }

        CreateSnow();
    }

    public void CreateSnow()
    {
        // If too slow or at the start of a swing, dont even bother
        if (AngularVelocity < .03f || Time < 5f || Main.dedServ)
            return;

        if (Right && !Released)
            return;

        for (int i = 0; i < 2; i++)
        {
            Vector2 pos = Vector2.Lerp(Rect().Bottom + PolarVector(22f, Projectile.rotation - SwordRotation), Rect().Top, Main.rand.NextFloat());
            Vector2 vel = -SwordDir * Main.rand.NextFloat(2f, 4f);
            int life = Main.rand.Next(19, 25);
            float scale = Main.rand.NextFloat(.4f, .8f);
            Color color = MulticolorLerp(Main.rand.NextFloat(0.2f, 0.8f), Color.Cyan, Color.DeepSkyBlue, Color.CornflowerBlue, Color.Violet);

            ParticleRegistry.SpawnDustParticle(pos, vel, life, scale, color);
            Dust.NewDustPerfect(pos + Main.rand.NextVector2Circular(30f, 30f), DustID.SilverCoin, vel, 0, default, Main.rand.NextFloat(.7f, .9f));
        }

        // Account for flask
        Projectile.EmitEnchantmentVisualsAt(Rect().RandomPoint(), 1, 1);
    }

    public override bool? CanDamage()
    {
        if (Right)
        {
            return Released ? null : false;
        }

        return SwingCompletion.BetweenNum(.55f, .8f, true) ? null : false;
    }

    // Create hitlag and pretty sparkles on hit with enemies
    public override void NPCHitEffects(in Vector2 start, in Vector2 end, NPC npc, NPC.HitInfo hit)
    {
        npc.velocity += SwordDir * Item.knockBack * npc.knockBackResist;

        bool big = Right && Released && Projectile.numHits <= 0;

        if (big)
        {
            /*
            for (int i = 0; i < 30; i++)
            {
                float completion = InverseLerp(0f, 30, i);
                ParticleRegistry.SpawnGlowParticle(start, Vector2.Zero, 50, 100f * completion, MulticolorLerp(completion, Color.DarkCyan, Color.Cyan, Color.White));

                Vector2 vel = SwordDir * Lerp(-40f, 40f, completion);
                if (vel == Vector2.Zero)
                    vel = SwordDir * 2f;
                int life = (int)Lerp(10, 50, Convert01To010(completion));
                float scale = (int)Lerp(.5f, 2f, Convert01To010(completion));
                ParticleRegistry.SpawnSparkParticle(start, vel, life, scale * 2f, Color.LightCyan);
            }
            */

            ParticleRegistry.SpawnDetailedBlastParticle(start, Vector2.Zero, Vector2.One * 200f, Vector2.Zero, 50, AuroraGuard.Icey, null, AuroraGuard.LightCornflower, true);
            Projectile.NewProj(npc.Bottom, Vector2.Zero, ModContent.ProjectileType<Glacier>(), Projectile.damage, Projectile.knockBack, Owner.whoAmI);
        }

        for (int i = 0; i < 20; i++)
        {
            Vector2 vel = SwordDir.RotatedByRandom(.21f) * Main.rand.NextFloat(1f, 8f);
            int life = Main.rand.Next(100, 125);
            float scale = Main.rand.NextFloat(50.2f, 60.9f);
            if (big)
            {
                scale *= 1.5f;
                ParticleRegistry.SpawnDustParticle(start, vel * .7f, life / 2, Main.rand.NextFloat(.4f, .8f), Color.LightCyan);
            }
            Color color = MulticolorLerp(Main.rand.NextFloat(0.2f, 0.8f), Color.Cyan, Color.DeepSkyBlue, Color.CornflowerBlue, Color.Lerp(Color.Violet, Color.Blue, .5f), Color.DarkCyan);
            ParticleRegistry.SpawnCloudParticle(start, vel, color, Color.DarkSlateBlue, life, scale, .8f);
            Dust.NewDustPerfect(start, DustID.SilverCoin, vel * Main.rand.NextFloat(.4f, .6f), Main.rand.Next(20, 50), default, Main.rand.NextFloat(.8f, 1.5f));
        }

        ScreenShakeSystem.New(new(.3f, .2f), start);
        AdditionsSound.ColdPunch.Play(Projectile.Center, .9f, 0f, .11f);
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
        AdditionsSound.ColdPunch.Play(Projectile.Center, .9f, 0f, .11f);
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

    public Color ColorFunct(SystemVector2 c, Vector2 pos) => Color.White * InverseLerp(0.04f, .1f, AngularVelocity);
    public float WidthFunct(float c) => Projectile.height * Projectile.scale;

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

        void draw()
        {
            if (trail != null && points != null && SwingCompletion > .55f)
            {
                if (Right && !Released)
                    return;

                ManagedShader slash = ShaderRegistry.SwingShader;
                slash.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.CrackedNoise), 1);

                Color col1 = new(125, 251, 255);
                Color col2 = new(86, 196, 227);
                Color col3 = new(21, 92, 173);

                slash.TrySetParameter("color", col1);
                slash.TrySetParameter("secondColor", col2);
                slash.TrySetParameter("thirdColor", col3);
                slash.TrySetParameter("trailSpeed", .4f);

                trail.DrawTrail(slash, points.Points, 200, true);
            }
        }


        Main.spriteBatch.Draw(Tex, Projectile.Center - Main.screenPosition, null, lightColor,
            Projectile.rotation + RotationOffset, origin, Projectile.scale, Effects, 0f);
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.Dusts);

        return false;
    }
}