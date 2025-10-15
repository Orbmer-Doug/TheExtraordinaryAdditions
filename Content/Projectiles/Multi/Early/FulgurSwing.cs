using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
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

namespace TheExtraordinaryAdditions.Content.Projectiles.Multi.Early;

public class FulgurSwing : BaseSwordSwing
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.FulgurSpear);

    public override void Defaults()
    {
        Projectile.ownerHitCheck = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 12 * MaxUpdates;
    }

    public override float SwingAngle => SwingDir == SwingDirection.Up ? PiOver2 : TwoPi + PiOver2;
    public override float Animation()
    {
        if (SwingDir == SwingDirection.Up)
            return Exp(3f).InOutFunction.Evaluate(Time, 0f, MaxTime, -1f, 1f);

        return new PiecewiseCurve()
            .Add(-1f, -.8f, .2f, MakePoly(3f).InFunction)
            .Add(-.8f, 1f, 1f, MakePoly(5f).OutFunction)
            .Evaluate(SwingCompletion);
    }

    public override int SwingTime => SwingDir == SwingDirection.Up ? 40 : 90;
    public bool Stabbing
    {
        get => Projectile.AdditionsInfo().ExtraAI[7] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[7] = value.ToInt();
    }

    public override bool? CanDamage()
    {
        if (SwingDir == SwingDirection.Down)
            return SwingCompletion.BetweenNum(.2f, .7f) ? null : false;

        return SwingCompletion.BetweenNum(.2f, 1f) ? null : false;
    }

    public override void OnSpawn(IEntitySource source)
    {
        SwingDir = SwingDirection.Up;
        Projectile.netUpdate = true;
    }

    public override void SafeInitialize()
    {
        points.Clear();
    }

    public override void SafeAI()
    {
        // Owner values
        Projectile.rotation = SwingOffset();
        Projectile.Center = Owner.GetFrontHandPositionImproved() - PolarVector(62f * Projectile.scale, Projectile.rotation - SwordRotation);
        Owner.heldProj = Projectile.whoAmI;
        Owner.SetDummyItemTime(2);
        Owner.ChangeDir(Direction);
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation - SwordRotation);
        Owner.itemRotation = WrapAngle(Projectile.rotation);

        // swoosh
        if (((Animation() >= .26f && SwingDir == SwingDirection.Up) || (Animation() >= .04f && SwingDir == SwingDirection.Down)) && !PlayedSound)
        {
            SoundID.DD2_GhastlyGlaivePierce.Play(Projectile.Center, 1f, 0f, .1f, null, 20, Name);
            PlayedSound = true;
        }

        if (trail == null || trail.Disposed)
            trail = new(WidthFunct, ColorFunct, (c) => Center.ToNumerics(), 20);

        if (SwingDir == SwingDirection.Down && MathF.Round(Animation(), 2) == .6f)
            PlayedSound = false;

        // Update trails
        if (TimeStop <= 0f)
        {
            points.Update(Projectile.Center + PolarVector(148f * Projectile.scale, Projectile.rotation - SwordRotation) - Center);

            if (Time % 20 == 0 && SwingCompletion.BetweenNum(.2f, .8f) && NPCTargeting.TryGetClosestNPC(new(Rect().Top, 400, true), out NPC target))
            {
                if (Item.CheckManaBetter(Owner, 2, true))
                {
                    Vector2 pos = Rect().Top;
                    if (this.RunLocal())
                    {
                        Vector2 rand = target.RandAreaInEntity();
                        FulgurZap chain = Main.projectile[Projectile.NewProj(pos, Vector2.Zero, ModContent.ProjectileType<FulgurZap>(),
                            (int)(Projectile.damage * .28f), Projectile.knockBack / 4, Owner.whoAmI, ai1: rand.X, ai2: rand.Y)].As<FulgurZap>();
                        chain.Sync();
                    }
                    SoundID.DD2_LightningBugZap.Play(pos, 1f, -.1f, .1f);
                }
            }
        }

        float scaleUp = MeleeScale * .9f;
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

        for (int i = 0; i < 1; i++)
        {
            Vector2 pos = Vector2.Lerp(Rect().Bottom + PolarVector(115f * Projectile.scale, Projectile.rotation - SwordRotation), Rect().Top, Main.rand.NextFloat());
            Vector2 vel = -SwordDir * Main.rand.NextFloat(4f, 8f);
            int life = Main.rand.Next(19, 25);
            float scale = Main.rand.NextFloat(.1f, .2f);
            Color color = Color.Lerp(Color.DeepSkyBlue, Color.DarkCyan, Main.rand.NextFloat(.2f, .6f));

            ParticleRegistry.SpawnSquishyLightParticle(pos, vel, life, scale, color, 1.2f, 2.4f);
        }

        // Account for flask
        Projectile.EmitEnchantmentVisualsAt(Rect().RandomPoint(), 1, 1);
    }

    // Create hitlag and pretty sparkles on hit with enemies
    public override void NPCHitEffects(in Vector2 start, in Vector2 end, NPC npc, NPC.HitInfo hit)
    {
        for (int i = 0; i < 20; i++)
        {
            Vector2 vel = SwordDir.RotatedByRandom(.21f) * Main.rand.NextFloat(6f, 10f);
            int life = Main.rand.Next(20, 35);
            float scale = Main.rand.NextFloat(1.2f, 1.9f);
            Color color = Color.DeepSkyBlue.Lerp(Color.Cyan, Main.rand.NextFloat(.2f, .5f));
            ParticleRegistry.SpawnSparkParticle(start + Main.rand.NextVector2Circular(20f, 20f), vel, life, scale, color);
        }
        npc.velocity += SwordDir * Item.knockBack * npc.knockBackResist;

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

    public TrailPoints points = new(20);
    public OptimizedPrimitiveTrail trail;
    private float WidthFunct(float c)
    {
        return 57f * Projectile.scale * MathF.Pow(SmoothStep(0f, 1f, SmoothStep(1f, 0f, c)), 2) * InverseLerp(0.016f, 0.07f, AngularVelocity);
    }

    private Color ColorFunct(SystemVector2 c, Vector2 pos)
    {
        // color doesn't matter here for the shader, just opacity
        return Color.White * GetLerpBump(0f, .01f, 1f, .8f, c.X) * Projectile.Opacity;
    }

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

        void draw()
        {
            if (trail == null || points == null || trail.Disposed)
                return;

            ManagedShader shader = ShaderRegistry.SwingShader;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.CrackedNoise), 1);
            shader.TrySetParameter("color", Color.LightCyan);
            shader.TrySetParameter("secondColor", Color.SkyBlue);
            shader.TrySetParameter("thirdColor", Color.DeepSkyBlue);
            shader.TrySetParameter("trailSpeed", 2f);
            trail.DrawTrail(shader, points.Points, 100, true);
        }

        Main.spriteBatch.Draw(Tex, Projectile.Center - Main.screenPosition, null, lightColor,
            Projectile.rotation + RotationOffset, origin, Projectile.scale, Effects, 0f);
        Main.spriteBatch.Draw(AssetRegistry.GetTexture(AdditionsTexture.FulgurSpear_Glow), Projectile.Center - Main.screenPosition, null, Color.White,
            Projectile.rotation + RotationOffset, origin, Projectile.scale, Effects, 0f);
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        return false;
    }
}