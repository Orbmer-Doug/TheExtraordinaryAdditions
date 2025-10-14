using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
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
public class AbsoluteHeadDrawer : ModSystem
{
    private static bool disallowSpecialHeadgearDrawing;
    private static bool anyoneIsUsingHeadgear;
    private static ManagedRenderTarget AfterimageTarget;
    private static ManagedRenderTarget AfterimageTargetPrevious;

    public static Texture2D ArmorOutlineTexture { get; private set; }

    public override void OnModLoad()
    {
        if (Main.netMode != NetmodeID.Server)
        {
            ArmorOutlineTexture = AssetRegistry.GetTexture(AdditionsTexture.AbsoluteGreathelm);
        }

        Main.QueueMainThreadAction(static () =>
        {
            AfterimageTarget = new(true, CreateScreenSizedTarget);
            AfterimageTargetPrevious = new(true, CreateScreenSizedTarget);
        });

        RenderTargetManager.RenderTargetUpdateLoopEvent += PrepareAfterimageTarget;
        On_LegacyPlayerRenderer.DrawPlayers += DrawHeadTarget;
        On_PlayerDrawLayers.DrawPlayer_21_Head += DisallowHeadDrawingIfNecessary;
    }

    public override void OnModUnload()
    {
        Main.QueueMainThreadAction(static () =>
        {
            AfterimageTarget?.Dispose();
            AfterimageTarget = null;
            AfterimageTargetPrevious?.Dispose();
            AfterimageTargetPrevious = null;
        });

        RenderTargetManager.RenderTargetUpdateLoopEvent -= PrepareAfterimageTarget;
        On_LegacyPlayerRenderer.DrawPlayers -= DrawHeadTarget;
        On_PlayerDrawLayers.DrawPlayer_21_Head -= DisallowHeadDrawingIfNecessary;
    }

    private void DrawHeadTarget(On_LegacyPlayerRenderer.orig_DrawPlayers orig, LegacyPlayerRenderer self, Camera camera, IEnumerable<Player> players)
    {
        if (anyoneIsUsingHeadgear)
        {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicWrap, DepthStencilState.None, camera.Rasterizer, null, camera.GameViewMatrix.TransformationMatrix);

            if (Main.LocalPlayer.cHead != 0)
                GameShaders.Armor.Apply(Main.LocalPlayer.cHead, Main.LocalPlayer);

            Main.spriteBatch.Draw(AfterimageTargetPrevious, Main.screenLastPosition - Main.screenPosition, LocalPlayerDrawManager.ShaderDrawAction is not null ? Color.Transparent : Color.White);
            Main.spriteBatch.End();
        }

        orig(self, camera, players);
    }

    private void DisallowHeadDrawingIfNecessary(On_PlayerDrawLayers.orig_DrawPlayer_21_Head orig, ref PlayerDrawSet drawinfo)
    {
        if (drawinfo.hideEntirePlayer || drawinfo.drawPlayer.dead)
            return;

        if (Main.gameMenu)
            disallowSpecialHeadgearDrawing = false;

        if (drawinfo.drawPlayer.head == AbsoluteGreathelm.HeadSlotID && disallowSpecialHeadgearDrawing)
        {
            Vector2 playerPosition = drawinfo.Position - Main.screenPosition + new Vector2(drawinfo.drawPlayer.width / 2, drawinfo.drawPlayer.height - drawinfo.drawPlayer.bodyFrame.Height / 2);
            Vector2 headDrawPosition = playerPosition;
            Rectangle outlineFrame = ArmorOutlineTexture.Frame(1, 20, 0, drawinfo.drawPlayer.headFrame.Y);
            Vector2 outlineOrigin = outlineFrame.Size() * 0.5f;

            DrawData outline = new(ArmorOutlineTexture, headDrawPosition, outlineFrame, Color.White, drawinfo.drawPlayer.bodyRotation, outlineOrigin, 1f, drawinfo.playerEffect)
            {
                shader = drawinfo.cHead
            };
            drawinfo.DrawDataCache.Add(outline);
            return;
        }

        orig(ref drawinfo);
    }

    private void PrepareAfterimageTarget()
    {
        anyoneIsUsingHeadgear = false;
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player p = Main.player[i];
            if (p.head != AbsoluteGreathelm.HeadSlotID || !p.active || p.dead)
                continue;

            anyoneIsUsingHeadgear = true;
            break;
        }

        if (!AssetRegistry.HasFinishedLoading || Main.gameMenu || !anyoneIsUsingHeadgear)
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
        var shader = ShaderRegistry.ViscousVoidShader;
        shader.TrySetParameter("colorShift", Color.AntiqueWhite.ToVector3());
        shader.TrySetParameter("lightDirection", Vector3.UnitZ);
        shader.TrySetParameter("normalMapCrispness", 0.46f);
        shader.TrySetParameter("normalMapZoom", new Vector2(1.7f, 1.4f));
        shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.TurbulentNoise), 1);
        shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.MeltNoise), 2);
        shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.WavyNeurons), 3);
        shader.Render();

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player p = Main.player[i];
            if (p.head != AbsoluteGreathelm.HeadSlotID || p.dead || !p.active)
                continue;

            PlayerDrawSet drawInfo = default;
            drawInfo.BoringSetup(p, [], [], [], p.TopLeft + Vector2.UnitY * p.gfxOffY, 0f, p.fullRotation, p.fullRotationOrigin);

            PlayerDrawLayers.DrawPlayer_21_Head(ref drawInfo);

            // Draw the headgear with the activated shader
            foreach (DrawData armorData in drawInfo.DrawDataCache)
            {
                DrawData modifiedData = armorData;
                modifiedData.shader = GameShaders.Armor.GetShaderIdFromItemId(ItemID.None);
                modifiedData.Draw(Main.spriteBatch);
            }
        }
    }

    public static void ApplyPsychedelicDiffusionEffects()
    {
        if (!AssetRegistry.HasFinishedLoading || !anyoneIsUsingHeadgear)
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