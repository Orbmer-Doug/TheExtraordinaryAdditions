using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles.Shader;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Core.Utilities;


// Note: DONT LEAVE THIS IN FINAL RELEASE
public class DebugSystem : ModSystem
{
    public override void PostUpdateEverything()
    {
    }
}

public static class AdditionsDebug
{
    public static void RotatedPoints(this RotatedRectangle rect)
    {
        SuperQuickDust(rect.Position, Color.Red);
        SuperQuickDust(rect.Top, Color.Yellow);
        SuperQuickDust(rect.TopRight, Color.Black);
        SuperQuickDust(rect.Right, Color.Blue);
        SuperQuickDust(rect.BottomRight, Color.Purple);
        SuperQuickDust(rect.Bottom, Color.Pink);
        SuperQuickDust(rect.BottomLeft, Color.White);
        SuperQuickDust(rect.Left, Color.SaddleBrown);
    }

    public static void DebugRectangle(this Rectangle rect, Color? color = null, int scale = 5, int life = 10)
    {
        Vector2[] corners = rect.Corners();

        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 corner = corners[i];
            Vector2 next = corners[(i + 1) % 4];

            DebugLine(corner, next, color, scale, life);
        }
    }

    public static void DebugLine(this Vector2 val, Vector2 val2, Color? color = null, int scale = 5, int life = 10)
    {
        for (float i = 0f; i < 1f; i += .03f)
        {
            Vector2 pos = Vector2.Lerp(val, val2, i);
            pos.SuperQuickDust(color, scale, life, val.SafeDirectionTo(val2) * 0f);
        }
    }

    public static void DebugField(this Vector2 viewerPosition, float viewerRotation, Vector2 targetPosition, float viewAngle, float maxDistance)
    {
        Vector2 viewerDirection = viewerRotation.ToRotationVector2();
        Vector2 directionEnd = viewerPosition + viewerDirection * maxDistance;
        AdditionsDebug.DebugLine(viewerPosition, directionEnd, Color.Orange);

        float halfAngle = viewAngle / 2f;
        Vector2 leftBoundary = (viewerRotation - halfAngle).ToRotationVector2() * maxDistance;
        Vector2 rightBoundary = (viewerRotation + halfAngle).ToRotationVector2() * maxDistance;
        AdditionsDebug.DebugLine(viewerPosition, viewerPosition + leftBoundary, Color.Blue); // Left boundary
        AdditionsDebug.DebugLine(viewerPosition, viewerPosition + rightBoundary, Color.Blue); // Right boundary
        bool inFOV = viewerPosition.IsInFieldOfView(viewerRotation, targetPosition, viewAngle, maxDistance);

        Dust.NewDustPerfect(targetPosition, inFOV ? DustID.GreenFairy : DustID.RedMoss, Vector2.Zero, 0, default, 1f);

        for (float angle = -halfAngle; angle <= halfAngle; angle += MathHelper.ToRadians(5f))
        {
            Vector2 conePoint = (viewerRotation + angle).ToRotationVector2() * maxDistance;
            AdditionsDebug.DebugLine(viewerPosition, viewerPosition + conePoint, Color.Gray);
        }
    }

    public static void SuperQuickDust(this Vector2 pos, Color? color = null, int scale = 5, int life = 10, Vector2? velocity = null) => ParticleRegistry.SpawnDebugParticle(pos, color, scale, life, velocity);
    public static void SuperQuickDust(this Point pos, Color? color = null, int scale = 5, int life = 10, Vector2? velocity = null) => ParticleRegistry.SpawnDebugParticle(pos.ToVector2(), color, scale, life, velocity);

    public static void TileDataAtMouse()
    {
        Vector2 world = Main.LocalPlayer.Additions().mouseWorld;
        Point tiles = world.ToTileCoordinates();
        Tile tile = ParanoidTileRetrieval(tiles.X, tiles.Y);

        if (tile != null)
            DirectlyDisplayText($"Has tile? {tile.HasTile} : Type={tile.TileType} : Solid Top? {Main.tileSolidTop[tile.TileType]} : Solid? {Main.tileSolid[tile.TileType]} : Half Block? {tile.IsHalfBlock} : Is unactuated? {tile.HasUnactuatedTile} : Slope={tile.Slope} : LiquidAmt={tile.LiquidAmount}", Main.DiscoColor);
    }

    public static void DetailedLog(this string message)
    {
        AdditionsMain.Instance.Logger.Debug("> " + message);
        DirectlyDisplayText(message);
    }

    #region SB
    public static void RenderRectangle(this SpriteBatch sb, Rectangle rect, Color? color = null)
    {
        Color c = color ?? Color.Red;
        int x = rect.X + rect.Width / 2;
        int y = rect.Y + rect.Height / 2;
        Point pos = (new Vector2(x, y) - Main.screenPosition).ToPoint();
        sb.Draw(AssetRegistry.GetTexture(AdditionsTexture.Pixel), new Rectangle(pos.X, pos.Y, rect.Width, rect.Height), null, c,
            0f, AssetRegistry.GetTexture(AdditionsTexture.Pixel).Size() / 2f, 0, 0f);
    }

    public static void RenderRectangle(this SpriteBatch sb, RotatedRectangle rect, Color? color = null, bool inverseRot = false)
    {
        Color c = color ?? Color.Red;
        int x = rect.X + rect.Width / 2;
        int y = rect.Y + rect.Height / 2;
        Point pos = (new Vector2(x, y) - Main.screenPosition).ToPoint();
        sb.Draw(AssetRegistry.GetTexture(AdditionsTexture.Pixel), new Rectangle(pos.X, pos.Y, rect.Width, rect.Height), null, c,
            inverseRot ? -rect.Rotation : rect.Rotation, AssetRegistry.GetTexture(AdditionsTexture.Pixel).Size() / 2f, 0, 0f);
    }


    #endregion
}