using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;
using Terraria.UI.Chat;
using TheExtraordinaryAdditions.Content.Items.Tools;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;
using static TheExtraordinaryAdditions.UI.GodDummyUI.DummyUI;

namespace TheExtraordinaryAdditions.UI.GodDummyUI;
public class DummyUI : SmartUIState
{
    public override int InsertionIndex(List<GameInterfaceLayer> layers) => layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
    public override InterfaceScaleType Scale => InterfaceScaleType.None;
    public static bool visible;
    public override bool Visible => visible;

    public static readonly Texture2D BGTex = AssetRegistry.GetTexture(AdditionsTexture.GodDummyUIBackground);
    public static readonly Texture2D ButtonTextures = AssetRegistry.GetTexture(AdditionsTexture.GodDummyButtons);
    public static Player Owner => Main.LocalPlayer;
    public Vector2 Position;

    public static int MaxLife = GodDummy.LifeAmount;
    public static int Defense = 0;
    public static float Size = 1f;
    public static bool Gravity = false;
    public static float Rotation = 0f;

    [Flags]
    public enum ButtonType
    {
        Life,
        Defense,
        Scale,
        Gravity,
        Rotation,
        Info,
        Reset,
    }
    public SmartUIElement[] Buttons = [new DummyButton(ButtonType.Life), new DummyButton(ButtonType.Defense),
        new DummyButton(ButtonType.Scale), new DummyButton(ButtonType.Rotation),
        new DummyButton(ButtonType.Gravity), new DummyButton(ButtonType.Info), new DummyButton(ButtonType.Reset)];

    private bool BeingDragged;
    public const int ButtonSize = 32;

    public override void OnInitialize()
    {
        Position = new(Main.screenWidth / 2 - BGTex.Size().X - 100, Main.screenHeight / 2);
        Top.Set(Position.Y - BGTex.Size().Y / 2, 0f);
        Left.Set(Position.X - BGTex.Size().X / 2, 0f);

        foreach (SmartUIElement button in Buttons)
        {
            Point pos = new();
            switch ((button as DummyButton).Type)
            {
                case ButtonType.Life:
                    pos = new(ButtonSize, ButtonSize);
                    break;
                case ButtonType.Defense:
                    pos = new(ButtonSize * 2, ButtonSize);
                    break;
                case ButtonType.Scale:
                    pos = new(ButtonSize * 3, ButtonSize);
                    break;
                case ButtonType.Gravity:
                    pos = new(ButtonSize * 4, ButtonSize);
                    break;
                case ButtonType.Rotation:
                    pos = new(ButtonSize, ButtonSize * 3);
                    break;
                case ButtonType.Reset:
                    pos = new(ButtonSize * 2, ButtonSize * 3);
                    break;
                case ButtonType.Info:
                    pos = new(ButtonSize * 4, ButtonSize * 3);
                    break;
            }

            AddElement(button, pos, 32, 32);
        }
        Height = new(BGTex.Size().Y, 0f);
        Width = new(BGTex.Size().X, 0f);
    }

    public override void SafeUpdate(GameTime gameTime)
    {
        if (!Main.playerInventory)
            visible = false;

        if (BeingDragged)
        {
            Position = Vector2.Lerp(Position, Main.MouseScreen, .5f);
            Top.Set(Position.Y - BGTex.Size().Y / 2, 0f);
            Left.Set(Position.X - BGTex.Size().X / 2, 0f);
            Recalculate();
            foreach (UIElement element in Elements)
            {
                Point pos = new();
                switch ((element as DummyButton).Type)
                {
                    case ButtonType.Life:
                        pos = new(ButtonSize, ButtonSize);
                        break;
                    case ButtonType.Defense:
                        pos = new(ButtonSize * 2, ButtonSize);
                        break;
                    case ButtonType.Scale:
                        pos = new(ButtonSize * 3, ButtonSize);
                        break;
                    case ButtonType.Gravity:
                        pos = new(ButtonSize * 4, ButtonSize);
                        break;
                    case ButtonType.Rotation:
                        pos = new(ButtonSize, ButtonSize * 3);
                        break;
                    case ButtonType.Reset:
                        pos = new(ButtonSize * 2, ButtonSize * 3);
                        break;
                    case ButtonType.Info:
                        pos = new(ButtonSize * 4, ButtonSize * 3);
                        break;
                }
                Point position = pos;
                element.Left.Set(position.X, 0f);
                element.Top.Set(position.Y, 0f);
                element.Recalculate();
            }
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        Vector2 backgroundScale = Vector2.One * Main.UIScale;

        // Draw the background
        spriteBatch.Draw(BGTex, Position, null, Color.White, 0f, BGTex.Size() / 2, backgroundScale, 0, 0f);

        const float textX = -5f;
        const float textY = -70f;
        const float textSpace = 25f;
        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, $"{GetTextValue("UI.DummyLife")} {MaxLife}", Position + new Vector2(textX, textY), Color.Orange, Color.Black, 0f, Vector2.Zero, backgroundScale * .75f);
        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, $"{GetTextValue("UI.DummyDefense")} {Defense}", Position + new Vector2(textX, textY + textSpace), Color.Orange, Color.Black, 0f, Vector2.Zero, backgroundScale * .75f);
        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, $"{GetTextValue("UI.DummyScale")} {Size}", Position + new Vector2(textX, textY + textSpace * 2), Color.Orange, Color.Black, 0f, Vector2.Zero, backgroundScale * .75f);
        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, $"{GetTextValue("UI.DummyGravity")} {!Gravity}", Position + new Vector2(textX, textY + textSpace * 3), Color.Orange, Color.Black, 0f, Vector2.Zero, backgroundScale * .75f);
        base.Draw(spriteBatch);
    }

    public override void SafeMiddleMouseDown(UIMouseEvent evt)
    {
        if (IsMouseHovering)
            BeingDragged = true;
    }

    public override void SafeMiddleMouseUp(UIMouseEvent evt)
    {
        BeingDragged = false;
    }
}

public class DummyButton(ButtonType type) : SmartUIElement
{
    public readonly ButtonType Type = type;
    public float Interpolant;

    public override void SafeClick(UIMouseEvent evt)
    {
        switch (Type)
        {
            case ButtonType.Life:
                if (MaxLife.BetweenNum(GodDummy.LifeAmount - 1, GodDummy.MaxLifeAmount))
                {
                    MaxLife += GodDummy.LifeAmount;
                    Interpolant = 1f;
                }
                else
                {
                    Interpolant = -1f;
                }

                break;
            case ButtonType.Defense:
                if (Defense.BetweenNum(-GodDummy.MaxDefense - 1, GodDummy.MaxDefense))
                {
                    Defense += 5;
                    Interpolant = 1f;
                }
                else
                {
                    Interpolant = -1f;
                }

                break;
            case ButtonType.Scale:
                if (Size.BetweenNum(.75f - .1f, GodDummy.MaxScale))
                {
                    Size += .25f;
                    Interpolant = 1f;
                }
                else
                {
                    Interpolant = -1f;
                }

                break;
            case ButtonType.Rotation:
                break;
            case ButtonType.Gravity:
                Gravity = true;
                Interpolant = 1f;
                break;
            case ButtonType.Reset:
                Size = 1f;
                MaxLife = GodDummy.MaxLifeAmount;
                Rotation = 0f;
                Defense = 0;

                Interpolant = 1f;
                break;
        }

        if (Interpolant > 0f)
            SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 1.2f, Pitch = .3f });
        else
            SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 1.2f, Pitch = -.3f });
    }

    public override void SafeRightClick(UIMouseEvent evt)
    {
        switch (Type)
        {
            case ButtonType.Life:
                if (MaxLife.BetweenNum(GodDummy.LifeAmount, GodDummy.MaxLifeAmount + 1))
                {
                    MaxLife -= GodDummy.LifeAmount;
                    Interpolant = 1f;
                }
                else
                {
                    Interpolant = -1f;
                }

                break;
            case ButtonType.Defense:
                if (Defense.BetweenNum(-GodDummy.MaxDefense, GodDummy.MaxDefense + 1))
                {
                    Defense -= 5;
                    Interpolant = 1f;
                }
                else
                {
                    Interpolant = -1f;
                }

                break;
            case ButtonType.Scale:
                if (Size.BetweenNum(.75f, GodDummy.MaxScale + .1f))
                {
                    Size -= .25f;
                    Interpolant = 1f;
                }
                else
                {
                    Interpolant = -1f;
                }

                break;
            case ButtonType.Rotation:
                break;
            case ButtonType.Gravity:
                Gravity = false;
                Interpolant = 1f;
                break;
        }

        if (Interpolant > 0f)
            SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 1.2f, Pitch = .3f });
        else
            SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 1.2f, Pitch = -.3f });
    }

    public override void SafeUpdate(GameTime gameTime)
    {
        Player p = Main.LocalPlayer;
        GlobalPlayer m = p.Additions();

        if (IsMouseHovering && Type == ButtonType.Rotation)
        {
            if (m.MouseRight.Current)
                Rotation = MathHelper.Clamp(Rotation - .05f, 0f, MathHelper.Pi);
            if (m.MouseLeft.Current)
                Rotation = MathHelper.Clamp(Rotation + .05f, 0f, MathHelper.Pi);
        }

        if (Interpolant < 0f)
            Interpolant += .05f;
        if (Interpolant > 0f)
            Interpolant -= .05f;

        if (IsMouseHovering)
        {
            if (Type == ButtonType.Info)
            {
                Main.instance.MouseText(GetTextValue("UI.DummyInfo"), 0, 0, -1, -1, -1, -1, 0);
            }
            Main.LocalPlayer.mouseInterface = true;
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        Color col = Color.White;
        Rectangle frame = ButtonTextures.Frame(7, 1, (int)Type);
        if (IsMouseHovering && Type != ButtonType.Info)
        {
            col = Color.Tan;
        }
        col = col.Lerp(Color.Red, 1f - InverseLerp(-1f, 0f, Interpolant)).Lerp(Color.Green, Interpolant);

        float rot = 0f;
        if (Type == ButtonType.Rotation)
        {
            rot = Rotation;
        }

        Vector2 pos = GetDimensions().ToRectangle().Center();
        if (Interpolant < 0f)
            pos += Main.rand.NextVector2Circular(Interpolant * 2f, Interpolant * 2f);

        spriteBatch.Draw(ButtonTextures, pos, frame, col, rot, frame.Size() / 2, 1f, 0, 0f);
    }
}