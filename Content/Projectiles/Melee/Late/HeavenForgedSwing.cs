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
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;
using static Terraria.Main;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Late;

public class HeavenForgedSwing : BaseSwordSwing
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.HeavenForgedSword);

    public int SwingCounter
    {
        get => (int)ModdedProj.ExtraAI[9];
        set => ModdedProj.ExtraAI[9] = value;
    }

    public bool DontChangeDir
    {
        get => ModdedProj.ExtraAI[10] == 1f;
        set => ModdedProj.ExtraAI[10] = value.ToInt();
    }

    public override int SwingTime => SwingCounter switch
    {
        0 => 40,
        1 => 60,
        2 => 45,
        _ => 222
    };

    public Quaternion Rotation
    {
        get;
        set;
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
        ProjectileID.Sets.TrailCacheLength[Type] = 100;
        ProjectileID.Sets.TrailingMode[Type] = 2;
    }

    public override void SafeInitialize()
    {
        points.Clear();
    }

    public override void SafeAI()
    {
        // Create the trail if needed
        if (trail == null || trail._disposed)
            trail = new(WidthFunct, ColorFunct, OffsetFunct);

        Quaternion start = EulerAnglesConversion(Direction, -.06f, .1f);
        Quaternion anticipation = EulerAnglesConversion(Direction, -1.96f, -.8f);
        Quaternion slash = EulerAnglesConversion(Direction, 2.65f, -1.2f);
        Quaternion end = EulerAnglesConversion(Direction, 3.95f, -1.4f);
        PiecewiseRotation sweep = new PiecewiseRotation().
                    Add(Sine.InOutFunction, anticipation, 0.3f, start).
                    Add(MakePoly(4).InFunction, slash, 0.7f).
                    Add(MakePoly(2).OutFunction, end, 1f);

        float forwardAngle = Utils.MultiLerp(SwingCompletion.Squared(), -1.4f, -PiOver2 + .1f, 0f);
        float spinAngle = Pi * MakePoly(3.5f).InOutFunction(1f - SwingCompletion) * -4f;
        Quaternion spin = EulerAnglesConversion(Direction, spinAngle + 3.95f, forwardAngle);
        Quaternion spinFinal = EulerAnglesConversion(Direction, (Pi * MakePoly(3.5f).InOutFunction(0f) * 4f) + 3.95f, 0f);

        Quaternion slam = EulerAnglesConversion(Direction, -.06f, .2f);
        Quaternion slamEnd = EulerAnglesConversion(Direction, -1.95f, -.55f);

        switch (SwingCounter % 3)
        {
            case 0:
                SwingDir = SwingDirection.Down;
                Rotation = sweep.Evaluate(SwingCompletion, Direction == -1f && SwingCompletion >= 0.7f, 1);
                DontChangeDir = true;

                if (Time == (int)(SwingTime * 0.84f))
                {
                    CreateBolts();
                    AdditionsSound.MediumSwing2.Play(Projectile.Center, 1.2f, 0f, .2f);
                }
                break;
            case 1:
                SwingDir = SwingDirection.Down;
                Rotation = spin;

                int wait = (int)(SwingTime * 0.6f);
                if (Time % wait == wait - 1 && SwingCompletion < .5f)
                {
                    CreateBolts();
                    AdditionsSound.MediumSwing.Play(Projectile.Center, 1.4f, 0f, .2f, 10);
                }
                if (Time % wait == wait - 1)
                    Projectile.ResetLocalNPCHitImmunity();

                break;
            case 2:
                SwingDir = SwingDirection.Up;
                PiecewiseRotation up = new PiecewiseRotation().
                    Add(MakePoly(8f).InOutFunction, slam, 1f, spinFinal);
                Rotation = up.Evaluate(SwingCompletion, SwingCompletion < .85f, -1);
                Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
                DontChangeDir = true;

                if (Time == (int)(SwingTime * 0.3f))
                {
                    CreateBolts();
                    Projectile.ResetLocalNPCHitImmunity();
                    AdditionsSound.MediumSwing2.Play(Projectile.Center, 1.4f, 0f, .14f);
                }
                break;
        }

        Projectile.Center = Owner.GetFrontHandPositionImproved();
        Projectile.rotation = Rect().Left.SafeDirectionTo(Rect().Top).ToRotation();

        // Owner values
        Owner.heldProj = Projectile.whoAmI;
        Owner.SetDummyItemTime(2);
        Owner.ChangeDir(Direction);
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Rect().Left.SafeDirectionTo(Rect().Top).ToRotation());
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

        if (this.RunLocal() && SwingCompletion >= 1f)
        {
            if (Modded.SafeMouseLeft.Current && VanishTime <= 0)
            {
                SwingCounter++;

                if (SwingCounter % 3 == 0)
                {
                    SwingCounter = 0;
                    Initialized = false;
                }
                else
                {
                    Time = 0f;
                }
                this.Sync();
            }
            else
            {
                VanishTime++;
                this.Sync();
            }
        }
    }

    public void CreateBolts()
    {
        if (this.RunLocal())
        {
            Vector2 position;
            for (int i = 0; i < 2; i++)
            {
                for (int a = -1; a <= 1; a += 2)
                {
                    Vector2 target = Modded.mouseWorld;
                    position = Owner.Center - new Vector2(Main.rand.NextFloat(100f, Main.screenWidth / 2) * Owner.direction, 600f * a);
                    position.Y -= 40 * i;

                    Vector2 vel = position.SafeDirectionTo(target) * Main.rand.NextFloat(10f, 20f);
                    vel.Y += Main.rand.NextFloat(-2f, 2f);

                    int proj = ModContent.ProjectileType<HeavenForgedSpear>();
                    int dmg = (int)(Projectile.damage * .33f);
                    Projectile.NewProj(position, vel, proj, dmg, Projectile.knockBack / 2, Owner.whoAmI);
                }
            }
        }
    }

    public override bool? CanDamage()
    {
        return base.CanDamage();
    }

    public override float SwingOffset()
    {
        return SwordRotation + SwingAngle * Animation() * (SwingDir != SwingDirection.Up).ToDirectionInt() * Direction * Owner.gravDir;
    }

    public override RotatedRectangle Rect()
    {
        Vector2 visibleSize = new(22f, 173f);
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
        for (int i = 0; i < 24; i++)
        {
            Vector2 vel = (Rect().Bottom.SafeDirectionTo(Rect().Top)).RotatedByRandom(.3f) * rand.NextFloat(7f, 15f);
            ParticleRegistry.SpawnSparkParticle(start + Main.rand.NextVector2Circular(9f, 9f), vel, Main.rand.Next(30, 40), Main.rand.NextFloat(.7f, 1f), Color.DeepSkyBlue);
        }

        AdditionsSound.etherealHit4.Play(start, 1f, 0f, .2f, 10, Name);
        npc.velocity += SwordDir * 8f * npc.knockBackResist;
    }

    public override void PlayerHitEffects(in Vector2 start, in Vector2 end, Player player, Player.HurtInfo info)
    {
        for (int i = 0; i < 24; i++)
        {
            Vector2 vel = (Rect().Bottom.SafeDirectionTo(Rect().Top)).RotatedByRandom(.3f) * rand.NextFloat(7f, 15f);
            ParticleRegistry.SpawnSparkParticle(start + Main.rand.NextVector2Circular(9f, 9f), vel, Main.rand.Next(30, 40), Main.rand.NextFloat(.7f, 1f), Color.DeepSkyBlue);
        }

        AdditionsSound.etherealHit4.Play(start, 1f, 0f, .2f, 10, Name);
    }

    public OptimizedPrimitiveTrail trail;
    private readonly ManualTrailPoints points = new(20);

    public static float WidthFunct(float c)
    {
        return 119f;
    }

    public Color ColorFunct(SystemVector2 c, Vector2 position)
    {
        float angularOffset = WrapAngle(Projectile.rotation - OldRotations[1]);
        float angularVelocity = MathF.Abs(angularOffset);
        float afterimageOpacity = InverseLerp(0.01f, 0.03f, angularVelocity);

        return Color.White * afterimageOpacity;
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
            trailShader.TrySetParameter("firstColor", new Color(120, 225, 246));
            trailShader.TrySetParameter("secondaryColor", new Color(136, 251, 224));
            trailShader.TrySetParameter("tertiaryColor", new Color(92, 227, 156));
            trailShader.TrySetParameter("flip", flip);

            trailShader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.Cosmos), 0, SamplerState.LinearWrap);
            trailShader.Matrix = Calculate3DPrimitiveMatrix(Projectile.Center, Rotation, Projectile.scale, InitialMouseAngle, 1);
            trail.DrawTrail(trailShader, points.Points, 40);
        }

        // Draw the main sword
        DrawTextureIn3D(Tex, Projectile.Center, Rotation, Projectile.scale, InitialMouseAngle, Color.White, false, (Direction < 0 ? -(int)SwingDir : (int)SwingDir) * (int)Owner.gravDir);

        if (SwingCompletion > .3f)
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

            PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.OverPlayers);
        }

        return false;
    }
}
