using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Common.Particles.Shader;

public abstract class ShaderParticle : ModType
{
    public struct ParticleData
    {
        public int Time;
        public int Lifetime;
        public Vector2 Position;
        public Vector2 Velocity;
        public Color Color;
        public float Rotation;
        public Vector2 Scale;

        public ParticleData(Vector2 position, Vector2 vel, int life, Vector2 scale, Color color, float rotation = 1f)
        {
            Time = 0;
            Lifetime = life;
            Position = position;
            Velocity = vel;
            Color = color;
            Rotation = rotation;
            Scale = scale;
        }
    }

    internal List<ParticleData> particles = [];

    public virtual void UpdateParticle(ref ParticleData particle, int index) { }

    protected int SpawnParticle(ParticleData data)
    {
        particles.Add(data);
        return particles.Count - 1;
    }

    protected void RemoveParticle(int index)
    {
        if (index >= 0 && index < particles.Count)
            particles.RemoveAt(index);
        return;
    }

    public abstract bool ShouldKill(ParticleData particle);

    public void GlobalUpdate()
    {
        FastParallel.For(0, particles.Count, (j, k, callback) =>
        {
            for (int i = j; i < k; i++)
            {
                var particle = particles[i];
                UpdateParticle(ref particle, i);
                particle.Time++;
                particles[i] = particle;
            }
        });

        for (int i = 0; i < particles.Count; i++)
            if (ShouldKill(particles[i]))
                RemoveParticle(i);
    }

    public const string Path = "TheExtraordinaryAdditions/Common/Particles/ShaderParticles/";


    internal List<ManagedRenderTarget> LayerTargets = [];

    /// <summary>
    /// Required utility that is used to determine whether this ShaderParticle has anything to draw.<br></br>
    /// This exists for efficiency, ensuring that as few operations are executed as possible when not required.
    /// </summary>
    public abstract bool Active
    {
        get;
    }

    /// <summary>
    /// The collection of all textures to draw on top of the ShaderParticle contents.
    /// </summary>
    public abstract IEnumerable<Texture2D> Layers
    {
        get;
    }

    /// <summary>
    /// The draw layer in which ShaderParticle should be drawn.
    /// </summary>
    public abstract ShaderParticleDrawLayer DrawContext
    {
        get;
    }

    /// <summary>
    /// The color that ShaderParticle should draw at the edge between air and particle contents.
    /// </summary>
    public abstract Color EdgeColor
    {
        get;
    }

    /// <summary>
    /// Whether the layer overlay contents from <see cref="Layers"/> should be fixed to the screen.<br></br>
    /// When true, the texture will be statically drawn to the screen with no respect for world position.
    /// </summary>
    public virtual bool FixedToScreen => false;

    /// <summary>
    /// Optionally overridable method for clearing particle instances as necessary. This is used automatically in contexts such as world unloads.
    /// </summary>
    public virtual void ClearInstances() { }

    /// <summary>
    /// Optionally overridable method that can be used to make layers offset when drawn, to allow for layer-specific animations. Defaults to <see cref="Vector2.Zero"/>, aka no animation.
    /// </summary>
    public virtual Vector2 CalculateManualOffsetForLayer(int layerIndex) => Vector2.Zero;

    /// <summary>
    /// Optionally overridable method that allows for <see cref="SpriteBatch"/> preparations prior to the drawing of the individual raw ShaderParticle instances <i>(Not the final result)</i>.<br></br>
    /// An example of this could be having the ShaderParticle particles drawn with <see cref="BlendState.Additive"/>.
    /// </summary>
    /// <param name="spriteBatch">Shorthand parameter for <see cref="Main.spriteBatch"/>.</param>
    public virtual void PrepareSpriteBatch(SpriteBatch spriteBatch) { }

    /// <summary>
    /// Optionally overridable method that defines for preparations for the render target. Defaults to using the typical texture overlay behavior.
    /// </summary>
    /// <param name="layerIndex">The layer index that should be prepared for.</param>
    public virtual void PrepareShaderForTarget(int layerIndex)
    {
        // Store the in an easy to use local variables.
        ManagedShader shader = ShaderRegistry.EdgeDetectionShader;

        // Fetch the layer texture. This is the texture that will be overlayed over the greyscale contents on the screen.
        Texture2D layerTexture = Layers.ElementAt(layerIndex);

        // Calculate the layer scroll offset. This is used to ensure that the texture contents of the given ShaderParticle have parallax, rather than being static over the screen
        // regardless of world position.
        // This may be toggled off optionally by the ShaderParticle.
        Vector2 screenSize = Main.ScreenSize.ToVector2() / 2f;
        Vector2 layerScrollOffset = Main.screenPosition / screenSize + CalculateManualOffsetForLayer(layerIndex);
        if (FixedToScreen)
            layerScrollOffset = Vector2.Zero;

        // Supply shader parameter values.
        shader.TrySetParameter("layerSize", layerTexture.Size());
        shader.TrySetParameter("screenSize", screenSize);
        shader.TrySetParameter("layerOffset", layerScrollOffset);
        shader.TrySetParameter("edgeColor", EdgeColor.ToVector4());
        shader.TrySetParameter("singleFrameScreenOffset", (Main.screenLastPosition - Main.screenPosition) / screenSize / 2);

        // Supply the ShaderParticle's layer texture to the graphics device so that the shader can read it.
        shader.SetTexture(layerTexture, 1, SamplerState.LinearWrap);

        // Apply the ShaderParticle shader.
        shader.Render();
    }

    /// <summary>
    /// Required overridable that is intended to draw all ShaderParticle instances. <br></br>
    /// <b>You must half the scale and draw position to fit in with the render target</b>
    /// </summary>
    public abstract void DrawInstances();

    public sealed override void Register()
    {
        ModTypeLookup<ShaderParticle>.Register(this);
        if (!ShaderParticleManager.ShaderParticle.Contains(this))
            ShaderParticleManager.ShaderParticle.Add(this);

        if (Main.netMode == NetmodeID.Server)
            return;

        Main.QueueMainThreadAction(() =>
        {
            int layerCount = Layers.Count();
            for (int i = 0; i < layerCount; i++)
            {
                // Create render targets at half resolution
                LayerTargets.Add(new ManagedRenderTarget(true, (w, h) => new RenderTarget2D(
                    Main.graphics.GraphicsDevice, w / 2, h / 2)));
            }
        });
    }

    /// <summary>
    /// Disposes of all unmanaged GPU resources used up by the <see cref="LayerTargets"/>. This is called automatically on mod unload.<br></br>
    /// <i>It is your responsibility to recreate layer targets later if you call this method manually.</i>
    /// </summary>
    public void Dispose()
    {
        for (int i = 0; i < LayerTargets.Count; i++)
            LayerTargets[i]?.Dispose();
    }
}

public enum ShaderParticleDrawLayer
{
    BeforeNPCs,
    AfterProjectiles,
    OverPlayers,
}

public class ShaderParticleManager : ModSystem
{
    internal static readonly List<ShaderParticle> ShaderParticle = [];

    public override void OnModLoad()
    {
        // Prepare event subscribers.
        RenderTargetManager.RenderTargetUpdateLoopEvent += PrepareShaderParticleTargets;
        On_Main.DrawProjectiles += DrawParticlesAfterProjectiles;
        On_Main.DrawNPCs += DrawParticlesBeforeNPCs;
        On_Main.DrawPlayers_AfterProjectiles += DrawParticlesOverPlayers;
    }

    public override void OnModUnload()
    {
        // Clear all unmanaged ShaderParticle target resources on the GPU on mod unload.
        Main.QueueMainThreadAction(() =>
        {
            foreach (ShaderParticle ShaderParticle in ShaderParticle)
                ShaderParticle?.Dispose();
            On_Main.DrawProjectiles -= DrawParticlesAfterProjectiles;
            On_Main.DrawNPCs -= DrawParticlesBeforeNPCs;
            On_Main.DrawPlayers_AfterProjectiles -= DrawParticlesOverPlayers;
        });
    }

    public override void OnWorldUnload()
    {
        foreach (ShaderParticle particle in ShaderParticle)
            particle.ClearInstances();
    }

    private void PrepareShaderParticleTargets()
    {
        // Get a list of all ShaderParticle currently in use.
        List<ShaderParticle> activeShaderParticle = ShaderParticle.Where(m => m.Active).ToList();

        // Don't bother wasting resources if particles are not in use at the moment.
        if (activeShaderParticle.Count == 0)
            return;

        // Prepare the sprite batch for drawing. Particles may restart the sprite batch via PrepareSpriteBatch if their implementation requires it.
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, null, Matrix.Identity);

        var gd = Main.instance.GraphicsDevice;
        foreach (ShaderParticle particle in activeShaderParticle)
        {
            // Update the particle collection.
            if (!Main.gamePaused)
                particle.GlobalUpdate();

            // Prepare the sprite batch in accordance to the needs of the particle instance. By default this does nothing, 
            particle.PrepareSpriteBatch(Main.spriteBatch);

            // Draw the raw contents of the particle to each of its render targets.
            foreach (ManagedRenderTarget target in particle.LayerTargets)
            {
                gd.SetRenderTarget(target);
                gd.Clear(Color.Transparent);
                particle.DrawInstances();

                // Flush particle contents to its render target and reset the sprite batch for the next iteration.
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, null, Matrix.Identity);
            }
        }

        // Return to the backbuffer and end the sprite batch.
        gd.SetRenderTarget(null);
        Main.spriteBatch.End();
    }

    private static void DrawParticlesAfterProjectiles(On_Main.orig_DrawProjectiles orig, Main self)
    {
        orig(self);

        if (AnyActiveShaderParticleAtLayer(ShaderParticleDrawLayer.AfterProjectiles))
        {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            DrawShaderParticle(ShaderParticleDrawLayer.AfterProjectiles);
            Main.spriteBatch.End();
        }
    }

    private static void DrawParticlesBeforeNPCs(On_Main.orig_DrawNPCs orig, Main self, bool behindTiles)
    {
        if (!behindTiles && AnyActiveShaderParticleAtLayer(ShaderParticleDrawLayer.BeforeNPCs))
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            DrawShaderParticle(ShaderParticleDrawLayer.BeforeNPCs);
            Main.spriteBatch.ResetToDefault();
        }
        orig(self, behindTiles);
    }

    private static void DrawParticlesOverPlayers(On_Main.orig_DrawPlayers_AfterProjectiles orig, Main self)
    {
        orig(self);
        if (AnyActiveShaderParticleAtLayer(ShaderParticleDrawLayer.OverPlayers))
        {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            DrawShaderParticle(ShaderParticleDrawLayer.OverPlayers);
            Main.spriteBatch.End();
        }
    }

    /// <summary>
    /// Checks if a ShaderParticle of a given layering type is currently in use. This is used to minimize needless <see cref="SpriteBatch"/> restarts when there is nothing to draw.
    /// </summary>
    /// <param name="layerType">The ShaderParticle layer type to check against.</param>
    internal static bool AnyActiveShaderParticleAtLayer(ShaderParticleDrawLayer layerType) =>
        ShaderParticle.Any(m => m.Active && m.DrawContext == layerType);

    /// <summary>
    /// Draws all ShaderParticle of a given <see cref="ShaderParticleDrawLayer"/>. Used for layer ordering reasons.
    /// </summary>
    /// <param name="layerType">The layer type to draw with.</param>
    public static void DrawShaderParticle(ShaderParticleDrawLayer layerType)
    {
        foreach (ShaderParticle shaderParticle in ShaderParticle.Where(m => m.DrawContext == layerType && m.Active))
        {
            for (int i = 0; i < shaderParticle.LayerTargets.Count; i++)
            {
                shaderParticle.PrepareShaderForTarget(i);

                Main.spriteBatch.Draw(shaderParticle.LayerTargets[i], Main.screenLastPosition - Main.screenPosition, null, Color.White, 0f, Vector2.Zero, 2f, 0, 0f);
            }
        }
    }
}