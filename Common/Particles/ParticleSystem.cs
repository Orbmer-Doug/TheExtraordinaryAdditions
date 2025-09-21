using Microsoft.Xna.Framework.Graphics;
using ReLogic.Threading;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Terraria;
using Terraria.Graphics.Light;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.CrossCode;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Graphics.Specific;
using TheExtraordinaryAdditions.Core.Utilities;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

// simd? where simds????
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Vector4 = Microsoft.Xna.Framework.Vector4;

namespace TheExtraordinaryAdditions.Common.Particles;

#region Definitions

// id maxxing
public enum ParticleTypes : byte
{
    Blood,
    BloodStreak,
    BloomLine,
    BloomPixel,
    Blur,
    BulletCasing,
    CartoonAnger,
    Cloud,
    CrossCodeBoll,
    CrossCodeHit,
    ChromaticAberration,
    Debug,
    DetailedBlast,
    Dust,
    Flash,
    Glow,
    HeavySmoke,
    LargeFire,
    LightningArc,
    Menacing,
    Mist,
    PulseRing,
    Shockwave,
    Smoke,
    Snowflake,
    Spark,
    Sparkle,
    SquishyLight,
    SquishyPixel,
    TechyHolosquare,
    Thunder,
    Twinkle,
}

[StructLayout(LayoutKind.Auto)]
public readonly struct LockOnDetails(Func<Vector2> lockOnCenter, Vector2 lockOnOffset)
{
    public readonly Func<Vector2> LockOnCenter = lockOnCenter;
    public readonly Vector2 LockOnOffset = lockOnOffset;

    public void Apply(ref Vector2 position)
    {
        if (LockOnCenter != null)
            position = LockOnCenter() + LockOnOffset;
    }
}

[Flags]
public enum CollisionTypes
{
    None = 0,
    Solid = 1 << 0,
    NonSolid = 1 << 1,
    Liquid = 1 << 2,
    NPC = 1 << 3,
    Player = 1 << 4,
    Projectile = 1 << 5
}

[StructLayout(LayoutKind.Auto)]
public readonly struct InitialValues(Vector2 velocity, Vector2 position, float opacity, float scale, Color color)
{
    public readonly Vector2 Velocity = velocity;
    public readonly Vector2 Position = position;
    public readonly float Opacity = opacity;
    public readonly float Scale = scale;
    public readonly Color Color = color;
}

// Note: The maximum size a struct should ideally be is anywhere below 1024 bytes (or 1 KB), the moment it goes higher than that performance goes kapoot
[StructLayout(LayoutKind.Auto)]
public unsafe struct ParticleData
{
    private const int MaxOldPositions = 10;
    private const byte CustomDataSize = 255;

    public Vector2 Position;
    public Vector2 Velocity;
    public Vector2 OldVelocity;
    public int Lifetime;
    public int Time;
    public float Scale;
    public float Opacity;
    public float Rotation;
    public Color Color;
    public ParticleTypes Type;
    public int Width;
    public int Height;
    public int Frame;
    public readonly int FrameCount;
    public Rectangle Hitbox;
    public PixelationLayer PixelationLayer => PixelationLayer.Dusts;
    public readonly bool Active => Time < Lifetime;
    public readonly int Direction => (sbyte)(Velocity.X >= 0 ? 1 : -1);

    public float TimeRatio;
    public float LifetimeRatio;

    public InitialValues Init;
    public LockOnDetails? LockOnDetails;

    // Fixed trail data
    private fixed float oldPositions[MaxOldPositions * 2]; // 2 floats per Vector2
    public Span<Vector2> OldPositions => MemoryMarshal.CreateSpan(ref Unsafe.As<float, Vector2>(ref oldPositions[0]), MaxOldPositions);

    public CollisionTypes AllowedCollisions;

    private fixed byte customData[CustomDataSize];
    public Span<byte> CustomData => MemoryMarshal.CreateSpan(ref customData[0], CustomDataSize);

    /// <summary>
    /// Helper to cast CustomData to a specific struct <br></br>
    /// Maximum size of a struct is 255 bytes. <see cref="float"/> is 4 bytes, <see cref="bool"/> is 4 bytes because of padding, etc. <br></br>
    /// Use packing if necessary
    /// </summary>
    /// <typeparam name="T">The struct</typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentException">Cant go above <see cref="CustomDataSize"/> bytes</exception>
    public ref T GetCustomData<T>() where T : unmanaged
    {
        if (sizeof(T) > CustomDataSize)
            throw new ArgumentException($"Type {typeof(T).Name} exceeds CustomData size ({CustomDataSize} bytes) by {sizeof(T)}.");
        return ref MemoryMarshal.AsRef<T>(CustomData);
    }

    private byte blendStateIndex;
    public readonly BlendState EffectiveBlendState => blendStateIndex == 0 ? ParticleRegistry.GetDefinition((byte)Type).BlendState : ParticleSystem.SupportedBlendStates[blendStateIndex - 1];
    public void SetBlendState(BlendState state)
    {
        int index = Array.IndexOf(ParticleSystem.SupportedBlendStates, state);
        blendStateIndex = (byte)(index == -1 ? 0 : index + 1);
    }
}

public enum DrawTypes
{
    /// <summary>
    /// Draw in <see cref="PixelationSystem"/> at a specified <see cref="PixelationLayer"/>
    /// </summary>
    Pixelize = 0,

    /// <summary>
    /// Draw in <see cref="LayeredDrawSystem"/> at a specified <see cref="PixelationLayer"/>
    /// </summary>
    Layered = 1 << 0,

    /// <summary>
    /// Most performant (due to draw system limitations), draws before dust
    /// </summary>
    Manual = 1 << 1,
}

public delegate void UpdateDelegate(ref ParticleData p);
public delegate void DrawDelegate(ref ParticleData p, SpriteBatch sb);
public delegate void OnCollisionDelegate(ref ParticleData p);
public delegate void OnSpawnDelegate(ref ParticleData p);
public delegate void OnKillDelegate(ref ParticleData p);

public readonly record struct ParticleTypeDefinition(
    Texture2D Texture,
    BlendState BlendState,
    UpdateDelegate Update,
    DrawDelegate Draw,
    DrawTypes DrawType,
    bool IsPrimitive,
    bool CanCull,
    OnCollisionDelegate OnCollision = null,
    OnSpawnDelegate OnSpawn = null,
    OnKillDelegate OnKill = null
);

#endregion

#region System
// could probably be like double the speed if i used atlas' but i dont feel like setting all that up
// spatial partioning who?? what quad trees??
[Autoload(Side = ModSide.Client)]
public sealed class ParticleSystem : ModSystem
{
    public static readonly ParticleSystem Instance = ModContent.GetInstance<ParticleSystem>();
    public const int BitsPerMask = sizeof(ulong) * 8;
    private uint maxParticles;
    private ParticleData[] particles;
    private ulong[] presenceMask;
    private int activeCount;

    // Type-specific behavior
    public static readonly ParticleTypeDefinition[] TypeDefinitions = new ParticleTypeDefinition[(int)(GetLastEnumValue<ParticleTypes>() + 1)];
    public ParticleData[] GetParticles() => particles;
    public ulong[] GetPresenceMask() => presenceMask;

    public override void OnModLoad()
    {
        ParticleRegistry.Initialize();

        maxParticles = 32768; // 2^15
        particles = new ParticleData[maxParticles];
        presenceMask = new ulong[Math.Max(1, maxParticles / BitsPerMask)];
        activeCount = 0;

        Main.QueueMainThreadAction(() =>
        {
            On_Main.DrawDust += DrawTarget_Dusts;
        });
    }

    public override void OnModUnload()
    {
        particles = null;
        presenceMask = null;
        activeCount = 0;

        Main.QueueMainThreadAction(() =>
        {
            On_Main.DrawDust -= DrawTarget_Dusts;
        });
    }

    public void Add(ParticleData particle)
    {
        if (activeCount >= maxParticles || Main.gamePaused || Main.dedServ)
            return;

        int index = AllocateIndex();
        particles[index] = particle;
        particles[index].OldPositions.Fill(particle.Position);
        particles[index].Init = new(particle.Velocity, particle.Position, particle.Opacity, particle.Scale, particle.Color);
        particles[index].Time = 0;
        TypeDefinitions[(byte)particle.Type].OnSpawn?.Invoke(ref particles[index]);
        activeCount++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int AllocateIndex()
    {
        for (int maskIndex = 0, baseIndex = 0; maskIndex < presenceMask.Length; maskIndex++, baseIndex += BitsPerMask)
        {
            int bitIndex = BitOperations.TrailingZeroCount(~presenceMask[maskIndex]);
            if (bitIndex != BitsPerMask)
            {
                int index = baseIndex + bitIndex;
                presenceMask[maskIndex] |= 1ul << bitIndex;
                return index;
            }
        }

        // Overwrite random index
        int randomIndex = Main.rand.Next((int)maxParticles);
        presenceMask[randomIndex / BitsPerMask] |= 1ul << randomIndex % BitsPerMask;
        return randomIndex;
    }

    public override void PostUpdateDusts()
    {
        if (Main.gamePaused)
            return;

        Update();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update()
    {
        Vector2 screenCenter = Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight) / 2f;
        const float MaxParticleDistanceSqr = 3000f * 3000f;

        // Using parallel is EXCEPTIONALLY worth it
        // Its the difference between 9 frames or 60 when there is over 10,000 particles on screen
        FastParallel.For(0, presenceMask.Length, (j, k, callback) =>
        {
            for (int maskIndex = j; maskIndex < k; maskIndex++)
            {
                ref ulong maskRef = ref presenceMask[maskIndex];
                ulong maskCopy = maskRef;
                int baseIndex = maskIndex * BitsPerMask;

                while (maskCopy != 0)
                {
                    int bitIndex = BitOperations.TrailingZeroCount(maskCopy);
                    maskCopy &= ~(1ul << bitIndex);
                    int index = baseIndex + bitIndex;
                    ref ParticleData p = ref particles[index];

                    // Update trail history
                    Span<Vector2> oldPos = p.OldPositions;
                    for (int i = oldPos.Length - 1; i >= 1; i--)
                        oldPos[i] = oldPos[i - 1];
                    oldPos[0] = p.Position;

                    // Core update
                    p.Time++;
                    p.TimeRatio = (float)p.Time / p.Lifetime;
                    p.LifetimeRatio = (float)(p.Lifetime - p.Time) / p.Lifetime;

                    // Movement and collision
                    p.OldVelocity = p.Velocity;

                    ParticleTypeDefinition def = TypeDefinitions[(byte)p.Type];
                    if (p.AllowedCollisions != CollisionTypes.None)
                    {
                        bool collide = false;
                        if (p.AllowedCollisions.HasFlag(CollisionTypes.Solid))
                        {
                            p.Velocity = Collision.TileCollision(p.Position, p.Velocity, p.Width, p.Height, p.AllowedCollisions.HasFlag(CollisionTypes.NonSolid));
                            Vector4 slope = Collision.SlopeCollision(p.Position, p.Velocity, p.Width, p.Height, 1f);
                            p.Position.X = slope.X;
                            p.Position.Y = slope.Y;
                            p.Velocity.X = slope.Z;
                            p.Velocity.Y = slope.W;
                        }

                        if (p.AllowedCollisions.HasFlag(CollisionTypes.Liquid))
                        {
                            p.Velocity = Collision.WaterCollision(p.Position, p.Velocity, p.Width, p.Height, p.AllowedCollisions.HasFlag(CollisionTypes.NonSolid), false, true);
                        }

                        if (p.AllowedCollisions.HasFlag(CollisionTypes.NPC))
                        {
                            foreach (NPC npc in Main.ActiveNPCs)
                            {
                                if (npc != null)
                                {
                                    Rectangle temp = p.Hitbox;
                                    p.Velocity = ResolveCollision(ref temp, npc.RotHitbox(), p.Velocity, out collide, 4);
                                }
                            }
                        }

                        if (p.AllowedCollisions.HasFlag(CollisionTypes.Projectile))
                        {
                            foreach (Projectile proj in Main.ActiveProjectiles)
                            {
                                if (proj != null)
                                {
                                    Rectangle temp = p.Hitbox;
                                    p.Velocity = ResolveCollision(ref temp, proj.RotHitbox(), p.Velocity, out collide, 4);
                                }
                            }
                        }

                        if (p.AllowedCollisions.HasFlag(CollisionTypes.Player))
                        {
                            foreach (Player player in Main.ActivePlayers)
                            {
                                if (player != null && !player.dead && !player.ghost)
                                {
                                    Rectangle temp = p.Hitbox;
                                    p.Velocity = ResolveCollision(ref temp, player.RotHitbox(), p.Velocity, out collide, 4);
                                }
                            }
                        }

                        if (p.Velocity != p.OldVelocity || collide)
                            def.OnCollision?.Invoke(ref p);
                        p.Position += p.Velocity;
                    }
                    else
                        p.Position += p.Velocity;

                    def.Update(ref p);
                    p.Hitbox = new Rectangle((int)(p.Position.X - p.Width / 2), (int)(p.Position.Y - p.Height / 2), p.Width / 4, p.Height / 4);

                    if ((Vector2.DistanceSquared(p.Position, screenCenter) >= MaxParticleDistanceSqr && def.CanCull) || !p.Active)
                    {
                        def.OnKill?.Invoke(ref p);
                        Interlocked.And(ref maskRef, ~(1ul << bitIndex));
                        Interlocked.Decrement(ref activeCount);
                    }
                }
            }
        });
    }

    private void DrawTarget_Dusts(On_Main.orig_DrawDust orig, Main self)
    {
        Draw(Main.spriteBatch);
        orig(self);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Action CreateDrawAction(ParticleTypeDefinition particleType, ParticleData particle)
    {
        return () =>
        {
            ParticleData temp = particle;
            particleType.Draw(ref temp, Main.spriteBatch);
        };
    }

    public static readonly BlendState[] SupportedBlendStates =
    [
        BlendState.AlphaBlend,
        BlendState.NonPremultiplied,
        BlendState.Additive,
    ];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Draw(SpriteBatch sb)
    {
        if (activeCount == 0)
            return;

        float maxScreenDim = Math.Max(Main.screenWidth, Main.screenHeight);
        float maxDistSqr = maxScreenDim * maxScreenDim;

        BlendState prev = Main.instance.GraphicsDevice.BlendState;
        foreach (var blendState in SupportedBlendStates)
        {
            sb.Begin(SpriteSortMode.Deferred, blendState, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.ZoomMatrix);
            for (int maskIndex = 0, baseIndex = 0; maskIndex < presenceMask.Length; maskIndex++, baseIndex += BitsPerMask)
            {
                ulong maskCopy = presenceMask[maskIndex];
                while (maskCopy != 0)
                {
                    int bitIndex = BitOperations.TrailingZeroCount(maskCopy);
                    maskCopy &= ~(1ul << bitIndex);
                    int index = baseIndex + bitIndex;
                    ref ParticleData p = ref particles[index];
                    ParticleTypeDefinition def = TypeDefinitions[(byte)p.Type];

                    if (def.CanCull)
                    {
                        if (Vector2.DistanceSquared(p.Position, Main.screenPosition + new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f)) >= maxDistSqr)
                            continue;
                    }

                    if (p.EffectiveBlendState == blendState)
                    {
                        DrawTypes type = def.DrawType;
                        if (type != DrawTypes.Manual)
                        {
                            Action drawAction = CreateDrawAction(def, particles[index]);
                            if (type == DrawTypes.Pixelize)
                            {
                                if (def.IsPrimitive)
                                    PixelationSystem.QueuePrimitiveRenderAction(drawAction, p.PixelationLayer, blendState);
                                else
                                    PixelationSystem.QueueTextureRenderAction(drawAction, p.PixelationLayer, blendState);
                            }
                            else if (type == DrawTypes.Layered)
                                LayeredDrawSystem.QueueDrawAction(drawAction, p.PixelationLayer, blendState);
                        }
                        else
                            TypeDefinitions[(byte)p.Type].Draw(ref p, sb);
                    }
                }
            }
            sb.End();
        }
        Main.instance.GraphicsDevice.BlendState = prev;
    }
}
#endregion

#region Particle Definitions
public readonly struct ParticleDefinition(
    ParticleTypes type,
    Texture2D texture,
    BlendState blendState,
    UpdateDelegate update,
    DrawDelegate draw,
    DrawTypes drawType,
    bool isPrimitive = false,
    bool canCull = true,
    OnCollisionDelegate onCollision = null,
    OnSpawnDelegate onSpawn = null,
    OnKillDelegate onKill = null)
{
    public readonly ParticleTypes Type = type;
    public readonly ParticleTypeDefinition Definition = new(
            texture, blendState, update, draw, drawType, isPrimitive, canCull, onCollision, onSpawn, onKill
        );
}

// Behold. Without abstraction we get 𝓉𝒽𝑒 𝓌𝒶𝓁𝓁 of static wrappers!
// the design is very human
public static class ParticleRegistry
{
    #region Initialization
    public static ParticleTypeDefinition GetDefinition(int type) => ParticleSystem.TypeDefinitions[type];
    public static ParticleTypeDefinition[] TypeDefinitions => ParticleSystem.TypeDefinitions;

    public static List<ParticleDefinition> Definitions { get; } = [];
    public static void RegisterDefinition(in ParticleDefinition definition) => Definitions.Add(definition);

    private static readonly Type[] ParticleDefinitionTypes =
    [
        typeof(BloodParticleDefinition),
        typeof(BloodStreakParticleDefinition),
        typeof(BloomLineParticleDefinition),
        typeof(BloomPixelParticleDefinition),
        typeof(BlurParticleDefinition),
        typeof(BulletCasingParticleDefinition),
        typeof(CartoonAngerParticleDefinition),
        typeof(CloudParticleDefinition),
        typeof(CrossCodeBollDefinition),
        typeof(CrossCodeHitDefinition),
        typeof(ChromaticAberrationDefinition),
        typeof(DetailedBlastParticleDefinition),
        typeof(DebugParticleDefinition),
        typeof(DustParticleDefinition),
        typeof(FlashParticleDefinition),
        typeof(GlowParticleDefinition),
        typeof(HeavySmokeParticleDefinition),
        typeof(LightningArcParticleDefinition),
        typeof(MenacingParticleDefinition),
        typeof(MistParticleDefinition),
        typeof(PulseRingParticleDefinition),
        typeof(ShockwaveParticleDefinition),
        typeof(SmokeParticleDefinition),
        typeof(SnowflakeParticleDefinition),
        typeof(SparkParticleDefinition),
        typeof(SparkleParticleDefinition),
        typeof(SquishyLightParticleDefinition),
        typeof(SquishyPixelParticleDefinition),
        typeof(TechyHolosquareParticleDefinition),
        typeof(ThunderParticleDefinition),
        typeof(TwinkleParticleDefinition)
        // Add CrossCodeBoll, CrossCodeHit, LargeFire if implemented
    ];

    public static void Initialize()
    {
        // we must tickle all the structs because they are lazy
        foreach (Type type in ParticleDefinitionTypes)
        {
            RuntimeHelpers.RunClassConstructor(type.TypeHandle);
        }

        // Load all definitions into the particle system
        foreach (ParticleDefinition definition in Definitions)
        {
            ParticleSystem.TypeDefinitions[(byte)definition.Type] = definition.Definition;
        }
    }

    #endregion

    // All the extra data structs
    #region Definitions
    private struct BloodParticleData
    {

    }

    private struct BloodStreakParticleData
    {

    }

    private struct BloomLineParticleData
    {

    }

    private struct BloomPixelData
    {
        public Color BloomColor;
        public float BloomScale;
        public Vector2? HomeInDestination;
        public bool Gravity;
        public bool Intense;
        public byte TrailLength;
        public float VelMult; // For homing acceleration
    }

    public struct BlurParticleData
    {
        public float Sigma; // Gaussian falloff smoothness (0.5 to 2.0)
    }

    private struct BulletCasingParticleData
    {
        public float RotAmt;
    }

    private struct CartoonAngerData
    {
        public int RandomID;
        public Color StartingColor;
        public Color EndingColor;
    }

    private struct CloudParticleData
    {
        public Color StartingColor;
        public Color EndingColor;
        public bool LightEffected;
        public float OpacityMultiplier;
        public byte TexType;
    }

    public enum CrosscodeHitType
    {
        Small,
        Medium,
        Big
    }

    private struct CrosscodeHitData
    {
        public CrosscodeHitType Type;
        public CrossDiscHoldout.Element Element;
    }

    public enum CrosscodeBollType
    {
        DieWallSmall,
        Die,
        DieWallBig,
        Trail,
    }

    private struct CrosscodeBollData
    {
        public CrosscodeBollType Type;
        public CrossDiscHoldout.Element Element;
    }

    public struct ChromaticAberrationData
    {
        public float Sigma; // Gaussian falloff smoothness (0.5 to 2.0)
    }

    private struct DetailedBlastData
    {
        public Vector2 From;
        public Vector2 To;
        public Color? AuraCol;
        public bool AltTex;
    }

    private struct DustData
    {
        public float Spin;
        public bool Glowing;
        public bool Gravity;
        public bool Wavy;
        public float Timer;
        public float Delay;
    }

    public struct FlashParticleData
    {
        public float Sigma; // Gaussian falloff smoothness (0.5 to 2.0)
    }

    private struct GlowParticleData
    {
        public Vector2? HomeInDestination;
        public bool Gravity;
    }

    public struct HeatHazeParticleData
    {
        public float Sigma; // Gaussian falloff smoothness (0.5 to 2.0)
    }

    private struct HeavySmokeData
    {
        public bool Glowing;
        public float Spin;
    }

    private unsafe struct LightningArcData
    {
        public Vector2 Vel;
        public fixed float PointsX[30];
        public fixed float PointsY[30];
        public bool PointsGenerated;
    }

    private struct LightningArcContext(ParticleData particle)
    {
        public float TimeRatio = particle.TimeRatio;
        public float Scale = particle.Scale;
        public Color Color = particle.Color;
        public float Opacity = particle.Opacity;
    }

    private struct MenacingParticleData
    {
        public float Time;
        public float Delay;
    }

    private struct MistParticleData
    {
        public float Spin;
        public Color Start;
        public Color End;
        public float Alpha;
    }

    private struct PulseRingData
    {
        public Vector2 Squish;
        public Color BaseColor;
        public bool UseAltTexture;
        public float OriginalScale;
        public float FinalScale;
    }

    public struct ShockwaveParticleData
    {
        public float Frequency;
        public float Chromatic;
        public float RingSize;
        public float MaxSize;
    }

    private struct SmokeParticleData
    {
        public float Alpha;
        public float InitAlpha;
        public Color Start;
        public Color End;
    }

    private struct SnowflakeParticleData
    {

    }

    private struct SparkParticleData
    {
        public Vector2? HomeInDestination;
        public bool Gravity;
    }

    private struct SparkleParticleData
    {
        public Color BloomColor;
        public float BloomScale;
        public float Spin;
    }

    private struct SquishyLightParticleData
    {
        public float SquishStrength;
        public float MaxSquish;
    }

    private struct SquishyPixelData
    {
        public Color BloomColor;
        public bool Gravity;
        public float Rot;
        public byte TrailLength;
    }

    private struct TechyHolosquareParticleData
    {
        public Rectangle TechFrame;
        public int Variant;
        public float Strength;
    }

    private struct ThunderParticleData
    {
        public Vector2 Squish;
        public float ShakePower;
    }

    private struct TwinkleParticleData
    {
        public int TotalStarPoints;
        public Color BackglowBloomColor;
        public Vector2 ScaleFactor;
    }
    #endregion

    // All the particles
    #region Registers
    public readonly struct BloodParticleDefinition
    {
        static BloodParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.Blood,
                texture: AssetRegistry.GetTexture(AdditionsTexture.BloodParticle2),
                blendState: BlendState.AlphaBlend,
                update: static (ref ParticleData p) =>
                {
                    p.Scale = MathF.Pow(MathHelper.SmoothStep(1, 0, p.TimeRatio), .2f) * p.Init.Scale;
                    p.Velocity.X *= 0.97f;
                    p.Velocity.Y = MathHelper.Clamp(p.Velocity.Y + 0.8f, -22f, 22f);
                    p.Opacity = -MathF.Pow(p.LifetimeRatio, 6f) + 1f;
                    p.Rotation = p.Velocity.ToRotation() + MathHelper.PiOver2;
                },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
                {
                    Texture2D texture = TypeDefinitions[(byte)ParticleTypes.Blood].Texture;
                    float verticalStretch = Utils.GetLerpValue(0f, 24f, Math.Abs(p.Velocity.Y), clamped: true) * 0.84f;
                    float brightness = MathF.Pow(Lighting.Brightness((int)(p.Position.X / 16f), (int)(p.Position.Y / 16f)), 0.15f);
                    Vector2 scale = new Vector2(1f, verticalStretch + 1f) * p.Scale * 0.1f;
                    sb.DrawBetter(texture, p.Position, null, p.Color * p.Opacity * brightness, p.Rotation, texture.Size() / 2, scale, 0);
                },
                drawType: DrawTypes.Manual,
                onCollision: static (ref ParticleData p) => p.Time = p.Lifetime
            ));
        }
    }

    public readonly struct BloodStreakParticleDefinition
    {
        static BloodStreakParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.BloodStreak,
                texture: AssetRegistry.GetTexture(AdditionsTexture.BloodParticle),
                blendState: BlendState.Additive,
                update: static (ref ParticleData p) =>
            {
                p.Velocity *= 0.95f;
                p.Opacity = MakePoly(4).OutFunction(p.LifetimeRatio);
                p.Rotation = p.Velocity.ToRotation();
            },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
            {
                Texture2D texture = TypeDefinitions[(byte)ParticleTypes.BloodStreak].Texture;
                float brightness = MathF.Pow(Lighting.Brightness(p.Position.ToTileCoordinates().X, p.Position.ToTileCoordinates().Y), 0.15f);
                Rectangle frame = texture.Frame(1, 3, 0, (int)(p.LifetimeRatio * 3f));
                Vector2 origin = frame.Size() * 0.5f;
                sb.DrawBetter(texture, p.Position, frame, p.Color * brightness * p.Opacity, p.Rotation, origin, p.Scale, 0);
            },
                drawType: DrawTypes.Pixelize
                ));
        }
    }

    public readonly struct BloomLineParticleDefinition
    {
        static BloomLineParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.BloomLine,
                texture: AssetRegistry.GetTexture(AdditionsTexture.BloomLineSmall),
                blendState: BlendState.Additive,
                update: static (ref ParticleData p) =>
            {
                p.Scale = p.LifetimeRatio * p.Init.Scale;
                p.Opacity = Circ.OutFunction(p.LifetimeRatio);
                p.Velocity *= 0.95f;
                p.Rotation = p.Velocity.ToRotation() + MathHelper.PiOver2;
            },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
            {
                Texture2D texture = TypeDefinitions[(byte)ParticleTypes.BloomLine].Texture;
                Vector2 scale = new Vector2(0.5f, 1.6f) * p.Scale;
                for (float i = .25f; i < 1f; i += .25f)
                {
                    sb.DrawBetter(texture, p.Position, null, p.Color, p.Rotation, texture.Size() / 2, scale * i, 0);
                    sb.DrawBetter(texture, p.Position, null, p.Color, p.Rotation, texture.Size() / 2, scale * new Vector2(0.45f, 1f) * i, 0);
                }
            },
                drawType: DrawTypes.Pixelize
                ));
        }
    }

    public readonly struct BloomPixelParticleDefinition
    {
        static BloomPixelParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.BloomPixel,
                texture: AssetRegistry.GetTexture(AdditionsTexture.Pixel),
                blendState: BlendState.Additive,
                update: static (ref ParticleData p) =>
            {
                ref BloomPixelData custom = ref p.GetCustomData<BloomPixelData>();

                // Fading behavior
                if (p.LifetimeRatio < 0.4f)
                {
                    p.Opacity *= 0.91f;
                    p.Scale *= 0.96f;
                    p.Velocity *= 0.94f;
                }
                p.Rotation += p.Velocity.X * 0.07f;

                // Homing logic
                if (custom.HomeInDestination.HasValue)
                {
                    Vector2 dest = custom.HomeInDestination.Value;
                    p.Velocity = Vector2.Lerp(p.Velocity, p.Position.SafeDirectionTo(dest) * custom.VelMult, 0.4f);
                    if (custom.VelMult < 26f)
                        custom.VelMult += 0.05f;
                    if (p.Position.WithinRange(dest, 10f))
                        p.Time = p.Lifetime;
                }
                else
                {
                    if (custom.Gravity)
                        p.Velocity.Y = MathHelper.Clamp(p.Velocity.Y + 0.3f, -10f, 18f);
                    else
                        p.Velocity *= 0.95f;
                }
            },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
            {
                ref BloomPixelData custom = ref p.GetCustomData<BloomPixelData>();
                Texture2D pixel = TypeDefinitions[(byte)ParticleTypes.SquishyPixel].Texture;
                Texture2D bloom = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);
                SpriteEffects direction = p.Direction.ToSpriteDirection();
                Vector2 bloomOrigin = bloom.Size() / 2f;
                Vector2 pixelOrigin = pixel.Size() / 2f;

                if (custom.TrailLength > 0)
                {
                    Span<Vector2> oldPos = p.OldPositions;
                    for (int i = 0; i < custom.TrailLength && i < oldPos.Length; i++)
                    {
                        float completion = 1f - InverseLerp(0f, custom.TrailLength, i);
                        if (custom.Intense)
                            sb.DrawBetter(bloom, p.Position, null, p.Color * p.Opacity * 0.55f, p.Rotation, bloomOrigin, custom.BloomScale * 0.15f, 0);
                        sb.DrawBetter(pixel, oldPos[i], null, p.Color * p.Opacity, p.Rotation, pixelOrigin, p.Scale * 6f * completion, direction);
                    }
                }
                else
                {
                    sb.DrawBetter(bloom, p.Position, null, p.Color * p.Opacity * (custom.Intense ? 1.1f : 0.55f), p.Rotation, bloomOrigin, custom.BloomScale * (custom.Intense ? .25f : .15f), 0);
                    sb.DrawBetter(pixel, p.Position, null, p.Color * p.Opacity, p.Rotation, pixelOrigin, p.Scale * 6f, direction);
                }
            },
                drawType: DrawTypes.Pixelize,
                onCollision: static (ref ParticleData p) =>
            {
                ref BloomPixelData custom = ref p.GetCustomData<BloomPixelData>();
                if (custom.Gravity)
                {
                    Vector2 oldVel = p.OldVelocity;
                    if (Math.Abs(p.Velocity.X - oldVel.X) > float.Epsilon)
                        p.Velocity.X = -oldVel.X * 0.9f;
                    if (Math.Abs(p.Velocity.Y - oldVel.Y) > float.Epsilon)
                        p.Velocity.Y = -oldVel.Y * 0.9f;
                }
            }
                ));
        }
    }

    public readonly struct BlurParticleDefinition
    {
        static BlurParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.Blur,
                texture: null,
                blendState: BlendState.AlphaBlend,
                update: static (ref ParticleData p) =>
                {
                    // Update custom data
                    ref BlurParticleData custom = ref p.GetCustomData<BlurParticleData>();

                    p.Opacity = p.Init.Opacity * p.LifetimeRatio; // Fade intensity
                    p.Scale = p.Init.Scale * p.LifetimeRatio; // Fade radius
                },
                draw: static (ref ParticleData p, SpriteBatch sb) => { },
                drawType: DrawTypes.Manual
            ));
        }
    }

    public readonly struct BulletCasingParticleDefinition
    {
        static BulletCasingParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.BulletCasing,
                texture: AssetRegistry.GetTexture(AdditionsTexture.AntiBulletShell),
                blendState: BlendState.AlphaBlend,
                update: static (ref ParticleData p) =>
            {
                ref BulletCasingParticleData custom = ref p.GetCustomData<BulletCasingParticleData>();
                p.Velocity.Y = MathHelper.Clamp(p.Velocity.Y + .2f, -22f, 22f);
                float HeatInterpolant = 1f - InverseLerp(0f, 100f, p.Time);
                Color HeatColor = Color.Lerp(Color.Chocolate * .9f, Color.Chocolate * 2f, HeatInterpolant);

                p.Rotation += p.Velocity.Length() * custom.RotAmt;
                p.Opacity = InverseLerp(p.Lifetime, p.Lifetime - 20f, p.Time);
                Lighting.AddLight(p.Position, HeatColor.ToVector3() * HeatInterpolant);
            },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
            {
                Texture2D texture = TypeDefinitions[(byte)ParticleTypes.BulletCasing].Texture;
                sb.DrawBetter(texture, p.Position, null, Lighting.GetColor(p.Position.ToTileCoordinates()) * p.Opacity, p.Rotation, texture.Size() / 2, p.Scale, 0);

                float HeatInterpolant = 1f - InverseLerp(0f, 100f, p.Time);
                Color HeatColor = Color.Lerp(Color.Chocolate * .9f, Color.Chocolate * 2f, HeatInterpolant);
                if (HeatInterpolant > 0f)
                {
                    const int amt = 20;
                    for (int i = 0; i < amt; i++)
                    {
                        Vector2 offset = (MathHelper.TwoPi * i / amt).ToRotationVector2() * (HeatInterpolant * 3f) - Main.screenPosition;
                        sb.DrawBetter(texture, p.Position + offset, null, HeatColor with { A = 40 } * HeatInterpolant * .95f, p.Rotation, texture.Size() / 2, p.Scale, 0);
                    }
                }
            },
                drawType: DrawTypes.Manual,
                onCollision: static (ref ParticleData p) =>
            {
                if (Math.Abs(p.Velocity.X - p.OldVelocity.X) > float.Epsilon)
                    p.Velocity.X = -p.OldVelocity.X * .5f;
                if (Math.Abs(p.Velocity.Y - p.OldVelocity.Y) > float.Epsilon)
                    p.Velocity.Y = -p.OldVelocity.Y * .5f;

                p.Velocity.X *= .85f;
            },
                onSpawn: static (ref ParticleData p) =>
            {
                ref BulletCasingParticleData custom = ref p.GetCustomData<BulletCasingParticleData>();
                custom.RotAmt = Main.rand.NextFloat(.01f, .03f);
            }
                ));
        }
    }

    public readonly struct CartoonAngerParticleDefinition
    {
        static CartoonAngerParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.CartoonAnger,
                texture: AssetRegistry.GetTexture(AdditionsTexture.CartoonAngerParticle),
                blendState: BlendState.Additive,
                update: static (ref ParticleData p) =>
            {
                ref CartoonAngerData custom = ref p.GetCustomData<CartoonAngerData>();
                float scaleFactor = MathHelper.Lerp(0.7f, 1.3f, Sin01(MathHelper.TwoPi * p.Time / 27f + custom.RandomID));
                p.Scale = Utils.Remap(p.Time, 0f, 30f, 0.01f, p.Init.Scale * scaleFactor);
                p.Color = Color.Lerp(custom.StartingColor, custom.EndingColor, p.TimeRatio);
                p.Opacity = MakePoly(3).OutFunction(p.LifetimeRatio);
            },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
            {
                Texture2D texture = TypeDefinitions[(byte)ParticleTypes.CartoonAnger].Texture;
                sb.DrawBetter(texture, p.Position, null, p.Color * p.Opacity, p.Rotation, texture.Size() / 2f, p.Scale, 0);
            },
                drawType: DrawTypes.Manual
                ));
        }
    }

    public readonly struct CloudParticleDefinition
    {
        static CloudParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.Cloud,
                texture: AssetRegistry.GetTexture(AdditionsTexture.NebulaGas1),
                blendState: BlendState.Additive,
                update: static (ref ParticleData p) =>
            {
                ref CloudParticleData custom = ref p.GetCustomData<CloudParticleData>();
                p.Velocity *= 0.987f;
                p.Color = Color.Lerp(custom.StartingColor, custom.EndingColor, p.LifetimeRatio);
                p.Rotation += (Math.Abs(p.Velocity.X) + Math.Abs(p.Velocity.Y)) * .007f * p.Velocity.X.NonZeroSign();
                p.Scale += 0.009f;
                p.Opacity = GetLerpBump(0f, .1f, 1f, .7f, p.TimeRatio) * custom.OpacityMultiplier;
            },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
            {
                ref CloudParticleData custom = ref p.GetCustomData<CloudParticleData>();
                Texture2D texture = custom.TexType switch
                {
                    2 => AssetRegistry.GetTexture(AdditionsTexture.NebulaGas3),
                    1 => AssetRegistry.GetTexture(AdditionsTexture.NebulaGas2),
                    _ => AssetRegistry.GetTexture(AdditionsTexture.NebulaGas1)
                };

                float brightness = MathF.Pow(Lighting.Brightness((int)(p.Position.X / 16f), (int)(p.Position.Y / 16f)), 0.15f) * 0.9f;
                Color col = p.Color * p.Opacity * (custom.LightEffected ? brightness : 1f);
                sb.DrawBetterRect(texture, ToTarget(p.Position, new(p.Scale * 2f)), null, col, p.Rotation, texture.Size() / 2f, 0);
            },
                drawType: DrawTypes.Pixelize
                ));
        }
    }

    public readonly struct CrossCodeBollDefinition
    {
        static CrossCodeBollDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.CrossCodeBoll,
                texture: AssetRegistry.GetTexture(AdditionsTexture.CrossCodeBoll),
                blendState: BlendState.AlphaBlend,
                update: static (ref ParticleData p) =>
                {
                    ref CrosscodeBollData data = ref p.GetCustomData<CrosscodeBollData>();
                    int maxFrames = 0;

                    switch (data.Element)
                    {
                        case CrossDiscHoldout.Element.Neutral:
                            switch (data.Type)
                            {
                                case CrosscodeBollType.DieWallSmall:
                                    maxFrames = 4;
                                    break;
                                case CrosscodeBollType.Die:
                                    maxFrames = 4;
                                    break;
                                case CrosscodeBollType.DieWallBig:
                                    maxFrames = 4;
                                    break;
                            }
                            break;
                        case CrossDiscHoldout.Element.Cold:
                            switch (data.Type)
                            {
                                case CrosscodeBollType.DieWallSmall:
                                    maxFrames = 5;
                                    break;
                                case CrosscodeBollType.Die:
                                    maxFrames = 5;
                                    break;
                                case CrosscodeBollType.DieWallBig:
                                    maxFrames = 7;
                                    break;
                                case CrosscodeBollType.Trail:
                                    maxFrames = 5;
                                    break;
                            }
                            break;
                        case CrossDiscHoldout.Element.Heat:
                            switch (data.Type)
                            {
                                case CrosscodeBollType.DieWallSmall:
                                    maxFrames = 5;
                                    break;
                                case CrosscodeBollType.Die:
                                    maxFrames = 4;
                                    break;
                                case CrosscodeBollType.DieWallBig:
                                    maxFrames = 5;
                                    break;
                                case CrosscodeBollType.Trail:
                                    maxFrames = 6;
                                    break;
                            }
                            break;
                        case CrossDiscHoldout.Element.Shock:
                            switch (data.Type)
                            {
                                case CrosscodeBollType.DieWallSmall:
                                    maxFrames = 5;
                                    break;
                                case CrosscodeBollType.Die:
                                    maxFrames = 4;
                                    break;
                                case CrosscodeBollType.DieWallBig:
                                    maxFrames = 5;
                                    break;
                                case CrosscodeBollType.Trail:
                                    maxFrames = 4;
                                    break;
                            }
                            break;
                        case CrossDiscHoldout.Element.Wave:
                            switch (data.Type)
                            {
                                case CrosscodeBollType.DieWallSmall:
                                    maxFrames = 5;
                                    break;
                                case CrosscodeBollType.Die:
                                    maxFrames = 4;
                                    break;
                                case CrosscodeBollType.DieWallBig:
                                    maxFrames = 6;
                                    break;
                                case CrosscodeBollType.Trail:
                                    maxFrames = 4;
                                    break;
                            }
                            break;
                    }

                    if (p.Time % 4 == 3)
                        p.Frame++;
                    if (p.Frame >= maxFrames)
                        p.Time = p.Lifetime;
                },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
                {
                    ref CrosscodeBollData data = ref p.GetCustomData<CrosscodeBollData>();
                    Texture2D texture = TypeDefinitions[(byte)ParticleTypes.CrossCodeBoll].Texture;

                    int x = 48 * p.Frame;
                    int y = 0;
                    switch (data.Element)
                    {
                        case CrossDiscHoldout.Element.Neutral:
                            switch (data.Type)
                            {
                                case CrosscodeBollType.DieWallSmall:
                                    break;
                                case CrosscodeBollType.Die:
                                    y = 48;
                                    break;
                                case CrosscodeBollType.DieWallBig:
                                    y = 48 * 2;
                                    break;
                            }
                            break;
                        case CrossDiscHoldout.Element.Cold:
                            switch (data.Type)
                            {
                                case CrosscodeBollType.DieWallSmall:
                                    y = 48 * 7;
                                    break;
                                case CrosscodeBollType.Die:
                                    y = 48 * 8;
                                    break;
                                case CrosscodeBollType.DieWallBig:
                                    y = 48 * 9;
                                    break;
                                case CrosscodeBollType.Trail:
                                    y = 48 * 10;
                                    break;
                            }
                            break;
                        case CrossDiscHoldout.Element.Heat:
                            switch (data.Type)
                            {
                                case CrosscodeBollType.DieWallSmall:
                                    y = 48 * 3;
                                    break;
                                case CrosscodeBollType.Die:
                                    y = 48 * 4;
                                    break;
                                case CrosscodeBollType.DieWallBig:
                                    y = 48 * 5;
                                    break;
                                case CrosscodeBollType.Trail:
                                    y = 48 * 6;
                                    break;
                            }
                            break;
                        case CrossDiscHoldout.Element.Shock:
                            switch (data.Type)
                            {
                                case CrosscodeBollType.DieWallSmall:
                                    y = 48 * 11;
                                    break;
                                case CrosscodeBollType.Die:
                                    y = 48 * 12;
                                    break;
                                case CrosscodeBollType.DieWallBig:
                                    y = 48 * 13;
                                    break;
                                case CrosscodeBollType.Trail:
                                    y = 48 * 14;
                                    break;
                            }
                            break;
                        case CrossDiscHoldout.Element.Wave:
                            switch (data.Type)
                            {
                                case CrosscodeBollType.DieWallSmall:
                                    y = 48 * 15;
                                    break;
                                case CrosscodeBollType.Die:
                                    y = 48 * 16;
                                    break;
                                case CrosscodeBollType.DieWallBig:
                                    y = 48 * 17;
                                    break;
                                case CrosscodeBollType.Trail:
                                    y = 48 * 18;
                                    break;
                            }
                            break;
                    }

                    sb.DrawBetter(texture, p.Position, new Rectangle(x, y, 48, 48), p.Color, p.Rotation, new(24), 1f);
                },
                drawType: DrawTypes.Manual
                ));
        }
    }

    public readonly struct CrossCodeHitDefinition
    {
        static CrossCodeHitDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.CrossCodeHit,
                texture: AssetRegistry.GetTexture(AdditionsTexture.CrossCodeHit),
                blendState: BlendState.AlphaBlend,
                update: static (ref ParticleData p) =>
                {
                    ref CrosscodeHitData data = ref p.GetCustomData<CrosscodeHitData>();
                    int maxFrames = 0;

                    switch (data.Element)
                    {
                        case CrossDiscHoldout.Element.Neutral:
                            switch (data.Type)
                            {
                                case CrosscodeHitType.Small:
                                    maxFrames = 6;
                                    break;
                                case CrosscodeHitType.Medium:
                                    maxFrames = 4;
                                    break;
                                case CrosscodeHitType.Big:
                                    maxFrames = 5;
                                    break;
                            }
                            break;
                        case CrossDiscHoldout.Element.Cold:
                            switch (data.Type)
                            {
                                case CrosscodeHitType.Small:
                                    maxFrames = 4;
                                    break;
                                case CrosscodeHitType.Medium:
                                    maxFrames = 4;
                                    break;
                                case CrosscodeHitType.Big:
                                    maxFrames = 7;
                                    break;
                            }
                            break;
                        case CrossDiscHoldout.Element.Heat:
                            switch (data.Type)
                            {
                                case CrosscodeHitType.Small:
                                    maxFrames = 4;
                                    break;
                                case CrosscodeHitType.Medium:
                                    maxFrames = 4;
                                    break;
                                case CrosscodeHitType.Big:
                                    maxFrames = 7;
                                    break;
                            }
                            break;
                        case CrossDiscHoldout.Element.Shock:
                            switch (data.Type)
                            {
                                case CrosscodeHitType.Small:
                                    maxFrames = 4;
                                    break;
                                case CrosscodeHitType.Medium:
                                    maxFrames = 4;
                                    break;
                                case CrosscodeHitType.Big:
                                    maxFrames = 6;
                                    break;
                            }
                            break;
                        case CrossDiscHoldout.Element.Wave:
                            switch (data.Type)
                            {
                                case CrosscodeHitType.Small:
                                    maxFrames = 4;
                                    break;
                                case CrosscodeHitType.Medium:
                                    maxFrames = 4;
                                    break;
                                case CrosscodeHitType.Big:
                                    maxFrames = 7;
                                    break;
                            }
                            break;
                    }

                    if (p.Time % 4 == 3)
                        p.Frame++;
                    if (p.Frame >= maxFrames)
                        p.Time = p.Lifetime;
                },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
                {
                    ref CrosscodeHitData data = ref p.GetCustomData<CrosscodeHitData>();
                    Texture2D texture = TypeDefinitions[(byte)ParticleTypes.CrossCodeHit].Texture;

                    int x = 128 * p.Frame;
                    int y = 0;

                    switch (data.Element)
                    {
                        case CrossDiscHoldout.Element.Neutral:
                            switch (data.Type)
                            {
                                case CrosscodeHitType.Small:
                                    break;
                                case CrosscodeHitType.Medium:
                                    y = 128;
                                    break;
                                case CrosscodeHitType.Big:
                                    y = 128 * 2;
                                    break;
                            }
                            break;
                        case CrossDiscHoldout.Element.Cold:
                            switch (data.Type)
                            {
                                case CrosscodeHitType.Small:
                                    y = 128 * 9;
                                    break;
                                case CrosscodeHitType.Medium:
                                    y = 128 * 10;
                                    break;
                                case CrosscodeHitType.Big:
                                    y = 128 * 11;
                                    break;
                            }
                            break;
                        case CrossDiscHoldout.Element.Heat:
                            switch (data.Type)
                            {
                                case CrosscodeHitType.Small:
                                    y = 128 * 3;
                                    break;
                                case CrosscodeHitType.Medium:
                                    y = 128 * 4;
                                    break;
                                case CrosscodeHitType.Big:
                                    y = 128 * 5;
                                    break;
                            }
                            break;
                        case CrossDiscHoldout.Element.Shock:
                            switch (data.Type)
                            {
                                case CrosscodeHitType.Small:
                                    y = 128 * 6;
                                    break;
                                case CrosscodeHitType.Medium:
                                    y = 128 * 7;
                                    break;
                                case CrosscodeHitType.Big:
                                    y = 128 * 8;
                                    break;
                            }
                            break;
                        case CrossDiscHoldout.Element.Wave:
                            switch (data.Type)
                            {
                                case CrosscodeHitType.Small:
                                    y = 128 * 12;
                                    break;
                                case CrosscodeHitType.Medium:
                                    y = 128 * 13;
                                    break;
                                case CrosscodeHitType.Big:
                                    y = 128 * 14;
                                    break;
                            }
                            break;
                    }

                    sb.DrawBetter(texture, p.Position, new Rectangle(x, y, 128, 128), p.Color, p.Rotation, new(64), 1f);
                },
                drawType: DrawTypes.Manual
                ));
        }
    }

    public readonly struct ChromaticAberrationDefinition
    {
        static ChromaticAberrationDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.ChromaticAberration,
                texture: null,
                blendState: BlendState.AlphaBlend,
                update: static (ref ParticleData p) =>
                {
                    // Update custom data
                    ref ChromaticAberrationData custom = ref p.GetCustomData<ChromaticAberrationData>();

                    p.Opacity = p.Init.Opacity * p.LifetimeRatio; // Fade intensity
                    p.Scale = p.Init.Scale * p.LifetimeRatio; // Fade radius
                },
                draw: static (ref ParticleData p, SpriteBatch sb) => { },
                drawType: DrawTypes.Manual
            ));
        }
    }

    public readonly struct DetailedBlastParticleDefinition
    {
        static DetailedBlastParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.DetailedBlast,
                texture: AssetRegistry.GetTexture(AdditionsTexture.DetailedBlast),
                blendState: BlendState.Additive,
                update: static (ref ParticleData p) =>
            {
                ref DetailedBlastData custom = ref p.GetCustomData<DetailedBlastData>();
                float progress = Circ.OutFunction(p.TimeRatio);
                Vector2 scale = Vector2.Lerp(custom.From, custom.To, progress);
                p.Scale = scale.Length();
                p.Opacity = GetLerpBump(0f, 0.1f, 1f, 0.5f, p.TimeRatio);

                float hitboxTiles = p.Scale / 16f;
                float desiredRadius = hitboxTiles;
                float intensity = CalculateIntensityForRadius(desiredRadius, LightMaskMode.None, 0.5f);
                Vector3 lightColor = p.Color.ToVector3() * intensity * p.Opacity;
                Lighting.AddLight(p.Position, lightColor);

                p.Velocity *= 0.95f;
            },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
            {
                ref DetailedBlastData custom = ref p.GetCustomData<DetailedBlastData>();
                Texture2D tex = custom.AltTex ? AssetRegistry.GetTexture(AdditionsTexture.DetailedBlast2) : TypeDefinitions[(byte)ParticleTypes.DetailedBlast].Texture;
                Vector2 scale = Vector2.Lerp(custom.From, custom.To, Circ.OutFunction(p.TimeRatio));
                Rectangle target = ToTarget(p.Position, scale);
                if (custom.AuraCol.HasValue)
                {
                    Texture2D aura = AssetRegistry.GetTexture(AdditionsTexture.HollowCircleHighRes);
                    Vector2 orig = aura.Size() / 2;
                    sb.DrawBetterRect(aura, target, null, custom.AuraCol.Value * p.Opacity, p.Rotation, orig, 0);
                    sb.DrawBetterRect(aura, target, null, custom.AuraCol.Value * p.Opacity, p.Rotation, orig, 0);
                }
                sb.DrawBetterRect(tex, target, null, p.Color * p.Opacity, p.Rotation, tex.Size() / 2f, 0);
            },
                drawType: DrawTypes.Pixelize,
                canCull: false
                ));
        }
    }

    public readonly struct DebugParticleDefinition
    {
        static DebugParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.Debug,
                texture: AssetRegistry.GetTexture(AdditionsTexture.Pixel),
                blendState: BlendState.AlphaBlend,
                update: static (ref ParticleData p) =>
            {
                p.Rotation = p.Velocity.ToRotation();
            },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
            {
                Texture2D texture = TypeDefinitions[(byte)ParticleTypes.Debug].Texture;
                sb.DrawBetterRect(texture, ToTarget(p.Position, p.Scale, p.Scale), null, p.Color, p.Rotation, texture.Size() / 2);
            },
                drawType: DrawTypes.Manual
                ));
        }
    }

    public readonly struct DustParticleDefinition
    {
        static DustParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.Dust,
                texture: AssetRegistry.GetTexture(AdditionsTexture.DustParticle),
                blendState: BlendState.AlphaBlend,
                update: static (ref ParticleData p) =>
            {
                ref DustData custom = ref p.GetCustomData<DustData>();

                if (custom.Glowing)
                    Lighting.AddLight(p.Position, p.Color.ToVector3() * .5f * p.Opacity);

                if (custom.Gravity && p.Time > 20f)
                    p.Velocity.Y = MathHelper.Clamp(p.Velocity.Y + .2f, -40f, 22f);

                if (custom.Wavy)
                    p.Velocity = p.Init.Velocity.VelEqualTrig(MathF.Cos, 24f, .5f, ref custom.Delay, ref custom.Timer);

                p.Velocity *= 0.98f;
                p.Rotation += custom.Spin * p.Velocity.X.NonZeroSign();

                p.Scale = MakePoly(3).OutFunction(p.LifetimeRatio) * p.Init.Scale;
                p.Opacity = GetLerpBump(0f, .1f, 1f, .6f, p.TimeRatio);
            },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
            {
                ref DustData custom = ref p.GetCustomData<DustData>();
                Texture2D texture = TypeDefinitions[(byte)ParticleTypes.Dust].Texture;
                Rectangle frame = new(12 * p.Frame, 0, 12, 10);
                Vector2 orig = frame.Size() / 2f;
                sb.DrawBetter(texture, p.Position, frame, p.Color * p.Opacity, p.Rotation, orig, p.Scale);
                if (custom.Glowing)
                {
                    Texture2D glow = AssetRegistry.GetTexture(AdditionsTexture.GlowHarsh);
                    sb.DrawBetterRect(glow, ToTarget(p.Position, p.Scale * texture.Width, p.Scale * texture.Height), null, p.Color * p.Opacity * .5f, p.Rotation, glow.Size() / 2);
                }
            },
                drawType: DrawTypes.Manual
                ));
        }
    }

    public readonly struct FlashParticleDefinition
    {
        static FlashParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.Flash,
                texture: null,
                blendState: BlendState.AlphaBlend,
                update: static (ref ParticleData p) =>
                {
                    // Update custom data
                    ref FlashParticleData custom = ref p.GetCustomData<FlashParticleData>();

                    p.Opacity = p.Init.Opacity * p.LifetimeRatio; // Fade intensity
                },
                draw: static (ref ParticleData p, SpriteBatch sb) => { },
                drawType: DrawTypes.Manual
            ));
        }
    }

    public readonly struct GlowParticleDefinition
    {
        static GlowParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.Glow,
                texture: AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall),
                blendState: BlendState.Additive,
                update: static (ref ParticleData p) =>
            {
                ref GlowParticleData custom = ref p.GetCustomData<GlowParticleData>();
                if (custom.HomeInDestination == null)
                {
                    if (custom.Gravity && p.Velocity.Length() < 12f)
                        p.Velocity.Y = MathHelper.Clamp(p.Velocity.Y + 0.2f, -22f, 22f);
                    p.Velocity *= 0.96f;
                }
                else
                {
                    p.Velocity = Vector2.Lerp(p.Velocity, Vector2.Normalize(custom.HomeInDestination.Value - p.Position), 0.3f);
                    if (Vector2.DistanceSquared(p.Position, custom.HomeInDestination.Value) < 10f * 10f)
                        p.Time = p.Lifetime;
                }
                p.Opacity = MathF.Pow(p.LifetimeRatio, 2) * p.Init.Opacity;
                p.Scale = p.LifetimeRatio * p.Init.Scale;

                float hitboxTiles = p.Scale / 16f;
                float desiredRadius = hitboxTiles / 2f;
                float intensity = CalculateIntensityForRadius(desiredRadius, LightMaskMode.None, 0.5f);
                Vector3 lightColor = p.Color.ToVector3() * intensity * p.Opacity;
                Lighting.AddLight(p.Position, lightColor);

                //Lighting.AddLight(p.Position, p.Color.ToVector3() * p.Scale / 16 * 0.6f * p.Opacity);
            },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
            {
                Texture2D texture = TypeDefinitions[(byte)ParticleTypes.Glow].Texture;
                Vector2 orig = texture.Size() / 2f;
                sb.DrawBetterRect(texture, ToTarget(p.Position, p.Scale, p.Scale), null, p.Color * p.Opacity * .3f, p.Rotation, orig);
                sb.DrawBetterRect(texture, ToTarget(p.Position, p.Scale, p.Scale), null, p.Color.Lerp(Color.White, .25f) * p.Opacity * .6f, p.Rotation, orig);
                sb.DrawBetterRect(texture, ToTarget(p.Position, p.Scale, p.Scale), null, p.Color.Lerp(Color.White, .4f) * p.Opacity, p.Rotation, orig);
            },
                drawType: DrawTypes.Manual
                ));
        }
    }

    public readonly struct HeavySmokeParticleDefinition
    {
        static HeavySmokeParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.HeavySmoke,
                texture: AssetRegistry.GetTexture(AdditionsTexture.HeavySmoke),
                blendState: BlendState.NonPremultiplied,
                update: static (ref ParticleData p) =>
            {
                ref HeavySmokeData custom = ref p.GetCustomData<HeavySmokeData>();

                if (custom.Glowing)
                    Lighting.AddLight(p.Position, p.Color.ToVector3() * .5f * p.Opacity);

                p.Opacity = MathF.Sin(MathHelper.PiOver2 + p.TimeRatio * MathHelper.PiOver2);
                p.Scale = p.LifetimeRatio * p.Init.Scale;

                p.Rotation += custom.Spin * p.Velocity.X.NonZeroSign();
                p.Velocity *= 0.98f;
            },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
            {
                Texture2D texture = TypeDefinitions[(byte)ParticleTypes.HeavySmoke].Texture;
                int timeFrame = (int)Math.Floor(p.Time / (p.Lifetime / 6f));
                Rectangle frame = new(p.Frame * 80, timeFrame * 80, 80, 80);
                SpriteEffects visualDirection = p.Direction.ToSpriteDirection();
                sb.DrawBetter(texture, p.Position, frame, p.Color * p.Opacity, p.Rotation, frame.Size() * 0.5f, p.Scale, visualDirection);
            },
                drawType: DrawTypes.Pixelize
                ));
        }
    }

    public unsafe readonly struct LightningArcParticleDefinition
    {
        static LightningArcParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.LightningArc,
                texture: AssetRegistry.InvisTex,
                blendState: BlendState.AlphaBlend,
                update: static (ref ParticleData p) =>
            {
                ref LightningArcData custom = ref p.GetCustomData<LightningArcData>();
                if (!custom.PointsGenerated)
                {
                    GenerateArcPoints(ref p, ref custom, initial: true);
                    custom.PointsGenerated = true;
                }
                else
                {
                    UpdateArcPoints(ref p, ref custom);
                }
            },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
            {
                ref LightningArcData custom = ref p.GetCustomData<LightningArcData>();
                if (!custom.PointsGenerated)
                    return;

                Vector2[] points = new Vector2[30];
                for (int i = 0; i < 30; i++)
                {
                    points[i] = new Vector2(custom.PointsX[i], custom.PointsY[i]);
                }

                ManagedShader shader = ShaderRegistry.LightningArcShader;
                shader.TrySetParameter("lifetimeRatio", p.TimeRatio);
                shader.TrySetParameter("erasureThreshold", 0.75f);
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.WavyNeurons), 1, SamplerState.LinearWrap);

                LightningArcContext context = new(p);
                OptimizedPrimitiveTrail trail = new(
                    widthFunction: completionRatio => ArcWidthFunction(context, completionRatio),
                    colorFunction: (factor, pos) => ArcColorFunction(context, factor),
                    offsetFunction: null,
                    maxTrailPoints: 30
                );
                trail.DrawTrail(shader, points, 30);
            },
                drawType: DrawTypes.Pixelize,
                isPrimitive: true
                ));
        }
    }

    #region Arc Things
    private static float ArcWidthFunction(LightningArcContext context, float completionRatio)
    {
        float lifetimeSquish = GetLerpBump(0.1f, 0.35f, 1f, 0.75f, context.TimeRatio);
        return MathHelper.Lerp(1f, 3f, Convert01To010(completionRatio)) * lifetimeSquish * context.Scale;
    }
    private static Color ArcColorFunction(LightningArcContext context, SystemVector2 factor)
    {
        return Color.Lerp(Color.White, context.Color, factor.X) * context.Opacity;
    }
    private unsafe static void GenerateArcPoints(ref ParticleData p, ref LightningArcData custom, bool initial)
    {
        Vector2 start = p.Position;
        Vector2 lengthForPerpendicular = custom.Vel.ClampLength(0f, 740f);
        Vector2 end = start + custom.Vel * Main.rand.NextFloat(0.67f, 1.2f) + Main.rand.NextVector2Circular(30f, 30f);
        Vector2 farFront = start - lengthForPerpendicular.RotatedByRandom(3.1f) * Main.rand.NextFloat(0.26f, 0.8f);
        Vector2 farEnd = end + lengthForPerpendicular.RotatedByRandom(3.1f) * 4f;

        for (int i = 0; i < 30; i++)
        {
            Vector2 point = Vector2.CatmullRom(farFront, start, end, farEnd, i / 29f);
            if (initial && Main.rand.NextBool(9))
                point += Main.rand.NextVector2Circular(10f, 10f);
            custom.PointsX[i] = point.X;
            custom.PointsY[i] = point.Y;
        }
    }
    private unsafe static void UpdateArcPoints(ref ParticleData p, ref LightningArcData custom)
    {
        for (int i = 0; i < 30; i += 2)
        {
            float trailCompletionRatio = i / 29f;
            float arcProtrudeAngleOffset = Main.rand.NextGaussian(0.63f) + MathHelper.PiOver2;
            float arcProtrudeDistance = Main.rand.NextGaussian(4.6f);
            if (Main.rand.NextBool(100))
                arcProtrudeDistance *= 3f;

            Vector2 arcOffset = custom.Vel.SafeNormalize(Vector2.Zero).RotatedBy(arcProtrudeAngleOffset) * arcProtrudeDistance;
            custom.PointsX[i] += arcOffset.X;
            custom.PointsY[i] += arcOffset.Y;
        }
    }
    #endregion

    public readonly struct MenacingParticleDefinition
    {
        static MenacingParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.Menacing,
                texture: AssetRegistry.GetTexture(AdditionsTexture.Menacing),
                blendState: BlendState.Additive,
                update: static (ref ParticleData p) =>
            {
                ref MenacingParticleData custom = ref p.GetCustomData<MenacingParticleData>();
                if (p.Time <= 10f)
                    p.Opacity = MathHelper.Clamp(p.Opacity + 0.1f, 0f, 1f);
                else if (p.Time >= p.Lifetime - 10f)
                    p.Opacity = MathHelper.Clamp(p.Opacity - 0.1f, 0f, 1f);

                float scaleSine = (1f + MathF.Sin(p.Time * 0.25f)) / 2f;
                p.Velocity = p.Init.Velocity.VelEqualTrig(MathF.Sin, 30f, .4f, ref custom.Delay, ref custom.Time);
                p.Scale = MathHelper.Lerp(p.Init.Scale * 0.85f, p.Init.Scale, scaleSine);
            },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
            {
                Texture2D texture = TypeDefinitions[(byte)ParticleTypes.Menacing].Texture;
                sb.DrawBetter(texture, p.Position, null, p.Color * p.Opacity, p.Rotation, texture.Size() / 2, p.Scale, 0);
            },
                drawType: DrawTypes.Manual
                ));
        }
    }

    public readonly struct MistParticleDefinition
    {
        static MistParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.Mist,
                texture: AssetRegistry.GetTexture(AdditionsTexture.MistParticle),
                blendState: BlendState.AlphaBlend,
                update: static (ref ParticleData p) =>
            {
                ref MistParticleData custom = ref p.GetCustomData<MistParticleData>();
                p.Rotation += custom.Spin * (p.Velocity.X > 0).ToDirectionInt();
                p.Velocity *= 0.9f;

                if (custom.Alpha > 90f)
                {
                    Lighting.AddLight(p.Position, p.Color.ToVector3() * 0.1f);
                    p.Scale += 0.01f;
                    custom.Alpha -= 3f;
                }
                else
                {
                    p.Scale *= 0.975f;
                    custom.Alpha -= 2f;
                }
                if (custom.Alpha < 0f)
                    p.Time = p.Lifetime;

                p.Color = Color.Lerp(custom.Start, custom.End, MathHelper.Clamp((255f - custom.Alpha - 100f) / 80f, 0f, 1f)) * (custom.Alpha / byte.MaxValue);
            },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
            {
                Texture2D texture = TypeDefinitions[(byte)ParticleTypes.Mist].Texture;
                Rectangle frame = texture.Frame(1, 3, 0, p.Frame);
                sb.DrawBetter(texture, p.Position, frame, p.Color with { A = 0 }, p.Rotation, frame.Size() * .5f, p.Scale, p.Direction.ToSpriteDirection());
            },
                drawType: DrawTypes.Pixelize
                ));
        }
    }

    public readonly struct PulseRingParticleDefinition
    {
        static PulseRingParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.PulseRing,
                texture: AssetRegistry.GetTexture(AdditionsTexture.HollowCircleHighRes),
                blendState: BlendState.Additive,
                update: static (ref ParticleData p) =>
            {
                ref PulseRingData custom = ref p.GetCustomData<PulseRingData>();
                p.Scale = MakePoly(4).OutFunction.Evaluate(custom.OriginalScale, custom.FinalScale, p.TimeRatio);
                p.Opacity = (float)Math.Sin(MathHelper.PiOver2 + p.TimeRatio * MathHelper.PiOver2);
                p.Color = custom.BaseColor * p.Opacity;
                Lighting.AddLight(p.Position, p.Color.ToVector3() * InverseLerp(0f, custom.FinalScale, p.Scale));
                p.Velocity *= 0.95f;
            },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
            {
                ref PulseRingData custom = ref p.GetCustomData<PulseRingData>();
                Texture2D tex = custom.UseAltTexture ? AssetRegistry.GetTexture(AdditionsTexture.HollowCircleFancy) : TypeDefinitions[(byte)ParticleTypes.PulseRing].Texture;
                sb.DrawBetterRect(tex, ToTarget(p.Position, p.Scale * custom.Squish), null, p.Color, p.Rotation, tex.Size() / 2f);
            },
                drawType: DrawTypes.Pixelize
                ));
        }
    }

    public readonly struct ShockwaveParticleDefinition
    {
        static ShockwaveParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.Shockwave,
                texture: AssetRegistry.InvisTex,
                blendState: BlendState.AlphaBlend,
                update: static (ref ParticleData p) =>
            {
                ref ShockwaveParticleData custom = ref p.GetCustomData<ShockwaveParticleData>();
                p.Opacity = Convert01To010(p.TimeRatio);
                p.Scale = MakePoly(2.2f).InOutFunction.Evaluate(0f, custom.MaxSize, p.TimeRatio);
            },
                draw: static (ref ParticleData p, SpriteBatch sb) => { },
                drawType: DrawTypes.Manual
                ));
        }
    }

    public readonly struct SmokeParticleDefinition
    {
        static SmokeParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.Smoke,
                texture: AssetRegistry.GetTexture(AdditionsTexture.Invisible),
                blendState: BlendState.Additive,
                update: static (ref ParticleData p) => { /* No update logic */ },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
            {
                Texture2D texture = TypeDefinitions[(byte)ParticleTypes.Smoke].Texture;
                sb.DrawBetter(texture, p.Position, null, p.Color, p.Rotation, texture.Size() / 2, p.Scale, 0);
            },
                drawType: DrawTypes.Manual
                ));
        }
    }

    public readonly struct SnowflakeParticleDefinition
    {
        static SnowflakeParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.Snowflake,
                texture: AssetRegistry.GetTexture(AdditionsTexture.Snowflake),
                blendState: BlendState.AlphaBlend,
                update: static (ref ParticleData p) =>
            {
                p.Velocity.X *= 0.96f;
                p.Velocity.Y = MathHelper.Clamp(p.Velocity.Y + 0.12f, -12f, 7f);
                p.Rotation += p.Velocity.Length() * 0.02f * p.Velocity.X.NonZeroSign();
                p.Scale = p.Opacity = GetLerpBump(0f, 0.2f, 1f, 0.9f, p.LifetimeRatio);
            },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
            {
                Rectangle frame = new(0, 26 * p.Frame, 26, 26);
                sb.DrawBetter(TypeDefinitions[(byte)ParticleTypes.Snowflake].Texture, p.Position, frame, Color.White * p.Opacity, p.Rotation, frame.Size() / 2, p.Scale, 0);
            },
                drawType: DrawTypes.Manual
                ));
        }
    }

    public readonly struct SparkParticleDefinition
    {
        static SparkParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.Spark,
                texture: AssetRegistry.GetTexture(AdditionsTexture.Gleam),
                blendState: BlendState.Additive,
                update: static (ref ParticleData p) =>
            {
                ref SparkParticleData custom = ref p.GetCustomData<SparkParticleData>();
                if (custom.HomeInDestination.HasValue)
                {
                    Vector2 dest = custom.HomeInDestination.Value;
                    float currentDirection = p.Velocity.ToRotation();
                    float idealDirection = (dest - p.Position).ToRotation();
                    p.Velocity = currentDirection.AngleLerp(idealDirection, 0.03f).ToRotationVector2() * p.Velocity.Length();
                    p.Velocity += (dest - p.Position) * 0.005f;
                    if (p.Position.WithinRange(dest, 10f))
                        p.Time = p.Lifetime;
                }
                else
                {
                    p.Velocity *= 0.94f;
                    if (p.Velocity.Length() < 12f && custom.Gravity)
                    {
                        p.Velocity.X *= 0.94f;
                        p.Velocity.Y += 0.25f;
                    }
                }
                p.Scale = p.LifetimeRatio * p.Init.Scale;
                p.Opacity = MathHelper.SmoothStep(1, 0, p.TimeRatio) * p.Init.Opacity;
                p.Rotation = p.Velocity.ToRotation() + MathHelper.PiOver2;
            },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
            {
                Texture2D texture = TypeDefinitions[(byte)ParticleTypes.Spark].Texture;
                sb.DrawBetter(texture, p.Position, null, p.Color * p.Opacity * 0.15f, p.Rotation, texture.Size() / 2, new Vector2(0.5f, 1.4f) * p.Scale * 2f, 0);
                sb.DrawBetter(texture, p.Position, null, Color.Lerp(Color.White, p.Color, 0.2f) * p.Opacity * 0.5f, p.Rotation, texture.Size() / 2, new Vector2(0.4f, 1.2f) * p.Scale * 1.5f, 0);
                sb.DrawBetter(texture, p.Position, null, Color.Lerp(Color.White, p.Color, 0.4f) * p.Opacity, p.Rotation, texture.Size() / 2, new Vector2(0.3f, 1f) * p.Scale, 0);
            },
                drawType: DrawTypes.Pixelize
                ));
        }
    }

    public readonly struct SparkleParticleDefinition
    {
        static SparkleParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.Sparkle,
                texture: AssetRegistry.GetTexture(AdditionsTexture.CritSpark),
                blendState: BlendState.Additive,
                update: static (ref ParticleData p) =>
            {
                ref SparkleParticleData custom = ref p.GetCustomData<SparkleParticleData>();
                p.Opacity = MathF.Pow(MathHelper.SmoothStep(1, 0, p.TimeRatio), 0.3f);
                p.Velocity *= 0.89f;
                p.Rotation += custom.Spin * p.Velocity.X.NonZeroSign() * (p.TimeRatio > 0.5f ? 1f : 0.5f);
                p.Scale = -MathF.Pow(p.TimeRatio, 7) + 1f * p.Init.Scale;
                Lighting.AddLight(p.Position, custom.BloomColor.ToVector3() * p.Opacity);
            },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
            {
                ref SparkleParticleData custom = ref p.GetCustomData<SparkleParticleData>();
                Texture2D sparkTexture = TypeDefinitions[(byte)ParticleTypes.Sparkle].Texture;
                Texture2D bloomTexture = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);
                float properBloomSize = sparkTexture.Height / (float)bloomTexture.Height + 0.05f;
                sb.DrawBetter(bloomTexture, p.Position, null, custom.BloomColor * p.Opacity * 0.5f, 0f, bloomTexture.Size() / 2f, p.Scale * custom.BloomScale * properBloomSize, 0);
                sb.DrawBetter(sparkTexture, p.Position, null, p.Color * p.Opacity, p.Rotation, sparkTexture.Size() / 2f, p.Scale, 0);
            },
                drawType: DrawTypes.Pixelize
                ));
        }
    }

    public readonly struct SquishyLightParticleDefinition
    {
        static SquishyLightParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.SquishyLight,
                texture: AssetRegistry.GetTexture(AdditionsTexture.Light),
                blendState: BlendState.Additive,
                update: static (ref ParticleData p) =>
            {
                p.Velocity *= p.LifetimeRatio >= 0.34f ? 0.93f : 1.02f;
                p.Opacity = p.LifetimeRatio > 0.5f ? Convert01To010(p.LifetimeRatio) * 0.2f + 0.8f : Convert01To010(p.LifetimeRatio);
                p.Scale = MakePoly(4).OutFunction(p.LifetimeRatio * p.Init.Scale) * 0.5f;
            },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
            {
                Texture2D texture = TypeDefinitions[(byte)ParticleTypes.SquishyLight].Texture;
                ref SquishyLightParticleData custom = ref p.GetCustomData<SquishyLightParticleData>();
                float squish = MathHelper.Clamp(p.Velocity.Length() / 10f * custom.SquishStrength, 1f, custom.MaxSquish);
                float rotation = p.Velocity.ToRotation() + MathHelper.PiOver2;
                Vector2 scale = new Vector2(p.Scale - p.Scale * squish * 0.3f, p.Scale * squish) * 0.6f;
                float properBloomSize = texture.Height / (float)AssetRegistry.GetTexture(AdditionsTexture.GlowSoft).Height;
                sb.DrawBetter(AssetRegistry.GetTexture(AdditionsTexture.GlowSoft), p.Position, null, p.Color * p.Opacity * 0.8f, rotation, AssetRegistry.GetTexture(AdditionsTexture.GlowSoft).Size() / 2f, scale * 2f * properBloomSize, 0);
                sb.DrawBetter(texture, p.Position, null, p.Color * p.Opacity * 0.8f, rotation, texture.Size() / 2, scale * 1.1f, 0);
                sb.DrawBetter(texture, p.Position, null, Color.White * p.Opacity * 0.9f, rotation, texture.Size() / 2, scale, 0);
            },
                drawType: DrawTypes.Pixelize
                ));
        }
    }

    public readonly struct SquishyPixelParticleDefinition
    {
        static SquishyPixelParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.SquishyPixel,
                texture: AssetRegistry.GetTexture(AdditionsTexture.Pixel),
                blendState: BlendState.Additive,
                update: static (ref ParticleData p) =>
            {
                ref SquishyPixelData custom = ref p.GetCustomData<SquishyPixelData>();
                p.Rotation += p.Velocity.X * 0.07f;
                p.Scale = (1f - MakePoly(5).OutFunction(p.TimeRatio)) * p.Init.Scale;
                p.Opacity = MathF.Pow(p.LifetimeRatio, 2.5f);
                p.Rotation = p.Velocity.ToRotation() + MathHelper.PiOver2;
                if (custom.Gravity && p.Time > 10f)
                    p.Velocity.Y = MathHelper.Clamp(p.Velocity.Y + 0.3f, -30f, 28f);
                p.Velocity *= 0.96f;
                p.Velocity = p.Velocity.RotatedBy(custom.Rot);
            },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
            {
                Texture2D texture = TypeDefinitions[(byte)ParticleTypes.SquishyPixel].Texture;
                ref SquishyPixelData custom = ref p.GetCustomData<SquishyPixelData>();
                Texture2D bloom = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);
                Vector2 orig = texture.Size() / 2;
                float squish = MathHelper.Clamp(p.Velocity.Length() / 5f, 1f, 2f);
                Vector2 scale = new Vector2(p.Scale - p.Scale * squish * 0.3f, p.Scale * squish) * 0.6f;
                if (custom.TrailLength > 0)
                {
                    Span<Vector2> oldPos = p.OldPositions;
                    for (int i = 0; i < custom.TrailLength && i < oldPos.Length; i++)
                    {
                        Vector2 old = oldPos[i];
                        float completion = 1f - InverseLerp(0f, custom.TrailLength, i);
                        sb.DrawBetter(bloom, old, null, p.Color * p.Opacity * completion, p.Rotation, bloom.Size() / 2, scale * 0.14f * completion, 0);
                        sb.DrawBetter(texture, old, null, p.Color * p.Opacity, p.Rotation, orig, scale * 7 * completion, 0);
                    }
                }
                else
                {
                    sb.DrawBetter(bloom, p.Position, null, p.Color * p.Opacity, p.Rotation, bloom.Size() / 2, scale * 0.14f, 0);
                    sb.DrawBetter(texture, p.Position, null, p.Color * p.Opacity, p.Rotation, orig, scale * 7, 0);
                }
            },
                drawType: DrawTypes.Pixelize
                ));
        }
    }

    public readonly struct TechyHolosquareParticleDefinition
    {
        static TechyHolosquareParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.TechyHolosquare,
                texture: AssetRegistry.GetTexture(AdditionsTexture.TechyHolosquare),
                blendState: BlendState.Additive,
                update: static (ref ParticleData p) =>
            {
                ref TechyHolosquareParticleData custom = ref p.GetCustomData<TechyHolosquareParticleData>();
                p.Opacity = (float)Math.Pow(p.LifetimeRatio, 0.5) * custom.Strength;
                Lighting.AddLight(p.Position, p.Color.ToVector3() * p.Opacity);
                p.Rotation = p.Velocity.ToRotation();
                p.Velocity *= 0.875f;
                p.Scale *= 0.96f;
            },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
            {
                ref TechyHolosquareParticleData custom = ref p.GetCustomData<TechyHolosquareParticleData>();
                Texture2D texture = TypeDefinitions[(byte)ParticleTypes.TechyHolosquare].Texture;

                for (int i = -1; i <= 1; i++)
                {
                    Color aberrationColor = Color.White;
                    switch (i)
                    {
                        case -1:
                            aberrationColor = new Color(255, 0, 0, 0);
                            break;
                        case 0:
                            aberrationColor = new Color(0, 255, 0, 0);
                            break;
                        case 1:
                            aberrationColor = new Color(0, 0, 255, 0);
                            break;
                    }
                    Vector2 offset = Utils.RotatedBy(PolarVector(1f, p.Rotation), MathHelper.PiOver2, default) * i;
                    offset *= custom.Strength;
                    sb.DrawBetter(texture, p.Position + offset, custom.TechFrame, p.Color.MultiplyRGB(aberrationColor) * p.Opacity, p.Rotation, custom.TechFrame.Size() / 2f, p.Scale, 0);
                }
                sb.DrawBetter(texture, p.Position, custom.TechFrame, p.Color * p.Opacity, p.Rotation, custom.TechFrame.Size() / 2f, p.Scale, 0);
            },
                drawType: DrawTypes.Pixelize
                ));
        }
    }

    public readonly struct ThunderParticleDefinition
    {
        static ThunderParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.Thunder,
                texture: AssetRegistry.GetTexture(AdditionsTexture.ThunderBolt),
                blendState: BlendState.Additive,
                update: static (ref ParticleData p) =>
            {
                ref ThunderParticleData custom = ref p.GetCustomData<ThunderParticleData>();
                p.LockOnDetails?.Apply(ref p.Position);
                Lighting.AddLight(p.Position, p.Color.ToVector3() * 3f);
                float fade = MakePoly(3f).InFunction(p.LifetimeRatio);
                p.Opacity = fade * p.Init.Opacity;
                custom.Squish.X = fade;
            },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
            {
                Texture2D texture = TypeDefinitions[(byte)ParticleTypes.Thunder].Texture;
                ref ThunderParticleData custom = ref p.GetCustomData<ThunderParticleData>();
                Vector2 shake = Vector2.One.RotatedByRandom(MathHelper.TwoPi) * p.LifetimeRatio * custom.ShakePower;
                Vector2 origin = new(texture.Width / 2f, texture.Height);
                Color drawColor = Color.Lerp(Color.White, p.Color, p.Time / (float)p.Lifetime);
                SpriteEffects flip = Main.GlobalTimeWrappedHourly % 30f < 15f ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                sb.DrawBetter(texture, p.Position + shake, null, p.Color * p.Opacity * 0.6f, p.Rotation, origin, custom.Squish * p.Scale, flip);
                sb.DrawBetter(texture, p.Position, null, drawColor * p.Opacity, p.Rotation, origin, custom.Squish * p.Scale, flip);
            },
                drawType: DrawTypes.Pixelize
                ));
        }
    }

    public readonly struct TwinkleParticleDefinition
    {
        static TwinkleParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.Twinkle,
                texture: AssetRegistry.GetTexture(AdditionsTexture.StarLong),
                blendState: BlendState.Additive,
                update: static (ref ParticleData p) =>
            {
                ref TwinkleParticleData custom = ref p.GetCustomData<TwinkleParticleData>();
                p.Opacity = GetLerpBump(0f, 10f, p.Lifetime, 16f, p.Time);
                p.LockOnDetails?.Apply(ref p.Position);
                p.Velocity *= 0.94f;
            },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
            {
                Texture2D texture = TypeDefinitions[(byte)ParticleTypes.Twinkle].Texture;
                ref TwinkleParticleData custom = ref p.GetCustomData<TwinkleParticleData>();
                Vector2 scale = custom.ScaleFactor * p.Opacity * 0.1f;
                scale *= MathF.Sin(Main.GlobalTimeWrappedHourly * 30f + p.Time * 0.08f) * 0.125f + 1f;
                int instanceCount = custom.TotalStarPoints / 2;
                Color backglowBloomColor = custom.BackglowBloomColor * p.Opacity;
                Color color = p.Color * p.Opacity;
                float spokesExtendOffset = 0.6f;

                sb.DrawBetter(AssetRegistry.GetTexture(AdditionsTexture.GlowSoft), p.Position, null, backglowBloomColor * 0.83f, 0f, AssetRegistry.GetTexture(AdditionsTexture.GlowSoft).Size() * 0.5f, scale * 7.2f, 0);
                sb.DrawBetter(AssetRegistry.GetTexture(AdditionsTexture.BloomFlare), p.Position, null, color, p.Rotation - Main.GlobalTimeWrappedHourly * 0.9f, AssetRegistry.GetTexture(AdditionsTexture.BloomFlare).Size() * 0.5f, scale * 0.42f, 0);
                sb.DrawBetter(AssetRegistry.GetTexture(AdditionsTexture.BloomFlare), p.Position, null, color, p.Rotation + Main.GlobalTimeWrappedHourly * 0.91f, AssetRegistry.GetTexture(AdditionsTexture.BloomFlare).Size() * 0.5f, scale * 0.42f, 0);

                for (int i = 0; i < instanceCount; i++)
                {
                    float rotationOffset = MathHelper.Pi * i / instanceCount;
                    Vector2 localScale = scale;
                    if (rotationOffset != 0f)
                        localScale *= MathF.Pow(MathF.Sin(rotationOffset), 1.5f);
                    for (float s = 1f; s > 0.3f; s -= 0.2f)
                        sb.DrawBetter(texture, p.Position, null, color, rotationOffset, texture.Size() * 0.5f, new Vector2(1f - (spokesExtendOffset - 0.6f) * 0.4f, spokesExtendOffset) * localScale * s, 0);
                }
            },
                drawType: DrawTypes.Pixelize
                ));
        }
    }
    #endregion

    // All the particle spawn methods
    #region Spawners
    public static void SafeSpawn(in ParticleData data)
    {
        if (Main.dedServ) // Because it can still try to sneak through
            return;
        ModContent.GetInstance<ParticleSystem>().Add(data);
    }

    public static void SpawnBloodParticle(Vector2 position, Vector2 velocity, int lifetime, float scale, Color color)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Scale = scale,
            Color = color,
            Type = ParticleTypes.Blood,
            AllowedCollisions = CollisionTypes.Solid | CollisionTypes.Liquid | CollisionTypes.Player | CollisionTypes.NPC,
        };
        SafeSpawn(particle);
    }

    public static void SpawnBloodStreakParticle(Vector2 position, Vector2 velocity, int lifetime, float scale, Color color)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Scale = scale,
            Color = color,
            Opacity = 1f,
            Type = ParticleTypes.BloodStreak,
            Width = 2,
            Height = 2
        };
        SafeSpawn(particle);
    }

    public static void SpawnBloomLineParticle(Vector2 position, Vector2 velocity, int lifetime, float scale, Color color)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Scale = scale,
            Color = color,
            Type = ParticleTypes.BloomLine,
        };
        SafeSpawn(particle);
    }

    public static void SpawnBloomPixelParticle(Vector2 position, Vector2 velocity, int lifetime, float scale,
Color color, Color bloomColor, Vector2? homeInDestination = null, float bloomScale = 1f,
byte trailLength = 0, bool gravity = false, bool intense = false)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Scale = scale,
            Color = color,
            Opacity = 1f,
            Type = ParticleTypes.BloomPixel,
            Width = 2,
            Height = 2,
            AllowedCollisions = gravity ? CollisionTypes.Solid | CollisionTypes.Player : CollisionTypes.None
        };
        ref BloomPixelData custom = ref particle.GetCustomData<BloomPixelData>();
        custom.BloomColor = bloomColor;
        custom.BloomScale = bloomScale;
        custom.HomeInDestination = homeInDestination;
        custom.Gravity = gravity;
        custom.Intense = intense;
        custom.TrailLength = Math.Min(trailLength, (byte)10); // Cap at OldPositions length
        custom.VelMult = velocity.Length();

        SafeSpawn(particle);
    }

    public static void SpawnBlurParticle(Vector2 position, int lifetime, float intensity, float radius, float sigma = .5f)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = Vector2.Zero,
            Lifetime = lifetime,
            Scale = radius,
            Color = Color.Transparent,
            Opacity = intensity,
            Type = ParticleTypes.Blur,
            Width = 2,
            Height = 2
        };
        ref BlurParticleData custom = ref particle.GetCustomData<BlurParticleData>();
        custom.Sigma = sigma;

        SafeSpawn(particle);
    }

    public static void SpawnBulletCasingParticle(Vector2 position, Vector2 velocity, float scale)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = SecondsToFrames(5),
            Scale = scale,
            Color = Color.White,
            Width = 20,
            Height = 20,
            Opacity = 1f,
            AllowedCollisions = CollisionTypes.Solid,
            Type = ParticleTypes.BulletCasing,
        };
        SafeSpawn(particle);
    }

    public static void SpawnCartoonAngerParticle(Vector2 position, int lifetime, float scale, float rotation, Color startingColor, Color endingColor)
    {
        ParticleData particle = new()
        {
            Position = position,
            Lifetime = lifetime,
            Scale = scale,
            Rotation = rotation,
            Color = startingColor,
            Opacity = 1f,
            Type = ParticleTypes.CartoonAnger,
            Width = 2,
            Height = 2
        };
        ref CartoonAngerData custom = ref particle.GetCustomData<CartoonAngerData>();
        custom.RandomID = Main.rand.Next(1000);
        custom.StartingColor = startingColor;
        custom.EndingColor = endingColor;
        SafeSpawn(particle);
    }

    /// <param name="scale">In pixels</param>
    public static void SpawnCloudParticle(Vector2 position, Vector2 velocity, Color startColor, Color endColor, int lifetime, float scale, float opacityEffectiveness, byte texture = 0, bool lightBrightness = false)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Scale = scale,
            Color = startColor,
            Opacity = 1f,
            Rotation = RandomRotation(),
            Type = ParticleTypes.Cloud,
            Width = 2,
            Height = 2
        };
        ref CloudParticleData custom = ref particle.GetCustomData<CloudParticleData>();
        custom.StartingColor = startColor;
        custom.EndingColor = endColor;
        custom.LightEffected = lightBrightness;
        custom.OpacityMultiplier = opacityEffectiveness;
        custom.TexType = texture;
        SafeSpawn(particle);
    }

    public static void SpawnCrossCodeBoll(Vector2 position, float rotation, CrosscodeBollType type, CrossDiscHoldout.Element element)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = Vector2.Zero,
            Rotation = rotation,
            Lifetime = 400,
            Scale = 1f,
            Color = Color.White,
            Type = ParticleTypes.CrossCodeBoll,
        };
        ref CrosscodeBollData data = ref particle.GetCustomData<CrosscodeBollData>();
        data.Element = element;
        data.Type = type;

        SafeSpawn(particle);
    }

    public static void SpawnCrossCodeHit(Vector2 position, CrosscodeHitType type, CrossDiscHoldout.Element element)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = Vector2.Zero,
            Rotation = RandomRotation(),
            Lifetime = 400,
            Scale = 1f,
            Color = Color.White,
            Type = ParticleTypes.CrossCodeHit,
        };
        ref CrosscodeHitData data = ref particle.GetCustomData<CrosscodeHitData>();
        data.Element = element;
        data.Type = type;

        SafeSpawn(particle);
    }

    public static void SpawnChromaticAberration(Vector2 position, int lifetime, float intensity, float radius, float sigma = .5f)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = Vector2.Zero,
            Lifetime = lifetime,
            Scale = radius,
            Color = Color.Transparent,
            Opacity = intensity,
            Type = ParticleTypes.ChromaticAberration,
            Width = 2,
            Height = 2
        };
        ref ChromaticAberrationData custom = ref particle.GetCustomData<ChromaticAberrationData>();
        custom.Sigma = sigma;

        SafeSpawn(particle);
    }

    public static void SpawnDetailedBlastParticle(Vector2 position, Vector2 fromSize, Vector2 toSize, Vector2 velocity, int life, Color color, float? rotation = null, Color? col = null, bool altTex = false)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = life,
            Scale = fromSize.Length(),
            Color = color,
            Opacity = 1f,
            Rotation = rotation ?? RandomRotation(),
            Type = ParticleTypes.DetailedBlast,
            Width = 2,
            Height = 2
        };
        ref DetailedBlastData custom = ref particle.GetCustomData<DetailedBlastData>();
        custom.From = fromSize;
        custom.To = toSize;
        custom.AuraCol = col;
        custom.AltTex = altTex;
        SafeSpawn(particle);
    }

    public static void SpawnDebugParticle(Vector2 pos, Color? color = null, int scale = 5, int life = 10, Vector2? velocity = null)
    {
        ParticleData particle = new()
        {
            Position = pos,
            Velocity = velocity ?? Vector2.Zero,
            Lifetime = life,
            Scale = scale,
            Color = color ?? Color.Red,
            Opacity = 1f,
            Type = ParticleTypes.Debug,
            Width = 2,
            Height = 2
        };
        SafeSpawn(particle);
    }
    public static void SpawnDebugParticle(Point pos, Color? color = null, int scale = 5, int life = 10, Vector2? velocity = null)
    {
        ParticleData particle = new()
        {
            Position = pos.ToVector2(),
            Velocity = velocity ?? Vector2.Zero,
            Lifetime = life,
            Scale = scale,
            Color = color ?? Color.Red,
            Opacity = 1f,
            Type = ParticleTypes.Debug,
            Width = 2,
            Height = 2
        };
        SafeSpawn(particle);
    }

    public static void SpawnDustParticle(Vector2 position, Vector2 velocity, int lifetime, float scale, Color color, float rotationspeed = .1f, bool fall = false, bool glowing = false, bool wavy = false, bool collide = true)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Scale = scale,
            Color = color,
            Type = ParticleTypes.Dust,
            Rotation = RandomRotation(),
            Frame = Main.rand.Next(6),
            Width = 2,
            Height = 2,
        };
        if (collide)
            particle.AllowedCollisions = CollisionTypes.Solid | CollisionTypes.Liquid;

        ref DustData custom = ref particle.GetCustomData<DustData>();
        custom.Spin = rotationspeed;
        custom.Glowing = glowing;
        custom.Gravity = fall;
        custom.Wavy = wavy;

        if (glowing)
            particle.SetBlendState(BlendState.Additive);

        SafeSpawn(particle);
    }

    public static void SpawnFlash(Vector2 position, int lifetime, float intensity, float radius, float sigma = .5f)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = Vector2.Zero,
            Lifetime = lifetime,
            Scale = radius,
            Color = Color.Transparent,
            Opacity = intensity,
            Type = ParticleTypes.Flash,
            Width = 2,
            Height = 2
        };
        ref FlashParticleData custom = ref particle.GetCustomData<FlashParticleData>();
        custom.Sigma = sigma;

        SafeSpawn(particle);
    }

    /// <param name="scale">In pixels</param>
    public static void SpawnGlowParticle(Vector2 position, Vector2 velocity, int lifetime, float scale, Color color, float opacity = 1f, bool gravity = false, Vector2? homeInDest = null, bool collide = false)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Scale = scale,
            Color = color,
            Opacity = opacity,
            Type = ParticleTypes.Glow,
            Width = 2,
            Height = 2,
            AllowedCollisions = collide ? CollisionTypes.NPC | CollisionTypes.Solid : CollisionTypes.None,
        };
        ref GlowParticleData custom = ref particle.GetCustomData<GlowParticleData>();
        custom.Gravity = gravity;
        custom.HomeInDestination = homeInDest;
        SafeSpawn(particle);
    }

    public static void SpawnHeavySmokeParticle(Vector2 position, Vector2 velocity, int lifetime, float scale, Color color, float opacity = 1f, bool glowing = true, float spinSpeed = 0.05f)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Scale = scale,
            Color = color,
            Opacity = opacity,
            Type = ParticleTypes.HeavySmoke,
            Rotation = RandomRotation(),
            Frame = Main.rand.Next(7),
        };
        ref HeavySmokeData custom = ref particle.GetCustomData<HeavySmokeData>();
        custom.Spin = spinSpeed;
        custom.Glowing = glowing;
        if (glowing)
            particle.SetBlendState(BlendState.Additive);
        SafeSpawn(particle);
    }

    public static void SpawnLightningArcParticle(Vector2 pos, Vector2 dist, int life, float scale, Color color)
    {
        ParticleData particle = new()
        {
            Position = pos,
            Velocity = Vector2.Zero,
            Lifetime = life,
            Scale = scale,
            Color = color,
            Opacity = 1f,
            Type = ParticleTypes.LightningArc,
            Width = 2,
            Height = 2
        };
        ref LightningArcData custom = ref particle.GetCustomData<LightningArcData>();
        custom.Vel = dist;
        custom.PointsGenerated = false;
        SafeSpawn(particle);
    }

    public static void SpawnMenacingParticle(Vector2 position, Vector2 velocity, int lifetime, float scale, Color color)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Scale = scale,
            Color = color,
            Type = ParticleTypes.Menacing,
        };
        SafeSpawn(particle);
    }

    public static void SpawnMistParticle(Vector2 position, Vector2 velocity, float scale, Color start, Color end, float alpha, float rotSpeed = 0f)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Scale = scale,
            Lifetime = SecondsToFrames(2),
            Frame = Main.rand.Next(3),
            Type = ParticleTypes.Mist,
        };
        ref MistParticleData custom = ref particle.GetCustomData<MistParticleData>();
        custom.Start = start;
        custom.End = end;
        custom.Alpha = alpha;
        custom.Spin = rotSpeed;
        SafeSpawn(particle);
    }

    public static void SpawnPulseRingParticle(Vector2 position, Vector2 velocity, int lifetime, float rot, Vector2 squish, float startScale, float endScale, Color color, bool altTex = false)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Scale = startScale,
            Color = color,
            Opacity = 1f,
            Rotation = rot,
            Type = ParticleTypes.PulseRing,
            Width = 2,
            Height = 2
        };
        ref PulseRingData custom = ref particle.GetCustomData<PulseRingData>();
        custom.Squish = squish;
        custom.BaseColor = color;
        custom.UseAltTexture = altTex;
        custom.OriginalScale = startScale;
        custom.FinalScale = endScale;
        SafeSpawn(particle);
    }

    public static void SpawnShockwaveParticle(Vector2 pos, int life, float frequency, float radius, float ringSize, float aberration = 0.2f,  Vector2? velocity = null)
    {
        ParticleData particle = new()
        {
            Position = pos,
            Velocity = velocity ?? Vector2.Zero,
            Lifetime = life,
            Scale = 0f,
            Type = ParticleTypes.Shockwave,
            Width = 2,
            Height = 2
        };
        ref ShockwaveParticleData custom = ref particle.GetCustomData<ShockwaveParticleData>();
        custom.Frequency = frequency;
        custom.Chromatic = aberration;
        custom.RingSize = ringSize / Main.ScreenSize.X;
        custom.MaxSize = radius;

        SafeSpawn(particle);
    }

    public static void SpawnSmokeParticle(Vector2 pos, Vector2 vel, float scale, Color start, Color end, float alpha)
    {
        ParticleData particle = new()
        {
            Position = pos,
            Velocity = vel,
            Lifetime = SecondsToFrames(2),
            Scale = scale * 0.2f,
            Color = start,
            Opacity = 1f,
            Type = ParticleTypes.Smoke,
            Width = 2,
            Height = 2
        };
        ref SmokeParticleData custom = ref particle.GetCustomData<SmokeParticleData>();
        custom.Alpha = custom.InitAlpha = alpha;
        custom.Start = start;
        custom.End = end;
        SafeSpawn(particle);
    }

    public static void SpawnSnowflakeParticle(Vector2 pos, Vector2 vel, int life, float scale)
    {
        ParticleData particle = new()
        {
            Position = pos,
            Velocity = vel,
            Lifetime = life,
            Scale = scale / 2,
            Color = Color.White,
            Opacity = 1f,
            Type = ParticleTypes.Snowflake,
            Width = 2,
            Height = 2,
            Frame = Main.rand.Next(4),
            AllowedCollisions = CollisionTypes.Solid | CollisionTypes.Liquid
        };
        SafeSpawn(particle);
    }

    public static void SpawnSparkParticle(Vector2 position, Vector2 velocity, int lifetime, float scale, Color color, bool gravity = false, bool collide = false, Vector2? homeInDest = null)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Scale = scale,
            Color = color,
            Opacity = 1f,
            Type = ParticleTypes.Spark,
            Width = 2,
            Height = 2,
            AllowedCollisions = collide ? CollisionTypes.Solid : CollisionTypes.None
        };
        ref SparkParticleData custom = ref particle.GetCustomData<SparkParticleData>();
        custom.HomeInDestination = homeInDest;
        custom.Gravity = gravity;
        SafeSpawn(particle);
    }

    public static void SpawnSparkleParticle(Vector2 position, Vector2 velocity, int lifetime, float scale, Color color, Color bloomColor, float bloomScale = 1f, float spin = 0.1f)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Scale = scale,
            Color = color,
            Opacity = 1f,
            Rotation = RandomRotation(),
            Type = ParticleTypes.Sparkle,
            Width = 2,
            Height = 2
        };
        ref SparkleParticleData custom = ref particle.GetCustomData<SparkleParticleData>();
        custom.BloomColor = bloomColor;
        custom.BloomScale = bloomScale;
        custom.Spin = spin;
        SafeSpawn(particle);
    }

    public static void SpawnSquishyLightParticle(Vector2 position, Vector2 velocity, int lifetime, float scale, Color color, float opacity = 1f, float squishPower = 1f, float maxSquish = 3f)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Scale = scale,
            Color = color,
            Opacity = opacity,
            Type = ParticleTypes.SquishyLight,
            Width = 2,
            Height = 2
        };
        ref SquishyLightParticleData custom = ref particle.GetCustomData<SquishyLightParticleData>();
        custom.SquishStrength = squishPower;
        custom.MaxSquish = maxSquish;
        SafeSpawn(particle);
    }

    public static void SpawnSquishyPixelParticle(Vector2 pos, Vector2 vel, int life, float scale, Color col, Color bloomCol, byte trailLength = 0, bool collide = false, bool fall = false, float velRot = 0f)
    {
        ParticleData particle = new()
        {
            Position = pos,
            Velocity = vel,
            Lifetime = life,
            Scale = scale,
            Color = col,
            Opacity = 1f,
            Type = ParticleTypes.SquishyPixel,
            Width = 2,
            Height = 2,
            AllowedCollisions = collide ? CollisionTypes.Solid : CollisionTypes.None
        };
        ref SquishyPixelData custom = ref particle.GetCustomData<SquishyPixelData>();
        custom.BloomColor = bloomCol;
        custom.Gravity = fall;
        custom.Rot = velRot;
        custom.TrailLength = Math.Min(trailLength, (byte)10);
        SafeSpawn(particle);
    }

    public static void SpawnTechyHolosquareParticle(Vector2 position, Vector2 velocity, int lifetime, float scale, Color color, float opacity = 1f, float strength = 1.4f)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Scale = scale,
            Color = color,
            Opacity = opacity,
            Rotation = Main.rand.NextFloat(MathHelper.TwoPi),
            Type = ParticleTypes.TechyHolosquare,
            Width = 2,
            Height = 2
        };
        ref TechyHolosquareParticleData custom = ref particle.GetCustomData<TechyHolosquareParticleData>();
        custom.Variant = Main.rand.Next(6);
        custom.TechFrame = custom.Variant switch
        {
            0 => new Rectangle(8, 0, 6, 6),
            1 => new Rectangle(6, 8, 10, 6),
            2 => new Rectangle(4, 16, 14, 8),
            3 => new Rectangle(2, 26, 18, 10),
            4 => new Rectangle(2, 38, 18, 8),
            5 => new Rectangle(6, 48, 12, 12),
            _ => new Rectangle(0, 0, 0, 0)
        };
        custom.Strength = strength;
        SafeSpawn(particle);
    }

    public static void SpawnThunderParticle(Vector2 position, int lifetime, float scale, Vector2 squish, float rotation, Color color, float opacity = 1f, float shakePower = 20f, LockOnDetails? lockOn = null)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = Vector2.Zero,
            Lifetime = lifetime,
            Scale = scale,
            Color = color,
            Opacity = opacity,
            Rotation = rotation,
            Type = ParticleTypes.Thunder,
            Width = 2,
            Height = 2,
            LockOnDetails = lockOn,
        };
        ref ThunderParticleData custom = ref particle.GetCustomData<ThunderParticleData>();
        custom.Squish = squish;
        custom.ShakePower = shakePower;
        SafeSpawn(particle);
    }

    public static void SpawnTwinkleParticle(Vector2 position, Vector2 velocity, int lifetime, Vector2 scaleFactor, Color color, int totalStarPoints, Color backglowBloomColor = default, float rotation = 0f, LockOnDetails? lockOnDetails = null)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Scale = 1f,
            Color = color,
            Opacity = 1f,
            Rotation = rotation,
            Type = ParticleTypes.Twinkle,
            Width = 2,
            Height = 2,
            LockOnDetails = lockOnDetails
        };
        ref TwinkleParticleData custom = ref particle.GetCustomData<TwinkleParticleData>();
        custom.TotalStarPoints = totalStarPoints;
        custom.BackglowBloomColor = backglowBloomColor;
        custom.ScaleFactor = scaleFactor;
        SafeSpawn(particle);
    }
    #endregion
}
#endregion

#region Utility
public static class ParticleUtilities
{
    public static List<ParticleData> FindParticles(ParticleTypes type, Action onFind = null)
    {
        ParticleSystem particleSystem = ModContent.GetInstance<ParticleSystem>();
        var particles = particleSystem.GetParticles();
        var presenceMask = particleSystem.GetPresenceMask();

        List<ParticleData> particleTypes = [];
        for (int maskIndex = 0, baseIndex = 0; maskIndex < presenceMask.Length; maskIndex++, baseIndex += ParticleSystem.BitsPerMask)
        {
            ulong maskCopy = presenceMask[maskIndex];
            while (maskCopy != 0)
            {
                int bitIndex = BitOperations.TrailingZeroCount(maskCopy);
                maskCopy &= ~(1ul << bitIndex);
                int index = baseIndex + bitIndex;
                ref ParticleData p = ref particles[index];

                if (p.Active && p.Type == type)
                {
                    onFind.Invoke();
                    particleTypes.Add(p);
                }
            }
        }

        return particleTypes;
    }
}
#endregion