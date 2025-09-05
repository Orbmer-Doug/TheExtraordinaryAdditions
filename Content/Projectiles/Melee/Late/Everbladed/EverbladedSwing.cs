using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;
using static Terraria.Main;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Late.Everbladed;

public class EverbladedSwing : BaseSwordSwing
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.EverbladedSwing);
    public ref float ForwardAngle => ref ModdedProj.ExtraAI[8];
    public ref float DesiredForwardAngle => ref ModdedProj.ExtraAI[9];

    public Quaternion Rotation
    {
        get;
        set;
    }

    internal enum Phase
    {
        Swinging,
        VisceralSlice
    }

    internal Phase CurrentPhase
    {
        get => (Phase)ModdedProj.ExtraAI[10];
        set => ModdedProj.ExtraAI[10] = (int)value;
    }

    public bool ReachedTarget
    {
        get => ModdedProj.ExtraAI[11] == 1f;
        set => ModdedProj.ExtraAI[11] = value.ToInt();
    }

    public override void WriteExtraAI(BinaryWriter writer)
    {
        writer.Write(Rotation.X);
        writer.Write(Rotation.Y);
        writer.Write(Rotation.Z);
        writer.Write(Rotation.W);
    }
    public override void GetExtraAI(BinaryReader reader)
    {
        float x = reader.ReadSingle();
        float y = reader.ReadSingle();
        float z = reader.ReadSingle();
        float w = reader.ReadSingle();
        Rotation = new(x, y, z, w);
    }

    public override void StaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 200;
        ProjectileID.Sets.TrailingMode[Type] = 2;
    }

    public override void Defaults()
    {
    }

    public override void SafeInitialize()
    {
        if (this.RunLocal())
        {
            SwingDir = SwingDir == SwingDirection.Down ? SwingDirection.Up : SwingDirection.Down;
            if (CurrentPhase == Phase.VisceralSlice)
            {
                ParticleRegistry.SpawnShockwaveParticle(Owner.Center, 20, .4f, 90f, 9f, .35f);
                AdditionsSound.BraveAttackDash03.Play(Owner.Center, 2f);
            }
        }
        points.Clear();
    }

    public override void SafeAI()
    {
        // Create the trail if needed
        if (trail == null || trail._disposed)
            trail = new(WidthFunct, ColorFunct, OffsetFunct);

        switch (CurrentPhase)
        {
            case Phase.Swinging:
                DoSwinging();
                break;
            case Phase.VisceralSlice:
                DoSlice();
                break;
        }
        Projectile.Center = Owner.GetFrontHandPositionImproved();
        Rotation = EulerAnglesConversion(1, Projectile.rotation, ForwardAngle);

        // Owner values
        Owner.heldProj = Projectile.whoAmI;
        Owner.SetDummyItemTime(2);
        Owner.ChangeDir(Direction);
        float armRot = (Rect().Left.SafeDirectionTo(Rect().Top).ToRotation() - PiOver2) * Owner.gravDir + (Owner.gravDir == -1 ? Pi : 0f);
        Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRot);
        Owner.itemRotation = WrapAngle(Projectile.rotation);

        float scaleUp = MeleeScale;
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
    }

    public override bool? CanDamage()
    {
        return CurrentPhase switch
        {
            Phase.Swinging => SwingCompletion.BetweenNum(.3f, .8f, true) ? null : false,
            Phase.VisceralSlice => true,
            _ => base.CanDamage(),
        };
    }

    public void DoSwinging()
    {
        if (this.RunLocal())
        {
            DesiredForwardAngle = Utils.Remap(Modded.mouseWorld.Distance(Owner.Center), 0f, screenHeight / 2, 0f, 1.5f);
            ForwardAngle = SmoothStep(ForwardAngle, DesiredForwardAngle, .15f);
        }

        Projectile.rotation = SwingOffset();

        // swoosh
        if (Animation() >= .26f && !PlayedSound)
        {
            AdditionsSound.BigSwing2.Play(Projectile.Center, 1f, 0f, .2f);
            PlayedSound = true;
        }

        // Reset if still holding left, otherwise fade
        if (this.RunLocal() && SwingCompletion >= 1f)
        {
            if (Modded.SafeMouseLeft.Current && VanishTime <= 0)
            {
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

    public const int SliceTime = 45;
    public const int SpinTime = 150;
    public void DoSlice()
    {
        if (Time < SliceTime)
        {
            Vector2 pos = Owner.RandAreaInEntity();
            Vector2 vel = -Projectile.velocity * rand.NextFloat(9f, 14f);
            int life = rand.Next(30, 50);
            float size = rand.NextFloat(.5f, .9f);
            ParticleRegistry.SpawnBloomLineParticle(pos, vel, life, size, Color.Crimson);
            ParticleRegistry.SpawnSparkleParticle(pos, vel, life, size * .7f, Color.DarkRed, Color.Crimson);
            ParticleRegistry.SpawnBloomPixelParticle(pos, vel, life * 2, size, Color.DarkRed, Color.Crimson, null, 1.4f, 9);

            Projectile.rotation = SwordRotation;
            Owner.velocity = Projectile.velocity * 100f * MakePoly(3f).InOutFunction.Evaluate(1f, 0f, InverseLerp(0f, SliceTime, Time));
            Modded.LungingDown = true;
        }
        else
        {
            if (Modded.LungingDown)
            {
                Projectile.ResetLocalNPCHitImmunity();
                Modded.LungingDown = false;
            }

            Projectile.localNPCHitCooldown = 7 * MaxUpdates;
            float completion = MathF.Round(InverseLerp(SliceTime, SliceTime + SpinTime, Time), 2);
            Projectile.rotation = (Pi * MakePoly(3.5f).InOutFunction(1f - completion) * (-4f * Direction)) + SwordRotation;
            ForwardAngle = MakePoly(2f).InOutFunction.Evaluate(0f, PiOver2, completion);

            if (completion == .35f || completion == .57f)
            {
                AdditionsSound.BraveSwingLarge.Play(Projectile.Center, 1.2f);
            }

            if (completion >= 1f)
                VanishTime++;
        }
    }
    public override float SwingOffset()
    {
        return SwordRotation + SwingAngle * Animation() * (SwingDir != SwingDirection.Up).ToDirectionInt() * Direction * Owner.gravDir;
    }

    public override RotatedRectangle Rect()
    {
        Vector2 visibleSize = new(18f, 204f * 1.7f);
        float y = 0f;
        if (Direction == -1 && SwingDir == SwingDirection.Down)
            y -= 14f;
        if (Direction == 1 && SwingDir == SwingDirection.Up)
            y -= 14f;

        Vector3 size3D = new(0, visibleSize.Y, 0);
        Vector3 tip;
        Vector2 begin, end;

        Quaternion angleFix = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -(3f * Pi / 4f));
        Quaternion final = Quaternion.Multiply(Rotation, angleFix);

        tip = Vector3.Transform(size3D, Quaternion.CreateFromRotationMatrix(Matrix.CreateFromQuaternion(final) * Matrix.CreateRotationZ(InitialMouseAngle)));
        begin = Projectile.Center;
        end = begin + new Vector2(tip.X, tip.Y);
        GetPrincipalAxes(final, out float roll, out float _, out float _);
        float projectedWidth = visibleSize.X * MathF.Abs(MathF.Cos(roll));
        RotatedRectangle rect = new(projectedWidth, begin, end);
        return rect;
    }

    public override void NPCHitEffects(in Vector2 start, in Vector2 end, NPC npc, NPC.HitInfo hit)
    {
        npc.velocity += SwordDir * 8f * npc.knockBackResist;

        switch (CurrentPhase)
        {
            case Phase.Swinging:

                for (int i = 0; i < 24; i++)
                {
                    Vector2 vel = (Rect().Top.SafeDirectionTo(Rect().Bottom) * (SwingDir != SwingDirection.Up).ToDirectionInt() * Direction).RotatedByRandom(.3f) * rand.NextFloat(7f, 15f);
                    float scale = rand.NextFloat(1f, 1.6f);

                    ParticleRegistry.SpawnMistParticle(start, vel, scale, Color.Crimson, Color.DarkRed, rand.NextFloat(90f, 190f), rand.NextFloat(-.05f, .05f));
                    ParticleRegistry.SpawnCloudParticle(start, vel, Color.Crimson, Color.Black, rand.Next(30, 50), rand.NextFloat(.1f, .3f), rand.NextFloat(.5f, .7f));
                    ParticleRegistry.SpawnBloodStreakParticle(start, vel, 20, .4f, Color.DarkRed);
                    ParticleRegistry.SpawnBloodParticle(start, vel, rand.Next(50, 70), rand.NextFloat(.3f, .8f), Color.Red);
                }

                if (this.RunLocal())
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 pos = npc.RandAreaInEntity();
                        HyperSlash slash = projectile[Projectile.NewProj(pos, Vector2.Zero, ModContent.ProjectileType<HyperSlash>(),
                            Projectile.damage, Projectile.knockBack, Owner.whoAmI)].As<HyperSlash>();

                        float dist = rand.NextFloat(400f, 500f);
                        float angle = RandomRotation();
                        float offset = rand.NextFloat(.3f, .9f);

                        slash.Start = pos + PolarVector(dist, angle + offset);
                        slash.Center = pos;
                        slash.End = pos + PolarVector(dist, angle - offset);
                    }
                }

                ScreenShakeSystem.New(new(.1f, .2f), start);
                AdditionsSound.MimicryLand.Play(start, 2.4f, 0f, .2f);

                break;
            case Phase.VisceralSlice:
                if (Time < SliceTime)
                {
                    for (int i = 0; i < 4; i++)
                        ParticleRegistry.SpawnBloodStreakParticle(start, Projectile.velocity.RotatedByRandom(.9f).SafeNormalize(Vector2.Zero) * rand.NextFloat(2f, 4f), rand.Next(50, 70), rand.NextFloat(.6f, 1.1f), Color.DarkRed);

                    ScreenShakeSystem.New(new(.4f, .3f), start);
                    AdditionsSound.IkeStab.Play(start, 1.4f, -.1f);
                }
                else
                {
                    for (int i = 0; i < 24; i++)
                    {
                        Vector2 vel = (Rect().Top.SafeDirectionTo(Rect().Bottom) * (SwingDir != SwingDirection.Up).ToDirectionInt() * Direction).RotatedByRandom(.3f) * rand.NextFloat(7f, 15f);
                        float scale = rand.NextFloat(1f, 1.6f);

                        ParticleRegistry.SpawnMistParticle(start, vel, scale, Color.Crimson, Color.DarkRed, rand.NextFloat(90f, 190f), rand.NextFloat(-.05f, .05f));
                        ParticleRegistry.SpawnCloudParticle(start, vel, Color.Crimson, Color.Black, rand.Next(30, 50), rand.NextFloat(.1f, .3f), rand.NextFloat(.5f, .7f));
                        ParticleRegistry.SpawnBloodStreakParticle(start, vel, 20, .4f, Color.DarkRed);
                        ParticleRegistry.SpawnBloodParticle(start, vel, rand.Next(50, 70), rand.NextFloat(.3f, .8f), Color.Red);
                    }

                    ScreenShakeSystem.New(new(.3f, .2f), start);
                    AdditionsSound.RoySpecial2.Play(start, 1.7f, 0f, .15f);
                }

                break;
        }
    }

    public override void PlayerHitEffects(in Vector2 start, in Vector2 end, Player player, Player.HurtInfo info)
    {
        for (int i = 0; i < 24; i++)
        {
            Vector2 vel = (Rect().Top.SafeDirectionTo(Rect().Bottom) * (SwingDir != SwingDirection.Up).ToDirectionInt() * Direction).RotatedByRandom(.3f) * rand.NextFloat(7f, 15f);
            float scale = rand.NextFloat(1f, 1.6f);

            ParticleRegistry.SpawnMistParticle(start, vel, scale, Color.Crimson, Color.DarkRed, rand.NextFloat(90f, 190f), rand.NextFloat(-.05f, .05f));
            ParticleRegistry.SpawnCloudParticle(start, vel, Color.Crimson, Color.Black, rand.Next(30, 50), rand.NextFloat(.1f, .3f), rand.NextFloat(.5f, .7f));
            ParticleRegistry.SpawnBloodStreakParticle(start, vel, 20, .4f, Color.DarkRed);
            ParticleRegistry.SpawnBloodParticle(start, vel, rand.Next(50, 70), rand.NextFloat(.3f, .8f), Color.Red);
        }

        ScreenShakeSystem.New(new(.1f, .2f), start);
        AdditionsSound.BraveSmashH01.Play(start, 1f, 0f, .3f);
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (CurrentPhase == Phase.Swinging)
        {
            modifiers.FinalDamage *= Lerp(1f, 2.5f, InverseLerp(0f, 1.5f, ForwardAngle));
        }
        else if (CurrentPhase == Phase.VisceralSlice && Time < SliceTime)
            modifiers.FinalDamage *= 2;
    }

    public OptimizedPrimitiveTrail trail;
    private readonly ManualTrailPoints points = new(20);

    public static float WidthFunct(float c)
    {
        return 258f;
    }

    public Color ColorFunct(SystemVector2 c, Vector2 position)
    {
        float angularOffset = WrapAngle(Projectile.rotation - OldRotations[1]);
        float angularVelocity = MathF.Abs(angularOffset);
        float afterimageOpacity = InverseLerp(0.012f, 0.07f, angularVelocity);

        return MulticolorLerp(c.X, Color.Crimson.Lerp(Color.White, .4f), Color.Crimson, Color.Red, Color.DarkRed, Color.Black) * afterimageOpacity;
    }

    public SystemVector2 OffsetFunct(float c) => -Projectile.Center.ToNumerics();

    public override bool PreDraw(ref Color lightColor)
    {
        // Determine the effects for drawing. These must be done here otherwise silly things WILL happen.
        Vector2 origin;
        bool flip = SwingDir != SwingDirection.Up;
        if (Direction == -1)
            flip = SwingDir == SwingDirection.Up;
        if (Owner.gravDir == -1)
            flip = !flip;

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

        // Prepare the zany trail
        void draw()
        {
            ManagedShader trailShader = ShaderRegistry.SwingShaderIntense;
            trailShader.TrySetParameter("firstColor", new Color(152, 15, 16));
            trailShader.TrySetParameter("secondaryColor", new Color(168, 50, 50));
            trailShader.TrySetParameter("tertiaryColor", new Color(173, 26, 16));
            trailShader.TrySetParameter("flip", flip);

            trailShader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.SuperWavyPerlin), 0, SamplerState.LinearWrap);
            trailShader.Matrix = Calculate3DPrimitiveMatrix(Projectile.Center, Rotation, Projectile.scale, InitialMouseAngle, 1);
            trail.DrawTrail(trailShader, points.Points);
        }

        // Draw the main sword
        DrawTextureIn3D(Tex, Projectile.Center, Rotation, Projectile.scale, InitialMouseAngle, Color.White, false, (Direction < 0 ? -(int)SwingDir : (int)SwingDir) * (int)Owner.gravDir);

        if ((CurrentPhase == Phase.VisceralSlice && Time > SliceTime + 25) || (CurrentPhase == Phase.Swinging && Time > 5f))
        {
            // Prepare the list of smoothened positions.
            const int oldPositionCount = 20;
            const int subdivisions = 10;
            float afterimageOffset = Tex.Height;
            List<Vector2> trailPositions = [];
            for (int i = 0; i < oldPositionCount; i++)
            {
                float startingRotation = Projectile.oldRot[i] - Projectile.rotation - SwordRotation;
                float endingRotation = Projectile.oldRot[i + 1] - Projectile.rotation - SwordRotation;
                for (int j = 0; j < subdivisions; j++)
                {
                    float rotation = startingRotation.AngleLerp(endingRotation, j / (float)subdivisions);
                    Vector2 trailVector = rotation.ToRotationVector2() * afterimageOffset;
                    trailPositions.Add(Projectile.Center + trailVector);
                }
            }

            points.SetPoints(trailPositions);

            PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.OverProjectiles);
        }

        return false;
    }
}