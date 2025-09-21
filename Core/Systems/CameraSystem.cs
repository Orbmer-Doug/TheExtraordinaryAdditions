using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Core.Systems;

/// <summary>
/// Primarily made by Mirsario in Terraria Overhaul
/// </summary>
[Autoload(Side = ModSide.Client)]
public sealed class CameraSystem : ModSystem
{
    /// <summary>
    /// Where the <see cref="Main.screenPosition"/> would be without modifications.
    /// </summary>
    public static Vector2 UnmodifiedCameraPosition =>
        Main.LocalPlayer.TopLeft + new Vector2(Main.LocalPlayer.width * 0.5f, Main.LocalPlayer.height - 21f) - Main.ScreenSize.ToVector2() * 0.5f + Vector2.UnitY * Main.LocalPlayer.gfxOffY;
    public static Rectangle CameraRect => new((int)Main.Camera.ScaledPosition.X, (int)Main.Camera.ScaledPosition.Y, (int)Main.Camera.ScaledSize.X, (int)Main.Camera.ScaledSize.Y);

    public delegate void CameraModifierDelegate(Action innerAction);

    private static bool limitCameraUpdateRateOverride = false;

    private readonly static SortedList<int, CameraModifierDelegate> cameraModifiers = [];

    private static Vector2 lastPositionRemainder;
    private static Vector2 screenCenter;
    public static Vector2 ScreenSize { get; private set; }
    public static Vector2 ScreenHalf { get; private set; }
    public static Rectangle ScreenRect { get; private set; }
    public static Vector2 MouseWorld { get; private set; }
    public static Vector2 ScreenCenter
    {
        get => screenCenter;
        set
        {
            Main.screenPosition = new Vector2(value.X - Main.screenWidth * 0.5f, value.Y - Main.screenHeight * 0.5f);
            UpdateCache();
        }
    }
    public static bool LimitCameraUpdateRate => limitCameraUpdateRateOverride;

    public override void Load()
    {
        // Floor camera position, restoring previous remainders before the next camera update.
        // Maximum priority.
        RegisterCameraModifier(int.MaxValue, innerAction =>
        {
            Main.screenPosition += lastPositionRemainder;

            innerAction();

            Vector2 flooredPosition = new(MathF.Floor(Main.screenPosition.X), MathF.Floor(Main.screenPosition.Y));

            flooredPosition += Vector2.One;

            lastPositionRemainder = Main.screenPosition - flooredPosition;

            Main.screenPosition = flooredPosition;
        });

        Main.QueueMainThreadAction(() =>
        {
            On_Main.DoDraw_UpdateCameraPosition += orig =>
            {
                if (Main.gameMenu)
                {
                    orig();
                    PostCameraUpdate();
                    return;
                }

                int i = 0;

                void ModifierRecursion()
                {
                    int iCopy = i++;

                    if (iCopy < cameraModifiers.Count)
                    {
                        cameraModifiers.Values[iCopy](ModifierRecursion);
                    }
                    else if (!LimitCameraUpdateRate || !TimeSystem.RenderOnlyFrame)
                    {
                        orig();
                    }
                }

                lock (cameraModifiers)
                {
                    ModifierRecursion();
                }

                PostCameraUpdate();
            };
        });
    }

    public override void PostSetupContent()
    {
        if (ModLoader.HasMod("HighFPSSupport"))
            limitCameraUpdateRateOverride = false;
    }

    public override void Unload()
    {
        lock (cameraModifiers)
        {
            cameraModifiers.Clear();
        }
    }

    public static void RegisterCameraModifier(int priority, CameraModifierDelegate function)
    {
        lock (cameraModifiers)
        {
            cameraModifiers.Add(-priority, function);
        }
    }

    private static void PostCameraUpdate()
    {
        UpdateCache();
    }

    private static void UpdateCache()
    {
        MouseWorld = Main.MouseWorld;
        ScreenSize = new(Main.screenWidth, Main.screenHeight);
        ScreenHalf = new(Main.screenWidth * 0.5f, Main.screenHeight * 0.5f);
        ScreenRect = new((int)Main.screenPosition.X, (int)Main.screenPosition.Y, Main.screenWidth, Main.screenHeight);
        screenCenter = new(Main.screenPosition.X + Main.screenWidth * 0.5f, Main.screenPosition.Y + Main.screenHeight * 0.5f);
    }

    public static Vector2 Position
    {
        get;
        set;
    }
    public static float Interpolant
    {
        get;
        set;
    }
    public static float Zoom
    {
        get;
        set;
    }
    public static bool ManualPause
    {
        get;
        set;
    }

    public override void ModifyScreenPosition()
    {
        if (Main.LocalPlayer.dead && !Main.gamePaused)
        {
            Zoom = MathHelper.Lerp(Zoom, 0f, 0.13f);
            Interpolant = 0f;
            return;
        }

        // Handle camera focus effects.
        if (Interpolant > 0f)
        {
            Vector2 idealScreenPosition = Position - Main.ScreenSize.ToVector2() * 0.5f;
            Main.screenPosition = Vector2.Lerp(Main.screenPosition, idealScreenPosition, Interpolant);
        }

        // Make interpolants gradually return to their original values.
        if (!Main.gamePaused && ManualPause == false)
        {
            Interpolant = MathHelper.Clamp(Interpolant - 0.06f, 0f, 1f);
            Zoom = MathHelper.Lerp(Zoom, 0f, 0.09f);
        }
        ManualPause = false;
    }

    public override void ModifyTransformMatrix(ref SpriteViewMatrix Transform)
    {
        Transform.Zoom *= 1f + Zoom;
    }

    /// <summary>
    /// Easily set the camera of the local player
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="interpolant"></param>
    /// <param name="zoom"></param>
    public static void SetCamera(Vector2 pos, float interpolant, float zoom = 0f, bool pause = false)
    {
        Position = pos;
        Interpolant = interpolant;
        Zoom = zoom;
        ManualPause = pause;
    }
}