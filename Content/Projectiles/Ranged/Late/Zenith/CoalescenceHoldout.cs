using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Late;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late.Zenith;

public class CoalescenceHoldout : BaseIdleHoldoutProjectile
{
    public override int AssociatedItemID => ModContent.ItemType<UnparalleledCoalescence>();
    public override int IntendedProjectileType => ModContent.ProjectileType<CoalescenceHoldout>();
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.UnparalleledCoalescence);
    public override void Defaults()
    {
        Projectile.width = 62;
        Projectile.height = 144;
        Projectile.DamageType = DamageClass.Ranged;
    }

    public enum CoalescenceState : int
    {
        Richochet,
        Pierce,
        Blast
    }

    public List<Vector2> Points = [];
    public const int MaxPoints = 50;
    public const float ReelDist = 30f;
    public static readonly float ReelTime = SecondsToFrames(3.5f);
    public ref float Time => ref Projectile.ai[0];
    public ref float Switch => ref Projectile.ai[1];
    public ref float StringCompletion => ref Projectile.ai[2];
    public ref float OldStringCompletion => ref Projectile.AdditionsInfo().ExtraAI[0];
    public ref float TotalTime => ref Projectile.AdditionsInfo().ExtraAI[2];
    public bool Init
    {
        get => Projectile.AdditionsInfo().ExtraAI[3] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[3] = value.ToInt();
    }
    public int StateCounter
    {
        get => (int)Projectile.AdditionsInfo().ExtraAI[4];
        set => Projectile.AdditionsInfo().ExtraAI[4] = value;
    }
    public CoalescenceState CurrentState
    {
        get => (CoalescenceState)StateCounter;
        set => StateCounter = (int)value;
    }
    public int LeadArrowIndex
    {
        get => (int)Projectile.AdditionsInfo().ExtraAI[5];
        set => Projectile.AdditionsInfo().ExtraAI[5] = value;
    }
    public int SecondArrowIndex
    {
        get => (int)Projectile.AdditionsInfo().ExtraAI[6];
        set => Projectile.AdditionsInfo().ExtraAI[6] = value;
    }
    public int ThirdArrowIndex
    {
        get => (int)Projectile.AdditionsInfo().ExtraAI[7];
        set => Projectile.AdditionsInfo().ExtraAI[7] = value;
    }

    public override void OnSpawn(IEntitySource source)
    {
        Switch = -1;
        Init = false;
        Projectile.netUpdate = true;
    }

    public override void WriteExtraAI(BinaryWriter writer)
    {
        writer.Write(Projectile.rotation);
    }
    public override void GetExtraAI(BinaryReader reader)
    {
        Projectile.rotation = reader.ReadSingle();
    }

    public int Dir => Projectile.velocity.X.NonZeroSign();
    public DivinityArrow[] Arrows = new DivinityArrow[3];
    public override void SafeAI()
    {
        Projectile.extraUpdates = 0;
        if (line == null || line.Disposed)
            line = new(c => 3f, (c, pos) => Color.Lerp(Color.Gold, Color.Goldenrod, c.X + Main.GlobalTimeWrappedHourly), null, MaxPoints);

        if (this.RunLocal())
        {
            Projectile.velocity = Center.SafeDirectionTo(Modded.mouseWorld);
            if (Projectile.velocity != Projectile.oldVelocity)
                Projectile.netUpdate = true;
        }
        Owner.ChangeDir(Dir);
        Projectile.rotation = Projectile.velocity.ToRotation();
        Projectile.spriteDirection = (Projectile.velocity.X > 0f).ToDirectionInt();
        Projectile.Center = Center + PolarVector(35f, Projectile.rotation) + PolarVector(10f * Dir * Owner.gravDir, Projectile.rotation + PiOver2);
        Owner.itemRotation = (Projectile.direction * Projectile.velocity).ToRotation();

        float reel = InverseLerp(0f, ReelTime, Time);
        float close = InverseLerp(0f, 22f, Time);

        float armRot = Projectile.rotation + .595f * Dir;
        float reelAnim = MakePoly(2.2f).InFunction.Evaluate(armRot, armRot + .73f * Dir, Switch != 0 ? reel : OldStringCompletion);
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, reelAnim);
        Owner.SetBackHandBetter(Player.CompositeArmStretchAmount.ThreeQuarters, Projectile.rotation + (.2f * Dir));

        Vector2 centerString = PolarVector(14f, Projectile.rotation - Pi);
        Vector2 drawBack = PolarVector(ReelDist * StringCompletion, Projectile.rotation - Pi);

        Vector2 top = Projectile.RotHitbox().Center + PolarVector(35f, Projectile.rotation - PiOver2) + centerString;
        Vector2 middle = Projectile.RotHitbox().Center + centerString + drawBack;
        Lighting.AddLight(middle, Color.Gold.ToVector3() * .7f);

        Vector2 bottom = Projectile.RotHitbox().Center + PolarVector(-35f, Projectile.rotation - PiOver2) + centerString;

        Points = [];
        cache ??= new(MaxPoints);
        for (int i = 0; i < MaxPoints; i++)
            Points.Add(MultiLerp(InverseLerp(0f, MaxPoints, i), top, middle, bottom));
        cache.SetPoints(Points);

        if (this.RunLocal() && Switch == -1 && Modded.SafeMouseLeft.Current)
        {
            Switch = 1;
        }

        if (Projectile.FinalExtraUpdate())
        {
            switch (Switch)
            {
                case 0:
                    Owner.itemTime = Owner.itemAnimation = 0;
                    StringCompletion = Elastic.OutFunction.Evaluate(OldStringCompletion, 0f, close);
                    if (close >= 1f)
                    {
                        Switch = -1;
                        Time = 0f;
                        this.Sync();
                    }
                    break;
                case 1:
                    StringCompletion = MakePoly(2.2f).InFunction.Evaluate(0f, 1f, reel);

                    if (!Init)
                    {
                        if (this.RunLocal())
                        {
                            LeadArrowIndex = Projectile.NewProj(middle, Projectile.velocity.SafeNormalize(Vector2.Zero),
                                ModContent.ProjectileType<DivinityArrow>(), Projectile.damage, Projectile.knockBack, Owner.whoAmI, Projectile.whoAmI, 0f, 0);
                            SecondArrowIndex = Projectile.NewProj(middle, Projectile.velocity.SafeNormalize(Vector2.Zero),
                                ModContent.ProjectileType<DivinityArrow>(), Projectile.damage, Projectile.knockBack, Owner.whoAmI, Projectile.whoAmI, 0f, 1);
                            ThirdArrowIndex = Projectile.NewProj(middle, Projectile.velocity.SafeNormalize(Vector2.Zero),
                                ModContent.ProjectileType<DivinityArrow>(), Projectile.damage, Projectile.knockBack, Owner.whoAmI, Projectile.whoAmI, 0f, -1);
                        }

                        Init = true;
                        this.Sync();
                    }

                    float reelPercent = MathF.Round(reel, 2);
                    if (reelPercent == .33f)
                    {
                        for (int p = 0; p < 3; p++)
                        {
                            int index = p == 0 ? LeadArrowIndex : p == 1 ? SecondArrowIndex : ThirdArrowIndex;
                            DivinityArrow arrow = Main.projectile[index].As<DivinityArrow>();
                            ParticleRegistry.SpawnSparkleParticle(arrow.TipOfArrow, Vector2.Zero, 20, 3.5f, Color.Gold, Color.DarkGoldenrod, 1.5f, 1.2f);
                        }

                        CurrentState = CoalescenceState.Richochet;
                        AdditionsSound.etherealBlazeStart.Play(middle, .7f, -.2f);
                        this.Sync();
                    }
                    if (reelPercent == .66f)
                    {
                        for (int p = 0; p < 3; p++)
                        {
                            int index = p == 0 ? LeadArrowIndex : p == 1 ? SecondArrowIndex : ThirdArrowIndex;
                            DivinityArrow arrow = Main.projectile[index].As<DivinityArrow>();
                            for (int i = -1; i <= 1; i += 2)
                            {
                                for (int a = -1; a <= 2; a += 2)
                                {
                                    for (int o = 0; o < 8; o++)
                                    {
                                        Vector2 pos = arrow.TipOfArrow;
                                        float speed = Main.rand.NextFloat(1f, 12f);
                                        Vector2 horiz = (i == -1 ? -Vector2.UnitX * speed : Vector2.UnitX * speed).RotatedBy(Projectile.rotation);
                                        Vector2 vert = (a == -1 ? -Vector2.UnitY * speed : Vector2.UnitY * speed).RotatedBy(Projectile.rotation);
                                        float size = 80.3f * (1f - InverseLerp(1f, 12f, speed));
                                        int life = 30;
                                        Color col = Color.Lerp(Color.Gold, Color.Red, .5f);
                                        ParticleRegistry.SpawnGlowParticle(pos, vert, life, size, col);
                                        ParticleRegistry.SpawnGlowParticle(pos, horiz, life, size, col);
                                    }
                                }
                            }
                        }

                        CurrentState = CoalescenceState.Pierce;
                        AdditionsSound.etherealBlazeStart.Play(middle);
                        this.Sync();
                    }
                    if (reelPercent == .99f)
                    {
                        for (int p = 0; p < 3; p++)
                        {
                            int index = p == 0 ? LeadArrowIndex : p == 1 ? SecondArrowIndex : ThirdArrowIndex;
                            DivinityArrow arrow = Main.projectile[index].As<DivinityArrow>();
                            float pulseScale = pulseScale = p == 0 ? 1.15f : p == 1 ? .75f : .5f;
                            ParticleRegistry.SpawnPulseRingParticle(arrow.TipOfArrow, Vector2.Zero, 30, arrow.Projectile.rotation, new(1f, .4f), 0f, 360f * pulseScale, Color.Gold, true);
                        }

                        CurrentState = CoalescenceState.Blast;
                        AdditionsSound.etherealBlazeStart.Play(middle, 1.5f, .2f);
                        this.Sync();
                    }
                    for (int p = 0; p < 3; p++)
                    {
                        int index = p == 0 ? LeadArrowIndex : p == 1 ? SecondArrowIndex : ThirdArrowIndex;
                        Main.projectile[index].ai[1] = (int)CurrentState;
                    }

                    if (!Modded.MouseLeft.Current && this.RunLocal())
                    {
                        AdditionsSound.etherealRelease2.Play(middle, Main.rand.NextFloat(.9f, 1.2f), 0f, .1f, 0, Name);
                        float speed = Utils.MultiLerp(InverseLerp(.33f, 1f, reel), 14f, 16f, 22f);

                        if (reel > .33f)
                        {
                            for (int i = 0; i < 20; i++)
                            {
                                Vector2 vel = Projectile.velocity.RotatedByRandom(.3f) * speed * Main.rand.NextFloat(.8f, 1.6f);
                                int life = Main.rand.Next(30, 40);
                                float scale = Main.rand.NextFloat(.9f, 1.7f);
                                Color color = Color.Gold.Lerp(Color.Goldenrod, Main.rand.NextFloat(.3f, .6f));
                                ParticleRegistry.SpawnSparkParticle(middle, vel, life, scale, color);
                                ParticleRegistry.SpawnSquishyLightParticle(middle, vel * 1.7f, life / 2, scale * 1.1f, color);
                            }
                        }

                        for (int p = 0; p < 3; p++)
                        {
                            int index = p == 0 ? LeadArrowIndex : p == 1 ? SecondArrowIndex : ThirdArrowIndex;
                            DivinityArrow arrow = Main.projectile[index].As<DivinityArrow>();
                            arrow.Release = true;
                            arrow.Projectile.velocity = (arrow.Projectile.rotation - PiOver2).ToRotationVector2() * speed;
                            arrow.Projectile.netUpdate = true;
                        }

                        StateCounter = -1;
                        Init = false;
                        OldStringCompletion = StringCompletion;
                        Switch = 0;
                        Time = 0f;
                        this.Sync();
                    }
                    break;
            }

            if (Switch > -1)
            {
                Time++;
                TotalTime++;
            }
            else
                TotalTime = 0f;
        }
    }

    public OptimizedPrimitiveTrail line;
    public TrailPoints cache;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (line == null || line.Disposed || cache == null)
                return;
            ManagedShader strings = ShaderRegistry.SmoothFlame;
            strings.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.DendriticNoise), 1);
            strings.TrySetParameter("heatInterpolant", 10f);
            line.DrawTrail(strings, cache.Points, 150);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.HeldProjectiles);

        Texture2D texture = Projectile.ThisProjectileTexture();
        Vector2 origin = texture.Size() * 0.5f;
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        float rotation = Projectile.rotation;
        SpriteEffects direction = FixedDirection();

        // Draw the main bow
        Main.spriteBatch.Draw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), rotation, origin, Projectile.scale, direction, 0f);
        Main.spriteBatch.Draw(AssetRegistry.GetTexture(AdditionsTexture.UnparalleledCoalescence_Glow), drawPosition, null, Projectile.GetAlpha(Color.White), rotation, origin, Projectile.scale, direction, 0f);
        return false;
    }
}
