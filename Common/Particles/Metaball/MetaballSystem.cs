using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector4 = Microsoft.Xna.Framework.Vector4;

namespace TheExtraordinaryAdditions.Common.Particles.Metaball;

public enum MetaballDrawLayers
{
    BeforeNPCs,
    BeforeProjectiles,
    AfterProjectiles,
    OverPlayers,
}

public enum MetaballTypes : byte
{
    Plasma,
    Lava,
    Onyx,
    Abyssal,
    Genedies,
}

public readonly struct InitMetaball(Vector2 vel, float scale, float opac, Color col, Point size, float rot)
{
    public readonly Vector2 InitVelocity = vel;
    public readonly float InitScale = scale;
    public readonly float InitOpacity = opac;
    public readonly Color InitColor = col;
    public readonly Point InitSize = size;
    public readonly float InitRotation = rot;
}

public unsafe struct Metaball
{
    private const byte CustomDataSize = 32;

    public Vector2 Position;
    public Vector2 OldVelocity;
    public Vector2 Velocity;
    public int Lifetime;
    public int Time;
    public float Scale;
    public float Opacity;
    public Color Color;
    public MetaballTypes Type;
    public Point Size;
    public float Rotation;
    public InitMetaball Init;
    public CollisionTypes AllowedCollisions;
    public Rectangle Hitbox;

    private fixed byte customData[CustomDataSize];
    public Span<byte> CustomData => MemoryMarshal.CreateSpan(ref customData[0], CustomDataSize);

    /// <summary>
    /// Helper to cast CustomData to a specific struct <br></br>
    /// Maximum size of a struct is 32 bytes. <see cref="float"/> is 4 bytes, <see cref="bool"/> is 4 bytes because of padding, etc. <br></br>
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

    public bool Active => Time < Lifetime; // Whether the metaball is still alive
}

public readonly record struct MetaballTypeDefinition(
    Texture2D Texture,
    ManagedShader Shader,
    UpdateDelegate Update,
    MetaballDrawLayers DrawLayer,
    DrawDelegate Draw,
    PrepareShaderDelegate PrepareShader,
    OnCollisionDelegate OnCollision
);

public delegate void UpdateDelegate(ref Metaball m);
public delegate void DrawDelegate(ref Metaball m, SpriteBatch sb);
public delegate void PrepareShaderDelegate(ManagedShader shader, ManagedRenderTarget target);
public delegate void OnCollisionDelegate(ref Metaball m);

[Autoload(Side = ModSide.Client)]
public class MetaballSystem : ModSystem
{
    public static MetaballSystem Instance => ModContent.GetInstance<MetaballSystem>();
    private Metaball[] metaballs = new Metaball[MaxMetaballs];
    private ulong[] presenceMask = new ulong[(MaxMetaballs + 63) / 64];

    // Metaballs work by having all the particles live on one render target, but this means other types can overpower another one since they both influence each other
    // So we make all types have their own
    private Dictionary<MetaballTypes, ManagedRenderTarget> typeRenderTargets = new();
    private ManagedShader genericShader;
    public const int MaxMetaballs = 8192;
    public Metaball[] GetMetaballs() => metaballs;
    public ulong[] GetPresenceMask() => presenceMask;
    public override void OnModLoad()
    {
        if (Main.netMode == NetmodeID.Server)
            return;

        Main.QueueMainThreadAction(() =>
        {
            // Initialize render targets for each metaball type
            foreach (MetaballTypes type in Enum.GetValues(typeof(MetaballTypes)))
            {
                typeRenderTargets[type] = new ManagedRenderTarget(true, (w, h) => new RenderTarget2D(
                    Main.graphics.GraphicsDevice, w / 2, h / 2));
            }

            genericShader = AssetRegistry.GetShader("AMetaball");
            MetaballRegistry.Initialize();
            RenderTargetManager.RenderTargetUpdateLoopEvent += RenderMetaballsToTarget;
            On_Main.DrawProjectiles += DrawMetaballsProjectiles;
            On_Main.DrawNPCs += DrawMetaballsBeforeNPCs;
            On_Main.DrawPlayers_AfterProjectiles += DrawMetaballsOverPlayers;
        });
    }

    public override void OnModUnload()
    {
        Main.QueueMainThreadAction(() =>
        {
            RenderTargetManager.RenderTargetUpdateLoopEvent -= RenderMetaballsToTarget;
            On_Main.DrawProjectiles -= DrawMetaballsProjectiles;
            On_Main.DrawNPCs -= DrawMetaballsBeforeNPCs;
            On_Main.DrawPlayers_AfterProjectiles -= DrawMetaballsOverPlayers;
            foreach (var target in typeRenderTargets.Values)
                target?.Dispose();
            typeRenderTargets.Clear();
        });
    }

    public void AddMetaball(Metaball metaball)
    {
        if (Main.gamePaused || Main.dedServ)
            return;

        int index = AllocateIndex();
        if (index != -1)
        {
            metaballs[index] = metaball;
            metaballs[index].Init = new(metaball.Velocity, metaball.Scale, metaball.Opacity, metaball.Color, metaball.Size, metaball.Rotation);
            SetBit(index, true);
        }
    }

    private int AllocateIndex()
    {
        for (int i = 0; i < presenceMask.Length; i++)
        {
            if (presenceMask[i] != ulong.MaxValue)
            {
                int bitIndex = BitOperations.TrailingZeroCount(~presenceMask[i]);
                int index = i * 64 + bitIndex;
                if (index < MaxMetaballs)
                {
                    presenceMask[i] |= (1ul << bitIndex);
                    return index;
                }
            }
        }
        return -1;
    }

    private void SetBit(int index, bool value)
    {
        int maskIndex = index / 64;
        int bitIndex = index % 64;
        if (value)
            presenceMask[maskIndex] |= (1ul << bitIndex);
        else
            presenceMask[maskIndex] &= ~(1ul << bitIndex);
    }

    public override void PostUpdateDusts()
    {
        if (Main.gamePaused || Main.dedServ)
            return;

        UpdateMetaballs();
    }

    private void UpdateMetaballs()
    {
        for (int maskIndex = 0; maskIndex < presenceMask.Length; maskIndex++)
        {
            ulong mask = presenceMask[maskIndex];
            int baseIndex = maskIndex * 64;
            while (mask != 0)
            {
                int bitIndex = BitOperations.TrailingZeroCount(mask);
                mask &= ~(1ul << bitIndex);
                int index = baseIndex + bitIndex;
                if (index >= MaxMetaballs) continue;
                ref Metaball m = ref metaballs[index];
                m.Time++;
                if (!m.Active)
                {
                    SetBit(index, false);
                    continue;
                }

                MetaballTypeDefinition def = MetaballRegistry.TypeDefinitions[(byte)m.Type];

                m.OldVelocity = m.Velocity;
                if (m.AllowedCollisions != CollisionTypes.None)
                {
                    bool collide = false;
                    if (m.AllowedCollisions.HasFlag(CollisionTypes.Solid))
                    {
                        m.Velocity = Collision.TileCollision(m.Position, m.Velocity, 1, 1, m.AllowedCollisions.HasFlag(CollisionTypes.NonSolid));
                        Vector4 slope = Collision.SlopeCollision(m.Position, m.Velocity, 1, 1, 1f);
                        m.Position.X = slope.X;
                        m.Position.Y = slope.Y;
                        m.Velocity.X = slope.Z;
                        m.Velocity.Y = slope.W;
                    }

                    if (m.AllowedCollisions.HasFlag(CollisionTypes.Liquid))
                    {
                        m.Velocity = Collision.WaterCollision(m.Position, m.Velocity, 1, 1, m.AllowedCollisions.HasFlag(CollisionTypes.NonSolid), false, true);
                    }

                    if (m.AllowedCollisions.HasFlag(CollisionTypes.NPC))
                    {
                        foreach (NPC npc in Main.ActiveNPCs)
                        {
                            if (npc != null)
                            {
                                Rectangle temp = m.Hitbox;
                                m.Velocity = ResolveCollision(ref temp, npc.RotHitbox(), m.Velocity, out collide, 4);
                            }
                        }
                    }

                    if (m.AllowedCollisions.HasFlag(CollisionTypes.Projectile))
                    {
                        foreach (Projectile proj in Main.ActiveProjectiles)
                        {
                            if (proj != null)
                            {
                                Rectangle temp = m.Hitbox;
                                m.Velocity = ResolveCollision(ref temp, proj.RotHitbox(), m.Velocity, out collide, 4);
                            }
                        }
                    }

                    if (m.AllowedCollisions.HasFlag(CollisionTypes.Player))
                    {
                        foreach (Player player in Main.ActivePlayers)
                        {
                            if (player != null)
                            {
                                Rectangle temp = m.Hitbox;
                                m.Velocity = ResolveCollision(ref temp, player.RotHitbox(), m.Velocity, out collide, 4);
                            }
                        }
                    }

                    if (m.Velocity != m.OldVelocity || collide)
                        def.OnCollision?.Invoke(ref m);
                    m.Position += m.Velocity;
                }
                else
                    m.Position += m.Velocity;

                def.Update?.Invoke(ref m);
                m.Hitbox = new((int)(m.Position.X - 2), (int)(m.Position.Y - 2), 4, 4);
            }
        }
    }

    private void RenderMetaballsToTarget()
    {
        GraphicsDevice gd = Main.instance.GraphicsDevice;

        // Render each type to its own render target
        foreach (MetaballTypes type in Enum.GetValues(typeof(MetaballTypes)))
        {
            ManagedRenderTarget target = typeRenderTargets[type];
            gd.SetRenderTarget(target);
            gd.Clear(Color.Transparent);

            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Matrix.Identity
            );

            for (int maskIndex = 0; maskIndex < presenceMask.Length; maskIndex++)
            {
                ulong mask = presenceMask[maskIndex];
                int baseIndex = maskIndex * 64;
                while (mask != 0)
                {
                    int bitIndex = BitOperations.TrailingZeroCount(mask);
                    mask &= ~(1ul << bitIndex);
                    int index = baseIndex + bitIndex;
                    if (index >= MaxMetaballs) 
                        continue;
                    Metaball metaball = metaballs[index];
                    if (!metaball.Active || metaball.Type != type)
                        continue;

                    MetaballTypeDefinition def = MetaballRegistry.TypeDefinitions[(byte)metaball.Type];

                    if (def.Draw == null)
                    {
                        Texture2D texture = def.Texture;
                        Vector2 screenDelta = (Main.screenLastPosition - Main.screenPosition);
                        Vector2 position = (metaball.Position + screenDelta - Main.screenPosition) / 2f;
                        Rectangle destRect = new(
                            (int)position.X,
                            (int)position.Y,
                            (int)(metaball.Size.X * metaball.Scale / 2f),
                            (int)(metaball.Size.Y * metaball.Scale / 2f)
                        );
                        Color color = metaball.Color * metaball.Opacity;

                        Main.spriteBatch.Draw(
                            texture,
                            destRect,
                            null,
                            color,
                            metaball.Rotation,
                            texture.Size() / 2,
                            SpriteEffects.None,
                            0f
                        );
                    }
                    else
                        def.Draw(ref metaball, Main.spriteBatch);
                }
            }

            Main.spriteBatch.End();
        }

        gd.SetRenderTarget(null);
    }

    private void DrawMetaballsProjectiles(On_Main.orig_DrawProjectiles orig, Main self)
    {
        DrawMetaballsAtLayer(MetaballDrawLayers.BeforeProjectiles);
        orig(self);
        DrawMetaballsAtLayer(MetaballDrawLayers.AfterProjectiles);
    }

    private void DrawMetaballsBeforeNPCs(On_Main.orig_DrawNPCs orig, Main self, bool behindTiles)
    {
        if (!behindTiles)
            DrawMetaballsAtLayer(MetaballDrawLayers.BeforeNPCs, true);
        orig(self, behindTiles);
    }

    private void DrawMetaballsOverPlayers(On_Main.orig_DrawPlayers_AfterProjectiles orig, Main self)
    {
        orig(self);
        DrawMetaballsAtLayer(MetaballDrawLayers.OverPlayers);
    }

    private void DrawMetaballsAtLayer(MetaballDrawLayers layer, bool resetSB = false)
    {
        bool hasMetaballs = false;
        for (int maskIndex = 0; maskIndex < presenceMask.Length; maskIndex++)
        {
            ulong mask = presenceMask[maskIndex];
            if (mask == 0)
                continue;
            int baseIndex = maskIndex * 64;
            while (mask != 0)
            {
                int bitIndex = BitOperations.TrailingZeroCount(mask);
                mask &= ~(1ul << bitIndex);
                int index = baseIndex + bitIndex;
                if (index >= MaxMetaballs)
                    continue;
                Metaball m = metaballs[index];
                if (m.Active && MetaballRegistry.TypeDefinitions[(byte)m.Type].DrawLayer == layer)
                {
                    hasMetaballs = true;
                    break;
                }
            }
            if (hasMetaballs)
                break;
        }

        if (!hasMetaballs)
            return;

        if (resetSB)
            Main.spriteBatch.End();
        Main.spriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            null,
            Main.GameViewMatrix.TransformationMatrix
        );

        foreach (MetaballTypes type in Enum.GetValues(typeof(MetaballTypes)))
        {
            MetaballTypeDefinition def = MetaballRegistry.TypeDefinitions[(byte)type];
            if (def.DrawLayer != layer)
                continue;

            // Check if this type has any active metaballs
            bool typeHasMetaballs = false;
            for (int maskIndex = 0; maskIndex < presenceMask.Length; maskIndex++)
            {
                ulong mask = presenceMask[maskIndex];
                int baseIndex = maskIndex * 64;
                while (mask != 0)
                {
                    int bitIndex = BitOperations.TrailingZeroCount(mask);
                    mask &= ~(1ul << bitIndex);
                    int index = baseIndex + bitIndex;
                    if (index >= MaxMetaballs)
                        continue;
                    Metaball m = metaballs[index];
                    if (m.Active && m.Type == type)
                    {
                        typeHasMetaballs = true;
                        break;
                    }
                }
                if (typeHasMetaballs)
                    break;
            }

            if (!typeHasMetaballs)
                continue;

            ManagedShader shader = def.Shader == null ? genericShader : AssetRegistry.GetShader(def.Shader.Name);
            def.PrepareShader?.Invoke(shader, typeRenderTargets[type]);
            shader.SetTexture(typeRenderTargets[type], 1, SamplerState.AnisotropicWrap);
            Main.spriteBatch.Draw(typeRenderTargets[type], Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, 0, 0f);
        }

        if (resetSB)
            Main.spriteBatch.ResetToDefault();
        else
            Main.spriteBatch.End();
    }

    public ReadOnlySpan<Metaball> GetActiveMetaballs(MetaballTypes type)
    {
        // First pass: count active metaballs of the specified type
        int count = 0;
        for (int maskIndex = 0; maskIndex < presenceMask.Length; maskIndex++)
        {
            ulong mask = presenceMask[maskIndex];
            int baseIndex = maskIndex * 64;
            while (mask != 0)
            {
                int bitIndex = BitOperations.TrailingZeroCount(mask);
                mask &= ~(1ul << bitIndex);
                int index = baseIndex + bitIndex;
                if (index >= MaxMetaballs)
                    continue;
                Metaball m = metaballs[index];
                if (m.Active && m.Type == type)
                    count++;
            }
        }

        // Allocate array for results
        Metaball[] result = new Metaball[count];
        int currentIndex = 0;

        // Second pass: collect active metaballs
        for (int maskIndex = 0; maskIndex < presenceMask.Length && currentIndex < count; maskIndex++)
        {
            ulong mask = presenceMask[maskIndex];
            int baseIndex = maskIndex * 64;
            while (mask != 0)
            {
                int bitIndex = BitOperations.TrailingZeroCount(mask);
                mask &= ~(1ul << bitIndex);
                int index = baseIndex + bitIndex;
                if (index >= MaxMetaballs)
                    continue;
                Metaball m = metaballs[index];
                if (m.Active && m.Type == type)
                {
                    result[currentIndex] = m;
                    currentIndex++;
                }
            }
        }

        return result.AsSpan();
    }

    public delegate void ModifyMetaballDelegate(ref Metaball metaball);
    public void ModifyActiveMetaballs(MetaballTypes type, ModifyMetaballDelegate modifier)
    {
        for (int maskIndex = 0; maskIndex < presenceMask.Length; maskIndex++)
        {
            ulong mask = presenceMask[maskIndex];
            int baseIndex = maskIndex * 64;
            while (mask != 0)
            {
                int bitIndex = BitOperations.TrailingZeroCount(mask);
                mask &= ~(1ul << bitIndex);
                int index = baseIndex + bitIndex;
                if (index >= MaxMetaballs)
                    continue;
                ref Metaball m = ref metaballs[index];
                if (m.Active && m.Type == type)
                {
                    modifier(ref m);
                }
            }
        }
    }
}

public static class MetaballRegistry
{
    public static readonly MetaballTypeDefinition[] TypeDefinitions = new MetaballTypeDefinition[(int)(GetLastEnumValue<MetaballTypes>() + 1)];

    public static void Initialize()
    {
        LavaMetaballDefinition();
        PlasmaMetaballDefinition();
        OnyxMetaballDefinition();
        AbyssalMetaballDefinition();
        GenediesMetaballDefinition();
    }

    #region Plasma
    private struct PlasmaData
    {
        public float Brightness;
    }

    public static void PlasmaMetaballDefinition()
    {
        TypeDefinitions[(byte)MetaballTypes.Plasma] = new MetaballTypeDefinition(
            Texture: AssetRegistry.GetTexture(AdditionsTexture.NebulaGas1),
            Shader: null,
            Update: static (ref Metaball m) =>
            {
                float timeRatio = (float)m.Time / m.Lifetime;
                m.Opacity = 1f - timeRatio;
                m.Color = Color.Lerp(Color.White.Lerp(Color.DarkOrange, 1f - m.GetCustomData<PlasmaData>().Brightness), Color.Chocolate, Animators.MakePoly(4f).OutFunction(timeRatio));
                m.Scale = 1f + (timeRatio * 0.1f);
            },
            Draw: null,
            DrawLayer: MetaballDrawLayers.AfterProjectiles,
            PrepareShader: static (ManagedShader shader, ManagedRenderTarget target) =>
            {
                shader.TrySetParameter("threshold", .5f);
                shader.TrySetParameter("epsilon", 0.2f);
                shader.Render();
            },
            OnCollision: null
        );
    }

    public static void SpawnPlasmaMetaball(Vector2 position, Vector2 velocity, int lifetime, int size, float brightness = 1f)
    {
        Metaball metaball = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Time = 0,
            Scale = 1f,
            Opacity = 1f,
            Type = MetaballTypes.Plasma,
            Size = new(size, size),
            Rotation = RandomRotation()
        };
        metaball.GetCustomData<PlasmaData>().Brightness = brightness;

        ModContent.GetInstance<MetaballSystem>().AddMetaball(metaball);
    }
    #endregion

    #region Lava
    public struct LavaData
    {
        public bool Collided;
        public int Damage;
        public bool Sticking;
        public int StuckEnemy;
        public Vector2 EnemyOffset;
        public int Owner;
        public bool Cosmetic;
    }

    public static void LavaMetaballDefinition()
    {
        TypeDefinitions[(byte)MetaballTypes.Lava] = new MetaballTypeDefinition(
            Texture: AssetRegistry.GetTexture(AdditionsTexture.BrightLight),
            Shader: AssetRegistry.GetShader("LavaMetaball"),
            Update: static (ref Metaball m) =>
            {
                ref LavaData data = ref m.GetCustomData<LavaData>();
                float timeRatio = (float)m.Time / m.Lifetime;
                m.Opacity = 1f - timeRatio;
                m.Scale = m.Init.InitScale * m.Opacity;

                if (!m.GetCustomData<LavaData>().Collided)
                {
                    if (m.Velocity.Y < 20f)
                        m.Velocity.Y += .24f;
                }

                // do sticky
                if (!data.Cosmetic)
                {
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (npc == null || npc.friendly || npc.immortal || npc.townNPC || npc.lifeMax <= 5)
                            continue;

                        Rectangle rect = new((int)(m.Position.X - m.Size.X / 2), (int)(m.Position.Y - m.Size.Y / 2), m.Size.X, m.Size.Y);
                        if (m.Hitbox.Intersects(npc.Hitbox))
                        {
                            if (npc.immune[data.Owner] == 0)
                            {
                                NPC.HitInfo info = npc.CalculateHitInfo(data.Damage, (npc.Center.X > m.Position.X).ToDirectionInt(), false, 0f, DamageClass.Magic, true, 0f);
                                npc.StrikeNPC(info);
                                npc.AddBuff(BuffID.OnFire, 180);
                                npc.immune[data.Owner] = 20;
                            }

                            if (!data.Sticking)
                            {
                                data.Sticking = true;
                                data.StuckEnemy = npc.whoAmI;
                                data.EnemyOffset = m.Position - npc.Center;
                                m.Velocity = Vector2.Zero;
                            }
                        }
                    }

                    if (data.StuckEnemy >= 0)
                    {
                        NPC enemy = Main.npc[data.StuckEnemy];
                        if (enemy == null || !enemy.active)
                        {
                            data.Sticking = false;
                        }
                        if (data.Sticking)
                        {
                            m.Velocity.Y = 0f;
                            m.Position = enemy.Center + data.EnemyOffset;
                        }
                    }
                }

                // actual pyroclast
                if ((data.Sticking || data.Collided) && Main.rand.NextBool(100) && m.Scale > .3f)
                {
                    int size = (int)(Main.rand.Next(30, 50) * m.Scale);
                    Metaball metaball = new()
                    {
                        Position = m.Position + Main.rand.NextVector2Circular(m.Size.X / 4, m.Size.Y / 4),
                        Velocity = Vector2.UnitY.RotatedByRandom(.3f) * -Main.rand.NextFloat(6f, 12f),
                        Lifetime = Main.rand.Next(30, 50),
                        Time = 0,
                        Scale = 1f,
                        Opacity = 1f,
                        Color = Color.White,
                        Type = MetaballTypes.Lava,
                        AllowedCollisions = CollisionTypes.Solid,
                        Size = new(size, size)
                    };
                    ref LavaData cinderData = ref metaball.GetCustomData<LavaData>();
                    cinderData.StuckEnemy = -1;
                    cinderData.Owner = data.Owner;
                    cinderData.Cosmetic = true;

                    ModContent.GetInstance<MetaballSystem>().AddMetaball(metaball);
                }

                // gloop
                if (Main.rand.NextBool(2000) && !data.Cosmetic)
                    SoundID.SplashWeak.Play(m.Position, 1f, 0f, .2f);

                // bright
                Lighting.AddLight(m.Position, Color.OrangeRed.ToVector3() * CalculateIntensityForRadius(m.Size.X / 16) * 2.8f * m.Scale);
            },
            DrawLayer: MetaballDrawLayers.AfterProjectiles,
            Draw: null,
            PrepareShader: (ManagedShader shader, ManagedRenderTarget target) =>
            {
                shader.TrySetParameter("time", Main.GlobalTimeWrappedHourly);
                shader.TrySetParameter("threshold", .5f);
                shader.TrySetParameter("epsilon", .1f);
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.BlobbyNoise), 2, SamplerState.LinearWrap);
                shader.Render();
            },
            OnCollision: (ref Metaball m) =>
            {
                if (!m.GetCustomData<LavaData>().Collided)
                {
                    m.Velocity = Vector2.Zero;
                    m.GetCustomData<LavaData>().Collided = true;
                }
            });
    }

    public static void SpawnLavaMetaball(Vector2 position, Vector2 velocity, int lifetime, int size, int owner, int damage)
    {
        Metaball metaball = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Time = 0,
            Scale = 1f,
            Opacity = 1f,
            Color = Color.White,
            Type = MetaballTypes.Lava,
            AllowedCollisions = CollisionTypes.Solid,
            Size = new(size, size)
        };
        ref LavaData data = ref metaball.GetCustomData<LavaData>();
        data.StuckEnemy = -1;
        data.Owner = owner;
        data.Damage = damage;

        ModContent.GetInstance<MetaballSystem>().AddMetaball(metaball);
    }
    #endregion

    #region Onyx
    public static void OnyxMetaballDefinition()
    {
        TypeDefinitions[(byte)MetaballTypes.Onyx] = new MetaballTypeDefinition(
            Texture: AssetRegistry.GetTexture(AdditionsTexture.BrightLight),
            Shader: AssetRegistry.GetShader("OnyxMetaball"),
            Update: static (ref Metaball m) =>
            {
                float timeRatio = (float)m.Time / m.Lifetime;
                m.Opacity = 1f - timeRatio;
                m.Scale = m.Init.InitScale * m.Opacity;
            },
            Draw: null,
            DrawLayer: MetaballDrawLayers.AfterProjectiles,
            PrepareShader: static (ManagedShader shader, ManagedRenderTarget target) =>
            {
                shader.TrySetParameter("globalTime", Main.GlobalTimeWrappedHourly);
                shader.TrySetParameter("threshold", .5f);
                shader.TrySetParameter("epsilon", .1f);
                shader.Render();
            },
            OnCollision: static (ref Metaball m) =>
            {
                m.Velocity = Vector2.Zero;
            }
        );
    }

    public static void SpawnOnyxMetaball(Vector2 position, Vector2 velocity, int lifetime, int size)
    {
        Metaball metaball = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Time = 0,
            Scale = 1f,
            Opacity = 1f,
            Color = Color.White,
            Type = MetaballTypes.Onyx,
            Size = new(size, size)
        };
        ModContent.GetInstance<MetaballSystem>().AddMetaball(metaball);
    }
    #endregion

    #region Abyssal
    public static void AbyssalMetaballDefinition()
    {
        TypeDefinitions[(byte)MetaballTypes.Abyssal] = new MetaballTypeDefinition(
            Texture: AssetRegistry.GetTexture(AdditionsTexture.SunGray),
            Shader: AssetRegistry.GetShader("AbyssalMetaball"),
            Update: static (ref Metaball m) =>
            {
                float timeRatio = (float)m.Time / m.Lifetime;
                m.Opacity = 1f - timeRatio;
                m.Scale = m.Init.InitScale * m.Opacity;
            },
            Draw: null,
            DrawLayer: MetaballDrawLayers.AfterProjectiles,
            PrepareShader: static (ManagedShader shader, ManagedRenderTarget target) =>
            {
                shader.TrySetParameter("globalTime", Main.GlobalTimeWrappedHourly);
                shader.TrySetParameter("threshold", .5f);
                shader.TrySetParameter("epsilon", .3f);
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.CausticNoise), 2, SamplerState.LinearWrap);
                shader.Render();
            },
            OnCollision: static (ref Metaball m) =>
            {
                m.Velocity = Vector2.Zero;
            }
        );
    }

    public static void SpawnAbyssalMetaball(Vector2 position, Vector2 velocity, int lifetime, int size)
    {
        Metaball metaball = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Rotation = RandomRotation(),
            Time = 0,
            Scale = 1f,
            Opacity = 1f,
            Color = Color.White,
            Type = MetaballTypes.Abyssal,
            Size = new(size, size)
        };
        ModContent.GetInstance<MetaballSystem>().AddMetaball(metaball);
    }
    #endregion

    #region Genedies
    public static void GenediesMetaballDefinition()
    {
        TypeDefinitions[(byte)MetaballTypes.Genedies] = new MetaballTypeDefinition(
            Texture: AssetRegistry.GetTexture(AdditionsTexture.NebulaGas2),
            Shader: AssetRegistry.GetShader("GenediesMetaball"),
            Update: static (ref Metaball m) =>
            {
                float timeRatio = (float)m.Time / m.Lifetime;
                m.Opacity = 1f - timeRatio;
                m.Scale = m.Init.InitScale * m.Opacity;
                m.Rotation += (MathF.Abs(m.Velocity.X) + MathF.Abs(m.Velocity.Y)) * .001f;
            },
            Draw: static (ref Metaball m, SpriteBatch sb) =>
            {
                Texture2D texture = TypeDefinitions[(byte)m.Type].Texture;
                Vector2 position = (m.Position - Main.screenPosition) / 2f;
                Rectangle destRect = new(
                    (int)position.X,
                    (int)position.Y,
                    (int)(m.Size.X * m.Scale / 2f * Utils.MultiLerp(InverseLerp(0f, m.Lifetime, m.Time), 0f, 1f, 0f)),
                    (int)(m.Size.Y * m.Scale / 2f)
                );
                Color color = m.Color * m.Opacity.Squared();

                sb.Draw(
                    texture,
                    destRect,
                    null,
                    color,
                    m.Rotation,
                    texture.Size() / 2,
                    SpriteEffects.None,
                    0f
                );
            },
            DrawLayer: MetaballDrawLayers.BeforeProjectiles,
            PrepareShader: static (ManagedShader shader, ManagedRenderTarget target) =>
            {
                shader.TrySetParameter("globalTime", TimeSystem.UpdateCount);
                shader.TrySetParameter("threshold", .5f);
                shader.TrySetParameter("epsilon", .3f);
                shader.Render();
            },
            OnCollision: static (ref Metaball m) =>
            {
                m.Velocity = Vector2.Zero;
            }
        );
    }

    public static void SpawnGenediesMetaball(Vector2 position, Vector2 velocity, int lifetime, int size)
    {
        Metaball metaball = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Rotation = RandomRotation(),
            Time = 0,
            Scale = 1f,
            Opacity = 1f,
            Color = Color.White,
            Type = MetaballTypes.Genedies,
            Size = new(size, size)
        };
        ModContent.GetInstance<MetaballSystem>().AddMetaball(metaball);
    }
    #endregion
}