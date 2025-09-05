using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
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
    public static GlobalPlayer ModdedPlayer => Owner.Additions();

    [Flags]
    public enum ButtonType
    {
        Life = 0,
        Defense = 1,
        Scale = 2,
        MoveSpeed = 3,
        Gravity = 4,
        IsBoss = 5,
        Rotation = 6,
        Info = 7,
        Reset = 8,
    }
    public SmartUIElement[] Buttons = [new DummyButton(ButtonType.Life), new DummyButton(ButtonType.Defense),
        new DummyButton(ButtonType.Scale), new DummyButton(ButtonType.Rotation),
        new DummyButton(ButtonType.Gravity), new DummyButton(ButtonType.MoveSpeed), new DummyButton(ButtonType.IsBoss),
        new DummyButton(ButtonType.Info), new DummyButton(ButtonType.Reset)];

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
            switch ((int)(button as DummyButton).Type)
            {
                case 0:
                    pos = new(ButtonSize, ButtonSize);
                    break;
                case 1:
                    pos = new(ButtonSize * 2, ButtonSize);
                    break;
                case 2:
                    pos = new(ButtonSize * 3, ButtonSize);
                    break;
                case 3:
                    pos = new(ButtonSize * 4, ButtonSize);
                    break;
                case 4:
                    pos = new(ButtonSize, ButtonSize * 3);
                    break;
                case 5:
                    pos = new(ButtonSize * 2, ButtonSize * 3);
                    break;
                case 6:
                    pos = new(ButtonSize * 3, ButtonSize * 3);
                    break;
                case 7:
                    pos = new(244, 46);
                    break;
                case 8:
                    pos = new(ButtonSize * 4, ButtonSize * 3);
                    break;
            }

            AddElement(button, pos, 32, 32);
        }
        Height = new(BGTex.Size().Y, 0f);
        Width = new(BGTex.Size().X, 0f);
    }

    public Vector2 Position;
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
                switch ((int)(element as DummyButton).Type)
                {
                    case 0:
                        pos = new(ButtonSize, ButtonSize);
                        break;
                    case 1:
                        pos = new(ButtonSize * 2, ButtonSize);
                        break;
                    case 2:
                        pos = new(ButtonSize * 3, ButtonSize);
                        break;
                    case 3:
                        pos = new(ButtonSize * 4, ButtonSize);
                        break;
                    case 4:
                        pos = new(ButtonSize, ButtonSize * 3);
                        break;
                    case 5:
                        pos = new(ButtonSize * 2, ButtonSize * 3);
                        break;
                    case 6:
                        pos = new(ButtonSize * 3, ButtonSize * 3);
                        break;
                    case 7:
                        pos = new(254, 56);
                        break;
                    case 8:
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

        Player p = Main.LocalPlayer;
        GlobalPlayer m = p.Additions();

        const float textX = -5f;
        const float textY = -70f;
        const float textSpace = 25f;
        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, $"Life: {m.DummyMaxLife}", Position + new Vector2(textX, textY), Color.Orange, Color.Black, 0f, Vector2.Zero, backgroundScale * .75f);
        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, $"Defence: {m.DummyDefense}", Position + new Vector2(textX, textY + textSpace), Color.Orange, Color.Black, 0f, Vector2.Zero, backgroundScale * .75f);
        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, $"Scale: {m.DummyScale}", Position + new Vector2(textX, textY + textSpace * 2), Color.Orange, Color.Black, 0f, Vector2.Zero, backgroundScale * .75f);
        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, $"Affected by gravity?: {m.DummyGravity}", Position + new Vector2(textX, textY + textSpace * 3), Color.Orange, Color.Black, 0f, Vector2.Zero, backgroundScale * .75f);
        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, $"Counted as a boss?: {m.DummyBoss}", Position + new Vector2(textX, textY + textSpace * 4), Color.Orange, Color.Black, 0f, Vector2.Zero, backgroundScale * .75f);
        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, $"Movement speed: {m.DummyMoveSpeed}", Position + new Vector2(textX, textY + textSpace * 5), Color.Orange, Color.Black, 0f, Vector2.Zero, backgroundScale * .75f);
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
        Player p = Main.LocalPlayer;
        GlobalPlayer m = p.Additions();
        switch (Type)
        {
            case ButtonType.Life:
                if (m.DummyMaxLife.BetweenNum(GodDummy.LifeAmount - 1, GodDummy.MaxLifeAmount))
                {
                    m.DummyMaxLife += GodDummy.LifeAmount;
                    Interpolant = 1f;
                }
                else
                {
                    Interpolant = -1f;
                }

                break;
            case ButtonType.Defense:
                if (m.DummyDefense.BetweenNum(-GodDummy.MaxDefense - 1, GodDummy.MaxDefense))
                {
                    m.DummyDefense += 5;
                    Interpolant = 1f;
                }
                else
                {
                    Interpolant = -1f;
                }

                break;
            case ButtonType.Scale:
                if (m.DummyScale.BetweenNum(.75f - .1f, GodDummy.MaxScale))
                {
                    m.DummyScale += .25f;
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
                m.DummyGravity = true;
                Interpolant = 1f;
                break;
            case ButtonType.MoveSpeed:
                if (m.DummyMoveSpeed.BetweenNum(0f - 1f, GodDummy.MaxSpeed))
                {
                    m.DummyMoveSpeed += 2f;
                    Interpolant = 1f;
                }
                else
                {
                    Interpolant = -1f;
                }

                break;
            case ButtonType.IsBoss:
                m.DummyBoss = true;
                Interpolant = 1f;
                break;
            case ButtonType.Reset:
                m.DummyScale = 1f;
                m.DummyMoveSpeed = 0f;
                m.DummyMaxLife = GodDummy.MaxLifeAmount;
                m.DummyBoss = m.DummyGravity = false;
                m.DummyRotation = 0f;
                m.DummyDefense = 0;

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
        Player p = Main.LocalPlayer;
        GlobalPlayer m = p.Additions();
        switch (Type)
        {
            case ButtonType.Life:
                if (m.DummyMaxLife.BetweenNum(GodDummy.LifeAmount, GodDummy.MaxLifeAmount + 1))
                {
                    m.DummyMaxLife -= GodDummy.LifeAmount;
                    Interpolant = 1f;
                }
                else
                {
                    Interpolant = -1f;
                }

                break;
            case ButtonType.Defense:
                if (m.DummyDefense.BetweenNum(-GodDummy.MaxDefense, GodDummy.MaxDefense + 1))
                {
                    m.DummyDefense -= 5;
                    Interpolant = 1f;
                }
                else
                {
                    Interpolant = -1f;
                }

                break;
            case ButtonType.Scale:
                if (m.DummyScale.BetweenNum(.75f, GodDummy.MaxScale + .1f))
                {
                    m.DummyScale -= .25f;
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
                m.DummyGravity = false;
                Interpolant = 1f;
                break;
            case ButtonType.MoveSpeed:
                if (m.DummyMoveSpeed.BetweenNum(0f, GodDummy.MaxSpeed + 1f))
                {
                    m.DummyMoveSpeed -= 2f;
                    Interpolant = 1f;
                }
                else
                {
                    Interpolant = -1f;
                }

                break;
            case ButtonType.IsBoss:
                m.DummyBoss = false;
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
                m.DummyRotation = MathHelper.Clamp(m.DummyRotation - .05f, 0f, MathHelper.Pi);
            if (m.MouseLeft.Current)
                m.DummyRotation = MathHelper.Clamp(m.DummyRotation + .05f, 0f, MathHelper.Pi);
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
        Player p = Main.LocalPlayer;
        GlobalPlayer m = p.Additions();

        Color col = Color.White;
        Rectangle frame = ButtonTextures.Frame(9, 1, (int)Type);
        if (IsMouseHovering && Type != ButtonType.Info)
        {
            col = Color.Tan;
        }
        col = col.Lerp(Color.Red, 1f - InverseLerp(-1f, 0f, Interpolant)).Lerp(Color.Green, Interpolant);

        float rot = 0f;
        if (Type == ButtonType.Rotation)
        {
            rot = m.DummyRotation;
        }

        Vector2 pos = GetDimensions().ToRectangle().Center();
        if (Interpolant < 0f)
            pos += Main.rand.NextVector2Circular(Interpolant * 2f, Interpolant * 2f);

        spriteBatch.Draw(ButtonTextures, pos, frame, col, rot, frame.Size() / 2, 1f, 0, 0f);
    }
}