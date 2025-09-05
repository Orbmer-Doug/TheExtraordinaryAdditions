using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Late;
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

public class TorrentialCleave : BaseSwordSwing
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TorrentialTides);

    public override int StopTimeFrames => 2;
    public override int SwingTime => State == TorrentialSwings.HeavyDown ? 100 : 80;
    public override float SwingAngle => (Pi * 9f) / 13f;
    public override float SwordRotation => PiOver2;

    public float IdealSize
    {
        get
        {
            if (State != TorrentialSwings.HeavyDown)
            {
                return 1f;
            }
            return 1.4f;
        }
    }

    public enum TorrentialSwings
    {
        Up,
        Down,
        Up2,
        HeavyDown,
    }

    public TorrentialSwings State
    {
        get => (TorrentialSwings)Projectile.Additions().ExtraAI[7];
        set => Projectile.Additions().ExtraAI[7] = (int)value;
    }

    public override float Animation()
    {
        return new PiecewiseCurve()
            .Add(-1f, -1.2f, .3f, MakePoly(3f).InOutFunction)
            .Add(-1.2f, -.95f, .4f, MakePoly(3f).InFunction)
            .Add(-.95f, 1f, 1f, MakePoly(State == TorrentialSwings.HeavyDown ? 21f : 14f).OutFunction)
            .Evaluate(InverseLerp(0f, MaxTime, Time));
    }

    public override void Defaults()
    {
        // Check for tiles
        Projectile.ownerHitCheck = true;
    }

    public override void OnSpawn(IEntitySource source)
    {
        State = TorrentialSwings.Up;
        SwingDir = SwingDirection.Up;
        Projectile.netUpdate = true;
        Projectile.netSpam = 0;
    }

    public override void SafeInitialize()
    {
        old.Clear();
    }

    public override void SafeAI()
    {
        // Owner values
        Owner.heldProj = Projectile.whoAmI;
        Owner.SetDummyItemTime(2);
        Owner.ChangeDir(Direction);
        Owner.itemRotation = WrapAngle(Projectile.rotation);

        Projectile.rotation = SwingOffset();
        Owner.SetFrontHandBetter(0, Projectile.rotation - SwordRotation);
        Projectile.Center = Owner.GetFrontHandPositionImproved() - PolarVector(10f, Projectile.rotation - SwordRotation);

        // swoosh
        if (Animation() >= .26f && !PlayedSound)
        {
            if (State == TorrentialSwings.HeavyDown)
            {
                AdditionsSound.HeavySwordSwing.Play(Projectile.Center, 2.6f, -.3f, .1f);
                Projectile.NewProj(Center, Projectile.velocity * 7f, ModContent.ProjectileType<OceanSlash>(), Projectile.damage, Projectile.knockBack, Owner.whoAmI);
            }
            else
                AdditionsSound.HeavySwordSwing.Play(Projectile.Center, 1.6f, 0f, .1f);
            PlayedSound = true;
        }

        if (trail == null || trail._disposed)
            trail = new(WidthFunct, ColorFunct, (c) => Center.ToNumerics(), 25);

        // Update trails
        if (TimeStop <= 0f)
        {
            old.Update(Rect().Center + PolarVector(64f * Projectile.scale, Projectile.rotation - SwordRotation) + Owner.velocity - Center);
        }

        float scaleUp = MeleeScale * IdealSize;
        if (VanishTime <= 0)
        {
            Projectile.scale = Lerp(Projectile.scale, MakePoly(3f).OutFunction(InverseLerp(0f, 10f * MaxUpdates, OverallTime)) * scaleUp, OverallTime > (10 * MaxUpdates) ? .1f : 1f);
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
                State = State == TorrentialSwings.Up ? TorrentialSwings.Down : State == TorrentialSwings.Down ? TorrentialSwings.Up2 : State == TorrentialSwings.Up2 ? TorrentialSwings.HeavyDown : TorrentialSwings.Up;
                SwingDir = (State == TorrentialSwings.Up || State == TorrentialSwings.Up2) ? SwingDirection.Up : SwingDirection.Down;
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

    // Create hitlag and pretty sparkles on hit with enemies
    public override void NPCHitEffects(in Vector2 start, in Vector2 end, NPC npc, NPC.HitInfo hit)
    {
        bool big = State == TorrentialSwings.HeavyDown;

        for (int i = 0; i < 44; i++)
        {
            Vector2 vel = SwordDir.RotatedByRandom(.31f) * Main.rand.NextFloat(4f, 17f);
            int life = Main.rand.Next(50, 60);
            float scale = Main.rand.NextFloat(.8f, 1.4f);
            Color smokeColor = MulticolorLerp(Utils.NextFloat(Main.rand), AbyssalCurrents.BrackishPalette);
            smokeColor = Color.Lerp(smokeColor, Color.Gray, 0.55f);

            ParticleRegistry.SpawnMistParticle(start, vel, scale, smokeColor, smokeColor * -.5f, 190);
            if (big)
            {
                ParticleRegistry.SpawnBloomLineParticle(start, vel.RotatedByRandom(.5f) * 2f, life / 2, scale * 1.2f, smokeColor);
                ParticleRegistry.SpawnGlowParticle(start, vel * .4f, life, scale * 60f, smokeColor, .7f);
            }
        }

        if (big)
            AdditionsSound.IkeFinal.Play(start, 2.4f, -.4f, 0f, 0, Name);
        else
            AdditionsSound.IkeSwordGroundHit.Play(start, 1.1f, 0f, .1f, 10, Name);

        ScreenShakeSystem.New(new(big ? .6f : .4f, .3f, 1400f), start);
        npc.velocity += SwordDir * Item.knockBack * npc.knockBackResist;
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

    public OptimizedPrimitiveTrail trail;
    public TrailPoints old = new(25);

    public float WidthFunct(float c)
    {
        return MakePoly(2f).InFunction.Evaluate(282f, 0f, c) * Projectile.scale;
    }

    public Color ColorFunct(SystemVector2 c, Vector2 position)
    {
        // Requires a little tweaking but is better than oddly specific completion times
        float opacity = InverseLerp(0.014f, 0.07f, AngularVelocity);

        return Color.White * opacity;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        // Determine the effects for drawing.
        Vector2 origin = new(Tex.Width / 2, Tex.Height);
        bool flip = SwingDir != SwingDirection.Up;
        if (Direction == -1)
            flip = SwingDir == SwingDirection.Up;

        if (flip)
        {
            Effects = SpriteEffects.None;
        }
        else
        {
            Effects = SpriteEffects.FlipHorizontally;
        }

        void draw()
        {
            if (trail == null || trail._disposed || old == null || old.Points == default || SwingCompletion < .4f)
                return;

            ManagedShader shader = ShaderRegistry.SwingShaderIntense;
            shader.TrySetParameter("firstColor", new Color(0, 136, 255));
            shader.TrySetParameter("secondaryColor", new Color(0, 110, 174));
            shader.TrySetParameter("tertiaryColor", new Color(0, 86, 113));
            shader.TrySetParameter("flip", flip);
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.CausticNoise), 0, SamplerState.LinearWrap);

            trail.DrawTrail(shader, old.Points);
        }

        Main.spriteBatch.Draw(Tex, Projectile.Center - Main.screenPosition, null, lightColor,
            Projectile.rotation, origin, Projectile.scale, Effects, 0f);
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.OverProjectiles);

        return false;
    }
}