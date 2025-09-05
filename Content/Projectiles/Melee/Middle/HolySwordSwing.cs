using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
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

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;

public class HolySwordSwing : BaseSwordSwing
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.RejuvenatedHolySword);

    public bool Mark
    {
        get => Projectile.Additions().ExtraAI[7] == 1f;
        set => Projectile.Additions().ExtraAI[7] = value.ToInt();
    }
    public ref float Shots => ref Projectile.Additions().ExtraAI[8];

    public float MaxScale => 2f * MeleeScale;
    public override int SwingTime => Mark ? 170 : base.SwingTime;
    public override int StopTimeFrames => 3;
    public const float ReelPercent = .3f;
    public const float SwingPercent = .75f;
    public const float EndPercent = 1f;

    public const float ThrustReel = .3f;
    public const float ThrustOut = 1f;
    public override float Animation()
    {
        if (Mark)
        {
            return new PiecewiseCurve()
            .Add(15f, -20f, ThrustReel, MakePoly(2.4f).OutFunction)
            .Add(-20f, 30f, ThrustOut, Circ.OutFunction)
            .Evaluate(SwingCompletion) * Clamp(Projectile.scale, 0f, 1f);
        }

        return new PiecewiseCurve()
            .Add(-1f, -1.2f, ReelPercent, MakePoly(2).OutFunction)
            .Add(-1.2f, .9f, SwingPercent, MakePoly(4.5f).InFunction)
            .Add(.9f, 1f, EndPercent, MakePoly(2).OutFunction)
            .Evaluate(SwingCompletion);
    }
    public Vector2 LightPos => Owner.Center - Vector2.UnitY * Main.screenHeight / 2;
    public const float LightWidth = .3f;

    public override bool CanHitPvp(Player target) => SwingCompletion.BetweenNum(ReelPercent, SwingPercent) && !Mark;
    public override bool? CanHitNPC(NPC target) => SwingCompletion.BetweenNum(ReelPercent, SwingPercent) && !Mark ? null : false;
    public override bool? CanCutTiles() => SwingCompletion.BetweenNum(ReelPercent, SwingPercent) && !Mark ? null : false;

    public override void SafeInitialize()
    {
        Shots = 0;
        points.Clear();
    }

    public override void SafeAI()
    {
        if (trail == null || trail._disposed)
            trail = new(tip, WidthFunct, ColorFunct, (c) => Center.ToNumerics(), 150);

        // Owner values
        if (!Mark)
            Projectile.Center = Owner.GetFrontHandPositionImproved();
        else
            Projectile.Center = Owner.GetFrontHandPositionImproved() + PolarVector(Animation(), Projectile.rotation - SwordRotation);

        Owner.heldProj = Projectile.whoAmI;
        Owner.SetDummyItemTime(2);
        Owner.ChangeDir(Direction);
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation - SwordRotation);
        Owner.itemRotation = WrapAngle(Projectile.rotation);

        // swoosh
        if (Animation() >= .26f && !PlayedSound)
        {
            if (!Mark)
                AdditionsSound.BreakerSwingSpecial.Play(Projectile.Center, Main.rand.NextFloat(.9f, 1.3f), 0f, .3f);
            else
                AdditionsSound.Heavenly.Play(Projectile.Center, .9f);

            PlayedSound = true;
        }

        if (VanishTime <= 0)
        {
            if (Mark)
            {
                if (SwingCompletion > ThrustReel)
                {
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (npc.CanHomeInto() && Utility.IsInFieldOfView(LightPos, PiOver2, npc.Center, LightWidth * 2f, 1000f) && npc.GetGlobalNPC<HolyGlobalNPC>().MarkedTime <= 0)
                        {
                            npc.GetGlobalNPC<HolyGlobalNPC>().MarkedTime = HolyGlobalNPC.TimeMarked;
                        }
                    }

                    if (!PlayedSound)
                    {
                        AdditionsSound.MediumSwing2.Play(Projectile.Center, Main.rand.NextFloat(.9f, 1.3f), 0f, .3f);
                        PlayedSound = true;
                    }
                }
                Time++;

                Projectile.scale = MakePoly(3f).InOutFunction.Evaluate(Time, 0f, 20f * MaxUpdates, 0f, MaxScale);
                Projectile.rotation = -PiOver4;
            }
            else
            {
                int count = Main.npc.Count((NPC npc) => npc.CanHomeInto() && npc.GetGlobalNPC<HolyGlobalNPC>().MarkedTime > 0);
                const int maxDarts = 7;
                if (SwingCompletion > ReelPercent && AngularVelocity > .1f && Shots < Clamp(count, 0, maxDarts))
                {
                    if (Time % 2 == 1)
                    {
                        foreach (NPC npc in Main.ActiveNPCs)
                        {
                            if (npc.CanHomeInto() && npc.GetGlobalNPC<HolyGlobalNPC>().MarkedTime > 0)
                            {
                                Vector2 pos = Rect().Top;
                                Projectile.NewProj(pos, Center.SafeDirectionTo(pos) * Main.rand.NextFloat(5f, 9f), ModContent.ProjectileType<HolyDart>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                                Shots++;
                                break;
                            }
                        }
                    }
                }

                Projectile.scale = MakePoly(3f).OutFunction(InverseLerp(0f, 10f * MaxUpdates, OverallTime)) * MaxScale;
                Projectile.rotation = SwingOffset();
            }
        }
        else
        {
            Projectile.scale = MakePoly(4f).OutFunction.Evaluate(VanishTime, 0f, 18f * MaxUpdates, MaxScale, 0f);
            if (Projectile.scale <= 0f)
                KillEffect();
            VanishTime++;
        }

        CreatePrettySparks();

        // Update trails
        if (TimeStop <= 0f)
        {
            points?.Update(Rect().Top + Owner.velocity - Center);
        }

        // Reset if still holding left, otherwise fade
        if (this.RunLocal() && SwingCompletion >= 1f)
        {
            if (Modded.SafeMouseLeft.Current && VanishTime <= 0 && !Mark)
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
    }

    public void CreatePrettySparks()
    {
        if (AngularVelocity <= 0.08f || SwingCompletion < ReelPercent)
            return;

        for (int i = 0; i < 2; i++)
        {
            Vector2 pos = (Rect().Bottom + PolarVector(28f * Projectile.scale, Projectile.rotation + SwordRotation)).Lerp(Rect().Top, Main.rand.NextFloat());
            int life = Main.rand.Next(20, 30);
            float scale = Main.rand.NextFloat(.2f, .5f);
            Color color = ColorFunct(SystemVector2.One * Main.rand.NextFloat(.1f, .6f), Vector2.Zero).Lerp(Color.OrangeRed, Main.rand.NextFloat(0f, .4f));
            ParticleRegistry.SpawnBloomPixelParticle(pos, SwordDir * Main.rand.NextFloat(7f, 12f), life, scale, color, color * 1.5f, null, 1.4f);
        }
    }

    // Create hitlag and pretty sparkles on hit with enemies
    public override void NPCHitEffects(in Vector2 start, in Vector2 end, NPC npc, NPC.HitInfo hit)
    {
        for (int i = 0; i < 17; i++)
        {
            int life = Main.rand.Next(30, 40);
            float scale = Main.rand.NextFloat(.9f, 1.6f);
            Color color = Color.Gold.Lerp(Color.Goldenrod, Main.rand.NextFloat(.3f, .7f));
            ParticleRegistry.SpawnSparkParticle(start + Main.rand.NextVector2Circular(9f, 9f), SwordDir.RotatedByRandom(.2f) * Main.rand.NextFloat(9f, 15f), life, scale, color);
        }

        for (int i = 0; i < Main.rand.Next(5, 9); i++)
        {
            int life = Main.rand.Next(30, 50);
            float scale = Main.rand.NextFloat(60.9f, 90.1f);
            Color color = Color.Gold.Lerp(Color.OrangeRed, Main.rand.NextFloat(.3f, .7f));
            ParticleRegistry.SpawnGlowParticle(start, SwordDir.RotatedByRandom(.21f) * Main.rand.NextFloat(2f, 10f), life, scale, color);
        }

        npc.velocity += SwordDir * Item.knockBack * npc.knockBackResist;

        ScreenShakeSystem.New(new(.1f, .1f), start);
        AdditionsSound.SwordSlice.Play(start, Main.rand.NextFloat(.9f, 1.2f), npc.IsFleshy() ? .12f : -.1f, .3f, 30);
        TimeStop = StopTime;
    }

    // Do the same for players (if it ever happened)
    public override void PlayerHitEffects(in Vector2 start, in Vector2 end, Player player, Player.HurtInfo info)
    {
        for (int i = 0; i < 17; i++)
        {
            int life = Main.rand.Next(30, 40);
            float scale = Main.rand.NextFloat(.9f, 1.6f);
            Color color = Color.Gold.Lerp(Color.Goldenrod, Main.rand.NextFloat(.3f, .7f));
            ParticleRegistry.SpawnSparkParticle(start + Main.rand.NextVector2Circular(9f, 9f), SwordDir.RotatedByRandom(.2f) * Main.rand.NextFloat(9f, 15f), life, scale, color);
        }

        for (int i = 0; i < Main.rand.Next(3, 5); i++)
        {
            int life = Main.rand.Next(20, 30);
            float scale = Main.rand.NextFloat(.9f, 1.1f);
            Color color = Color.Chocolate.Lerp(Color.OrangeRed, Main.rand.NextFloat(.3f, .7f));
            ParticleRegistry.SpawnGlowParticle(start, SwordDir.RotatedByRandom(.21f) * Main.rand.NextFloat(2f, 10f), life, scale, color);
        }

        ScreenShakeSystem.New(new(.1f, .1f), start);
        AdditionsSound.SwordSlice.Play(start, Main.rand.NextFloat(.9f, 1.2f), .12f, .3f, 30);
        TimeStop = StopTime;
    }

    public OptimizedPrimitiveTrail trail;
    public TrailPoints points = new(20);
    public static readonly ITrailTip tip = new RoundedTip(20);
    public static float WidthFunct(float c)
    {
        return SmoothStep(1f, 0f, c) * 30f;
    }

    public Color ColorFunct(SystemVector2 c, Vector2 position)
    {
        // Requires a little tweaking but is better than oddly specific completion times
        float opacity = InverseLerp(0.016f, 0.07f, AngularVelocity);

        return MulticolorLerp(c.X, Color.Gold, Color.Goldenrod, Color.DarkGoldenrod) * opacity;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        if (Mark)
        {
            ManagedShader effect = ShaderRegistry.SpreadTelegraph;
            effect.TrySetParameter("centerOpacity", GetLerpBump(ThrustReel, .7f, 1f, .3f, SwingCompletion) * 2f);
            effect.TrySetParameter("mainOpacity", GetLerpBump(ThrustReel, .8f, 1f, .2f, SwingCompletion) * 3f);
            effect.TrySetParameter("halfSpreadAngle", LightWidth * InverseLerp(ThrustReel, .4f, SwingCompletion));
            effect.TrySetParameter("edgeColor", Color.DarkGoldenrod);

            effect.TrySetParameter("centerColor", Color.Gold);
            effect.TrySetParameter("edgeBlendLength", 0.14f);
            effect.TrySetParameter("edgeBlendStrength", 13f);

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive, effect.Effect);
            Texture2D invis = AssetRegistry.InvisTex;
            Main.EntitySpriteDraw(invis, LightPos - Main.screenPosition, null, Color.White, PiOver2, invis.Size() / 2, 2400f, 0, 0f);
            Main.spriteBatch.ExitShaderRegion();
        }

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
            if (trail != null && !trail._disposed && points != null)
            {
                ManagedShader shader = ShaderRegistry.SpecialLightningTrail;
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.FlameMap2), 1);
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.Cosmos), 2);
                trail.DrawTippedTrail(shader, points.Points, tip, true, 150, true);
            }
        }


        Main.spriteBatch.Draw(Tex, Projectile.Center - Main.screenPosition, null, lightColor,
            Projectile.rotation + RotationOffset, origin, Projectile.scale, Effects, 0f);

        if (SwingCompletion > ReelPercent)
            PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.OverProjectiles);

        return false;
    }
}

public class HolyGlobalNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;
    public int MarkedTime;
    public Vector2 CrossPos;
    public static readonly int TimeMarked = SecondsToFrames(10);
    public override bool AppliesToEntity(NPC entity, bool lateInstantiation)
    {
        return entity.realLife <= 0;
    }
    public override void PostAI(NPC npc)
    {
        if (MarkedTime > 0)
        {
            CrossPos = Vector2.SmoothStep(CrossPos, npc.Center + Vector2.UnitY * -(npc.height + (Sin01(MarkedTime * .01f) * 40f)), .5f);
            MarkedTime--;
        }
    }
    public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        if (MarkedTime > 0)
        {
            Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.HolyCross);
            Vector2 drawPosition = CrossPos - screenPos;
            Color backglow = Color.Gold;
            Vector2 spinPoint = -Vector2.UnitY * 4f;
            float rotation = Main.GlobalTimeWrappedHourly * 2f;
            float scale = Clamp(npc.scale, 0f, 1f) * GetLerpBump(0f, 20f, TimeMarked, TimeMarked - 20f, MarkedTime);

            for (int i = 0; i < 6; i++)
            {
                Vector2 spinStart = drawPosition + Utils.RotatedBy(spinPoint, (double)(rotation - (float)Math.PI * i / 3f), default);
                Color glowAlpha = backglow;
                glowAlpha.A = 25;
                Main.spriteBatch.Draw(tex, spinStart, null, glowAlpha * .85f, 0f, tex.Size() / 2, scale, 0, 0f);
            }
            spriteBatch.Draw(tex, drawPosition, null, Color.White, 0f, tex.Size() / 2, scale, 0, 0f);
        }
    }
}