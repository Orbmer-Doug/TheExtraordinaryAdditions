using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics;
using Terraria.Graphics.Renderers;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Late;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Core.Graphics.Specific;

[Autoload(Side = ModSide.Client)]
public class AbsoluteLeggingsDrawer : ModSystem
{
    private static bool disallowSpecialLeggingDrawing;
    private static bool anyoneIsUsingLeggings;
    private static ManagedRenderTarget AfterimageTarget;
    private static ManagedRenderTarget AfterimageTargetPrevious;

    public static Texture2D ArmorOutlineTexture { get; private set; }

    public override void OnModLoad()
    {
        if (Main.netMode != NetmodeID.Server)
        {
            ArmorOutlineTexture = AssetRegistry.GetTexture(AdditionsTexture.AbsoluteGreaves_Legs);
        }

        RenderTargetManager.RenderTargetUpdateLoopEvent += PrepareAfterimageTarget;
        Main.QueueMainThreadAction(() =>
        {
            AfterimageTarget = new(true, CreateScreenSizedTarget);
            AfterimageTargetPrevious = new(true, CreateScreenSizedTarget);
        });
        On_LegacyPlayerRenderer.DrawPlayers += DrawLegsTarget;
        On_PlayerDrawLayers.DrawPlayer_13_Leggings += DisallowLeggingDrawingIfNecessary;
    }

    public override void OnModUnload()
    {
        RenderTargetManager.RenderTargetUpdateLoopEvent -= PrepareAfterimageTarget;
        On_LegacyPlayerRenderer.DrawPlayers -= DrawLegsTarget;
        On_PlayerDrawLayers.DrawPlayer_13_Leggings -= DisallowLeggingDrawingIfNecessary;
    }

    private void DrawLegsTarget(On_LegacyPlayerRenderer.orig_DrawPlayers orig, LegacyPlayerRenderer self, Camera camera, IEnumerable<Player> players)
    {
        if (anyoneIsUsingLeggings)
        {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicWrap, DepthStencilState.None, camera.Rasterizer, null, camera.GameViewMatrix.TransformationMatrix);

            if (Main.LocalPlayer.cLegs != 0)
                GameShaders.Armor.Apply(Main.LocalPlayer.cLegs, Main.LocalPlayer);

            Main.spriteBatch.Draw(AfterimageTargetPrevious, Main.screenLastPosition - Main.screenPosition, LocalPlayerDrawManager.ShaderDrawAction is not null ? Color.Transparent : Color.White);
            Main.spriteBatch.End();
        }

        orig(self, camera, players);
    }

    private void DisallowLeggingDrawingIfNecessary(On_PlayerDrawLayers.orig_DrawPlayer_13_Leggings orig, ref PlayerDrawSet drawinfo)
    {
        if (drawinfo.hideEntirePlayer || drawinfo.drawPlayer.dead)
            return;

        if (Main.gameMenu)
            disallowSpecialLeggingDrawing = false;

        if (drawinfo.drawPlayer.legs == AbsoluteGreaves.LegsSlotID && disallowSpecialLeggingDrawing)
        {
            // Use the same position calculation as DrawPlayerLeggings for consistency
            Vector2 playerPosition = new Vector2(
                (int)(drawinfo.Position.X - Main.screenPosition.X - drawinfo.drawPlayer.bodyFrame.Width / 2 + drawinfo.drawPlayer.width / 2),
                (int)(drawinfo.Position.Y - Main.screenPosition.Y + drawinfo.drawPlayer.height - drawinfo.drawPlayer.bodyFrame.Height + 4f)
            ) + drawinfo.drawPlayer.legPosition + drawinfo.legVect;

            Rectangle outlineFrame = drawinfo.drawPlayer.legFrame;

            DrawData outline = new(ArmorOutlineTexture, playerPosition, outlineFrame, Color.White, drawinfo.drawPlayer.legRotation, drawinfo.legVect, 1f, drawinfo.playerEffect)
            {
                shader = drawinfo.cLegs
            };
            drawinfo.DrawDataCache.Add(outline);

            return;
        }

        orig(ref drawinfo);
    }

    private void PrepareAfterimageTarget()
    {
        anyoneIsUsingLeggings = false;
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player p = Main.player[i];
            if (p.legs != AbsoluteGreaves.LegsSlotID || !p.active || p.dead)
                continue;

            anyoneIsUsingLeggings = true;
            break;
        }

        if (!AssetRegistry.HasFinishedLoading || Main.gameMenu || !anyoneIsUsingLeggings)
            return;

        var gd = Main.instance.GraphicsDevice;

        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.Default, Main.Rasterizer, null, Matrix.Identity);
        gd.SetRenderTarget(AfterimageTarget);
        gd.Clear(Color.Transparent);

        bool probablyUsingSniperEffects = Main.LocalPlayer.scope || Main.LocalPlayer.HeldMouseItem().type == ItemID.SniperRifle || Main.LocalPlayer.HeldMouseItem().type == ItemID.Binoculars;
        if (!probablyUsingSniperEffects || CameraSystem.UnmodifiedCameraPosition.WithinRange(Main.screenPosition, Main.LocalPlayer.velocity.Length() + 60f))
            Main.spriteBatch.Draw(AfterimageTargetPrevious, Vector2.Zero, Color.White);

        DrawPlayerArmorToTarget();

        ApplyPsychedelicDiffusionEffects();

        Main.spriteBatch.End();
        gd.SetRenderTarget(null);
    }

    public static void DrawPlayerArmorToTarget()
    {
        ManagedShader shader = ShaderRegistry.ViscousVoidShader;
        shader.TrySetParameter("colorShift", Color.AntiqueWhite.ToVector3());
        shader.TrySetParameter("lightDirection", Vector3.UnitZ);
        shader.TrySetParameter("normalMapCrispness", 0.86f);
        shader.TrySetParameter("normalMapZoom", new Vector2(0.7f, 0.4f));
        shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.TurbulentNoise), 1);
        shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.MeltNoise), 2);
        shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.WavyNeurons), 3);
        shader.Render();

        foreach (Player p in Main.ActivePlayers)
        {
            if (p.legs != AbsoluteGreaves.LegsSlotID || p.dead || !p.active)
                continue;

            PlayerDrawSet drawinfo = default;
            drawinfo.BoringSetup(p, [], [], [], p.position, 0f, p.fullRotation, p.fullRotationOrigin);

            Player drawPlayer = drawinfo.drawPlayer;
            if (drawPlayer.legs <= 0)
                continue;

            // Store original DrawDataCache and create a new one for leggings
            var originalDrawDataCache = drawinfo.DrawDataCache;
            drawinfo.DrawDataCache = [];

            // Calculate position
            Vector2 position = new Vector2(
                (int)(drawinfo.Position.X - Main.screenPosition.X - drawPlayer.bodyFrame.Width / 2 + drawPlayer.width / 2),
                (int)(drawinfo.Position.Y - Main.screenPosition.Y + drawPlayer.height - drawPlayer.bodyFrame.Height + 4f)
            ) + drawPlayer.legPosition + drawinfo.legVect;

            // Determine texture and frame
            Texture2D texture = TextureAssets.ArmorLeg[drawPlayer.legs].Value;
            Rectangle frame = drawPlayer.legFrame;

            // Apply color with stealth and shadow adjustments
            Color color = drawPlayer.GetImmuneAlphaPure(drawinfo.colorArmorLegs, drawinfo.shadow);
            if (drawinfo.shadow != 0f)
            {
                color *= 1f - drawinfo.shadow;
            }

            // Create DrawData for leggings
            DrawData leggingsDrawData = new(
                texture,
                position,
                frame,
                Color.White,
                drawPlayer.legRotation,
                drawinfo.legVect,
                1f,
                drawinfo.playerEffect
            )
            {
                shader = drawinfo.cLegs
            };

            // Add to cache
            drawinfo.DrawDataCache.Add(leggingsDrawData);

            // Handle glowmask if applicable
            if (drawPlayer.legs > 0 && drawPlayer.legs < ArmorIDs.Legs.Count)
            {
                if (drawinfo.legsGlowMask != -1)
                {
                    DrawData glowDrawData = new(
                        TextureAssets.GlowMask[drawinfo.legsGlowMask].Value,
                        position,
                        frame,
                        drawinfo.legsGlowColor,
                        drawPlayer.legRotation,
                        drawinfo.legVect,
                        1f,
                        drawinfo.playerEffect
                    )
                    {
                        shader = drawinfo.cLegs
                    };
                    drawinfo.DrawDataCache.Add(glowDrawData);
                }
            }

            // Capture results
            DrawData[] result = [.. drawinfo.DrawDataCache];

            // Restore original cache
            drawinfo.DrawDataCache = originalDrawDataCache;

            foreach (DrawData data in result)
            {
                DrawData modifiedData = data;
                modifiedData.shader = GameShaders.Armor.GetShaderIdFromItemId(ItemID.None);
                modifiedData.Draw(Main.spriteBatch);
            }
        }
    }

    public static void ApplyPsychedelicDiffusionEffects()
    {
        if (!AssetRegistry.HasFinishedLoading || !anyoneIsUsingLeggings)
            return;

        var gd = Main.instance.GraphicsDevice;
        gd.SetRenderTarget(AfterimageTargetPrevious);
        gd.Clear(Color.Transparent);

        var afterimageShader = ShaderRegistry.ViscousAfterimageShader;
        afterimageShader.TrySetParameter("uScreenResolution", Main.ScreenSize.ToVector2());
        afterimageShader.TrySetParameter("warpSpeed", 0.00030f);
        afterimageShader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.TurbulentNoise), 1);
        afterimageShader.Render();

        Main.spriteBatch.Draw(AfterimageTarget, Vector2.Zero, Color.White);
    }
}