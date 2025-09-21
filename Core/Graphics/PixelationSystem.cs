using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using static TheExtraordinaryAdditions.Core.Graphics.ManagedRenderTarget;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace TheExtraordinaryAdditions.Core.Graphics;

[Flags]
public enum PixelationLayer : byte
{
    UnderPlayers = 1 << 0,
    HeldProjectiles = 1 << 1,
    OverPlayers = 1 << 2,
    UnderNPCs = 1 << 3,
    OverNPCs = 1 << 4,
    UnderProjectiles = 1 << 5,
    OverProjectiles = 1 << 6,
    Dusts = 1 << 7
}

public static class DrawActionPool
{
    public static readonly Stack<DrawAction> Pool = new();
    public static readonly int InitialCapacity = Main.maxProjectiles + Main.maxNPCs + Main.maxPlayers + 15000;

    static DrawActionPool()
    {
        for (int i = 0; i < InitialCapacity; i++)
            Pool.Push(default);
    }

    public static DrawAction Rent(Action renderAction, BlendState blend, bool isTexture, ManagedShader effect = null, object groupId = null)
    {
        DrawAction action = Pool.Count > 0 ? Pool.Pop() : default;
        return new DrawAction(renderAction, blend, isTexture, effect, groupId);
    }

    public static void Return()
    {
        Pool.Push(default);
    }
}

public readonly record struct DrawAction
{
    public Action RenderAction { get; init; }
    public BlendState Blend { get; init; }
    public ManagedShader Shader { get; init; }
    public object GroupId { get; init; }
    public bool IsTexture { get; init; }

    public DrawAction(Action renderAction, BlendState blend, bool isTexture, ManagedShader effect = null, object groupId = null)
    {
        RenderAction = renderAction;
        Blend = blend;
        Shader = effect;
        GroupId = groupId;
        IsTexture = isTexture;
    }
}

public static class DrawActionGrouper
{
    private static readonly Dictionary<BlendState, Dictionary<object, List<DrawAction>>> BlendGroupGroups = [];
    private static readonly Dictionary<BlendState, List<DrawAction>> BlendFallback = [];
    private static readonly object UngroupedSentinel = new();
    private static readonly List<DrawAction>[] GroupListPool = new List<DrawAction>[32];
    private static int groupListPoolIndex = 0;

    static DrawActionGrouper()
    {
        var supportedBlendStates = new[] { BlendState.AlphaBlend, BlendState.Additive, BlendState.NonPremultiplied };
        foreach (var blend in supportedBlendStates)
        {
            BlendGroupGroups[blend] = [];
            BlendFallback[blend] = [];
        }
        for (int i = 0; i < GroupListPool.Length; i++)
            GroupListPool[i] = [];
    }

    private static List<DrawAction> RentGroupList()
    {
        if (groupListPoolIndex < GroupListPool.Length)
        {
            var list = GroupListPool[groupListPoolIndex++];
            list.Clear();
            return list;
        }
        return [];
    }

    private static void ResetGroupListPool()
    {
        groupListPoolIndex = 0;
    }

    public static void GroupByBlendAndGroupId(ReadOnlySpan<DrawAction> primitiveActions, ReadOnlySpan<DrawAction> textureActions, Action<BlendState, Dictionary<object, List<DrawAction>>> processBlendGroup)
    {
        ResetGroupListPool();

        // Clear previous frame's data
        foreach (var blendDict in BlendGroupGroups.Values)
        {
            foreach (var groupList in blendDict.Values)
                groupList.Clear();
            blendDict.Clear();
        }
        foreach (var blendList in BlendFallback.Values)
            blendList.Clear();

        // Group primitive actions
        foreach (var action in primitiveActions)
        {
            var blendDict = BlendGroupGroups[action.Blend];
            if (action.GroupId != null)
            {
                if (!blendDict.TryGetValue(action.GroupId, out var groupList))
                {
                    groupList = RentGroupList();
                    blendDict[action.GroupId] = groupList;
                }
                groupList.Add(action);
            }
            else
            {
                BlendFallback[action.Blend].Add(action);
            }
        }

        // Group texture actions
        foreach (var action in textureActions)
        {
            var blendDict = BlendGroupGroups[action.Blend];
            if (action.GroupId != null)
            {
                if (!blendDict.TryGetValue(action.GroupId, out var groupList))
                {
                    groupList = RentGroupList();
                    blendDict[action.GroupId] = groupList;
                }
                groupList.Add(action);
            }
            else
            {
                BlendFallback[action.Blend].Add(action);
            }
        }

        // Process grouped actions
        foreach (var blendEntry in BlendGroupGroups)
        {
            if (blendEntry.Value.Count > 0)
                processBlendGroup(blendEntry.Key, blendEntry.Value);
        }

        // Process ungrouped actions
        foreach (var blendEntry in BlendFallback)
        {
            if (blendEntry.Value.Count > 0)
                processBlendGroup(blendEntry.Key, new Dictionary<object, List<DrawAction>> { [UngroupedSentinel] = blendEntry.Value });
        }
    }

    public static object UngroupedKey => UngroupedSentinel;
}

/// <summary>
/// Facilitates all rendering actions associated with pixelated drawing, for textures and for primitives. <br></br>
/// This is done with the intention of bringing complicated shaders and textures down to the resolution of Terraria for the sake of consistency.
/// </summary>
[Autoload(Side = ModSide.Client)]
public class PixelationSystem : ModSystem
{
    private static readonly Dictionary<PixelationLayer, Dictionary<BlendState, ManagedRenderTarget>> RenderTargetsByLayer = [];
    private static readonly Dictionary<PixelationLayer, List<DrawAction>> PrimitiveDrawActionsByLayer = [];
    private static readonly Dictionary<PixelationLayer, List<DrawAction>> TextureDrawActionsByLayer = [];
    private static readonly BlendState[] SupportedBlendStates = [BlendState.AlphaBlend, BlendState.Additive, BlendState.NonPremultiplied];
    private static readonly RenderTargetInitializationAction PixelTargetInitializer = (width, height) => new RenderTarget2D(Main.instance.GraphicsDevice, width / 2, height / 2);
    public static bool CurrentlyRendering { get; private set; }
    private static PixelationLayer ActiveLayers;

    public override void Load()
    {
        Main.QueueMainThreadAction(() =>
        {
            On_Main.CheckMonoliths += DrawToTargets;
            On_Main.DoDraw_DrawNPCsOverTiles += DrawTarget_NPCs;
            On_Main.DrawProjectiles += DrawTarget_Projectiles;
            On_PlayerDrawLayers.DrawHeldProj += DrawTarget_HeldProj;
            On_Main.DrawPlayers_AfterProjectiles += DrawTarget_Players;
            On_Main.DrawDust += DrawTarget_Dusts;

            foreach (PixelationLayer layer in Enum.GetValues(typeof(PixelationLayer)))
            {
                RenderTargetsByLayer[layer] = [];
                PrimitiveDrawActionsByLayer[layer] = new List<DrawAction>(DrawActionPool.InitialCapacity);
                TextureDrawActionsByLayer[layer] = new List<DrawAction>(DrawActionPool.InitialCapacity);
            }
        });
    }

    public override void Unload()
    {
        Main.QueueMainThreadAction(() =>
        {
            On_Main.CheckMonoliths -= DrawToTargets;
            On_Main.DoDraw_DrawNPCsOverTiles -= DrawTarget_NPCs;
            On_Main.DrawProjectiles -= DrawTarget_Projectiles;
            On_PlayerDrawLayers.DrawHeldProj -= DrawTarget_HeldProj;
            On_Main.DrawPlayers_AfterProjectiles -= DrawTarget_Players;
            On_Main.DrawDust -= DrawTarget_Dusts;

            foreach (var layerTargets in RenderTargetsByLayer.Values)
                foreach (var target in layerTargets.Values)
                    target.Dispose();
            RenderTargetsByLayer.Clear();
        });
    }

    private void DrawToTargets(On_Main.orig_CheckMonoliths orig)
    {
        if (Main.gameMenu)
        {
            orig();
            return;
        }

        CurrentlyRendering = true;

        // Explicitly clear all render targets to prevent lingering
        foreach (var layer in RenderTargetsByLayer.Keys)
        {
            var layerTargets = RenderTargetsByLayer[layer];
            foreach (var target in layerTargets.Values)
                if (!target.IsUninitialized)
                    target.SwapToRenderTarget(Color.Transparent);
        }

        PixelationLayer layers = ActiveLayers;
        if (layers == 0)
        {
            // Clear all actions if no layers are active
            foreach (var layer in PrimitiveDrawActionsByLayer.Keys)
            {
                var primitives = PrimitiveDrawActionsByLayer[layer];
                for (int i = 0; i < primitives.Count; i++)
                    DrawActionPool.Return();
                
                primitives.Clear();

                var textures = TextureDrawActionsByLayer[layer];
                for (int i = 0; i < textures.Count; i++)
                    DrawActionPool.Return();
                
                textures.Clear();
            }
        }
        else
        {
            while (layers != 0)
            {
                PixelationLayer layer = (PixelationLayer)(1 << BitOperations.TrailingZeroCount((int)layers));
                if (PrimitiveDrawActionsByLayer[layer].Count > 0 || TextureDrawActionsByLayer[layer].Count > 0)
                    DrawToRenderTarget(layer, PrimitiveDrawActionsByLayer[layer], TextureDrawActionsByLayer[layer]);
                layers &= ~layer;
            }
        }

        ActiveLayers = 0;
        Main.instance.GraphicsDevice.SetRenderTarget(null);
        CurrentlyRendering = false;
        orig();
    }

    private static void DrawToRenderTarget(PixelationLayer layer, List<DrawAction> primitiveDrawActions, List<DrawAction> textureDrawActions)
    {
        if (primitiveDrawActions.Count == 0 && textureDrawActions.Count == 0)
            return;

        SpriteBatch sb = Main.spriteBatch;
        GraphicsDevice device = Main.instance.GraphicsDevice;

        Span<DrawAction> primitiveSpan = CollectionsMarshal.AsSpan(primitiveDrawActions);
        Span<DrawAction> textureSpan = CollectionsMarshal.AsSpan(textureDrawActions);

        DrawActionGrouper.GroupByBlendAndGroupId(primitiveSpan, textureSpan, (blend, groupGroups) =>
        {
            ManagedRenderTarget target = GetOrCreateRenderTarget(layer, blend);
            target.SwapToRenderTarget(Color.Transparent);

            BlendState prevBlend = device.BlendState;
            RasterizerState prevRasterizer = device.RasterizerState;
            DepthStencilState prevDepthStencil = device.DepthStencilState;
            Rectangle prevScissor = device.ScissorRectangle;
            Viewport prevViewport = device.Viewport;
            
            device.RasterizerState = RasterizerState.CullNone;
            device.DepthStencilState = DepthStencilState.None;
            device.ScissorRectangle = new Rectangle(0, 0, target.Target.Width, target.Target.Height);
            device.Viewport = new Viewport(0, 0, target.Target.Width, target.Target.Height);
            device.BlendState = blend;

            foreach (var groupEntry in groupGroups)
            {
                List<DrawAction> actions = groupEntry.Value;
                bool isSpriteBatchActive = false;

                foreach (var action in actions)
                {
                    bool isTextureAction = action.IsTexture || action.Shader != null;
                    if (isTextureAction)
                    {
                        // Start a new batch for each texture action to respect GroupId
                        if (isSpriteBatchActive)
                            sb.End();
                        sb.Begin(SpriteSortMode.Deferred, blend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, action.Shader?.Effect, Matrix.CreateScale(0.5f));
                        isSpriteBatchActive = true;
                        action.Shader?.Render();
                        action.RenderAction();
                    }
                    else
                    {
                        if (isSpriteBatchActive)
                        {
                            sb.End();
                            isSpriteBatchActive = false;
                        }
                        action.RenderAction?.Invoke();
                    }
                }

                if (isSpriteBatchActive)
                    sb.End();
            }

            device.BlendState = prevBlend;
            device.RasterizerState = prevRasterizer;
            device.DepthStencilState = prevDepthStencil;
            device.ScissorRectangle = prevScissor;
            device.Viewport = prevViewport;
        });

        // Clear actions
        foreach (var action in primitiveSpan)
            DrawActionPool.Return();
        primitiveDrawActions.Clear();
        foreach (var action in textureSpan)
            DrawActionPool.Return();
        textureDrawActions.Clear();
    }

    private static ManagedRenderTarget GetOrCreateRenderTarget(PixelationLayer layer, BlendState blend)
    {
        var layerTargets = RenderTargetsByLayer[layer];
        if (!layerTargets.TryGetValue(blend, out ManagedRenderTarget target))
        {
            target = new ManagedRenderTarget(true, PixelTargetInitializer, subjectToGarbageCollection: true);
            layerTargets[blend] = target;
        }
        return target;
    }

    private static bool IsSupportedBlendState(BlendState blend) => blend == BlendState.AlphaBlend || blend == BlendState.Additive || blend == BlendState.NonPremultiplied;
    
    /// <summary>
    /// Renders a primitive (e.g. a trail) in half-resolution on a specified draw layer.
    /// </summary>
    /// <param name="renderAction">The draw action to perform in the pixelation system.</param>
    /// <param name="layer">What layer to be drawn at.</param>
    /// <param name="blendState">The desired blend state. Defaults to <see cref="BlendState.AlphaBlend"/>.</param>
    /// <exception cref="ArgumentException">If a invalid <paramref name="blendState"/> was inputted.</exception>
    public static void QueuePrimitiveRenderAction(Action renderAction, PixelationLayer layer, BlendState blendState = null)
    {
        ArgumentNullException.ThrowIfNull(renderAction);
        BlendState blend = blendState ?? BlendState.AlphaBlend;
        if (!IsSupportedBlendState(blend))
            throw new ArgumentException($"BlendState {blend} is not supported.");
        var action = DrawActionPool.Rent(renderAction, blend, isTexture: false);
        PrimitiveDrawActionsByLayer[layer].Add(action);
        ActiveLayers |= layer;
    }

    /// <summary>
    /// Renders a sprite in half-resolution on a specific draw layer.
    /// </summary>
    /// <remarks>
    /// Textures in graphics device slots >= 1 may be cleared due to the other <see cref="SpriteBatch.End"/>'s in the system. <br></br>
    /// Slot 0 is set by <see cref="SpriteBatch.Draw"/>, but for anything higher than that you must call <see cref="ManagedShader.SetTexture(Texture2D, int, SamplerState)"/> in <paramref name="renderAction"/> before the draw call <i>constantly</i>.
    /// </remarks>
    /// <param name="renderAction">The draw action to perform in the pixelation system.</param>
    /// <param name="layer">What layer to be drawn at.</param>
    /// <param name="blendState">The desired blend state. Defaults to <see cref="BlendState.AlphaBlend"/>.</param>
    /// <param name="effect">The effect to apply to the spritebatch.</param>
    /// <param name="groupId">If a group is specified, then all of this group will be drawn together under one spritebatch. 
    /// <br></br>Leave null if you want logic like variables (e.g. a timer) specific to a projectile being passed into shader parameters to not effect all projectiles of that shader.</param>
    /// <exception cref="ArgumentException">If a invalid <paramref name="blendState"/> was inputted.</exception>
    public static void QueueTextureRenderAction(Action renderAction, PixelationLayer layer, BlendState blendState = null, ManagedShader effect = null, object groupId = null)
    {
        ArgumentNullException.ThrowIfNull(renderAction);
        BlendState blend = blendState ?? BlendState.AlphaBlend;
        if (!IsSupportedBlendState(blend))
            throw new ArgumentException($"BlendState {blend} is not supported.");
        var action = DrawActionPool.Rent(renderAction, blend, isTexture: true, effect, groupId);
        TextureDrawActionsByLayer[layer].Add(action);
        ActiveLayers |= layer;
    }

    #region Target Drawing
    private static void DrawTarget_NPCs(On_Main.orig_DoDraw_DrawNPCsOverTiles orig, Main self)
    {
        DrawTargetScaled(PixelationLayer.UnderNPCs);
        orig(self);
        DrawTargetScaled(PixelationLayer.OverNPCs);
    }

    private static void DrawTarget_Projectiles(On_Main.orig_DrawProjectiles orig, Main self)
    {
        DrawTargetScaled(PixelationLayer.UnderProjectiles);
        orig(self);
        DrawTargetScaled(PixelationLayer.OverProjectiles);
    }

    private static void DrawTarget_Players(On_Main.orig_DrawPlayers_AfterProjectiles orig, Main self)
    {
        DrawTargetScaled(PixelationLayer.UnderPlayers);
        orig(self);
        DrawTargetScaled(PixelationLayer.OverPlayers);
    }

    private static void DrawTarget_HeldProj(On_PlayerDrawLayers.orig_DrawHeldProj orig, PlayerDrawSet drawinfo, Projectile proj)
    {
        DrawTargetScaled(PixelationLayer.HeldProjectiles, endSB: true);
        orig(drawinfo, proj);
    }

    private static void DrawTarget_Dusts(On_Main.orig_DrawDust orig, Main self)
    {
        orig(self);
        DrawTargetScaled(PixelationLayer.Dusts);
    }

    private static void DrawTargetScaled(PixelationLayer layer, bool endSB = false)
    {
        SpriteBatch sb = Main.spriteBatch;
        var targets = RenderTargetsByLayer[layer];

        foreach (BlendState blend in SupportedBlendStates)
        {
            if (targets.TryGetValue(blend, out ManagedRenderTarget target) && !target.IsUninitialized)
            {
                if (endSB)
                    sb.End();

                sb.Begin(SpriteSortMode.Deferred, blend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                sb.Draw(target.Target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, 0, 0f);
                sb.End();

                if (endSB)
                    sb.Begin(default, default, Main.DefaultSamplerState, default, RasterizerState.CullNone, default, Main.GameViewMatrix.TransformationMatrix);
            }
        }
    }
    #endregion
}