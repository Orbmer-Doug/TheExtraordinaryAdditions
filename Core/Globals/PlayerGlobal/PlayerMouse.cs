using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Core.Globals.PlayerGlobal;

public sealed class PlayerMouse : ModPlayer
{
    public readonly record struct MouseButtonState(bool JustPressed, bool Current, bool JustReleased);

    // Any state the mouse is in
    public MouseButtonState MouseLeft;
    public MouseButtonState MouseRight;
    public MouseButtonState MouseMiddle;

    // Checks for if the mouse is in the world
    public MouseButtonState SafeMouseLeft;
    public MouseButtonState SafeMouseRight;
    public MouseButtonState SafeMouseMiddle;

    /// <summary>
    /// Captures the last 15 positions of the players cursor
    /// </summary>
    public Vector2[] OldMouseWorld = new Vector2[15];

    public Vector2 MouseWorld;
    public Vector2 MouseScreen;

    /// <summary>
    /// The larger this number is the more "fast" the mouse is going
    /// </summary>
    public float OldMouseWorldDistance;

    public bool CanUseMouseButton => !Main.mapFullscreen
                                     && !Player.mouseInterface && !PlayerInput.WritingText && Main.hasFocus;

    public override void PreUpdate()
    {
        if (Main.CurrentPlayer.whoAmI == Player.whoAmI)
        {
            TriggersPack trigger = PlayerInput.Triggers;
            MouseLeft = new(trigger.JustPressed.MouseLeft, trigger.Current.MouseLeft, trigger.JustReleased.MouseLeft);
            MouseRight = new(trigger.JustPressed.MouseRight, trigger.Current.MouseRight,
                trigger.JustReleased.MouseRight);
            MouseMiddle = new(trigger.JustPressed.MouseMiddle, trigger.Current.MouseMiddle,
                trigger.JustReleased.MouseMiddle);

            SafeMouseLeft = new(
                trigger.JustPressed.MouseLeft && CanUseMouseButton,
                trigger.Current.MouseLeft && CanUseMouseButton,
                trigger.JustReleased.MouseLeft && CanUseMouseButton);

            SafeMouseRight = new(
                trigger.JustPressed.MouseRight && CanUseMouseButton,
                trigger.Current.MouseRight && CanUseMouseButton,
                trigger.JustReleased.MouseRight && CanUseMouseButton);

            SafeMouseMiddle = new(
                trigger.JustPressed.MouseMiddle && CanUseMouseButton,
                trigger.Current.MouseMiddle && CanUseMouseButton,
                trigger.JustReleased.MouseMiddle && CanUseMouseButton);

            MouseScreen = new Vector2(PlayerInput.MouseX, PlayerInput.MouseY);
            Vector2 transform = Vector2.Transform(MouseScreen,
                Matrix.Invert(Main.GameViewMatrix?.ZoomMatrix ?? Matrix.Identity));
            MouseWorld = transform + Main.screenPosition + (Main.screenPosition - Main.screenLastPosition);
            if ((int) Player.gravDir == -1)
                MouseWorld.Y = Main.screenPosition.Y + (Main.screenPosition - Main.screenLastPosition).Y +
                    Main.screenHeight - transform.Y;

            if (OldMouseWorld == null)
            {
                OldMouseWorld = new Vector2[15];
                for (int i = 0; i < 15; i++)
                    OldMouseWorld[i] = MouseWorld;
            }

            for (int j = OldMouseWorld.Length - 1; j > 0; j--)
                OldMouseWorld[j] = OldMouseWorld[j - 1];
            OldMouseWorld[0] = MouseWorld;

            OldMouseWorldDistance = Vector2.Distance(OldMouseWorld[0], OldMouseWorld[^1]) / OldMouseWorld.Length;
        }
    }
}
