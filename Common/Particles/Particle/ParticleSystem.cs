using Microsoft.Xna.Framework.Graphics;
using ReLogic.Threading;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

// simd? where simds????
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector4 = Microsoft.Xna.Framework.Vector4;

namespace TheExtraordinaryAdditions.Common.Particles;

// could probably be like double the speed if i used atlas' but i dont feel like setting all that up
// spatial partioning who?? what quad trees??
// if all else fails literally just use Friflo.Engine.ECS or something

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
    LightningArc,
    Menacing,
    Mist,
    PulseRing,
    Shockwave,
    Snowflake,
    Spark,
    Sparkle,
    SquishyLight,
    SquishyPixel,
    TechyHolosquare,
    Thunder,
    Twinkle,
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
[Autoload(Side = ModSide.Client)]
public sealed class ParticleSystem : ModSystem
{
    public static ParticleSystem Instance => ModContent.GetInstance<ParticleSystem>();
    public const uint MaxParticles = 32768; // 2^15
    private static ParticleData[] particles;
    private static ulong[] presenceMask;
    public static BitmaskUtils.BitmaskEnumerable ActiveParticles => new BitmaskUtils.BitmaskEnumerable(presenceMask.AsSpan(0, presenceMask.Length), MaxParticles);
    private int activeCount;

    // Type-specific behavior
    public static readonly ParticleTypeDefinition[] TypeDefinitions = new ParticleTypeDefinition[(int)(GetLastEnumValue<ParticleTypes>() + 1)];
    public ParticleData[] GetParticles() => particles;
    public ulong[] GetPresenceMask() => presenceMask;

    public override void OnModLoad()
    {
        ParticleRegistry.Initialize();

        particles = new ParticleData[MaxParticles];
        presenceMask = BitmaskUtils.CreateMask(MaxParticles);
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
        if (activeCount >= MaxParticles || Main.gamePaused || Main.dedServ)
            return;

        int index = BitmaskUtils.AllocateIndex(presenceMask, MaxParticles, true);
        particles[index] = particle;
        particles[index].OldPositions.Fill(particle.Position);
        particles[index].Init = new(particle.Velocity, particle.Position, particle.Opacity, particle.Scale, particle.Color);
        particles[index].Time = 0;
        TypeDefinitions[(byte)particle.Type].OnSpawn?.Invoke(ref particles[index]);
        activeCount++;
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
                int baseIndex = maskIndex * BitmaskUtils.BitsPerMask;

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
        foreach (BlendState blendState in SupportedBlendStates)
        {
            sb.Begin(SpriteSortMode.Deferred, blendState, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.ZoomMatrix);
            foreach (int index in ActiveParticles)
            {
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

public static partial class ParticleRegistry
{
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
        typeof(SnowflakeParticleDefinition),
        typeof(SparkParticleDefinition),
        typeof(SparkleParticleDefinition),
        typeof(SquishyLightParticleDefinition),
        typeof(SquishyPixelParticleDefinition),
        typeof(TechyHolosquareParticleDefinition),
        typeof(ThunderParticleDefinition),
        typeof(TwinkleParticleDefinition)
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

    public static void SafeSpawn(in ParticleData data)
    {
        if (Main.dedServ) // Because it can still try to sneak through
            return;
        ModContent.GetInstance<ParticleSystem>().Add(data);
    }
}
#endregion