using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Config;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Vector4 = Microsoft.Xna.Framework.Vector4;

namespace TheExtraordinaryAdditions.Core.Graphics.Resources;

public class ManagedScreenShader : IDisposable
{
    /// <summary>
    /// All deferred textures that should be applied when the shader is applied
    /// </summary>
    private readonly Dictionary<int, DeferredTexture> _deferredTextures = [];

    /// <summary>
    /// A managed copy of all parameter data. Used to minimize excess SetValue calls, in cases where the value aren't actually being changed
    /// </summary>
    internal readonly Dictionary<string, object> ParameterCache;

    public Ref<Effect> Shader { get; internal set; }

    public Effect WrappedEffect => Shader.Value;

    public bool Disposed { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>
    /// The standard parameter name prefix for texture sizes
    /// </summary>
    public const string TextureSizeParameterPrefix = "textureSize";

    /// <summary>
    /// Represents a texture that is supplied to a filter when its shader is ready to be applied
    /// </summary>
    /// <param name="texture">The texture to use</param>
    /// <param name="index">The index in the <see cref="GraphicsDevice.Textures"/> array that the texture should go in</param>
    /// <param name="samplerState">An optional sampler state that should be used alongside the texture. Does nothing if <see langword="null"/></param>
    public readonly struct DeferredTexture(Texture2D texture, int index, SamplerState samplerState)
    {
        public readonly Texture2D Texture = texture;
        public readonly int Index = index;
        public readonly SamplerState SamplerState = samplerState;
    }

    /// <summary>
    /// A wrapper class for <see cref="Effect"/> that is focused around screen filter effects.
    /// </summary>
    internal ManagedScreenShader(Ref<Effect> shader)
    {
        Shader = shader;

        // Initialize the parameter cache.
        ParameterCache = [];
    }

    internal bool ParameterIsCachedAsValue(string parameterName, object value)
    {
        // If the parameter cache has not registered this parameter yet, that means it can't have changed, because there's nothing to compare against
        if (!ParameterCache.TryGetValue(parameterName, out object parameter))
            return false;

        return parameter?.Equals(value) ?? false;
    }

    /// <summary>
    /// Attempts to send parameter data to the GPU for the filter to use
    /// </summary>
    /// <param name="parameterName">The name of the parameter. This must correspond with the parameter name in the filter</param>
    /// <param name="value">The value to supply to the parameter</param>
    public bool TrySetParameter(string parameterName, object value)
    {
        // Shaders do not work on servers
        if (Main.netMode == NetmodeID.Server)
            return false;

        // Check if the parameter even exists
        EffectParameter parameter = Shader.Value.Parameters[parameterName];
        if (parameter is null)
            return false;

        if (ParameterIsCachedAsValue(parameterName, value))
            return false;

        // Store the value in the cache
        ParameterCache[parameterName] = value;

        // There isn't really a good way to sent stuff over to the GPU due to no simple type numbers can be converted to
        // So until something better appears this switch will do
        try
        {
            switch (value)
            {
                case bool b:
                    parameter.SetValue(b);
                    return true;
                case bool[] b2:
                    parameter.SetValue(b2);
                    return true;
                case int i:
                    parameter.SetValue(i);
                    return true;
                case int[] i2:
                    parameter.SetValue(i2);
                    return true;
                case float f:
                    parameter.SetValue(f);
                    return true;
                case float[] f2:
                    parameter.SetValue(f2);
                    return true;
                case Vector2 v2:
                    parameter.SetValue(v2);
                    return true;
                case Vector2[] v22:
                    parameter.SetValue(v22);
                    return true;
                case Vector3 v3:
                    parameter.SetValue(v3);
                    return true;
                case Vector3[] v32:
                    parameter.SetValue(v32);
                    return true;
                case Color c:
                    parameter.SetValue(c.ToVector3());
                    return true;
                case Vector4 v4:
                    parameter.SetValue(v4);
                    return true;
                case Rectangle rect:
                    parameter.SetValue(new Vector4(rect.X, rect.Y, rect.Width, rect.Height));
                    return true;
                case Vector4[] v42:
                    parameter.SetValue(v42);
                    return true;
                case Matrix m:
                    parameter.SetValue(m);
                    return true;
                case Matrix[] m2:
                    parameter.SetValue(m2);
                    return true;
                case Texture2D t:
                    parameter.SetValue(t);
                    return true;
                default:
                    return false;
            }
        }
        catch
        {
            AdditionsMain.Instance.Logger.Error(
                $"ruh roh the shader {Shader.Value.Name} tried to set a parameter with an unsupported type of {value.GetType().Name}");
            return false;
        }
    }

    /// <summary>
    /// Sets a texture at a given index for this shader to use <br></br>
    /// Remember, index 0 may be populated with whatever was passed into a <see cref="SpriteBatch"/>.Draw call
    /// </summary>
    /// <param name="texture">The texture to supply</param>
    /// <param name="textureIndex">The index to place the texture in</param>
    /// <param name="samplerStateOverride">Which sampler should be used for the texture</param>
    public void SetTexture(Texture2D texture, int textureIndex, SamplerState samplerStateOverride = null)
    {
        if (Main.netMode == NetmodeID.Server)
            return;

        DeferredTexture deferredTexture = new(texture, textureIndex, samplerStateOverride);
        _deferredTextures[textureIndex] = deferredTexture;
    }

    /// <summary>
    /// Call to indicate that the filter should be active. This needs to happen each frame it should be active for
    /// </summary>
    public void Activate() => IsActive = true;

    /// <summary>
    /// Automatically called at the end of each update, after updating the filter
    /// </summary>
    public void Deactivate() => IsActive = false;

    /// <summary>
    /// Apply the filter
    /// </summary>
    /// <param name="setCommonParams">If true, this will automatically try to set certain parameters in the shader, such as globalTime</param>
    /// <param name="pass">Specify a specific pass to use, if the shader has multiple</param>
    public void Apply(bool setCommonParams = true, string pass = null)
    {
        if (setCommonParams)
        {
            TrySetParameter("globalTime", Main.GlobalTimeWrappedHourly);
            TrySetParameter("screenPosition", Main.screenPosition);
            TrySetParameter("screenSize", new Vector2(Main.screenWidth, Main.screenHeight));
            TrySetParameter("zoom", Main.GameZoomTarget);
        }

        GraphicsDevice gd = Main.instance.GraphicsDevice;
        foreach (DeferredTexture textureWrapper in _deferredTextures.Values)
        {
            int textureIndex = textureWrapper.Index;
            Texture2D texture = textureWrapper.Texture;
            SamplerState samplerStateOverride = textureWrapper.SamplerState;
            WrappedEffect.Parameters[$"{TextureSizeParameterPrefix}{textureIndex}"]?.SetValue(texture.Size());

            gd.Textures[textureIndex] = texture;
            if (samplerStateOverride is not null)
                gd.SamplerStates[textureIndex] = samplerStateOverride;
        }

        WrappedEffect.CurrentTechnique.Passes[pass ?? ManagedShader.DefaultPassName].Apply();
    }

    public void Dispose()
    {
        if (Disposed)
            return;

        Disposed = true;
        Main.QueueMainThreadAction(Shader.Value.Dispose);
        ParameterCache.Clear();
        GC.SuppressFinalize(this);
    }
}

[Autoload(Side = ModSide.Client)]
public sealed class ScreenShaderUpdates : ModSystem
{
    public static ManagedRenderTarget MainTarget { get; private set; }
    public static ManagedRenderTarget AuxiliaryTarget { get; private set; }

    private static readonly List<ManagedScreenShader> ActiveFilters = [];
    private static readonly List<ManagedScreenShader> BlurParticles = [];
    private static readonly List<ManagedScreenShader> ChromaticAberrationParticles = [];
    private static readonly List<ManagedScreenShader> FlashParticles = [];
    private static readonly List<ManagedScreenShader> ShockwaveParticles = [];
    private static readonly List<DrawAction> DrawActions = new(9000);

    public static void QueueDrawAction(Action renderAction, BlendState blendState = null, ManagedShader effect = null,
        string groupID = null)
    {
        ArgumentNullException.ThrowIfNull(renderAction);
        BlendState blend = blendState ?? BlendState.AlphaBlend;
        DrawAction action =
            new DrawAction(renderAction, blend, isTexture: true, effect, groupID); // Treat as texture for consistency
        DrawActions.Add(action);
    }

    private readonly struct ParticleShaderHandler(
        ParticleTypes type,
        string shaderName,
        List<ManagedScreenShader> shaderList)
    {
        public readonly ParticleTypes ParticleType = type;
        public readonly string ShaderName = shaderName;
        public readonly List<ManagedScreenShader> ShaderList = shaderList;

        public void ProcessParticle(ref ParticleData particle)
        {
            if (!particle.Active || particle.Type != ParticleType)
                return;

            ManagedScreenShader shader = ScreenShaderPool.GetShader(ShaderName);
            ConfigureCommonParameters(shader, ref particle);
            ConfigureSpecificParameters(shader, ref particle);
            shader.Activate();
            ShaderList.Add(shader);
        }

        private static void ConfigureCommonParameters(ManagedScreenShader shader, ref ParticleData particle)
        {
            shader.TrySetParameter("intensity", particle.Opacity * AdditionsConfigClient.Instance.VisualIntensity);
            shader.TrySetParameter("screenPos", GetTransformedScreenCoords(particle.Position));
            shader.TrySetParameter("screenSize", new Vector2(Main.screenWidth, Main.screenHeight));
            shader.TrySetParameter("zoom", Main.GameViewMatrix.Zoom.X);
            shader.TrySetParameter("radius", particle.Scale / Main.screenWidth);
        }

        private void ConfigureSpecificParameters(ManagedScreenShader shader, ref ParticleData particle)
        {
            switch (ParticleType)
            {
                case ParticleTypes.Blur:
                    ref ParticleRegistry.BlurParticleData blurData =
                        ref particle.GetCustomData<ParticleRegistry.BlurParticleData>();
                    shader.TrySetParameter("falloffSigma", blurData.Sigma);
                    break;
                case ParticleTypes.ChromaticAberration:
                    ref ParticleRegistry.ChromaticAberrationData aberrationData =
                        ref particle.GetCustomData<ParticleRegistry.ChromaticAberrationData>();
                    shader.TrySetParameter("falloffSigma", aberrationData.Sigma);
                    break;
                case ParticleTypes.Flash:
                    ref ParticleRegistry.FlashParticleData flashData =
                        ref particle.GetCustomData<ParticleRegistry.FlashParticleData>();
                    shader.TrySetParameter("falloffSigma", flashData.Sigma);
                    break;
                case ParticleTypes.Shockwave:
                    ref ParticleRegistry.ShockwaveParticleData shockData =
                        ref particle.GetCustomData<ParticleRegistry.ShockwaveParticleData>();
                    shader.TrySetParameter("frequency", shockData.Frequency);
                    shader.TrySetParameter("chromatic", shockData.Chromatic);
                    shader.TrySetParameter("ringSize", shockData.RingSize);
                    break;
            }
        }
    }

    private static readonly ParticleShaderHandler[] ParticleHandlers =
    [
        new ParticleShaderHandler(ParticleTypes.Blur, "BlurFilter", BlurParticles),
        new ParticleShaderHandler(ParticleTypes.ChromaticAberration, "ChromaticAberration",
            ChromaticAberrationParticles),
        new ParticleShaderHandler(ParticleTypes.Flash, "FlashFilter", FlashParticles),
        new ParticleShaderHandler(ParticleTypes.Shockwave, "ShockwaveShader", ShockwaveParticles)
    ];

    public override void OnModLoad()
    {
        if (Main.netMode == NetmodeID.Server)
            return;

        Main.QueueMainThreadAction(static () =>
        {
            MainTarget = new ManagedRenderTarget(true, ManagedRenderTarget.CreateScreenSizedTarget);
            AuxiliaryTarget = new ManagedRenderTarget(true, ManagedRenderTarget.CreateScreenSizedTarget);
        });
    }

    public override void OnModUnload()
    {
        if (Main.netMode == NetmodeID.Server)
            return;

        Main.QueueMainThreadAction(static () =>
        {
            MainTarget.Dispose();
            AuxiliaryTarget.Dispose();
        });
    }

    internal static void ApplyScreenFilters(RenderTarget2D _, RenderTarget2D screenTarget1, RenderTarget2D _2,
        Color clearColor)
    {
        RenderTarget2D target1 = null;
        RenderTarget2D target2 = screenTarget1;

        // Handle gravity flipping
        if ((int) Main.player[Main.myPlayer].gravDir == -1)
        {
            target1 = AuxiliaryTarget;
            Main.instance.GraphicsDevice.SetRenderTarget(target1);
            Main.instance.GraphicsDevice.Clear(clearColor);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.Default, RasterizerState.CullNone, null,
                Matrix.Invert(Main.GameViewMatrix.EffectMatrix));
            Main.spriteBatch.Draw(target2, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 1f,
                SpriteEffects.FlipVertically, 0f);
            Main.spriteBatch.End();
            target2 = AuxiliaryTarget;
        }

        // Clear particle shader lists
        foreach (ParticleShaderHandler handler in ParticleHandlers)
            handler.ShaderList.Clear();

        // Process particles
        foreach (int index in ParticleSystem.ActiveParticles)
        {
            ref ParticleData particle = ref ParticleSystem.Instance.GetParticles()[index];
            foreach (ParticleShaderHandler handler in ParticleHandlers)
                handler.ProcessParticle(ref particle);
        }

        // Collect all active filters
        ActiveFilters.Clear();
        foreach (ManagedScreenShader filter in AssetRegistry.GennedShaders.Filters.Values)
        {
            if (filter.IsActive)
                ActiveFilters.Add(filter);
        }

        ActiveFilters.AddRange(ActiveShaders);
        foreach (ParticleShaderHandler handler in ParticleHandlers)
            ActiveFilters.AddRange(handler.ShaderList);

        // Apply filters
        foreach (ManagedScreenShader filter in ActiveFilters)
        {
            target1 = (target2 != MainTarget.Target) ? MainTarget : AuxiliaryTarget;
            Main.instance.GraphicsDevice.SetRenderTarget(target1);
            Main.instance.GraphicsDevice.Clear(clearColor);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            filter.Apply(false);
            Main.spriteBatch.Draw(target2, Vector2.Zero, Main.ColorOfTheSkies);
            Main.spriteBatch.End();
            target2 = (target2 != MainTarget.Target) ? MainTarget : AuxiliaryTarget;
        }

        // Render final output
        if (target1 != null)
        {
            Main.instance.GraphicsDevice.SetRenderTarget(screenTarget1);
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            Main.spriteBatch.Draw(target1, Vector2.Zero, Color.White);
            Main.spriteBatch.End();
        }

        // Check if there is any draw actions for objects requesting to draw over screen shaders
        List<DrawAction> actions = DrawActions;
        if (actions.Count != 0)
        {
            SpriteBatch sb = Main.spriteBatch;
            Span<DrawAction> actionSpan = CollectionsMarshal.AsSpan(actions);
            DrawActionGrouper.GroupByBlendAndGroupId(actionSpan, [], (blend, groupGroups) =>
            {
                foreach (KeyValuePair<string, List<DrawAction>> groupEntry in groupGroups)
                {
                    string groupId = groupEntry.Key;
                    List<DrawAction> groupActions = groupEntry.Value;

                    if (groupId == DrawActionGrouper.UngroupedKey)
                    {
                        // Ungrouped: each action gets its own batch to respect shader parameters
                        foreach (DrawAction action in groupActions)
                        {
                            if (action.RenderAction == null)
                                continue;

                            sb.Begin(SpriteSortMode.Deferred, action.Blend, Main.DefaultSamplerState,
                                DepthStencilState.None, RasterizerState.CullNone, action.Shader?.Effect,
                                Main.GameViewMatrix.TransformationMatrix);
                            action.Shader?.Render();
                            action.RenderAction();
                            sb.End();
                        }
                    }
                    else if (groupActions.Count == 1)
                    {
                        // Single grouped action: treat like ungrouped for safety
                        DrawAction action = groupActions[0];
                        if (action.RenderAction == null)
                            continue;

                        sb.Begin(SpriteSortMode.Deferred, action.Blend, Main.DefaultSamplerState,
                            DepthStencilState.None, RasterizerState.CullNone, action.Shader?.Effect,
                            Main.GameViewMatrix.TransformationMatrix);
                        action.Shader?.Render();
                        action.RenderAction();
                        sb.End();
                    }
                    else
                    {
                        // Grouped: batch actions with same GroupId, assuming shared shader parameters
                        ManagedShader sharedShader = groupActions[0].Shader;
                        sb.Begin(SpriteSortMode.Deferred, blend, Main.DefaultSamplerState, DepthStencilState.None,
                            RasterizerState.CullNone, sharedShader?.Effect, Main.GameViewMatrix.TransformationMatrix);
                        foreach (DrawAction action in groupActions)
                        {
                            if (action.RenderAction == null)
                                continue;

                            // Only call Render if shader matches to avoid parameter conflicts
                            if (action.Shader == sharedShader)
                                action.Shader?.Render();
                            action.RenderAction();
                        }

                        sb.End();
                    }
                }
            });

            // Clear actions and return to pool
            actions.Clear();
        }

        // Return shaders to pool
        foreach (ParticleShaderHandler handler in ParticleHandlers)
        {
            foreach (ManagedScreenShader shader in handler.ShaderList)
                ScreenShaderPool.ReturnShader(handler.ShaderName, shader);
        }

        for (int i = ShaderEntities.Count - 1; i >= 0; i--)
        {
            IHasScreenShader entity = ShaderEntities[i];
            if (entity.IsEntityActive())
                continue;

            if (entity.HasShader)
                entity.ReleaseShader();
        }
    }

    public override void ClearWorld()
    {
        for (int i = ShaderEntities.Count - 1; i >= 0; i--)
        {
            IHasScreenShader entity = ShaderEntities[i];
            entity.ReleaseShader();
        }
    }

    private static readonly HashSet<ManagedScreenShader> ActiveShaders = [];
    public static readonly List<IHasScreenShader> ShaderEntities = [];

    public static void RegisterEntity(IHasScreenShader entity)
    {
        if (!ShaderEntities.Contains(entity))
            ShaderEntities.Add(entity);
    }

    public static void UnregisterEntity(IHasScreenShader entity)
    {
        ShaderEntities.Remove(entity);
    }

    public override void PostUpdateEverything()
    {
        if (Main.netMode == NetmodeID.Server)
            return;

        ActiveShaders.Clear();
        foreach (ManagedScreenShader filter in AssetRegistry.GennedShaders.Filters.Values)
            filter.Deactivate();

        foreach (IHasScreenShader entity in CollectionsMarshal.AsSpan(ShaderEntities))
        {
            if (entity.HasShader && entity.Shader != null && entity.Shader.IsActive)
                ActiveShaders.Add(entity.Shader);
        }
    }

    public override void Unload()
    {
        ScreenShaderPool.Unload();
    }
}

public sealed class ScreenModifierManager : ModSystem
{
    private record ScreenModifierInfo(ScreenTargetModifierDelegate Info, byte Layer);

    public delegate void ScreenTargetModifierDelegate(RenderTarget2D finalTexture, RenderTarget2D screenTarget1,
        RenderTarget2D screenTarget2, Color clearColor);

    private static List<ScreenModifierInfo> _screenModifiers;

    /// <summary>
    /// The layer of screen filters in the modifiers
    /// </summary>
    public const byte FilterLayer = 200;

    public override void Load()
    {
        On_FilterManager.EndCapture += EndCaptureDetour;
        _screenModifiers = [];
        RegisterScreenModifier(ScreenShaderUpdates.ApplyScreenFilters, FilterLayer);
    }

    public override void Unload()
    {
        On_FilterManager.EndCapture -= EndCaptureDetour;
        _screenModifiers.Clear();
    }

    /// <summary>
    /// Call to register a screen modifier delegate at the provided layer. Each registered modifier is ran in ascending layer order
    /// </summary>
    public static void RegisterScreenModifier(ScreenTargetModifierDelegate screenTargetModifierDelegate, byte layer)
    {
        if (Main.dedServ)
            return;

        _screenModifiers.Add(new(screenTargetModifierDelegate, layer));

        if (_screenModifiers.Count > 1)
            _screenModifiers = [.. _screenModifiers.OrderBy(element => element.Layer)];
    }

    private void EndCaptureDetour(On_FilterManager.orig_EndCapture orig, FilterManager self,
        RenderTarget2D finalTexture, RenderTarget2D screenTarget1, RenderTarget2D screenTarget2, Color clearColor)
    {
        foreach (ScreenModifierInfo screenModifier in _screenModifiers)
            screenModifier.Info(finalTexture, screenTarget1, screenTarget2, clearColor);

        orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
    }
}

// This Provokes Evil
public static class ScreenShaderPool
{
    private static readonly Dictionary<string, Queue<ManagedScreenShader>> ShaderPools = [];
    public static readonly Dictionary<string, Ref<Effect>> BaseEffects = [];

    public static void InitializePool(string filterName, Ref<Effect> baseEffect, int initialCapacity = 10)
    {
        if (Main.netMode == NetmodeID.Server || ShaderPools.ContainsKey(filterName))
            return;

        BaseEffects[filterName] = baseEffect;
        ShaderPools[filterName] = new Queue<ManagedScreenShader>();

        for (int i = 0; i < initialCapacity; i++)
        {
            Effect clonedEffect = baseEffect.Value.Clone();
            ShaderPools[filterName].Enqueue(new ManagedScreenShader(new Ref<Effect>(clonedEffect)));
        }
    }

    public static ManagedScreenShader GetShader(string filterName)
    {
        if (!ShaderPools.TryGetValue(filterName, out Queue<ManagedScreenShader> pool) || pool.Count == 0)
        {
            if (!BaseEffects.TryGetValue(filterName, out Ref<Effect> baseEffect))
            {
                AdditionsMain.Instance.Logger.Error(
                    $"Shader '{filterName}' not found in BaseEffects. Available: {string.Join(", ", BaseEffects.Keys)}");
                throw new ArgumentException($"No pool initialized for filter '{filterName}'.");
            }

            Effect clonedEffect = baseEffect.Value.Clone();
            return new ManagedScreenShader(new Ref<Effect>(clonedEffect));
        }

        ManagedScreenShader shader = pool.Dequeue();
        shader.Deactivate();
        shader.ParameterCache.Clear();
        return shader;
    }

    public static void ReturnShader(string filterName, ManagedScreenShader shader)
    {
        if (Main.netMode == NetmodeID.Server ||
            !ShaderPools.TryGetValue(filterName, out Queue<ManagedScreenShader> pool))
            return;

        shader.Deactivate();
        shader.ParameterCache.Clear();
        pool.Enqueue(shader);
    }

    public static void Unload()
    {
        foreach (Queue<ManagedScreenShader> pool in ShaderPools.Values)
        {
            while (pool.Count > 0)
            {
                ManagedScreenShader shader = pool.Dequeue();
                shader.Dispose();
            }
        }

        ShaderPools.Clear();
        BaseEffects.Clear();
    }
}

public interface IHasScreenShader
{
    /// <summary>
    /// The shader itself
    /// </summary>
    ManagedScreenShader Shader { get; }

    /// <summary>
    /// Should be used to dictate when to activate/deactivate the shader
    /// </summary>
    bool HasShader { get; }

    /// <summary>
    /// Update all the screen shaders parameters
    /// </summary>
    void UpdateShader();

    // boilerplate, probably should fix this?
    /// <summary>
    /// Register the shader into <see cref="ScreenShaderPool"/>
    /// </summary>
    void InitializeShader();

    /// <summary>
    /// Unregister the shader
    /// </summary>
    void ReleaseShader();

    /// <summary>
    /// Used to tell <see cref="ScreenShaderUpdates"/> when to remove a object in case it failed to call <see cref="ReleaseShader"/> <br></br>
    /// Most notably seen when something like a projectile gets removed with setting active to false and its <see cref="ModProjectile.OnKill(int)"/> not being called
    /// </summary>
    /// <returns>Whether this object is successfully updating in-game</returns>
    bool IsEntityActive();
}
