using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Common.Particles.Shader;

public unsafe struct ShaderParticle()
{
    public Vector2 Position;
    public Vector2 Velocity;
    public Vector2 OldVelocity;
    public Vector2 Size;
    public int Lifetime;
    public int Time;
    public float Opacity;
    public Color Color;
    public ShaderParticleTypes Type;
    public float Rotation;
}

public readonly record struct ShaderParticleTypeDefinition(
    Texture2D Texture,
    Texture2D LayerTexture,
    ManagedShader Shader,
    UpdateDelegate Update,
    ShouldKillDelegate ShouldKill,
    ShaderParticleDrawLayers DrawLayer,
    DrawDelegate Draw,
    PrepareSBDelegate PrepareSB,
    PrepareShaderDelegate PrepareShader,
    LayerOffsetDelegate LayerOffset,
    Color EdgeColor
);

public delegate void UpdateDelegate(ref ShaderParticle s);
public delegate void DrawDelegate(ref ShaderParticle s, SpriteBatch sb);
public delegate bool ShouldKillDelegate(ref ShaderParticle s);
public delegate void PrepareSBDelegate();
public delegate void PrepareShaderDelegate(ManagedShader shader, ManagedRenderTarget target, ShaderParticleTypes type);
public delegate Vector2 LayerOffsetDelegate();

public enum ShaderParticleTypes : byte
{
    Stygain,
    Molten,
    Epidemic,
    Cosmic,
}

public enum ShaderParticleDrawLayers
{
    BeforeNPCs,
    BeforeProjectiles,
    AfterProjectiles,
    OverPlayers,
}

[Autoload(Side = ModSide.Client)]
public class ShaderParticleSystem : ModSystem
{
    public static ShaderParticleSystem Instance => ModContent.GetInstance<ShaderParticleSystem>();
    public const uint MaxShaderParticles = 16384;
    private static ShaderParticle[] particles = new ShaderParticle[MaxShaderParticles];
    private static ulong[] presenceMask = BitmaskUtils.CreateMask(MaxShaderParticles);
    public static BitmaskUtils.BitmaskEnumerable ActiveShaderParticles => new BitmaskUtils.BitmaskEnumerable(presenceMask.AsSpan(0, presenceMask.Length), MaxShaderParticles);
    private static Dictionary<ShaderParticleTypes, ManagedRenderTarget> typeRenderTargets = new();

    public override void OnModLoad()
    {
        // Prepare event subscribers
        Main.QueueMainThreadAction(() =>
        {
            foreach (ShaderParticleTypes type in Enum.GetValues(typeof(ShaderParticleTypes)))
                typeRenderTargets[type] = new ManagedRenderTarget(true, (w, h) => new RenderTarget2D(
                    Main.graphics.GraphicsDevice, w / 2, h / 2));
            ShaderParticleRegistry.Initialize();
            RenderTargetManager.RenderTargetUpdateLoopEvent += PrepareShaderParticleTargets;
            On_Main.DrawProjectiles += DrawParticlesWithProjectiles;
            On_Main.DrawNPCs += DrawParticlesBeforeNPCs;
            On_Main.DrawPlayers_AfterProjectiles += DrawParticlesOverPlayers;
        });
    }

    public override void OnModUnload()
    {
        // Clear all unmanaged ShaderParticle target resources on the GPU on mod unload
        Main.QueueMainThreadAction(() =>
        {
            foreach (ManagedRenderTarget target in typeRenderTargets.Values)
                target?.Dispose();
            RenderTargetManager.RenderTargetUpdateLoopEvent -= PrepareShaderParticleTargets;
            On_Main.DrawProjectiles -= DrawParticlesWithProjectiles;
            On_Main.DrawNPCs -= DrawParticlesBeforeNPCs;
            On_Main.DrawPlayers_AfterProjectiles -= DrawParticlesOverPlayers;
        });
    }

    public override void OnWorldUnload()
    {
        BitmaskUtils.Clear(presenceMask);
    }

    public void AddParticle(ShaderParticle metaball)
    {
        if (Main.gamePaused || Main.dedServ)
            return;

        int index = BitmaskUtils.AllocateIndex(presenceMask, MaxShaderParticles);
        if (index != -1)
        {
            particles[index] = metaball;
        }
    }

    public override void PostUpdateDusts()
    {
        if (Main.gamePaused || Main.dedServ)
            return;

        foreach (int index in ActiveShaderParticles)
        {
            ref ShaderParticle s = ref particles[index];
            s.Time++;
            ShaderParticleTypeDefinition def = ShaderParticleRegistry.TypeDefinitions[(byte)s.Type];

            if (def.ShouldKill(ref s))
            {
                BitmaskUtils.SetBit(presenceMask, index, false);
                continue;
            }

            s.OldVelocity = s.Velocity;
            s.Position += s.Velocity;
            def.Update?.Invoke(ref s);
        }
    }

    private void PrepareShaderParticleTargets()
    {
        GraphicsDevice gd = Main.instance.GraphicsDevice;
        foreach (ShaderParticleTypes type in Enum.GetValues(typeof(ShaderParticleTypes)))
        {
            ManagedRenderTarget target = typeRenderTargets[type];
            gd.SetRenderTarget(target);
            gd.Clear(Color.Transparent);

            ShaderParticleTypeDefinition def = ShaderParticleRegistry.TypeDefinitions[(byte)type];

            // Prepare the sprite batch in accordance to the needs of the particle instance
            if (def.PrepareSB != null)
                def.PrepareSB();
            else
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, Main.Rasterizer, null, Matrix.Identity);

            // Draw the raw contents of the particle to each of its render targets.
            foreach (int index in ActiveShaderParticles)
            {
                ShaderParticle particle = particles[index];
                if (def.ShouldKill(ref particle) || particle.Type != type)
                    continue;

                if (def.Draw != null)
                    def.Draw(ref particle, Main.spriteBatch);
                else
                {
                    Vector2 origin = def.Texture.Size() * 0.5f;
                    Vector2 scale = Vector2.One * (particle.Size / def.Texture.Size());
                    Main.spriteBatch.PixelDraw(def.Texture, particle.Position, null, particle.Color, 0f, origin, scale, SpriteEffects.None);
                }
            }
            Main.spriteBatch.End();
        }

        // Return to the backbuffer and end the sprite batch
        gd.SetRenderTarget(null);
    }

    private static void DrawParticlesWithProjectiles(On_Main.orig_DrawProjectiles orig, Main self)
    {
        DrawShaderParticle(ShaderParticleDrawLayers.BeforeProjectiles);
        orig(self);
        DrawShaderParticle(ShaderParticleDrawLayers.AfterProjectiles);
    }

    private static void DrawParticlesBeforeNPCs(On_Main.orig_DrawNPCs orig, Main self, bool behindTiles)
    {
        if (!behindTiles)
            DrawShaderParticle(ShaderParticleDrawLayers.BeforeNPCs, true);
        orig(self, behindTiles);
    }

    private static void DrawParticlesOverPlayers(On_Main.orig_DrawPlayers_AfterProjectiles orig, Main self)
    {
        orig(self);
        DrawShaderParticle(ShaderParticleDrawLayers.OverPlayers);
    }

    /// <summary>
    /// Draws all ShaderParticle of a given <see cref="ShaderParticleDrawLayers"/>. Used for layer ordering reasons.
    /// </summary>
    /// <param name="layerType">The layer type to draw with.</param>
    public static void DrawShaderParticle(ShaderParticleDrawLayers layerType, bool resetSB = false)
    {
        bool hasParticles = false;
        foreach (int index in ActiveShaderParticles)
        {
            ShaderParticle s = particles[index];
            ShaderParticleTypeDefinition def = ShaderParticleRegistry.TypeDefinitions[(byte)s.Type];
            if (!def.ShouldKill(ref s) && def.DrawLayer == layerType)
            {
                hasParticles = true;
                break;
            }
        }
        if (!hasParticles)
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

        foreach (ShaderParticleTypes type in Enum.GetValues(typeof(ShaderParticleTypes)))
        {
            ShaderParticleTypeDefinition def = ShaderParticleRegistry.TypeDefinitions[(byte)type];
            if (def.DrawLayer != layerType)
                continue;

            // Check if this type has any active metaballs
            bool typeHasMetaballs = false;
            foreach (int index in ActiveShaderParticles)
            {
                ShaderParticle s = particles[index];
                if (!ShaderParticleRegistry.TypeDefinitions[(byte)s.Type].ShouldKill(ref s) && s.Type == type)
                {
                    typeHasMetaballs = true;
                    break;
                }

                if (typeHasMetaballs)
                    break;
            }

            if (!typeHasMetaballs)
                continue;

            ManagedShader shader = def.Shader;
            shader.SetTexture(typeRenderTargets[type], 1, SamplerState.AnisotropicWrap);
            if (def.PrepareShader != null)
                def.PrepareShader.Invoke(shader, typeRenderTargets[type], type);
            else
            {
                Vector2 screenSize = Main.ScreenSize.ToVector2() / 2f;
                Vector2 layerScrollOffset = Main.screenPosition / screenSize + def.LayerOffset();
                shader.TrySetParameter("layerSize", def.LayerTexture.Size());
                shader.TrySetParameter("screenSize", screenSize);
                shader.TrySetParameter("layerOffset", layerScrollOffset);
                shader.TrySetParameter("edgeColor", def.EdgeColor.ToVector4());
                shader.TrySetParameter("singleFrameScreenOffset", (Main.screenLastPosition - Main.screenPosition) / screenSize / 2);
                shader.SetTexture(def.LayerTexture, 2, SamplerState.LinearWrap);
                shader.Render();
            }

            Main.spriteBatch.Draw(typeRenderTargets[type], Main.screenLastPosition - Main.screenPosition, null, Color.White, 0f, Vector2.Zero, 2f, 0, 0f);
        }

        if (resetSB)
            Main.spriteBatch.ResetToDefault();
        else
            Main.spriteBatch.End();
    }
}

public static class ShaderParticleRegistry
{
    public static readonly ShaderParticleTypeDefinition[] TypeDefinitions = new ShaderParticleTypeDefinition[(int)(GetLastEnumValue<ShaderParticleTypes>() + 1)];

    public static void Initialize()
    {
        StygainParticleDefinition();
        MoltenParticleDefinition();
        EpidemicParticleDefinition();
        CosmicParticleDefinition();
    }

    #region Stygain
    private static void StygainParticleDefinition()
    {
        TypeDefinitions[(byte)ShaderParticleTypes.Stygain] = new ShaderParticleTypeDefinition(
            Texture: AssetRegistry.GetTexture(AdditionsTexture.BasicCircularCircle),
            LayerTexture: AssetRegistry.GetTexture(AdditionsTexture.TurbulentNoise),
            Shader: ShaderRegistry.EdgeDetectionShader,
            Update: static (ref ShaderParticle s) =>
            {
                s.Color = MulticolorLerp(InverseLerp(0f, s.Size.Length(), 30f), Color.Black.Lerp(Color.DarkRed, .5f), Color.DarkRed, Color.Red, Color.Crimson, Color.Crimson * 2f);
                s.Velocity *= .98f;
                s.Rotation += s.Velocity.Length() * .1f;
                s.Size *= .975f;
                if (s.Velocity.Length() < 2f)
                    s.Size *= .9f;
            },
            ShouldKill: static (ref ShaderParticle s) => s.Size.Length() <= 0.01f,
            Draw: static (ref ShaderParticle s, SpriteBatch sb) =>
            {
                ShaderParticleTypeDefinition def = TypeDefinitions[(byte)s.Type];
                Vector2 origin = def.Texture.Size() * 0.5f;
                float squish = MathHelper.Clamp(s.Velocity.Length() / 5f, 1f, 2f);
                Vector2 scale = new Vector2(s.Size.X - s.Size.X * squish * 0.3f, s.Size.Y * squish) * .6f;

                Main.spriteBatch.PixelDraw(def.Texture, s.Position, null, s.Color, s.Rotation, origin, scale / def.Texture.Size(), SpriteEffects.None);
            },
            DrawLayer: ShaderParticleDrawLayers.BeforeNPCs,
            PrepareSB: null,
            PrepareShader: null,
            LayerOffset: static () => (Vector2.One * (float)Math.Cos(Main.GlobalTimeWrappedHourly * 0.041f) * 2f).RotatedBy((float)Math.Cos(Main.GlobalTimeWrappedHourly * 0.08f) * 0.97f),
            EdgeColor: Color.DarkRed
        );
    }

    public static void SpawnStygainParticle(Vector2 position, Vector2 velocity, float size)
    {
        ShaderParticle particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = 60,
            Time = 0,
            Opacity = 1f,
            Type = ShaderParticleTypes.Stygain,
            Size = new(size, size),
            Rotation = RandomRotation()
        };

        ShaderParticleSystem.Instance.AddParticle(particle);
    }
    #endregion

    #region Molten
    private static void MoltenParticleDefinition()
    {
        TypeDefinitions[(byte)ShaderParticleTypes.Molten] = new ShaderParticleTypeDefinition(
            Texture: AssetRegistry.GetTexture(AdditionsTexture.BrightLight),
            LayerTexture: AssetRegistry.InvisTex,
            Shader: ShaderRegistry.AdditiveFusableParticleEdgeShader,
            Update: static (ref ShaderParticle s) =>
            {
                ShaderParticleTypeDefinition def = TypeDefinitions[(byte)s.Type];
                s.Size = Vector2.Clamp(s.Size - new Vector2(0.24f), Vector2.Zero, Vector2.One * 200f) * 0.9956f;
                if (s.Size.Length() < 15f)
                    s.Size *= 0.95f - 0.9f;

                s.Color = def.EdgeColor * 1.2f;
                s.Color.B = (byte)(s.Color.B + (int)(MathF.Cos(s.Position.Y * 0.015f + Main.GlobalTimeWrappedHourly * 0.1f) * 3f));
                float brightnessInterpolant = InverseLerp(10f, 2f, s.Time) * 0.67f;
                s.Color = Color.Lerp(s.Color, Color.Wheat, brightnessInterpolant);
            },
            ShouldKill: static (ref ShaderParticle s) => s.Size.Length() <= 1,
            Draw: null,
            DrawLayer: ShaderParticleDrawLayers.OverPlayers,
            PrepareSB: static () =>
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.Default, Main.Rasterizer, null, Matrix.Identity);
            },
            PrepareShader: static (shader, target, type) =>
            {
                Vector2 renderTargetSize = Main.ScreenSize.ToVector2() / 2f;
                shader.TrySetParameter("screenArea", renderTargetSize);
                shader.TrySetParameter("layerOffset", Vector2.Zero);
                shader.TrySetParameter("singleFrameScreenOffset", Vector2.Zero);
                shader.Render();
            },
            LayerOffset: static () => Vector2.Zero,
            EdgeColor: new(255, 56, 3)
        );
    }

    public static void SpawnMoltenParticle(Vector2 position, float size)
    {
        ShaderParticle particle = new()
        {
            Position = position,
            Velocity = Vector2.Zero,
            Lifetime = 0,
            Time = 0,
            Opacity = 1f,
            Color = Color.White,
            Type = ShaderParticleTypes.Molten,
            Size = new(size, size),
            Rotation = 0
        };

        ShaderParticleSystem.Instance.AddParticle(particle);
    }
    #endregion

    #region Epidemic
    private static void EpidemicParticleDefinition()
    {
        TypeDefinitions[(byte)ShaderParticleTypes.Epidemic] = new ShaderParticleTypeDefinition(
            Texture: AssetRegistry.GetTexture(AdditionsTexture.BasicCircle),
            LayerTexture: AssetRegistry.GetTexture(AdditionsTexture.WavyBlotchNoise),
            Shader: ShaderRegistry.EdgeDetectionShader,
            Update: static (ref ShaderParticle s) =>
            {
                s.Size = Vector2.Clamp(s.Size - new Vector2(0.31f), Vector2.Zero, Vector2.One * 200f) * 0.9956f;
                s.Velocity *= .97f;
                s.Rotation += s.Velocity.Length() * .1f;
            },
            ShouldKill: static (ref ShaderParticle s) => s.Size.Length() <= .2f,
            Draw: null,
            DrawLayer: ShaderParticleDrawLayers.BeforeProjectiles,
            PrepareSB: null,
            PrepareShader: null,
            LayerOffset: static () => (Vector2.One * (float)Math.Cos(Main.GlobalTimeWrappedHourly * 0.041f) * 2f).RotatedBy((float)Math.Cos(Main.GlobalTimeWrappedHourly * 0.08f) * 0.97f),
            EdgeColor: Color.DarkOliveGreen
        );
    }

    public static void SpawnEpidemicParticle(Vector2 position, Vector2 velocity, float size)
    {
        ShaderParticle particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = 60,
            Time = 0,
            Opacity = 1f,
            Color = Color.DarkOliveGreen,
            Type = ShaderParticleTypes.Epidemic,
            Size = Vector2.One * size,
            Rotation = RandomRotation()
        };

        ShaderParticleSystem.Instance.AddParticle(particle);
    }
    #endregion

    #region Cosmic
    private static void CosmicParticleDefinition()
    {
        TypeDefinitions[(byte)ShaderParticleTypes.Cosmic] = new ShaderParticleTypeDefinition(
            Texture: AssetRegistry.GetTexture(AdditionsTexture.BasicCircularCircle),
            LayerTexture: AssetRegistry.GetTexture(AdditionsTexture.PurpleNebulaBright),
            Shader: ShaderRegistry.EdgeDetectionShader,
            Update: static (ref ShaderParticle s) =>
            {
                s.Size *= .97f;
                s.Velocity *= .98f;
            },
            ShouldKill: static (ref ShaderParticle s) => s.Size.Length() <= 0.5f,
            Draw: null,
            DrawLayer: ShaderParticleDrawLayers.BeforeProjectiles,
            PrepareSB: null,
            PrepareShader: null,
            LayerOffset: static () => Vector2.UnitX * Main.GlobalTimeWrappedHourly * 0.03f,
            EdgeColor: Color.BlueViolet
        );
    }

    public static void SpawnCosmicParticle(Vector2 position, Vector2 velocity, float size)
    {
        ShaderParticle particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = 60,
            Time = 0,
            Opacity = 1f,
            Color = Color.White,
            Type = ShaderParticleTypes.Cosmic,
            Size = new(size, size),
            Rotation = RandomRotation()
        };

        ShaderParticleSystem.Instance.AddParticle(particle);
    }
    #endregion
}