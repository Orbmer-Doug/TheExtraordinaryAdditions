using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;

namespace TheExtraordinaryAdditions.Content.NPCs.BossBars;

public class AsterlinBossbar : ModBossBar
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.AsterlinBossbar);

    public override Asset<Texture2D> GetIconTexture(ref Rectangle? iconFrame)
    {
        return ModContent.Request<Texture2D>(AssetRegistry.GetTexturePath(AdditionsTexture.Asterlin_Head_Boss));
    }

    public override bool PreDraw(SpriteBatch spriteBatch, NPC npc, ref BossBarDrawParams drawParams)
    {
        (Texture2D barTexture, Vector2 barCenter, _, _, Color iconColor, float life, float lifeMax, float shield, float shieldMax, float iconScale, bool showText, Vector2 textOffset) = drawParams;

        life = npc.life;
        lifeMax = npc.lifeMax;

        Asterlin aster = npc.As<Asterlin>();
        float lifeRatio = aster.CurrentState == Asterlin.AsterlinAIType.DesperationDrama ? 1f - aster.PowerInterpolant : npc.life == 1 && aster.DoneDesperationTransition ? 0f : InverseLerp(lifeMax, 0, life);

        int headTextureIndex = NPCID.Sets.BossHeadTextures[npc.type];
        if (headTextureIndex == -1)
        {
            NPCLoader.BossHeadSlot(npc, ref headTextureIndex);
            if (headTextureIndex == -1)
                return false;
        }

        Texture2D iconTexture = TextureAssets.NpcHeadBoss[headTextureIndex].Value;
        Rectangle iconFrame = iconTexture.Frame();

        Point barSize = new(456, 22);
        Point topLeftOffset = new(32, 24);
        int frameCount = 6;

        Rectangle bgFrame = barTexture.Frame(verticalFrames: frameCount, frameY: 3);
        Color bgColor = Color.White * 0.2f;

        int scale = (int)(barSize.X * lifeRatio);
        scale -= scale % 2;
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
        Main.spriteBatch.PrepareForShaders(null, true);
        ManagedShader shader = AssetRegistry.GetShader("AsterlinHealthbar");
        shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.TechyNoise), 1, SamplerState.LinearWrap);
        shader.TrySetParameter("res", barSize.ToVector2() / 2f);
        shader.TrySetParameter("ratio", lifeRatio);
        shader.TrySetParameter("golden", npc.life == 1);
        shader.Render();

        Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.Pixel);
        Main.spriteBatch.Draw(tex, new Rectangle(((int)barTopLeft.X), ((int)barTopLeft.Y), ((int)barSize.X), ((int)barSize.Y)), null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0);
        Main.spriteBatch.ResetToDefaultUI();

        // Frame
        Rectangle frameFrame = barTexture.Frame(verticalFrames: frameCount, frameY: 0);
        spriteBatch.Draw(barTexture, topLeft, frameFrame, Color.White, 0f, Vector2.Zero, 1f, 0, 0f);

        // Icon
        Vector2 iconOffset = new(0f, 10f);
        Vector2 iconSize = new(34f, 46f);
        Vector2 iconPosition = iconOffset + iconSize * 0.5f;
        spriteBatch.Draw(iconTexture, topLeft + iconPosition, iconFrame, iconColor, 0f, iconFrame.Size() / 2f, iconScale * .6f, 0, 0f);

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