using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;
using static System.MathF;
using static TheExtraordinaryAdditions.Content.Projectiles.Base.BaseSwordSwing;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

public class CyberneticSword : ProjOwnedByNPC<Asterlin>
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CyberneticSword);

    #region Variables
    public Texture2D Tex => Projectile.ThisProjectileTexture();
    public ref float Time => ref Projectile.ai[0];
    public ref float OverallTime => ref Projectile.ai[1];
    public bool PlayedSound
    {
        get => Projectile.ai[2] == 1f;
        set => Projectile.ai[2] = value.ToInt();
    }
    public ref float VanishTime => ref Projectile.AdditionsInfo().ExtraAI[0];
    public ref float RotationOffset => ref Projectile.AdditionsInfo().ExtraAI[1];
    public bool Initialized
    {
        get => Projectile.AdditionsInfo().ExtraAI[2] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[2] = value.ToInt();
    }
    public ref float InitialMouseAngle => ref Projectile.AdditionsInfo().ExtraAI[3];
    public SwingDirection SwingDir
    {
        get => (SwingDirection)Projectile.AdditionsInfo().ExtraAI[4];
        set => Projectile.AdditionsInfo().ExtraAI[4] = (int)value;
    }
    public float[] OldRotations = new float[5];
    public SpriteEffects Effects
    {
        get => (SpriteEffects)Projectile.spriteDirection;
        set => Projectile.spriteDirection = (int)value;
    }
    public int Direction
    {
        get => Projectile.direction;
        set => Projectile.direction = value;
    }

    public static readonly int MaxUpdates = 3;

    public Vector2 Center => Owner.Center;

    public static readonly float SwordRotation = PiOver4;
    public static readonly float SwingAngle = TwoPi / 3f;
    public int MaxTime => (int)(Asterlin.Swings_SwingSpeed * MaxUpdates);

    /// <summary>
    /// The difference in rotation based on the last frame.
    /// </summary>
    public float AngularVelocity => Abs(WrapAngle(Projectile.rotation - OldRotations[1]));

    public float SwingCompletion => InverseLerp(0f, MaxTime, Time, true);
    public Vector2 SwordDir;

    /// <summary>
    /// Controls the easing for <see cref="SwingOffset"/>
    /// </summary>
    public float Animation()
    {
        return Animators.Exp(2.2f).InOutFunction.Evaluate(Time, 0f, MaxTime, -1f, 1f);
    }

    public float SwingOffset()
    {
        return InitialMouseAngle + SwingAngle * Animation() * (SwingDir != SwingDirection.Up).ToDirectionInt() * Direction;
    }

    public RotatedRectangle Rect()
    {
        return new(44f * Projectile.scale, Projectile.Center, Projectile.Center + PolarVector(194f * Projectile.scale, Projectile.rotation));
    }
    #endregion

    #region Netwerking
    public override void SendAI(BinaryWriter writer)
    {
        writer.Write((sbyte)Projectile.direction);
        writer.Write((float)Projectile.rotation);
        writer.Write((sbyte)Projectile.spriteDirection);
    }
    public override void ReceiveAI(BinaryReader reader)
    {
        Projectile.direction = (sbyte)reader.ReadSByte();
        Projectile.rotation = (float)reader.ReadSingle();
        Projectile.spriteDirection = (sbyte)reader.ReadSByte();
    }
    #endregion

    public sealed override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 5;
        ProjectileID.Sets.TrailingMode[Type] = 2;
        ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true;
        ProjectileID.Sets.CanHitPastShimmer[Type] = true;
        ProjectileID.Sets.CanDistortWater[Type] = false;
    }

    public sealed override void SetDefaults()
    {
        Projectile.timeLeft = 10000;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.friendly = false;
        Projectile.hostile = true;

        Projectile.penetrate = -1;
        Projectile.MaxUpdates = MaxUpdates;

        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;

        Projectile.netImportant = true;
    }

    #region Collision

    /// <summary>
    /// Defaults to <see cref="Rect"/> seeing if it intersects with a target.
    /// </summary>
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return Rect().Intersects(targetHitbox);
    }

    public override bool? CanDamage() => SwingCompletion.BetweenNum(.3f, .8f, true) ? null : false;

    public override void CutTiles()
    {
        DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
        Utils.PlotTileLine(Rect().BottomLeft, Rect().TopRight, Rect().Width, DelegateMethods.CutTiles);
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        RotatedRectangle rect = Rect();

        // Try to get a accurate point of collision
        if (CheckLinearCollision(rect.BottomLeft, rect.TopRight, target.Hitbox, out Vector2 start, out Vector2 end))
        {
            PlayerHitEffects(start, end, target, info);
        }

        // Otherwise just choose a random spot to not break cohesion.
        else
            PlayerHitEffects(target.RotHitbox().RandomPoint(), target.RotHitbox().RandomPoint(), target, info);
    }

    public void PlayerHitEffects(in Vector2 start, in Vector2 end, Player player, Player.HurtInfo info)
    {
        for (int i = 0; i < 5; i++)
        {
            Vector2 vel = SwordDir.RotatedByRandom(.5f) * Main.rand.NextFloat(9f, 12f);
            ParticleRegistry.SpawnBloodStreakParticle(start, vel.SafeNormalize(Vector2.Zero), 30, Main.rand.NextFloat(.4f, .6f), Color.Crimson);
        }
        AdditionsSound.RoySpecial2.Play(start, .6f, 0f, .3f);
    }
    #endregion

    public override void SafeAI()
    {
        Projectile.width = Tex?.Width ?? 1;
        Projectile.height = Tex?.Height ?? 1;

        for (int i = OldRotations.Length - 1; i > 0; i--)
        {
            OldRotations[i] = OldRotations[i - 1];
        }

        OldRotations[0] = Projectile.rotation;

        if (!Initialized)
        {
            old.Clear();
            Projectile.ResetLocalNPCHitImmunity();

            // Reset time and sync
            PlayedSound = false;

            Projectile.velocity = Center.SafeDirectionTo(ModOwner.Target.Center);
            Direction = Projectile.velocity.X.NonZeroSign();
            InitialMouseAngle = Projectile.velocity.ToRotation();
            Time = 0f;

            this.Sync();
            Initialized = true;
        }

        Projectile.Center = ModOwner.RightHandPosition;
        Projectile.timeLeft = 200;
        ModOwner.SetDirection(-Direction);
        Projectile.rotation = SwingOffset();
        ModOwner.SetRightHandTarget(ModOwner.RightArm.RootPosition + PolarVector(ModOwner.AngledRightArmLength, Projectile.rotation));

        if (SwingCompletion >= .5f && !PlayedSound)
        {
            float maxRad = SwingAngle * 2;
            float dist = ModOwner.AngledRightArmLength;
            float initDir = InitialMouseAngle;
            float angleOffset = 0f;
            float speed = 1f;
            float off = Main.rand.NextFloat(0f, .4f);

            for (int i = 0; i < Asterlin.Swings_DartWaves; i++)
            {
                for (int j = 0; j < Asterlin.Swings_DartAmount; j++)
                {
                    float completion = InverseLerp(0f, Asterlin.Swings_DartAmount - 1, j);
                    float angle = initDir + MathHelper.Lerp(-maxRad / 2, maxRad / 2, completion) + angleOffset + off;
                    Vector2 pos = Owner.Center + PolarVector(dist, angle);
                    Vector2 vel = PolarVector(10f, angle);
                    if (this.RunServer())
                        SpawnProjectile(pos, vel * speed, ModContent.ProjectileType<GodPiercingDart>(), Asterlin.LightAttackDamage, 0f);
                }
                angleOffset = maxRad / (2 * (Asterlin.Swings_DartAmount - 1));
                speed /= 2;
            }

            AdditionsSound.MediumSwing.Play(Projectile.Center, .6f, 0f, .2f, 20, Name);
            PlayedSound = true;
            Projectile.netUpdate = true;
        }

        if (trail == null || trail.Disposed)
            trail = new(WidthFunct, ColorFunct, (c) => Center.ToNumerics(), 25);

        old.Update(Rect().Bottom + PolarVector(140f, Projectile.rotation) + Owner.velocity - Center);

        if (VanishTime <= 0)
        {
            Projectile.scale = MakePoly(3f).OutFunction(InverseLerp(0f, 10f * MaxUpdates, OverallTime));
        }
        else
        {
            Projectile.scale = MakePoly(4f).OutFunction.Evaluate(VanishTime, 0f, 18f * MaxUpdates, 1f, 0f);
            if (Projectile.scale <= 0f)
                Projectile.Kill();
            VanishTime++;
        }

        if (SwingCompletion >= 1f)
        {
            if (Owner.AdditionsInfo().ExtraAI[0] < Asterlin.Swings_MaxSwingCount)
            {
                SwingDir = SwingDir == SwingDirection.Up ? SwingDirection.Down : SwingDirection.Up;
                Initialized = false;
                Owner.AdditionsInfo().ExtraAI[0]++;
                ModOwner.Sync();
                Projectile.netUpdate = true;
            }
            else
            {
                VanishTime++;
            }
        }

        if (AngularVelocity > .03f && Time > 5f && Time % 2 == 1)
        {
            for (int i = 0; i < 2; i++)
            {
                Vector2 pos = Vector2.Lerp(Rect().Bottom + PolarVector(16f, Projectile.rotation), Rect().Top, Main.rand.NextFloat());
                Vector2 vel = -SwordDir * Main.rand.NextFloat(4f, 8f);
                int life = Main.rand.Next(19, 25);
                float scale = Main.rand.NextFloat(.4f, .8f);
                Color color = ColorFunct(new(0f, Main.rand.NextFloat()), Vector2.Zero);
                ParticleRegistry.SpawnTechyHolosquareParticle(pos, vel, life, scale, color, 1f);
            }
        }

        SwordDir = (Projectile.rotation + PiOver2).ToRotationVector2() * (SwingDir != SwingDirection.Up).ToDirectionInt() * Direction;

        Time++;
        OverallTime++;
    }

    /// <summary>
    /// Glue to Asterlin
    /// </summary>
    public override bool ShouldUpdatePosition() => false;

    public OptimizedPrimitiveTrail trail;
    public TrailPoints old = new(25);
    public float WidthFunct(float c)
    {
        return SmoothStep(1f, 0f, c) * 137f * Projectile.scale;
    }

    public Color ColorFunct(SystemVector2 c, Vector2 position)
    {
        float opacity = InverseLerp(0.011f, 0.07f, AngularVelocity);
        return Color.White * opacity;
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

            RotationOffset = PiOver4;
            Effects = SpriteEffects.None;
        }
        else
        {
            origin = new Vector2(Tex.Width, Tex.Height);

            RotationOffset = PI - PiOver4;
            Effects = SpriteEffects.FlipHorizontally;
        }

        void draw()
        {
            if (trail == null || old == null || SwingCompletion < .4f)
                return;

            ManagedShader shader = AssetRegistry.GetShader("CyberneticTrail");
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.TechyNoise), 0, SamplerState.AnisotropicWrap);
            shader.TrySetParameter("firstColor", Color.Cyan.ToVector3());
            shader.TrySetParameter("secondaryColor", Color.Cyan.ToVector3());
            shader.TrySetParameter("tertiaryColor", Color.DarkCyan.ToVector3());
            shader.TrySetParameter("flip", !flip);
            trail.DrawTrail(shader, old.Points, 400, true, true);
        }

        Main.spriteBatch.Draw(Tex, Projectile.Center - Main.screenPosition, null, Color.White,
                    Projectile.rotation + RotationOffset, origin, Projectile.scale, Effects, 0f);
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.OverNPCs);
        return false;
    }
}