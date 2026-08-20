using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Daybreak.Common.Rendering;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Graphics.Meshes;
using TheExtraordinaryAdditions.Core.Graphics.Resources;
using TheExtraordinaryAdditions.Core.Graphics.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;
using static Terraria.Main;
using static TheExtraordinaryAdditions.Core.Utilities.QuaternionUtils;
using Utils = Terraria.Utils;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Late;

public class HeavenForgedSwing : BaseSwordSwing
{
    public override string Texture => AssetRegistry.GennedTextures.HeavenForgedSword.Path;

    public int SwingCounter
    {
        get => (int) ProjInfo.ExtraAI[9];
        set => ProjInfo.ExtraAI[9] = value;
    }

    public override int SwingTime => SwingCounter switch
    {
        0 => 40,
        1 => 60,
        2 => 30,
        _ => 222
    };

    public Quaternion Rotation { get; set; }

    public const int MaxTrailPoints = 30;
    public Trail3D Trail;
    public TrailPoints3D Points = new(MaxTrailPoints);

    /// <summary>
    /// Smoothed angular-velocity alpha for the current frame
    /// </summary>
    public float TrailAlpha;

    public new Quaternion[] OldRotations = new Quaternion[5];

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
        Points.Clear();
    }

    public override void SafeAI()
    {
        // Create the trail if needed
        if (Trail == null || Trail.Disposed)
            Trail = new(WidthFunct, ColorFunct, OffsetFunct);

        if (TimeStop <= 0f)
        {
            for (int i = OldRotations.Length - 1; i > 0; i--)
            {
                OldRotations[i] = OldRotations[i - 1];
            }

            OldRotations[0] = Rotation;
        }

        bool cw = Direction == 1;
        Quaternion start = CreateFromPolarAngles(PiOver2, .1f, cw);
        Quaternion anticipation = CreateFromPolarAngles(-.3f, -.8f, cw);
        Quaternion slash = CreateFromPolarAngles(2.65f, -1.1f, cw);
        const float endSide = -1.4f;
        Quaternion end = CreateFromPolarAngles(3.95f, endSide, cw);

        Quaternion spin(float completion)
        {
            float forwardAngle = Utils.MultiLerp(completion.Squared(), endSide, -PiOver2 + .4f, 0f);
            float spinAngle = Pi * MakePoly(3.5f).InOutFunction(1f - completion) * 6f;
            return CreateFromPolarAngles(spinAngle + 3.95f, forwardAngle, cw);
        }

        Quaternion slam = CreateFromPolarAngles(PiOver2, .3f, cw);

        switch (SwingCounter % 3)
        {
            case 0:
                SwingDir = SwingDirection.Down;
                Rotation = new PiecewiseRotation()
                    .Add(MakePoly(2f).InOutFunction, anticipation, 0.4f, start)
                    .Add(MakePoly(4).InFunction, slash, 0.7f, optimalRoute: true)
                    .Add(MakePoly(2).OutFunction, end, 1f)
                    .Evaluate(SwingCompletion);

                if (Time == (int) (SwingTime * 0.84f))
                {
                    AssetRegistry.GennedSounds.MediumSwing2.Play(Projectile.Center, 1.2f, 0f, .2f);
                }

                break;
            case 1:
                SwingDir = SwingDirection.Up;
                Rotation = spin(SwingCompletion);

                int wait = (int) (SwingTime * 0.6f);
                if (Time % wait == wait - 1 && SwingCompletion < .5f)
                {
                    AssetRegistry.GennedSounds.MediumSwing.Play(Projectile.Center, 1.4f, 0f, .2f, 10);
                }

                if (Time % wait == wait - 1)
                    Projectile.ResetLocalNPCHitImmunity();

                wait = (int) (SwingTime * .1f);
                float dot1 = Clamp01(Math.Abs(Quaternion.Dot(OldRotations[0], OldRotations[1])));
                if (Time % wait == wait - 1 && 2f * MathF.Acos(dot1) > .1f)
                    CreateBolts();

                break;
            case 2:
                SwingDir = SwingDirection.Up;
                Rotation = spin(1f).Slerp(slam, MakePoly(8f).InOutFunction(SwingCompletion));
                Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());

                if (Time == (int) (SwingTime * 0.3f))
                {
                    Projectile.ResetLocalNPCHitImmunity();
                    AssetRegistry.GennedSounds.MediumSwing2.Play(Projectile.Center, 1.4f, 0f, .14f);
                }

                break;
        }

        RotatedRectangle rect = Rect();
        Projectile.Center = Owner.GetFrontHandPositionImproved();
        Projectile.rotation = rect.Top.AngleTo(rect.Bottom);

        // Find the orthonormal key frame of the blade 
        Vector3 size3D = new(0, Projectile.height, 0);
        Quaternion angleFix = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, ThreePIOver4 * Direction);
        Quaternion fixedRot = Quaternion.Multiply(Rotation, angleFix);
        Quaternion final = Quaternion.Concatenate(fixedRot,
            Quaternion.CreateFromAxisAngle(Vector3.UnitZ, InitialMouseAngle));

        // Deriving from the forward vector and shifting it Pi/2 on its own axis guarantees the normal is always defined relative to the blades current basis
        Quaternion normalRot = Quaternion.Concatenate(final, Quaternion.CreateFromAxisAngle(final.Forward(), PiOver2));
        Vector3 normal = Vector3.Normalize(Vector3.Transform(size3D, normalRot));
        Vector3 center = Vector3.Transform(size3D * .9f, final);

        // Compute angular delta between the two most recent recorded rotations
        // The dot product gives cos(half-angle) between the two orientations
        float dot = Clamp01(Math.Abs(Quaternion.Dot(OldRotations[0], OldRotations[1])));

        // Gives the full rotation angle
        float angularDelta = 2f * MathF.Acos(dot);

        // The delta (in radians per frame) at which alpha reaches 1
        const float referenceAngularSpeed = 0.12f;

        // Smoothly remap delta onto [0,1] with a slight ease so that very slow motion
        // fades hard and moderate motion looks full. SmoothStep avoids a harsh knee.
        float smoothAlpha = SmoothStep(0f, 1f, angularDelta / referenceAngularSpeed);

        // Lerp toward the new value rather than snapping, so fast to slow transitions bleed out gracefully over a few frames rather than cutting off abruptly
        const float smoothing = 0.18f;
        TrailAlpha = Lerp(TrailAlpha, smoothAlpha, 1f - MathF.Pow(1f - smoothing, 1f / MaxUpdates));

        Vector3 finalNormal = Vector3.Cross(center, normal);
        Points.Update(new(center, finalNormal, TrailAlpha));

        // Owner values
        Owner.heldProj = Projectile.whoAmI;
        Owner.SetDummyItemTime(2);
        Owner.ChangeDir(Direction);
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation + MathHelper.Pi);
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
                    Time = 0;
                }
            }
            else
            {
                VanishTime++;
            }

            this.Sync();
        }
    }

    public void CreateBolts()
    {
        if (this.RunLocal())
        {
            RotatedRectangle rect = Rect();
            Vector2 position = rect.Top;

            Vector2 vel = rect.Bottom.SafeDirectionTo(rect.Top) * rand.NextFloat(10f, 15f);
            vel.Y += rand.NextFloat(-2f, 2f);

            int proj = ModContent.ProjectileType<HeavenForgedSpear>();
            int dmg = (int) (Projectile.damage * .25f);
            Projectile.NewProj(position, vel, proj, dmg, Projectile.knockBack / 2, Owner.whoAmI);

            for (int i = 0; i < 10; i++)
            {
                ParticleRegistry.SpawnSparkleParticle(position, vel / 3 + RandomVelocity(1f, 1f, 2f),
                    rand.Next(30, 40), rand.NextFloat(.2f, .3f), Color.Cyan, Color.CornflowerBlue, 1.4f);
                ParticleRegistry.SpawnBloomPixelParticle(position, vel / 3 + RandomVelocity(1.4f, 2f, 5f),
                    rand.Next(20, 30), rand.NextFloat(.4f, .5f), Color.Cyan, Color.DeepSkyBlue, null, 1f, 4);
            }

            AssetRegistry.GennedSounds.etherealSwordAttackBasic2.Play(position, .3f, .5f, .3f, 20);
        }
    }

    public override float SwingOffset()
    {
        return 0f;
    }

    public override RotatedRectangle Rect()
    {
        Vector2 visibleSize = new(22f, 173f);
        Vector3 size3D = new(0, visibleSize.Y, 0);

        Quaternion angleFix = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, ThreePIOver4 * Direction);
        Quaternion final = Quaternion.Concatenate(Quaternion.Multiply(Rotation, angleFix),
            Quaternion.CreateFromAxisAngle(Vector3.UnitZ, InitialMouseAngle));

        Vector3 tip = Vector3.Transform(size3D,
            Quaternion.CreateFromRotationMatrix(Matrix.CreateFromQuaternion(final)));
        Vector2 begin = Projectile.Center;
        Vector2 end = begin + new Vector2(tip.X, tip.Y);
        float projectedWidth = visibleSize.X * MathF.Abs(MathF.Cos(final.AngleAroundZ()));
        RotatedRectangle rect = new(projectedWidth, begin, end);
        return rect;
    }

    public override void NPCHitEffects(in Vector2 start, in Vector2 end, NPC npc, NPC.HitInfo hit)
    {
        for (int i = 0; i < 24; i++)
        {
            Vector2 vel =
                Projectile.rotation.ToRotationVector2()
                    .Perp(SwingDir == (Direction == -1 ? SwingDirection.Up : SwingDirection.Down))
                    .RotatedByRandom(.3f) * rand.NextFloat(7f, 15f);
            ParticleRegistry.SpawnSparkParticle(start + rand.NextVector2Circular(9f, 9f), vel, rand.Next(30, 40),
                rand.NextFloat(.7f, 1f), Color.DeepSkyBlue);
        }

        AssetRegistry.GennedSounds.etherealHit4.Play(start, 1f, 0f, .2f, 10, Name);
        npc.velocity += SwordDir * 8f * npc.knockBackResist;
    }

    public override void PlayerHitEffects(in Vector2 start, in Vector2 end, Player player, Player.HurtInfo info)
    {
        for (int i = 0; i < 24; i++)
        {
            Vector2 vel =
                Projectile.rotation.ToRotationVector2()
                    .Perp(SwingDir == (Direction == -1 ? SwingDirection.Up : SwingDirection.Down))
                    .RotatedByRandom(.3f) * rand.NextFloat(7f, 15f);
            ParticleRegistry.SpawnSparkParticle(start + rand.NextVector2Circular(9f, 9f), vel, rand.Next(30, 40),
                rand.NextFloat(.7f, 1f), Color.DeepSkyBlue);
        }

        AssetRegistry.GennedSounds.etherealHit4.Play(start, 1f, 0f, .2f, 10, Name);
    }

    public static float WidthFunct(float c)
    {
        return 139f;
    }

    public Color ColorFunct(SystemVector2 c)
    {
        return Color.Cyan;
    }

    public SystemVector3 OffsetFunct(float c) => new(Projectile.Center.ToNumerics(), 0f);

    public override bool PreDraw(ref Color lightColor)
    {
        // Prepare the zany trail
        void draw()
        {
            bool flip = SwingDir != SwingDirection.Up;
            if (Direction == -1)
                flip = SwingDir == SwingDirection.Up;
            if ((int) Owner.gravDir == -1)
                flip = !flip;
            ManagedShader shader = AssetRegistry.GennedShaders.SwingShaderIntense;
            shader.TrySetParameter("firstColor", new Color(120, 225, 246));
            shader.TrySetParameter("secondaryColor", new Color(136, 251, 224));
            shader.TrySetParameter("tertiaryColor", new Color(92, 227, 156));
            shader.TrySetParameter("flip", flip);

            shader.SetTexture(AssetRegistry.GennedTextures.Cosmos, 0, SamplerState.LinearWrap);
            Trail.DrawTrail(shader, Points.Points, MaxTrailPoints * 6, true, GetOrthographicMeshMatrix(false));
        }

        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.HeldProjectiles);

        spriteBatch.End(out SpriteBatchSnapshot ss);
        spriteBatch.Begin(ss with {BlendState = BlendState.AlphaBlend});
        // Draw the main sword
        bool flip = (Direction < 0 ? -(int) SwingDir : (int) SwingDir) == -1;
        Draw3D(Tex, Projectile.Center, Rotation, Projectile.scale, InitialMouseAngle, Color.White, Vector2.Zero, flip,
            Direction == 1 ? ThreePIOver2 : PiOver2, Direction == 1 ? -PiOver2 : 0f);
        spriteBatch.Restart(ss);

        return false;
    }
}
