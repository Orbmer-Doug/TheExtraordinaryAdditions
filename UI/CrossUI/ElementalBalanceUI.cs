using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.CrossCode;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.CrossCode.CrossDiscHoldout;

namespace TheExtraordinaryAdditions.UI.CrossUI;

public class ElementalBalanceUI : SmartUIState
{
    public static readonly Texture2D elementBase = AssetRegistry.GetTexture(AdditionsTexture.ElementalBalanceBase);
    public static readonly Texture2D fill = AssetRegistry.GetTexture(AdditionsTexture.ElementalBalanceFill);
    public static readonly Texture2D neutral = AssetRegistry.GetTexture(AdditionsTexture.Neutral);
    public static readonly Texture2D ice = AssetRegistry.GetTexture(AdditionsTexture.Ice);
    public static readonly Texture2D fire = AssetRegistry.GetTexture(AdditionsTexture.Fire);
    public static readonly Texture2D shock = AssetRegistry.GetTexture(AdditionsTexture.Shock);
    public static readonly Texture2D wave = AssetRegistry.GetTexture(AdditionsTexture.Wave);
    public static readonly Texture2D outline = AssetRegistry.GetTexture(AdditionsTexture.ElementalBalanceOutline);
    public static readonly Texture2D index = AssetRegistry.GetTexture(AdditionsTexture.Index);
    public static readonly Texture2D background = AssetRegistry.GetTexture(AdditionsTexture.Background);

    public override int InsertionIndex(List<GameInterfaceLayer> layers) => layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");

    public float MoveCounter;
    public const int MoveTime = 35;
    public const int ShowTime = 12;
    public static bool visible;
    public override bool Visible => visible;
    private readonly int[] ElementCounters = new int[5];
    public override void OnInitialize()
    {
        MoveCounter = 0;
    }
    public override void Draw(SpriteBatch spriteBatch)
    {
        int held = Main.LocalPlayer.heldProj;
        if (held == -1)
            return;
        Projectile disc = Main.projectile?[Main.LocalPlayer.heldProj];
        if (disc == null || disc.active == false || disc.type != ModContent.ProjectileType<CrossDiscHoldout>())
            return;
        CrossDiscHoldout holdout = Main.projectile[Main.LocalPlayer.heldProj].As<CrossDiscHoldout>();
        ElementalBalance balance = Main.LocalPlayer.GetModPlayer<ElementalBalance>();
        GlobalPlayer modded = Main.LocalPlayer.Additions();

        DrawOverload(Main.LocalPlayer, balance);

        float uiScale = Main.UIScale;
        ref float stateAI = ref disc.ai[0];
        Element element = holdout.State;
        int mode = (int)element;

        bool button = AdditionsKeybinds.OpenCrossDiscUI.Current && balance.CircuitOverload <= 0;

        Vector2 rightPos = new(Main.screenWidth / 2 + (300 * (2f - uiScale)), Main.screenHeight / 2 - (350 * (2f - uiScale)));
        Vector2 centerPos = new(Main.screenWidth / 2, Main.screenHeight / 2);

        float moveCompletion = InverseLerp(0f, MoveTime, MoveCounter);
        float invCompletion = 1f - moveCompletion;
        Vector2 position = Vector2.Lerp(rightPos, centerPos, Animators.Back.InOutFunction(moveCompletion));

        int adjustedMode = (element == Element.Neutral ? 1 :
            element == Element.Cold ? 1 : element == Element.Heat ? 3 : element == Element.Shock ? 2 : element == Element.Wave ? 4 : 1) - 1;
        Rectangle neutralIndexFrame = index.Frame(4, 2, adjustedMode, 0);
        Color backgroundCol = Color.White * moveCompletion;

        spriteBatch.Draw(background, position, null, backgroundCol, 0f, background.Size() / 2, moveCompletion * uiScale, 0, 0f);

        Vector2 indexOrig = index.Frame(4, 2, 0, 0).Size() / 2;

        int DistFromBackground = (int)(48 * moveCompletion * uiScale);

        // Ice
        Vector2 icePos = position - Vector2.UnitY * DistFromBackground;
        spriteBatch.Draw(index, icePos, index.Frame(4, 2, 0, 1), backgroundCol, 0f, indexOrig, moveCompletion * uiScale, 0, 0f);

        // Shock
        Vector2 shockPos = position + Vector2.UnitX * DistFromBackground;
        spriteBatch.Draw(index, shockPos, index.Frame(4, 2, 1, 1), backgroundCol, 0f, indexOrig, moveCompletion * uiScale, 0, 0f);

        // Fire
        Vector2 firePos = position + Vector2.UnitY * DistFromBackground;
        spriteBatch.Draw(index, firePos, index.Frame(4, 2, 2, 1), backgroundCol, 0f, indexOrig, moveCompletion * uiScale, 0, 0f);

        // Wave
        Vector2 wavePos = position - Vector2.UnitX * DistFromBackground;
        spriteBatch.Draw(index, wavePos, index.Frame(4, 2, 3, 1), backgroundCol, 0f, indexOrig, moveCompletion * uiScale, 0, 0f);

        // Neutral
        spriteBatch.Draw(index, position, neutralIndexFrame, backgroundCol, 0f, indexOrig, moveCompletion * uiScale, 0, 0f);

        // Check which side the mouse is on and highlight
        if (moveCompletion > 0f)
        {
            Vector2[] mouseEdges = [MouseScreenHitbox.TopLeft(), MouseScreenHitbox.TopRight(), MouseScreenHitbox.BottomLeft(), MouseScreenHitbox.BottomRight()];
            int width = Main.screenWidth;
            Vector2 topLeft = position + new Vector2(-width, -width);
            Vector2 topRight = position + new Vector2(width, -width);
            Vector2 bottomLeft = position + new Vector2(-width, width);
            Vector2 bottomRight = position + new Vector2(width, width);

            bool iceSide = IsIntersecting(mouseEdges, [position, topLeft, topRight]);
            bool fireSide = IsIntersecting(mouseEdges, [position, bottomRight, bottomLeft]);
            bool shockSide = IsIntersecting(mouseEdges, [position, topRight, bottomRight]);
            bool waveSide = IsIntersecting(mouseEdges, [position, topLeft, bottomLeft]);
            bool neutralMiddle = new Rectangle((int)position.X - (elementBase.Height / 2), (int)position.Y - (elementBase.Height / 2), elementBase.Height, elementBase.Height).Intersects(MouseScreenHitbox);

            if (neutralMiddle)
            {
                for (int i = 0; i < ElementCounters.Length; i++)
                {
                    if (i != 0)
                        ElementCounters[i] = 0;
                }

                if (moveCompletion >= 1f)
                {
                    stateAI = (int)Element.Neutral;
                    ElementCounters[0]++;
                }

                DrawElementOutline(spriteBatch, InverseLerp(0f, ShowTime, ElementCounters[0]) * uiScale * moveCompletion, position, Element.Neutral);
            }
            else if (iceSide)
            {
                for (int i = 0; i < ElementCounters.Length; i++)
                {
                    if (i != 1)
                        ElementCounters[i] = 0;
                }

                if (moveCompletion >= 1f)
                {
                    stateAI = (int)Element.Cold;
                    ElementCounters[1]++;
                }

                DrawElementOutline(spriteBatch, InverseLerp(0f, ShowTime, ElementCounters[1]) * uiScale * moveCompletion, icePos, Element.Cold);
            }
            else if (fireSide)
            {
                for (int i = 0; i < ElementCounters.Length; i++)
                {
                    if (i != 2)
                        ElementCounters[i] = 0;
                }

                if (moveCompletion >= 1f)
                {
                    stateAI = (int)Element.Heat;
                    ElementCounters[2]++;
                }

                DrawElementOutline(spriteBatch, InverseLerp(0f, ShowTime, ElementCounters[2]) * uiScale * moveCompletion, firePos, Element.Heat);
            }
            else if (shockSide)
            {
                for (int i = 0; i < ElementCounters.Length; i++)
                {
                    if (i != 3)
                        ElementCounters[i] = 0;
                }

                if (moveCompletion >= 1f)
                {
                    stateAI = (int)Element.Shock;
                    ElementCounters[3]++;
                }

                DrawElementOutline(spriteBatch, InverseLerp(0f, ShowTime, ElementCounters[3]) * uiScale * moveCompletion, shockPos, Element.Shock);
            }
            else if (waveSide)
            {
                for (int i = 0; i < ElementCounters.Length; i++)
                {
                    if (i != 4)
                        ElementCounters[i] = 0;
                }

                if (moveCompletion >= 1f)
                {
                    stateAI = (int)Element.Wave;
                    ElementCounters[4]++;
                }

                DrawElementOutline(spriteBatch, InverseLerp(0f, ShowTime, ElementCounters[4]) * uiScale * moveCompletion, wavePos, Element.Wave);
            }
        }
        else
        {
            for (int i = 0; i < ElementCounters.Length; i++)
                ElementCounters[i] = 0;
        }

        float baseScale = uiScale * invCompletion;
        Color baseCol = Color.White * invCompletion;
        spriteBatch.Draw(elementBase, position, null, baseCol, -MathHelper.PiOver2, elementBase.Size() / 2, baseScale, 0, 0f);

        Rectangle fillRectangle = new(0, 0, (int)(fill.Width * balance.ElementCompletion), fill.Height);
        spriteBatch.Draw(fill, position, fillRectangle, baseCol, -MathHelper.PiOver2, fill.Size() / 2, baseScale, 0, 0f);

        switch (element)
        {
            case Element.Neutral:
                spriteBatch.Draw(neutral, position, null, baseCol, 0f, neutral.Size() / 2, baseScale, 0, 0f);
                break;
            case Element.Cold:
                spriteBatch.Draw(ice, position, null, baseCol, 0f, ice.Size() / 2, baseScale, 0, 0f);
                break;
            case Element.Heat:
                spriteBatch.Draw(fire, position, null, baseCol, 0f, fire.Size() / 2, baseScale, 0, 0f);
                break;
            case Element.Shock:
                spriteBatch.Draw(shock, position, null, baseCol, 0f, shock.Size() / 2, baseScale, 0, 0f);
                break;
            case Element.Wave:
                spriteBatch.Draw(wave, position, null, baseCol, 0f, wave.Size() / 2, baseScale, 0, 0f);
                break;
        }

        if (button)
            MoveCounter = MathHelper.Clamp(MoveCounter + 1, 0f, MoveTime);
        else
            MoveCounter = MathHelper.Clamp(MoveCounter - 1, 0f, MoveTime);
    }

    private static void DrawElementOutline(SpriteBatch spriteBatch, float scale, Vector2 pos, Element element)
    {
        Color col = Color.White;
        spriteBatch.Draw(outline, pos, null, col, -MathHelper.PiOver2, outline.Size() / 2, scale, 0, 0f);

        switch (element)
        {
            case Element.Neutral:
                spriteBatch.Draw(neutral, pos, null, col, 0f, neutral.Size() / 2, scale, 0, 0f);
                break;
            case Element.Cold:
                spriteBatch.Draw(ice, pos, null, col, 0f, ice.Size() / 2, scale, 0, 0f);
                break;
            case Element.Heat:
                spriteBatch.Draw(fire, pos, null, col, 0f, fire.Size() / 2, scale, 0, 0f);
                break;
            case Element.Shock:
                spriteBatch.Draw(shock, pos, null, col, 0f, shock.Size() / 2, scale, 0, 0f);
                break;
            case Element.Wave:
                spriteBatch.Draw(wave, pos, null, col, 0f, wave.Size() / 2, scale, 0, 0f);
                break;
        }
    }

    public static readonly Texture2D Overload = AssetRegistry.GetTexture(AdditionsTexture.Overlay);
    private void DrawOverload(Player player, ElementalBalance element)
    {
        Vector2 screenPos = Main.screenPosition;

        float flicker = Sin01(Main.GlobalTimeWrappedHourly * (player.GetModPlayer<ElementalBalance>().CircuitOverload > 0 ? 15f : 5f)) + .5f;
        float opacity = element.ElementCompletion * flicker;
        if (element.ElementCompletion >= .5f)
            opacity *= 2f;

        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);
        for (int i = 0; i < 4; i++)
        {
            Vector2 pos = new();
            float rotation = 0f;
            SpriteEffects fx = SpriteEffects.None;
            float width = Main.screenWidth;
            float height = Main.screenHeight;
            Rectangle frame = Overload.Frame(2, 1, player.GetModPlayer<ElementalBalance>().CircuitOverload > 0 ? 1 : 0, 0);
            float overWidth = frame.Width * .5f;
            float overHeight = frame.Height * .5f;
            switch (i)
            {
                case 0: // Top Left
                    pos = new Vector2(-width / 2f + overWidth, -height / 2f + overHeight);
                    rotation = 0f;
                    fx = SpriteEffects.None;
                    break;
                case 1: // Bottom Left
                    pos = new Vector2(-width / 2f + overWidth, height / 2f - overHeight);
                    rotation = 0f;
                    fx = SpriteEffects.FlipVertically;
                    break;
                case 2: // Top Right
                    pos = new Vector2(width / 2f - overWidth, -height / 2f + overHeight);
                    rotation = 0f;
                    fx = SpriteEffects.FlipHorizontally;
                    break;
                case 3: // Bottom Right
                    pos = new Vector2(width / 2f - overWidth, height / 2f - overHeight);
                    rotation = MathHelper.Pi;
                    fx = SpriteEffects.None;
                    break;
            }

            Main.EntitySpriteDraw(Overload, new Vector2(Main.screenWidth / 2, Main.screenHeight / 2) + pos, frame, Color.White * opacity, rotation, frame.Size() * .5f, 1f, fx);
        }
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);

    }
}