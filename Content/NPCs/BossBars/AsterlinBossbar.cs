using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics.Resources;
using Asterlin = TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Asterlin;

namespace TheExtraordinaryAdditions.Content.NPCs.BossBars;

public class AsterlinBossbar : ModBossBar
{
    public override string Texture => AssetRegistry.GennedTextures.AsterlinBossbar.Path;

    public override Asset<Texture2D> GetIconTexture(ref Rectangle? iconFrame)
    {
        return ModContent.Request<Texture2D>(AssetRegistry.GennedTextures.Asterlin_Head_Boss.Path);
    }

    public override bool PreDraw(SpriteBatch spriteBatch, NPC npc, ref BossBarDrawParams drawParams)
    {
        drawParams.Deconstruct(out Texture2D barTexture,
            out Vector2 barCenter,
            out Color _,
            out Texture2D _,
            out Rectangle _,
            out Color iconColor,
            out float _,
            out float _,
            out float _,
            out float _,
            out float iconScale,
            out bool showText,
            out Vector2 textOffset);

        life = npc.life;
        lifeMax = npc.lifeMax;

        Asterlin aster = npc.As<Asterlin>();
        float lifeRatio = aster.CurrentState == Asterlin.AsterlinAIType.DesperationDrama ? 1f - aster.PowerInterpolant :
            npc.life == 1 && aster.DoneDesperationTransition ? 0f : InverseLerp(lifeMax, 0, life);

        int headTextureIndex = NPCID.Sets.BossHeadTextures[npc.type];
        if (headTextureIndex == -1)
        {
            NPCLoader.BossHeadSlot(npc, ref headTextureIndex);
            if (headTextureIndex == -1)
                return false;
        }

        Texture2D headtex = TextureAssets.NpcHeadBoss[headTextureIndex].Value;
        Rectangle headframe = headtex.Frame();

        Point barSize = new(456, 22);
        Point topLeftOffset = new(32, 24);
        const int frameCount = 6;

        Rectangle bgFrame = barTexture.Frame(verticalFrames: frameCount, frameY: 3);
        Color bgColor = Color.White * 0.2f;

        Rectangle barFrame = barTexture.Frame(verticalFrames: frameCount, frameY: 2);
        barFrame.X += topLeftOffset.X;
        barFrame.Y += topLeftOffset.Y;
        barFrame.Width = 2;
        barFrame.Height = barSize.Y;

        Rectangle barPosition = Utils.CenteredRectangle(barCenter, barSize.ToVector2());
        Vector2 barTopLeft = barPosition.TopLeft();
        Vector2 topLeft = barTopLeft - topLeftOffset.ToVector2();

        // Background
        spriteBatch.Draw(barTexture, topLeft, bgFrame, bgColor, 0f, Vector2.Zero, 1f, 0, 0f);

        // Bar
        Main.spriteBatch.EnterShaderRegion();
        ManagedShader shader = AssetRegistry.GennedShaders.AsterlinHealthbar;
        shader.SetTexture(AssetRegistry.GennedTextures.TechyNoise, 1, SamplerState.LinearWrap);
        shader.TrySetParameter("res", barSize.ToVector2() / 2f);
        shader.TrySetParameter("ratio", lifeRatio);
        shader.TrySetParameter("golden", npc.life == 1);
        shader.Render();

        Texture2D tex = AssetRegistry.GennedTextures.Pixel;
        Main.spriteBatch.Draw(tex,
            new Rectangle(((int) barTopLeft.X), ((int) barTopLeft.Y), ((int) barSize.X), ((int) barSize.Y)), null,
            Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0);
        Main.spriteBatch.ResetToDefaultUI();

        // Frame
        Rectangle frameFrame = barTexture.Frame(verticalFrames: frameCount, frameY: 0);
        spriteBatch.Draw(barTexture, topLeft, frameFrame, Color.White, 0f, Vector2.Zero, 1f, 0, 0f);

        // Icon
        Vector2 iconOffset = new(0f, 10f);
        Vector2 iconSize = new(34f, 46f);
        Vector2 iconPosition = iconOffset + iconSize * 0.5f;
        spriteBatch.Draw(tex, topLeft + iconPosition, headframe, iconColor, 0f, headframe.Size() / 2f,
            iconScale * .6f, 0, 0f);

        // Health text
        if (BigProgressBarSystem.ShowText && showText)
        {
            if (shield > 0f)
                BigProgressBarHelper.DrawHealthText(spriteBatch, barPosition, textOffset, shield, shieldMax);
            else
                BigProgressBarHelper.DrawHealthText(spriteBatch, barPosition, textOffset, life, lifeMax);
        }

        return false;
    }
}
