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
public class AbsoluteTorsoDrawer : ModSystem
{
    private static bool disallowSpecialTorsoDrawing;
    private static bool anyoneIsUsingTorso;
    private static ManagedRenderTarget AfterimageTarget;
    private static ManagedRenderTarget AfterimageTargetPrevious;
    private static ManagedRenderTarget BaseTorsoTarget;
    private static ManagedRenderTarget BrokenGlassTarget;

    public static Texture2D ArmorOutlineTexture { get; private set; }

    public override void OnModLoad()
    {
        if (Main.netMode != NetmodeID.Server)
        {
            ArmorOutlineTexture = AssetRegistry.GetTexture(AdditionsTexture.AbsoluteCoreplate_Body);
        }

        Main.QueueMainThreadAction(static () =>
        {
            AfterimageTarget = new(true, CreateScreenSizedTarget);
            AfterimageTargetPrevious = new(true, CreateScreenSizedTarget);
            BaseTorsoTarget = new(true, CreateScreenSizedTarget);
            BrokenGlassTarget = new(true, CreateScreenSizedTarget);
        });
        
        RenderTargetManager.RenderTargetUpdateLoopEvent += PrepareAfterimageTarget;
        On_LegacyPlayerRenderer.DrawPlayers += DrawTorsoTarget;
        On_PlayerDrawLayers.DrawPlayer_17_TorsoComposite += DisallowTorsoDrawingIfNecessary;
    }

    public override void OnModUnload()
    {
        Main.QueueMainThreadAction(static () =>
        {
            AfterimageTarget?.Dispose();
            AfterimageTarget = null;
            AfterimageTargetPrevious?.Dispose();
            AfterimageTargetPrevious = null;
            BaseTorsoTarget?.Dispose();
            BaseTorsoTarget = null;
            BrokenGlassTarget?.Dispose();
            BrokenGlassTarget = null;
        });

        RenderTargetManager.RenderTargetUpdateLoopEvent -= PrepareAfterimageTarget;
        On_LegacyPlayerRenderer.DrawPlayers -= DrawTorsoTarget;
        On_PlayerDrawLayers.DrawPlayer_17_TorsoComposite -= DisallowTorsoDrawingIfNecessary;
    }

    private void DrawTorsoTarget(On_LegacyPlayerRenderer.orig_DrawPlayers orig, LegacyPlayerRenderer self, Camera camera, IEnumerable<Player> players)
    {
        if (anyoneIsUsingTorso)
        {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicWrap, DepthStencilState.None, camera.Rasterizer, null, camera.GameViewMatrix.TransformationMatrix);

            if (Main.LocalPlayer.cBody != 0)
                GameShaders.Armor.Apply(Main.LocalPlayer.cBody, Main.LocalPlayer);

            Main.spriteBatch.Draw(AfterimageTargetPrevious, Main.screenLastPosition - Main.LocalPlayer.velocity * .1f - Main.screenPosition, LocalPlayerDrawManager.ShaderDrawAction is not null ? Color.Transparent : Color.White);
            Main.spriteBatch.Draw(BrokenGlassTarget, Main.screenLastPosition - Main.screenPosition, Color.White);
            Main.spriteBatch.End();
        }

        orig(self, camera, players);
    }

    private void DisallowTorsoDrawingIfNecessary(On_PlayerDrawLayers.orig_DrawPlayer_17_TorsoComposite orig, ref PlayerDrawSet drawinfo)
    {
        if (drawinfo.hideEntirePlayer || drawinfo.drawPlayer.dead)
            return;

        if (Main.gameMenu)
            disallowSpecialTorsoDrawing = false;

        if (drawinfo.drawPlayer.body == AbsoluteCoreplate.BodySlotID && disallowSpecialTorsoDrawing)
        {
            int num = drawinfo.armorAdjust;
            Rectangle bodyFrame = drawinfo.drawPlayer.bodyFrame;
            bodyFrame.X += num;
            bodyFrame.Width -= num;
            if (drawinfo.drawPlayer.direction == -1)
                num = 0;

            Vector2 pos = new Vector2(
                (int)(drawinfo.Position.X - Main.screenPosition.X - drawinfo.drawPlayer.bodyFrame.Width / 2 + drawinfo.drawPlayer.width / 2) + num,
                (int)(drawinfo.Position.Y - Main.screenPosition.Y + drawinfo.drawPlayer.height - drawinfo.drawPlayer.bodyFrame.Height + 4f)
            ) + drawinfo.drawPlayer.bodyPosition + new Vector2(drawinfo.drawPlayer.bodyFrame.Width / 2, drawinfo.drawPlayer.bodyFrame.Height / 2);

            DrawData outline = new(ArmorOutlineTexture, pos, bodyFrame, Color.White, drawinfo.drawPlayer.bodyRotation, drawinfo.bodyVect, 1f, drawinfo.playerEffect)
            {
                shader = drawinfo.drawPlayer.cBody
            };

            drawinfo.DrawDataCache.Add(outline);
            return;
        }

        orig(ref drawinfo);
    }

    private void PrepareAfterimageTarget()
    {
        anyoneIsUsingTorso = false;
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player p = Main.player[i];
            if (p.body != AbsoluteCoreplate.BodySlotID || !p.active || p.dead)
                continue;

            anyoneIsUsingTorso = true;
            break;
        }

        if (!AssetRegistry.HasFinishedLoading || Main.gameMenu || !anyoneIsUsingTorso)
            return;

        var gd = Main.instance.GraphicsDevice;

        // Draw the chestplate
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.Default, Main.Rasterizer, null, Matrix.Identity);
        gd.SetRenderTarget(BaseTorsoTarget);
        gd.Clear(Color.Transparent);

        DrawPlayerArmorToTarget();

        Main.spriteBatch.End();

        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.Default, Main.Rasterizer, null, Matrix.Identity);
        gd.SetRenderTarget(BrokenGlassTarget);
        gd.Clear(Color.Transparent);

        var aura = ShaderRegistry.ViscousAfterimageShader;
        aura.TrySetParameter("uScreenResolution", Main.ScreenSize.ToVector2());
        aura.TrySetParameter("warpSpeed", 0.00280f);
        aura.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.TurbulentNoise), 1);
        aura.Render();

        Main.spriteBatch.Draw(BaseTorsoTarget, Vector2.Zero, Color.White);
        Main.spriteBatch.End();

        // Apply the afterimage effect to AfterimageTarget
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.Default, Main.Rasterizer, null, Matrix.Identity);
        gd.SetRenderTarget(AfterimageTarget);
        gd.Clear(Color.Transparent);

        bool probablyUsingSniperEffects = Main.LocalPlayer.scope || Main.LocalPlayer.HeldMouseItem().type == ItemID.SniperRifle || Main.LocalPlayer.HeldMouseItem().type == ItemID.Binoculars;
        if (!probablyUsingSniperEffects || CameraSystem.UnmodifiedCameraPosition.WithinRange(Main.screenPosition, Main.LocalPlayer.velocity.Length() + 60f))
            Main.spriteBatch.Draw(AfterimageTargetPrevious, Vector2.Zero, Color.White);

        Main.spriteBatch.Draw(BaseTorsoTarget, Vector2.Zero, Color.White);

        ApplyPsychedelicDiffusionEffects();

        Main.spriteBatch.End();
        gd.SetRenderTarget(null);
    }

    public static void DrawPlayerArmorToTarget()
    {
        var shader = ShaderRegistry.ViscousVoidShader;
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
            if (p.body != AbsoluteCoreplate.BodySlotID || p.dead || !p.active)
                continue;

            PlayerDrawSet drawinfo = default;
            drawinfo.BoringSetup(p, [], [], [], p.position, 0f, p.fullRotation, p.fullRotationOrigin);

            // Ensure the player has a valid body armor
            if (drawinfo.drawPlayer.body <= 0)
                continue;

            // Store the original DrawDataCache to restore it later
            var originalDrawDataCache = drawinfo.DrawDataCache;
            drawinfo.DrawDataCache = [];

            // Calculate position and vectors for torso rendering
            Vector2 position = new Vector2(
                (int)(drawinfo.Position.X - Main.screenPosition.X - (drawinfo.drawPlayer.bodyFrame.Width / 2) + (drawinfo.drawPlayer.width / 2)),
                (int)(drawinfo.Position.Y - Main.screenPosition.Y + drawinfo.drawPlayer.height - drawinfo.drawPlayer.bodyFrame.Height + 4f)
            ) + drawinfo.drawPlayer.bodyPosition + new Vector2(drawinfo.drawPlayer.bodyFrame.Width / 2, drawinfo.drawPlayer.bodyFrame.Height / 2);

            Vector2 bodyVect = drawinfo.bodyVect;

            // Create DrawData for the torso
            Texture2D texture = TextureAssets.ArmorBodyComposite[drawinfo.drawPlayer.body].Value;
            DrawData torsoDrawData = new(
                texture,
                position,
                drawinfo.compTorsoFrame,
                Color.White,
                drawinfo.drawPlayer.bodyRotation,
                bodyVect,
                1f,
                drawinfo.playerEffect
            )
            {
                shader = drawinfo.cBody
            };

            PlayerDrawLayers.DrawCompositeArmorPiece(ref drawinfo, CompositePlayerDrawContext.Torso, torsoDrawData);

            // Capture the resulting DrawData from the cache
            DrawData[] result = [.. drawinfo.DrawDataCache];

            // Restore the original DrawDataCache
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
        if (!AssetRegistry.HasFinishedLoading || !anyoneIsUsingTorso)
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