using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;

public class BergcrusherSwing : BaseSwordSwing
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Bergcrusher);

    public override int StopTimeFrames => 2;
    public override int SwingTime => 50;

    public override float Animation()
    {
        if (SwingDir == SwingDirection.Down)
            return new PiecewiseCurve()
            .Add(-1f, -1.2f, .45f, MakePoly(3f).InFunction)
            .Add(-1.2f, 1f, 1f, MakePoly(5f).OutFunction)
            .Evaluate(InverseLerp(0f, MaxTime, Time));

        return new PiecewiseCurve()
            .Add(-1f, -.8f, .45f, MakePoly(3f).InFunction)
            .Add(-.8f, 1f, 1f, MakePoly(5f).OutFunction)
            .Evaluate(InverseLerp(0f, MaxTime, Time));
    }

    public override void OnSpawn(IEntitySource source)
    {
        SwingDir = SwingDirection.Down;
        this.Sync();
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

    public RotatedRectangle BladeRect()
    {
        Vector2 start = Rect().Bottom + PolarVector(53f, Projectile.rotation) + PolarVector(47f, Projectile.rotation - MathHelper.PiOver2);
        Vector2 end = start + PolarVector(66f, Projectile.rotation) + PolarVector(62f, Projectile.rotation - MathHelper.PiOver2);
        return new RotatedRectangle(66f, start, end);
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
            if (this.RunLocal())
                Projectile.NewProj(Center, Projectile.velocity * 14f, ModContent.ProjectileType<Bergwave>(), (int)(Projectile.damage * .5f), .3f, Owner.whoAmI);
            AdditionsSound.BraveIceSlash.Play(Projectile.Center, 1f, -.2f, .1f);
            PlayedSound = true;
        }

        if (trail == null || trail._disposed)
            trail = new(WidthFunct, ColorFunct, (c) => Center.ToNumerics(), 15 * MaxUpdates);

        // Update trails
        if (TimeStop <= 0f)
        {
            old.Update(BladeRect().Center + Owner.velocity - Center);
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

        BergMist();
    }

    public void BergMist()
    {
        // If too slow or at the start of a swing, dont even bother
        if (AngularVelocity < .03f || Time < 5f || Time % 2 == 0)
            return;

        for (int i = 0; i < 2; i++)
        {
            Vector2 pos = BladeRect().RandomPoint();
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

    // Create hitlag and pretty sparkles on hit with enemies
    public override void NPCHitEffects(in Vector2 start, in Vector2 end, NPC npc, NPC.HitInfo hit)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 vel = SwordDir.RotatedByRandom(.31f) * Main.rand.NextFloat(1f, 5f);
            int life = Main.rand.Next(100, 125);
            float scale = Main.rand.NextFloat(50.2f, 60.9f);
            Color color = MulticolorLerp(Main.rand.NextFloat(0.2f, 0.8f), Color.Cyan, Color.DeepSkyBlue, Color.CornflowerBlue, Color.Lerp(Color.Violet, Color.Blue, .5f), Color.DarkCyan);
            ParticleRegistry.SpawnCloudParticle(start, vel, color, Color.DarkSlateBlue, life, scale, .8f);
            Dust.NewDustPerfect(start, DustID.SilverCoin, vel * Main.rand.NextFloat(.4f, .6f), Main.rand.Next(20, 50), default, Main.rand.NextFloat(.8f, 1.5f));
        }

        for (int i = 0; i < (npc.boss ? 4 : 2); i++)
        {
            if (this.RunLocal())
                Projectile.NewProj(npc.RandAreaInEntity(), SwordDir.RotatedByRandom(.4f) * Main.rand.NextFloat(8f, 14f), ModContent.ProjectileType<BergIcicle>(), (int)(Projectile.damage * .3f), 0f, Projectile.owner);
        }

        if (SwingDir == SwingDirection.Down)
            npc.velocity += Vector2.UnitY * 20f * npc.knockBackResist;
        else
            npc.velocity += SwordDir * Item.knockBack * npc.knockBackResist;

        AdditionsSound.ColdPunch.Play(Projectile.Center, .9f, 0f, .11f);
    }

    // Do the same for players (if it ever happened)
    public override void PlayerHitEffects(in Vector2 start, in Vector2 end, Player player, Player.HurtInfo info)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 vel = SwordDir.RotatedByRandom(.31f) * Main.rand.NextFloat(1f, 5f);
            int life = Main.rand.Next(100, 125);
            float scale = Main.rand.NextFloat(50.2f, 60.9f);
            Color color = MulticolorLerp(Main.rand.NextFloat(0.2f, 0.8f), Color.Cyan, Color.DeepSkyBlue, Color.CornflowerBlue, Color.Lerp(Color.Violet, Color.Blue, .5f), Color.DarkCyan);
            ParticleRegistry.SpawnCloudParticle(start, vel, color, Color.DarkSlateBlue, life, scale, .8f);
            Dust.NewDustPerfect(start, DustID.SilverCoin, vel * Main.rand.NextFloat(.4f, .6f), Main.rand.Next(20, 50), default, Main.rand.NextFloat(.8f, 1.5f));
        }

        AdditionsSound.ColdPunch.Play(Projectile.Center, .9f, 0f, .11f);
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (!BladeRect().Intersects(target.Hitbox))
            modifiers.FinalDamage /= 2;
    }

    public OptimizedPrimitiveTrail trail;
    public TrailPoints old = new(25);

    public static float WidthFunct(float c)
    {
        return SmoothStep(0f, 1f, SmoothStep(1f, 0f, c)) * 91f;
    }

    public Color ColorFunct(SystemVector2 c, Vector2 position)
    {
        // Requires a little tweaking but is better than oddly specific completion times
        float opacity = InverseLerp(0.022f, 0.07f, AngularVelocity);

        return MulticolorLerp(c.X, new Color(125, 251, 255), new Color(86, 196, 227), new Color(21, 92, 173)) * opacity;
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

            ManagedShader slash = ShaderRegistry.BloodBeacon;
            slash.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.CrackedNoise), 1);
            trail.DrawTrail(slash, old.Points, 200, true);
        }

        Main.spriteBatch.Draw(Tex, Projectile.Center - Main.screenPosition, null, lightColor,
            Projectile.rotation + RotationOffset, origin, Projectile.scale, Effects, 0f);
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.OverProjectiles);

        return false;
    }
}