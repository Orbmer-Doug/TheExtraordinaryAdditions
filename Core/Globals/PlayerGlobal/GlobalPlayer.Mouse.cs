using Terraria;
using Terraria.GameInput;

namespace TheExtraordinaryAdditions.Core.Globals;

public partial class GlobalPlayer
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
    public Vector2[] oldMouseWorld = new Vector2[15];

    public Vector2 mouseWorld;
    public Vector2 mouseScreen;

    /// <summary>
    /// The larger this number is the more "fast" the mouse is going
    /// </summary>
    public float oldMouseWorldDistance;

    public bool CanUseMouseButton => !Main.mapFullscreen
        && !Player.mouseInterface && !PlayerInput.WritingText && Main.hasFocus;

    public void UpdateMouse()
    {
        if (Main.CurrentPlayer.whoAmI == Player.whoAmI)
        {
            TriggersPack trigger = PlayerInput.Triggers;
            MouseLeft = new(trigger.JustPressed.MouseLeft, trigger.Current.MouseLeft, trigger.JustReleased.MouseLeft);
            MouseRight = new(trigger.JustPressed.MouseRight, trigger.Current.MouseRight, trigger.JustReleased.MouseRight);
            MouseMiddle = new(trigger.JustPressed.MouseMiddle, trigger.Current.MouseMiddle, trigger.JustReleased.MouseMiddle);

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

            mouseScreen = new Vector2(PlayerInput.MouseX, PlayerInput.MouseY);
            Vector2 transform = Vector2.Transform(mouseScreen, Matrix.Invert(Main.GameViewMatrix?.ZoomMatrix ?? Matrix.Identity));
            mouseWorld = transform + Main.screenPosition + (Main.screenPosition - Main.screenLastPosition);
            if (Player.gravDir == -1f)
                mouseWorld.Y = Main.screenPosition.Y + (Main.screenPosition - Main.screenLastPosition).Y + Main.screenHeight - transform.Y;

            if (oldMouseWorld == null)
            {
                for (int i = 0; i < oldMouseWorld.Length; i++)
                    oldMouseWorld[i] = mouseWorld;
            }
            for (int j = oldMouseWorld.Length - 1; j > 0; j--)
                oldMouseWorld[j] = oldMouseWorld[j - 1];
            oldMouseWorld[0] = mouseWorld;

            oldMouseWorldDistance = Vector2.Distance(oldMouseWorld[0], oldMouseWorld[^1]) / oldMouseWorld.Length;
        }
    }
}
