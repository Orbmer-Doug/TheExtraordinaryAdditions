using Microsoft.Xna.Framework.Graphics;
using Terraria;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;

public class MimicrySlash : BaseSwordSwing
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Mimicry);

    public override int StopTimeFrames => 2;
    public override int SwingTime => 34;

    public override float Animation()
    {
        return new PiecewiseCurve()
            .Add(-1f, -.8f, .45f, MakePoly(3f).InFunction)
            .Add(-.8f, 1f, 1f, MakePoly(5f).OutFunction)
            .Evaluate(InverseLerp(0f, MaxTime, Time));
    }

    public override void Defaults()
    {
        // Check for tiles
        Projectile.ownerHitCheck = true;
    }

    public override void SafeInitialize()
    {
        Projectile.numHits = 0;
        old.Clear();
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
            AdditionsSound.MimicrySwing.Play(Projectile.Center, 1.6f, 0f, .1f);
            PlayedSound = true;
        }

        if (trail == null || trail.Disposed)
            trail = new(WidthFunct, ColorFunct, (c) => Center.ToNumerics(), 15 * MaxUpdates);

        // Update trails
        if (TimeStop <= 0f)
        {
            old.Update(Rect().Bottom + PolarVector(65f * Projectile.scale, Projectile.rotation - SwordRotation) + Owner.velocity - Center);
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

        RedMist();
    }

    public void RedMist()
    {
        // If too slow or at the start of a swing, dont even bother
        if (AngularVelocity < .03f || Time < 5f || Time % 2 == 0)
            return;

        // Account for flask
        Projectile.EmitEnchantmentVisualsAt(Rect().RandomPoint(), 1, 1);
    }

    // Create hitlag and pretty sparkles on hit with enemies
    public override void NPCHitEffects(in Vector2 start, in Vector2 end, NPC npc, NPC.HitInfo hit)
    {
        for (int i = 0; i < 15; i++)
        {
            Vector2 vel = SwordDir.RotatedByRandom(.31f) * Main.rand.NextFloat(4f, 7f);
            int life = Main.rand.Next(50, 60);
            float scale = Main.rand.NextFloat(30f, 40f);
            Color color = Color.Crimson;
            ParticleRegistry.SpawnGlowParticle(start, vel * .6f, life, scale, color, .6f);
            ParticleRegistry.SpawnMistParticle(start, vel * Main.rand.NextFloat(.6f, 1.4f), Main.rand.NextFloat(.6f, .8f), color, Color.DarkRed, Main.rand.NextFloat(120f, 200f));
        }

        if (Projectile.numHits <= 0)
            Owner.Heal(10);

        npc.velocity += SwordDir * Item.knockBack * npc.knockBackResist;
        TargetObliteration(npc);
        TimeStop = StopTime;
    }

    // Do the same for players (if it ever happened)
    public override void PlayerHitEffects(in Vector2 start, in Vector2 end, Player player, Player.HurtInfo info)
    {
        for (int i = 0; i < 15; i++)
        {
            Vector2 vel = SwordDir.RotatedByRandom(.31f) * Main.rand.NextFloat(4f, 7f);
            int life = Main.rand.Next(50, 60);
            float scale = Main.rand.NextFloat(30f, 40f);
            Color color = Color.Crimson;
            ParticleRegistry.SpawnGlowParticle(start, vel * .6f, life, scale, color, .6f);
            ParticleRegistry.SpawnMistParticle(start, vel, Main.rand.NextFloat(.4f, .6f), color, Color.DarkRed, Main.rand.NextFloat(120f, 200f));
        }
        TimeStop = StopTime;
    }

    public static void TargetObliteration(NPC target)
    {
        // If the target is not just real tiny and is ded, make them go kaboom
        if (target.IsFleshy() && target.life <= 0 && target.Size.Length() > 20f)
        {
            for (int i = 0; i < 30; i++)
            {
                Vector2 pos = target.RandAreaInEntity();
                Vector2 vel = Main.rand.NextVector2CircularLimited(14f, 14f, .4f, 1f);
                float scale = Main.rand.NextFloat(.8f, 1.2f);
                int life = Main.rand.Next(20, 40);
                Color color = Color.Crimson.Lerp(Color.DarkRed, Main.rand.NextFloat());

                ParticleRegistry.SpawnBloodParticle(pos, vel, life, scale, color);
                if (i % 2 == 1)
                {
                    ParticleRegistry.SpawnGlowParticle(pos, vel, life / 2, scale, color, .3f);
                }
                ParticleRegistry.SpawnMistParticle(pos, vel, scale, color, Color.DarkRed, Main.rand.NextByte(90, 230));
                if (i % 4 == 3)
                {
                    ParticleRegistry.SpawnBloodStreakParticle(pos, vel.SafeNormalize(Vector2.Zero), life, scale / 2, color);
                }
            }

            AdditionsSound.MimicryLand.Play(target.Center, 4f, -.45f, 0f, 1, "Obliteration");
        }
        else
        {
            AdditionsSound.MimicryLand.Play(target.Center, 2.5f, 0f, .2f);
        }
    }

    public OptimizedPrimitiveTrail trail;
    public TrailPoints old = new(25);

    public static float WidthFunct(float c)
    {
        return SmoothStep(0f, 1f, SmoothStep(1f, 0f, c)) * 84f;
    }

    public Color ColorFunct(SystemVector2 c, Vector2 position)
    {
        // Requires a little tweaking but is better than oddly specific completion times
        float opacity = InverseLerp(0.022f, 0.07f, AngularVelocity);

        return MulticolorLerp(c.X, Color.Crimson, Color.Red, Color.DarkRed) * opacity;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        // Determine the effects for drawing.
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
            if (trail == null || old == null || SwingCompletion < .45f || SwingCompletion > .95f)
                return;

            ManagedShader shader = ShaderRegistry.DissipatedGlowTrail;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.noise), 0);
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.FlameMap2), 1);
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.Cosmos), 2);
            shader.TrySetParameter("OutlineColor", Color.Red.ToVector3());
            trail.DrawTrail(shader, old.Points);
        }


        Main.spriteBatch.Draw(Tex, Projectile.Center - Main.screenPosition, null, lightColor,
            Projectile.rotation + RotationOffset, origin, Projectile.scale, Effects, 0f);
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.OverProjectiles);

        return false;
    }
}