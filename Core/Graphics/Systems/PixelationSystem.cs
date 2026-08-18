using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics.Resources;
using static TheExtraordinaryAdditions.Core.Graphics.Resources.ManagedRenderTarget;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace TheExtraordinaryAdditions.Core.Graphics.Systems;

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

public readonly struct PrimitiveDrawEntry(
    Action renderAction,
    BlendState blend)
{
    public Action RenderAction { get; } = renderAction;
    public BlendState Blend { get; } = blend;
}

public readonly struct SpriteDrawEntry(
    Texture2D tex,
    Vector2 pos,
    Rectangle? source,
    Color color,
    float rotation,
    Vector2 origin,
    Vector2 scale,
    SpriteEffects fx,
    bool destination,
    BlendState blend = null,
    ManagedShader shader = null,
    string group = null)
{
    public readonly Texture2D Texture = tex;
    public readonly Vector2 Position = pos;
    public readonly Rectangle? SourceRectangle = source;
    public readonly Color Color = color;
    public readonly float Rotation = rotation;
    public readonly Vector2 Origin = origin;
    public readonly Vector2 Scale = scale;
    public readonly SpriteEffects Effects = fx;
    public readonly bool Destination = destination;

    public readonly BlendState Blend = blend;
    public readonly ManagedShader Shader = shader;
    public readonly string GroupID = group;
}

static file class DrawActionGrouper
{
    private const string UngroupedSentinel = nameof(DrawActionGrouper);

    private static readonly Dictionary<BlendState, Dictionary<string, List<SpriteDrawEntry>>> TextureBlendGroups = [];
    private static readonly Dictionary<BlendState, List<PrimitiveDrawEntry>> PrimitiveBlendGroups = [];

    private static readonly List<SpriteDrawEntry>[] GroupListPool = new List<SpriteDrawEntry>[32];
    private static int _groupListPoolIndex = 0;

    static DrawActionGrouper()
    {
        foreach (BlendState blend in PixelationSystem.SupportedBlendStates)
        {
            TextureBlendGroups[blend] = [];
            PrimitiveBlendGroups[blend] = [];
        }

        for (int i = 0; i < GroupListPool.Length; i++)
            GroupListPool[i] = [];
    }

    private static List<SpriteDrawEntry> RentGroupList()
    {
        if (_groupListPoolIndex < GroupListPool.Length)
        {
            List<SpriteDrawEntry> list = GroupListPool[_groupListPoolIndex++];
            list.Clear();
            return list;
        }

        return [];
    }

    private static void ResetGroupListPool() => _groupListPoolIndex = 0;

    public static void GroupAndProcess(
        ReadOnlySpan<PrimitiveDrawEntry> primitiveActions,
        ReadOnlySpan<SpriteDrawEntry> spriteEntries,
        Action<BlendState, List<PrimitiveDrawEntry>> processPrimitives,
        Action<BlendState, Dictionary<string, List<SpriteDrawEntry>>> processTextures)
    {
        ResetGroupListPool();

        // Clear previous frame's data
        foreach (BlendState blend in PixelationSystem.SupportedBlendStates)
        {
            Dictionary<string, List<SpriteDrawEntry>> blendDict = TextureBlendGroups[blend];
            foreach (List<SpriteDrawEntry> groupList in blendDict.Values)
                groupList.Clear();
            blendDict.Clear();

            PrimitiveBlendGroups[blend].Clear();
        }

        // Group primitives by blend only, preserving order
        foreach (PrimitiveDrawEntry action in primitiveActions)
            PrimitiveBlendGroups[action.Blend].Add(action);

        // Group sprite entries by blend, then by GroupID
        foreach (SpriteDrawEntry entry in spriteEntries)
        {
            Dictionary<string, List<SpriteDrawEntry>> blendDict = TextureBlendGroups[entry.Blend];
            string key = entry.GroupID ?? UngroupedSentinel;

            if (!blendDict.TryGetValue(key, out List<SpriteDrawEntry> groupList))
            {
                groupList = RentGroupList();
                blendDict[key] = groupList;
            }

            groupList.Add(entry);
        }

        // Dispatch primitives per blend state
        foreach (BlendState blend in PixelationSystem.SupportedBlendStates)
        {
            List<PrimitiveDrawEntry> primitiveList = PrimitiveBlendGroups[blend];
            if (primitiveList.Count > 0)
                processPrimitives(blend, primitiveList);
        }

        // Dispatch texture groups per blend state
        foreach (BlendState blend in PixelationSystem.SupportedBlendStates)
        {
            Dictionary<string, List<SpriteDrawEntry>> blendDict = TextureBlendGroups[blend];
            if (blendDict.Count > 0)
                processTextures(blend, blendDict);
        }
    }
}

/// <summary>
/// Facilitates all rendering actions associated with pixelated drawing, for textures and for primitives. <br></br>
/// This is done with the intention of bringing complicated shaders and textures down to the resolution of Terraria for the sake of consistency.
/// </summary>
[Autoload(Side = ModSide.Client)]
public class PixelationSystem : ModSystem
{
    internal static readonly BlendState[] SupportedBlendStates =
        [BlendState.AlphaBlend, BlendState.Additive, BlendState.NonPremultiplied];

    public static readonly int InitialCapacity = Main.maxProjectiles + Main.maxNPCs + 50_000;

    private static readonly Dictionary<PixelationLayer, Dictionary<BlendState, ManagedRenderTarget>>
        RenderTargetsByLayer = [];

    private static readonly Dictionary<PixelationLayer, List<PrimitiveDrawEntry>> PrimitiveDrawActionsByLayer = [];
    private static readonly Dictionary<PixelationLayer, List<SpriteDrawEntry>> SpriteQueuesByLayer = [];

    private static readonly RenderTargetInitializationAction PixelTargetInitializer = (width, height) =>
        new RenderTarget2D(Main.instance.GraphicsDevice, width / 2, height / 2);

    public static bool CurrentlyRendering { get; private set; }
    private static PixelationLayer _activeLayers;

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
                PrimitiveDrawActionsByLayer[layer] = new List<PrimitiveDrawEntry>(InitialCapacity);
                SpriteQueuesByLayer[layer] = new List<SpriteDrawEntry>(InitialCapacity);
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

            foreach (Dictionary<BlendState, ManagedRenderTarget> layerTargets in RenderTargetsByLayer.Values)
            foreach (ManagedRenderTarget target in layerTargets.Values)
                target.Dispose();
            RenderTargetsByLayer.Clear();
        });
    }

    private static void DrawToTargets(On_Main.orig_CheckMonoliths orig)
    {
        if (Main.gameMenu)
        {
            orig();
            return;
        }

        CurrentlyRendering = true;

        // Explicitly clear all render targets to prevent lingering
        foreach (PixelationLayer layer in RenderTargetsByLayer.Keys)
        {
            Dictionary<BlendState, ManagedRenderTarget> layerTargets = RenderTargetsByLayer[layer];
            foreach (ManagedRenderTarget target in layerTargets.Values)
                if (!target.IsUninitialized)
                    target.SwapToRenderTarget(Color.Transparent);
        }

        PixelationLayer layers = _activeLayers;
        if (layers == 0)
        {
            // Clear all actions if no layers are active
            foreach (PixelationLayer layer in PrimitiveDrawActionsByLayer.Keys)
            {
                List<PrimitiveDrawEntry> primitives = PrimitiveDrawActionsByLayer[layer];
                primitives.Clear();

                List<SpriteDrawEntry> textures = SpriteQueuesByLayer[layer];
                textures.Clear();
            }
        }
        else
        {
            while (layers != 0)
            {
                PixelationLayer layer = (PixelationLayer) (1 << BitOperations.TrailingZeroCount((int) layers));
                if (PrimitiveDrawActionsByLayer[layer].Count > 0 || SpriteQueuesByLayer[layer].Count > 0)
                    DrawToRenderTarget(layer, PrimitiveDrawActionsByLayer[layer], SpriteQueuesByLayer[layer]);
                layers &= ~layer;
            }
        }

        _activeLayers = 0;
        Main.instance.GraphicsDevice.SetRenderTarget(null);
        CurrentlyRendering = false;
        orig();
    }

    private static void DrawToRenderTarget(PixelationLayer layer, List<PrimitiveDrawEntry> primitiveDrawActions,
        List<SpriteDrawEntry> textureDrawActions)
    {
        if (primitiveDrawActions.Count == 0 && textureDrawActions.Count == 0)
            return;

        SpriteBatch sb = Main.spriteBatch;
        GraphicsDevice device = Main.instance.GraphicsDevice;

        Span<PrimitiveDrawEntry> primitiveSpan = CollectionsMarshal.AsSpan(primitiveDrawActions);
        Span<SpriteDrawEntry> spriteSpan = CollectionsMarshal.AsSpan(textureDrawActions);

        DrawActionGrouper.GroupAndProcess(
            primitiveSpan,
            spriteSpan,
            processPrimitives: (blend, actions) =>
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

                foreach (PrimitiveDrawEntry action in actions)
                    action.RenderAction?.Invoke();
                    
                device.BlendState = prevBlend;
                device.RasterizerState = prevRasterizer;
                device.DepthStencilState = prevDepthStencil;
                device.ScissorRectangle = prevScissor;
                device.Viewport = prevViewport;
            },
            processTextures: (blend, groupGroups) =>
            {
                ManagedRenderTarget target = GetOrCreateRenderTarget(layer, blend);
                target.SwapToRenderTarget(Color.Transparent);

                foreach (KeyValuePair<string, List<SpriteDrawEntry>> groupEntry in groupGroups)
                {
                    List<SpriteDrawEntry> entries = groupEntry.Value;
                    if (entries.Count == 0)
                        continue;
                        
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

                    // One spritebatch per group.
                    // Shader is uniform across a group, so take it from the first entry.
                    ManagedShader groupShader = entries[0].Shader;
                    sb.Begin(SpriteSortMode.Deferred, blend, SamplerState.PointClamp,
                        DepthStencilState.None, RasterizerState.CullNone,
                        groupShader?.Effect, Matrix.CreateScale(0.5f));

                    groupShader?.Render();

                    foreach (SpriteDrawEntry entry in entries)
                    {
                        if (entry.Destination)
                        {
                            sb.Draw(entry.Texture,
                                new Rectangle((int) entry.Position.X, (int) entry.Position.Y, (int) entry.Scale.X,
                                    (int) entry.Scale.Y), entry.SourceRectangle, entry.Color, entry.Rotation,
                                entry.Origin, entry.Effects, 0f);
                        }
                        else
                        {
                            sb.Draw(entry.Texture, entry.Position, entry.SourceRectangle, entry.Color,
                                entry.Rotation, entry.Origin, entry.Scale, entry.Effects, 0f);
                        }
                    }

                    sb.End();
                    
                    device.BlendState = prevBlend;
                    device.RasterizerState = prevRasterizer;
                    device.DepthStencilState = prevDepthStencil;
                    device.ScissorRectangle = prevScissor;
                    device.Viewport = prevViewport;
                }
            });

        // Clear actions
        primitiveDrawActions.Clear();
        textureDrawActions.Clear();
    }

    private static ManagedRenderTarget GetOrCreateRenderTarget(PixelationLayer layer, BlendState blend)
    {
        Dictionary<BlendState, ManagedRenderTarget> layerTargets = RenderTargetsByLayer[layer];
        if (layerTargets.TryGetValue(blend, out ManagedRenderTarget target))
            return target;

        target = new ManagedRenderTarget(true, PixelTargetInitializer, subjectToGarbageCollection: true);
        layerTargets[blend] = target;

        return target;
    }

    private static bool IsSupportedBlendState(BlendState blend) => blend == BlendState.AlphaBlend ||
                                                                   blend == BlendState.Additive ||
                                                                   blend == BlendState.NonPremultiplied;

    /// <summary>
    /// Renders a primitive (e.g. a trail) in half-resolution on a specified draw layer.
    /// </summary>
    /// <param name="renderAction">The draw action to perform in the pixelation system.</param>
    /// <param name="layer">What layer to be drawn at.</param>
    /// <param name="blendState">The desired blend state. Defaults to <see cref="BlendState.AlphaBlend"/>.</param>
    /// <exception cref="ArgumentException">If a invalid <paramref name="blendState"/> was inputted.</exception>
    public static void QueuePrimitiveRenderAction(Action renderAction, PixelationLayer layer,
        BlendState blendState = null)
    {
        ArgumentNullException.ThrowIfNull(renderAction);
        BlendState blend = blendState ?? BlendState.AlphaBlend;
        if (!IsSupportedBlendState(blend))
            throw new ArgumentException($"BlendState {blend} is not supported.");
        PrimitiveDrawActionsByLayer[layer].Add(new PrimitiveDrawEntry(renderAction, blend));
        _activeLayers |= layer;
    }

    /// <summary>
    /// Renders a sprite in half-resolution on a specific draw layer.
    /// </summary>
    /// <remarks>
    /// Textures in graphics device slots >= 1 may be cleared due to the other <see cref="SpriteBatch.End"/>'s in the system. <br></br>
    /// Slot 0 is set by <see cref="SpriteBatch.Draw"/>, but for anything higher than that you must call <see cref="ManagedShader.SetTexture(Texture2D, int, SamplerState)"/> in <paramref name="renderAction"/> before the draw call <i>constantly</i>.
    /// </remarks>
    /// <br></br>Leave null if you want logic like variables (e.g. a timer) specific to a projectile being passed into shader parameters to not effect all projectiles of that shader.
    /// <param name="tex">The texture to use</param>
    /// <param name="pos">The position in screen space</param>
    /// <param name="source">The extracted rectangle of space, if any</param>
    /// <param name="color">What color to tint this sprite</param>
    /// <param name="rotation">The rotation in radians</param>
    /// <param name="origin">Where the rotation pivot of this sprite should be</param>
    /// <param name="scale">The size in pixels</param>
    /// <param name="fx">What orientation the sprite should be</param>
    /// <param name="layer">What layer to be drawn at</param>
    /// <param name="dest">If this should draw at a destination rectangle</param>
    /// <param name="blendState">The desired blend state. Defaults to <see cref="BlendState.AlphaBlend"/></param>
    /// <param name="shader">The effect to apply to the spritebatch</param>
    /// <param name="groupID">If a group is specified, then all of this group will be drawn together under one spritebatch</param>
    /// <exception cref="ArgumentException">If an invalid <paramref name="blendState"/> was inputted</exception>
    public static void QueueTextureRenderAction(
        Texture2D tex,
        Vector2 pos,
        Rectangle? source,
        Color color,
        float rotation,
        Vector2 origin,
        Vector2 scale,
        SpriteEffects fx,
        PixelationLayer layer,
        bool dest,
        BlendState blendState = null,
        ManagedShader shader = null,
        string groupID = null)
    {
        BlendState blend = blendState ?? BlendState.AlphaBlend;
        if (!IsSupportedBlendState(blend))
            throw new ArgumentException($"BlendState {blend} is not supported.");

        // Texture actions not needing a shader will get automatically grouped, unless specified
        if (groupID == null && shader == null)
            groupID = $"__Sentinel_{layer}_{blend}";

        SpriteQueuesByLayer[layer].Add(new SpriteDrawEntry(tex, pos, source, color, rotation, origin, scale, fx, dest,
            blendState,
            shader, groupID));
        _activeLayers |= layer;
    }

    /// <summary>
    /// Renders a sprite in half-resolution on a specific draw layer.
    /// </summary>
    /// <remarks>
    /// Textures in graphics device slots >= 1 may be cleared due to the other <see cref="SpriteBatch.End"/>'s in the system. <br></br>
    /// Slot 0 is set by <see cref="SpriteBatch.Draw"/>, but for anything higher than that you must call <see cref="ManagedShader.SetTexture(Texture2D, int, SamplerState)"/> in <paramref name="renderAction"/> before the draw call <i>constantly</i>.
    /// </remarks>
    /// <br></br>Leave null if you want logic like variables (e.g. a timer) specific to a projectile being passed into shader parameters to not effect all projectiles of that shader.
    /// <param name="tex">The texture to use</param>
    /// <param name="pos">The position in screen space</param>
    /// <param name="source">The extracted rectangle of space, if any</param>
    /// <param name="color">What color to tint this sprite</param>
    /// <param name="rotation">The rotation in radians</param>
    /// <param name="origin">Where the rotation pivot of this sprite should be</param>
    /// <param name="scale">The multiplier of the textures base size</param>
    /// <param name="fx">What orientation the sprite should be</param>
    /// <param name="layer">What layer to be drawn at</param>
    /// <param name="dest">If this should draw at a destination rectangle</param>
    /// <param name="blendState">The desired blend state. Defaults to <see cref="BlendState.AlphaBlend"/></param>
    /// <param name="shader">The effect to apply to the spritebatch</param>
    /// <param name="groupID">If a group is specified, then all of this group will be drawn together under one spritebatch</param>
    /// <exception cref="ArgumentException">If an invalid <paramref name="blendState"/> was inputted</exception>
    public static void QueueTextureRenderAction(
        Texture2D tex,
        Vector2 pos,
        Rectangle? source,
        Color color,
        float rotation,
        Vector2 origin,
        float scale,
        SpriteEffects fx,
        PixelationLayer layer,
        bool dest,
        BlendState blendState = null,
        ManagedShader shader = null,
        string groupID = null)
    {
        BlendState blend = blendState ?? BlendState.AlphaBlend;
        if (!IsSupportedBlendState(blend))
            throw new ArgumentException($"BlendState {blend} is not supported.");

        // Texture actions not needing a shader will get automatically grouped, unless specified
        if (groupID == null && shader == null)
            groupID = $"__Sentinel_{layer}_{blend}";

        SpriteQueuesByLayer[layer].Add(new SpriteDrawEntry(tex, pos, source, color, rotation, origin,
            new(scale), fx, dest,
            blendState,
            shader, groupID));
        _activeLayers |= layer;
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

    private static void DrawTarget_HeldProj(On_PlayerDrawLayers.orig_DrawHeldProj orig, PlayerDrawSet drawinfo,
        Projectile proj)
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
        Dictionary<BlendState, ManagedRenderTarget> targets = RenderTargetsByLayer[layer];

        foreach (BlendState blend in SupportedBlendStates)
        {
            if (!targets.TryGetValue(blend, out ManagedRenderTarget target) || target.IsUninitialized)
                continue;

            if (endSB)
                sb.End();

            sb.Begin(SpriteSortMode.Deferred, blend, SamplerState.PointClamp, DepthStencilState.None,
                Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            sb.Draw(target.Target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, 0, 0f);
            sb.End();

            if (endSB)
                sb.Begin(default, default, Main.DefaultSamplerState, default, RasterizerState.CullNone, default,
                    Main.GameViewMatrix.TransformationMatrix);
        }
    }

    #endregion
}
