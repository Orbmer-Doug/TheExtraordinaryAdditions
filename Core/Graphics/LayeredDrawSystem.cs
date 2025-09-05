using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;

namespace TheExtraordinaryAdditions.Core.Graphics;

[Autoload(Side = ModSide.Client)]
public class LayeredDrawSystem : ModSystem
{
    private static readonly Dictionary<PixelationLayer, List<DrawAction>> DrawActionsByLayer = [];

    public override void Load()
    {
        Main.QueueMainThreadAction(() =>
        {
            On_Main.DoDraw_DrawNPCsOverTiles += DrawLayer_NPCs;
            On_Main.DrawProjectiles += DrawLayer_Projectiles;
            On_Main.DrawPlayers_AfterProjectiles += DrawLayer_Players;
            On_Main.DrawDust += DrawLayer_Dusts;
            On_PlayerDrawLayers.DrawHeldProj += DrawLayer_HeldProj;

            foreach (PixelationLayer layer in Enum.GetValues(typeof(PixelationLayer)))
            {
                DrawActionsByLayer[layer] = new List<DrawAction>(DrawActionPool.InitialCapacity / 2);
            }
        });
    }

    public override void Unload()
    {
        Main.QueueMainThreadAction(() =>
        {
            On_Main.DoDraw_DrawNPCsOverTiles -= DrawLayer_NPCs;
            On_Main.DrawProjectiles -= DrawLayer_Projectiles;
            On_Main.DrawPlayers_AfterProjectiles -= DrawLayer_Players;
            On_Main.DrawDust -= DrawLayer_Dusts;
            On_PlayerDrawLayers.DrawHeldProj -= DrawLayer_HeldProj;

            foreach (var actions in DrawActionsByLayer.Values)
            {
                foreach (var action in actions)
                    DrawActionPool.Return();
                actions.Clear();
            }
            DrawActionsByLayer.Clear();
        });
    }

    private void DrawLayer_NPCs(On_Main.orig_DoDraw_DrawNPCsOverTiles orig, Main self)
    {
        DrawLayer(PixelationLayer.UnderNPCs);
        orig(self);
        DrawLayer(PixelationLayer.OverNPCs);
    }

    private void DrawLayer_Projectiles(On_Main.orig_DrawProjectiles orig, Main self)
    {
        DrawLayer(PixelationLayer.UnderProjectiles);
        orig(self);
        DrawLayer(PixelationLayer.OverProjectiles);
    }

    private void DrawLayer_Players(On_Main.orig_DrawPlayers_AfterProjectiles orig, Main self)
    {
        DrawLayer(PixelationLayer.UnderPlayers);
        orig(self);
        DrawLayer(PixelationLayer.OverPlayers);
    }

    private void DrawLayer_Dusts(On_Main.orig_DrawDust orig, Main self)
    {
        orig(self);
        DrawLayer(PixelationLayer.Dusts);
    }

    private void DrawLayer_HeldProj(On_PlayerDrawLayers.orig_DrawHeldProj orig, PlayerDrawSet drawinfo, Projectile proj)
    {
        DrawLayer(PixelationLayer.HeldProjectiles, true);
        orig(drawinfo, proj);
    }

    private static void DrawLayer(PixelationLayer layer, bool endSB = false)
    {
        var actions = DrawActionsByLayer[layer];
        if (actions.Count == 0)
            return;

        SpriteBatch sb = Main.spriteBatch;
        Span<DrawAction> actionSpan = CollectionsMarshal.AsSpan(actions);

        DrawActionGrouper.GroupByBlendAndGroupId(actionSpan, [], (blend, groupGroups) =>
        {
            foreach (var groupEntry in groupGroups)
            {
                object groupId = groupEntry.Key;
                List<DrawAction> groupActions = groupEntry.Value;

                if (groupId == DrawActionGrouper.UngroupedKey)
                {
                    // Ungrouped: each action gets its own batch to respect shader parameters
                    foreach (var action in groupActions)
                    {
                        if (action.RenderAction == null)
                            continue;

                        if (endSB)
                            sb.End();

                        sb.Begin(SpriteSortMode.Deferred, action.Blend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, action.Shader?.Effect, Main.GameViewMatrix.TransformationMatrix);
                        action.Shader?.Render();
                        action.RenderAction();
                        sb.End();

                        if (endSB)
                            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                    }
                }
                else if (groupActions.Count == 1)
                {
                    // Single grouped action: treat like ungrouped for safety
                    var action = groupActions[0];
                    if (action.RenderAction == null)
                        continue;

                    if (endSB)
                        sb.End();

                    sb.Begin(SpriteSortMode.Deferred, action.Blend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, action.Shader?.Effect, Main.GameViewMatrix.TransformationMatrix);
                    action.Shader?.Render();
                    action.RenderAction();
                    sb.End();

                    if (endSB)
                        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                }
                else
                {
                    // Grouped: batch actions with same GroupId, assuming shared shader parameters
                    if (endSB)
                        sb.End();

                    ManagedShader sharedShader = groupActions[0].Shader;
                    sb.Begin(SpriteSortMode.Deferred, blend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, sharedShader?.Effect, Main.GameViewMatrix.TransformationMatrix);
                    foreach (var action in groupActions)
                    {
                        if (action.RenderAction == null)
                            continue;

                        // Only call Render if shader matches to avoid parameter conflicts
                        if (action.Shader == sharedShader)
                            action.Shader?.Render();
                        action.RenderAction();
                    }
                    sb.End();

                    if (endSB)
                        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                }
            }
        });

        // Clear actions and return to pool
        foreach (var action in actionSpan)
            DrawActionPool.Return();
        actions.Clear();
    }

    public static void QueueDrawAction(Action renderAction, PixelationLayer layer, BlendState blendState = null, ManagedShader effect = null, object groupId = null)
    {
        ArgumentNullException.ThrowIfNull(renderAction);
        BlendState blend = blendState ?? BlendState.AlphaBlend;
        var action = DrawActionPool.Rent(renderAction, blend, isTexture: true, effect, groupId); // Treat as texture for consistency
        DrawActionsByLayer[layer].Add(action);
    }
}