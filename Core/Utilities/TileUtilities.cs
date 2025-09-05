using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using TheExtraordinaryAdditions.Core.Systems;

namespace TheExtraordinaryAdditions.Core.Utilities;

public static partial class Utility
{
    public static Vector2? FindNearestSurface(Vector2 searchOrigin, bool searchDown, float maxDistance, int searchWidth, bool finePrecision = false)
    {
        // Convert search origin to tile coordinates
        int startX = (int)(searchOrigin.X / 16f);
        int startY = (int)(searchOrigin.Y / 16f);

        // Calculate the maximum search depth in tiles
        int maxDepth = (int)(maxDistance / 16f);

        // Define the x-range to search (centered on startX)
        int minX = Math.Max(0, startX - searchWidth);
        int maxX = Math.Min(Main.maxTilesX - 1, startX + searchWidth);

        Vector2? closestSurface = null;
        float closestDistance = float.MaxValue;

        // Search each column within the x-range
        for (int x = minX; x <= maxX; x++)
        {
            // Start at the y-coordinate of the search origin
            int y = startY;

            // Determine the direction to scan
            int yIncrement = searchDown.ToDirectionInt();
            int maxY = Math.Clamp(startY + (yIncrement * maxDepth), 0, Main.maxTilesY - 1);

            // Scan vertically in the specified direction
            while (y >= 0 && y < Main.maxTilesY && (searchDown ? y <= maxY : y >= maxY))
            {
                Tile tile = ParanoidTileRetrieval(x, y);
                Tile adjacentTile = searchDown
                    ? ParanoidTileRetrieval(x, y - 1) // Tile above for downward search
                    : ParanoidTileRetrieval(x, y + 1); // Tile below for upward search

                // Check if this tile is a surface or ceiling
                bool isSolid = tile.HasUnactuatedTile && (Main.tileSolid[tile.TileType] || (Main.tileSolidTop[tile.TileType] && tile.TileFrameY == 0));
                bool hasAirAdjacent = adjacentTile == null || !adjacentTile.HasUnactuatedTile || !Main.tileSolid[adjacentTile.TileType];

                if (isSolid && hasAirAdjacent)
                {
                    bool validGroundSlope = tile.Slope != SlopeType.Solid && tile.Slope != SlopeType.SlopeUpLeft && tile.Slope != SlopeType.SlopeUpRight;
                    bool validCeilingSlope = tile.Slope != SlopeType.Solid && tile.Slope != SlopeType.SlopeDownLeft && tile.Slope != SlopeType.SlopeDownRight;

                    // Found a surface (downward) or ceiling (upward); calculate its position
                    Vector2 surfacePos;
                    if (finePrecision)
                    {
                        if (searchDown)
                        {
                            // Downward: Top edge of the tile
                            float tileTopY = y * 16f;
                            if (tile.IsHalfBlock)
                                tileTopY += 8f;

                            // For slopes, calculate the y-coordinate based on the slope type
                            float leftY = tileTopY;
                            float rightY = tileTopY;
                            if (validGroundSlope && !tile.IsHalfBlock)
                            {
                                switch (tile.Slope)
                                {
                                    case SlopeType.SlopeDownLeft:
                                        leftY = tileTopY;
                                        rightY = tileTopY + 16f;
                                        break;
                                    case SlopeType.SlopeDownRight:
                                        leftY = tileTopY + 16f;
                                        rightY = tileTopY;
                                        break;
                                }
                            }

                            // Define the line segment for the tile's top edge
                            Vector2 leftEdge = new(x * 16f, leftY);
                            Vector2 rightEdge = new(x * 16f + 16f, rightY);

                            // Project searchOrigin onto the line segment to find the closest point
                            surfacePos = ClosestPointOnLineSegment(searchOrigin, leftEdge, rightEdge);
                        }
                        else
                        {
                            // Upward: Bottom edge of the tile (ceiling)
                            float tileBottomY = y * 16f + 16f;

                            // For slopes, calculate the y-coordinate based on the slope type
                            // Note: For ceilings, we need to consider the bottom edge of the slope
                            float leftY = tileBottomY;
                            float rightY = tileBottomY;
                            if (validCeilingSlope && !tile.IsHalfBlock)
                            {
                                switch (tile.Slope)
                                {
                                    case SlopeType.SlopeUpLeft:
                                        leftY = tileBottomY;
                                        rightY = tileBottomY - 16f; // Bottom-right is higher
                                        break;
                                    case SlopeType.SlopeUpRight:
                                        leftY = tileBottomY - 16f; // Bottom-left is higher
                                        rightY = tileBottomY;
                                        break;
                                }
                            }

                            // Define the line segment for the tile's bottom edge
                            Vector2 leftEdge = new(x * 16f, leftY);
                            Vector2 rightEdge = new(x * 16f + 16f, rightY);

                            // Project searchOrigin onto the line segment to find the closest point
                            surfacePos = ClosestPointOnLineSegment(searchOrigin, leftEdge, rightEdge);
                        }
                    }
                    else
                    {
                        // Default behavior: center of the edge
                        if (searchDown)
                        {
                            surfacePos = new Vector2(x * 16f + 8f, y * 16f);
                            if (tile.IsHalfBlock || validGroundSlope)
                                surfacePos.Y += 8f;
                        }
                        else
                        {
                            surfacePos = new Vector2(x * 16f + 8f, y * 16f + 16f);
                            if (validCeilingSlope)
                                surfacePos.Y -= 8f;
                        }
                    }

                    // Calculate distance to searchOrigin
                    float distance = Vector2.Distance(searchOrigin, surfacePos);

                    // Update the closest surface if this one is closer
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestSurface = surfacePos;
                    }

                    // Stop searching this column; we've found the topmost surface or bottommost ceiling
                    break;
                }

                y += yIncrement;
            }
        }

        return closestSurface;
    }

    public static bool IsThereAChasm(Entity entity, int widthThreshold, out Point start, out Point end, bool requiresLOS = false)
    {
        // Convert entity position to tile coordinates
        int ex = (int)entity.Center.X >> 4; // X tile at entity's center
        int ey = (int)entity.position.Y + entity.height >> 4; // Y tile just below entity's feet
        int direction = entity.direction; // 1 (right) or -1 (left)
        int heightInTiles = (int)Math.Ceiling(entity.height / 16f); // Entity height in tiles (e.g., 3 for player)
        int clearanceHeight = heightInTiles; // Clearance height above the chasm, set to entity's height

        int currentCount = 0; // Count of consecutive chasm tiles
        int startX = -1; // Starting x of potential chasm
        int x = ex; // Current x position in tile coordinates
        bool chasmFound = false;

        // Loop in the entity's direction until we find a valid chasm or hit world boundaries
        while (true)
        {
            x += direction; // Move in the direction
            if (x < 0 || x >= Main.maxTilesX)
                break;

            // Check if the column at x is a chasm
            bool isChasm = true;

            // 1. Check the chasm depth: from ey to ey + heightInTiles - 1
            for (int dy = 0; dy < heightInTiles; dy++)
            {
                int checkY = ey + dy;
                if (checkY < Main.maxTilesY)
                {
                    Tile tile = Framing.GetTileSafely(x, checkY);
                    //new Vector2(x, checkY).ToWorldCoordinates().SuperQuickDust(Color.Yellow);

                    if (tile != null && tile.HasTile && Main.tileSolid[tile.TileType])
                    {
                        isChasm = false;
                        break;
                    }
                }
                // If checkY >= Main.maxTilesY, it's below the world, considered empty
            }

            // 2. Check the clearance above: from ey - 1 to ey - clearanceHeight
            if (isChasm)
            {
                int clearanceStart = Math.Max(0, ey - clearanceHeight);
                for (int y = ey - 1; y >= clearanceStart; y--)
                {
                    Tile tile = Framing.GetTileSafely(x, y);
                    //new Vector2(x, y).ToWorldCoordinates().SuperQuickDust(Color.Red);

                    if (tile != null && tile.HasTile && Main.tileSolid[tile.TileType])
                    {
                        isChasm = false;
                        break;
                    }
                }
            }

            if (isChasm)
            {
                if (currentCount == 0)
                    startX = x; // Mark start of chasm

                currentCount++; // Increment consecutive chasm tiles

                if (currentCount >= widthThreshold && !chasmFound) // Only check path if we haven't already found a valid chasm
                {
                    // Check if the path to the chasm's start is unobstructed, if required
                    if (requiresLOS)
                    {
                        // Check each column between the entity and the chasm's start for a wall
                        int minX = Math.Min(ex, startX);
                        int maxX = Math.Max(ex, startX);
                        bool pathObstructed = false;

                        for (int checkX = minX; checkX <= maxX; checkX++)
                        {
                            // Skip the entity's position and the chasm's start position
                            if (checkX == ex || checkX == startX)
                                continue;

                            // Check for a vertical wall of solid tiles at least heightInTiles tall
                            int solidCount = 0;
                            int startY = ey - 1; // Start checking at the tile above the entity's feet
                            int endY = Math.Max(0, ey - heightInTiles); // Check up to the entity's height above

                            for (int y = startY; y >= endY; y--)
                            {
                                Tile tile = Framing.GetTileSafely(checkX, y);
                                if (tile != null && tile.HasTile && Main.tileSolid[tile.TileType])
                                {
                                    solidCount++;
                                    if (solidCount >= heightInTiles)
                                    {
                                        pathObstructed = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    solidCount = 0; // Reset count if we hit a non-solid tile
                                }
                            }

                            if (pathObstructed)
                                break;
                        }

                        if (pathObstructed)
                        {
                            // Path is obstructed by a wall, reset and continue searching
                            currentCount = 0;
                            continue;
                        }
                    }

                    // Path is clear (or not required), mark chasm as found
                    chasmFound = true;
                }
            }

            else if (chasmFound)
            {
                // We've found a valid chasm and this tile is not part of it, return immediately
                int chasmStart = startX; // First tile of the chasm
                int chasmEnd = x - direction; // Last tile of the chasm (step back one since x is now non-chasm)
                start = new Point(chasmStart, ey);
                end = new Point(chasmEnd, ey);
                return true;
            }

            else
            {
                currentCount = 0; // Reset count if chasm is interrupted before widthThreshold
            }
        }

        if (chasmFound)
        {
            int chasmStart = startX; // First tile of the chasm
            int chasmEnd = x; // Last tile before hitting the boundary
            start = new Point(chasmStart, ey);
            end = new Point(chasmEnd, ey);
            //start.ToWorldCoordinates().SuperQuickDust(Color.Purple, 15);
            //end.ToWorldCoordinates().SuperQuickDust(Color.Blue, 15);
            return true;
        }

        // No chasm found
        start = default;
        end = default;
        return false;
    }

    public class TileData
    {
        public TileTypeData tileTypeData;

        public WallTypeData wallTypeData;

        public TileWallWireStateData tileWallWireStateData;

        public LiquidData liquidData;

        public TileWallBrightnessInvisibilityData tileWallBrightnessInvisibilityData;

        public ref TileTypeData TileTypeData => ref tileTypeData;

        public ref WallTypeData WallTypeData => ref wallTypeData;

        public ref TileWallWireStateData TileWallWireStateData => ref tileWallWireStateData;

        public ref LiquidData LiquidData => ref liquidData;

        public ref TileWallBrightnessInvisibilityData TileWallBrightnessInvisibilityData => ref tileWallBrightnessInvisibilityData;

        public static implicit operator TileData(Tile tile)
        {
            TileData tileData = new()
            {
                TileTypeData = tile.Get<TileTypeData>(),
                WallTypeData = tile.Get<WallTypeData>(),
                TileWallWireStateData = tile.Get<TileWallWireStateData>(),
                LiquidData = tile.Get<LiquidData>(),
                TileWallBrightnessInvisibilityData = tile.Get<TileWallBrightnessInvisibilityData>()
            };
            return tileData;
        }
    }

    public static bool SolidCollisionFix(Vector2 Position, int Width, int Height, bool acceptTopSurfaces = false)
    {
        int value = (int)(Position.X / 16f) - 1;
        int value2 = (int)((Position.X + Width) / 16f) + 2;
        int value3 = (int)(Position.Y / 16f) - 1;
        int value4 = (int)((Position.Y + Height) / 16f) + 2;
        int num = Utils.Clamp(value, 0, Main.maxTilesX - 1);
        value2 = Utils.Clamp(value2, 0, Main.maxTilesX - 1);
        value3 = Utils.Clamp(value3, 0, Main.maxTilesY - 1);
        value4 = Utils.Clamp(value4, 0, Main.maxTilesY - 1);
        Vector2 vector = default;
        for (int i = num; i < value2; i++)
        {
            for (int j = value3; j < value4; j++)
            {
                Tile tile = Main.tile[i, j];
                if (tile == null || !tile.HasUnactuatedTile)
                    continue;

                bool flag = Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
                if (acceptTopSurfaces)
                    flag |= (Main.tileSolidTop[tile.TileType] && tile.TileFrameY == 0) || TileID.Sets.Platforms[tile.TileType];

                if (flag)
                {
                    vector.X = i * 16;
                    vector.Y = j * 16;
                    int num2 = 16;
                    if (tile.IsHalfBlock)
                    {
                        vector.Y += 8f;
                        num2 -= 8;
                    }

                    if (Position.X + Width > vector.X && Position.X < vector.X + 16f && Position.Y + Height > vector.Y && Position.Y < vector.Y + num2)
                        return true;
                }
            }
        }

        return false;
    }

    public static bool IsInWorld(this Point16 point) => point.X >= 0 && point.Y >= 0 && point.X < Main.maxTilesX && point.Y < Main.maxTilesY;
    public static bool IsInWorld(this Point point) => point.X >= 0 && point.Y >= 0 && point.X < Main.maxTilesX && point.Y < Main.maxTilesY;
    public static Vector2 ClampInWorld(this Vector2 vector) => new(
        MathHelper.Clamp(vector.X, Main.leftWorld, Main.rightWorld),
        MathHelper.Clamp(vector.Y, Main.topWorld, Main.bottomWorld));

    /// Notes:
    /// <see cref="Collision.TileCollision(Vector2, Vector2, int, int, bool, bool, int)"/> and <see cref="Collision.AnyCollision(Vector2, Vector2, int, int, bool)"/> take a inputted velocity and return it in relation to if there is a tile there or not, 0 if there is
    /// <see cref="Collision.SolidCollision(Vector2, int, int)"/> can be used as a boolean operator to determine if a position intersects a solid tile, useful for reaper scythe like things
    public static bool CheckSolidGround(this Player player, int solidGroundAhead = 0, int airExposureNeeded = 0)
    {
        if (player.velocity.Y != 0f)
        {
            return false;
        }
        bool ConditionMet = true;
        int playerCenterX = (int)player.Center.X / 16;
        int playerCenterY = (int)(player.position.Y + player.height - 1f) / 16 + 1;
        for (int i = 0; i <= solidGroundAhead; i++)
        {
            ConditionMet = Main.tile[playerCenterX + player.direction * i, playerCenterY].IsTileSolidGround();
            if (!ConditionMet)
            {
                return ConditionMet;
            }
            for (int j = 1; j <= airExposureNeeded; j++)
            {
                Tile checkedTile = Main.tile[playerCenterX + player.direction * i, playerCenterY - j];
                ConditionMet = !(checkedTile != null) || !checkedTile.HasUnactuatedTile || !Main.tileSolid[checkedTile.TileType];
                if (!ConditionMet)
                {
                    return ConditionMet;
                }
            }
        }
        return ConditionMet;
    }

    public static bool Active(this Tile tile, bool countActuater = true)
    {
        if (tile.Get<TileWallWireStateData>().IsActuated == true && countActuater == true)
            return false;

        return tile.Get<TileWallWireStateData>().HasTile;
    }

    public static bool AnyExposedAir(int x, int y)
    {
        Tile left = Framing.GetTileSafely(x - 1, y);
        Tile right = Framing.GetTileSafely(x + 1, y);
        Tile top = Framing.GetTileSafely(x, y - 1);
        Tile bottom = Framing.GetTileSafely(x, y + 1);

        return !left.HasTile || !right.HasTile || !top.HasTile || !bottom.HasTile;
    }

    public static Tile ParanoidTileRetrieval(int x, int y)
    {
        if (!WorldGen.InWorld(x, y, 0))
        {
            return default;
        }
        return Main.tile[x, y];
    }

    public static float GetTileRNG(this Point tilePos, int shift = 0)
    {
        return (float)(Math.Sin(tilePos.X * 17.07947 + shift * 36) + Math.Sin(tilePos.Y * 25.13274)) * 0.25f + 0.5f;
    }

    public static Vector2 FindSmashSpot(this NPC NPC, Vector2 target)
    {
        Vector2 pos = target;
        Point world = default;
        for (int i = 0; i < 36; i++)
        {
            world = new Point(Utils.ToTileCoordinates(pos).X, Utils.ToTileCoordinates(pos).Y + i);
            if (WorldGen.InWorld(world.X, world.Y, 0) && WorldGen.SolidTileAllowTopSlope(world.X, world.Y + i))
            {
                pos.Y = (int)(pos.Y / 16f) * 16f + i * 16 - NPC.height / 2f - 8f;
                break;
            }
        }
        return pos;
    }

    public static Vector2 FindSmashSpot(this Projectile Projectile, Vector2 target)
    {
        Vector2 pos = target;
        Point world = default;
        for (int i = 0; i < 32; i++)
        {
            world = new Point(Utils.ToTileCoordinates(pos).X, Utils.ToTileCoordinates(pos).Y + i);
            if (WorldGen.InWorld(world.X, world.Y, 0) && WorldGen.SolidTileAllowTopSlope(world.X, world.Y + i))
            {
                pos.Y = (int)(pos.Y / 16f) * 16f + i * 16 - Projectile.height / 2f + 16f;
                break;
            }
        }
        return pos;
    }

    public static bool IsTileSolid(this Tile tile) => tile.HasUnactuatedTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];

    public static Point ToSafeTileCoordinates(this Vector2 vec)
    {
        return new Point((int)MathHelper.Clamp((int)vec.X / 16f, 0, Main.maxTilesX), (int)MathHelper.Clamp((int)vec.Y / 16f, 0, Main.maxTilesY));
    }

    public static bool ClearPath(Vector2 start, Vector2 end)
    {
        Vector2 direction = end - start;

        for (int i = 0; i < direction.Length(); i += 4)
        {
            Vector2 toLookAt = start + Vector2.Normalize(direction) * i;

            if (Framing.GetTileSafely((int)(toLookAt.X / 16), (int)(toLookAt.Y / 16)).HasTile && Main.tileSolid[Framing.GetTileSafely((int)(toLookAt.X / 16), (int)(toLookAt.Y / 16)).TileType])
                return false;
        }

        return true;
    }

    public static bool IsTileSolidOrPlatform(this Tile tile) => tile != null && tile.HasUnactuatedTile && Main.tileSolid[tile.TileType];

    public static bool IsEdgeTile(int x, int y)
    {
        Tile leftTile = Framing.GetTileSafely(x - 1, y);
        Tile rightTile = Framing.GetTileSafely(x + 1, y);
        Tile topTile = Framing.GetTileSafely(x, y - 1);
        Tile bottomTile = Framing.GetTileSafely(x, y + 1);

        bool isEdge =
            !(leftTile.HasTile && Main.tileSolid[leftTile.TileType]) ||
            !(rightTile.HasTile && Main.tileSolid[rightTile.TileType]) ||
            !(topTile.HasTile && Main.tileSolid[topTile.TileType]) ||
            !(bottomTile.HasTile && Main.tileSolid[bottomTile.TileType]);

        return isEdge;
    }

    public static bool IsTileSolidGround(this Tile tile)
    {
        if (tile != null && tile.HasUnactuatedTile)
        {
            if (!Main.tileSolid[tile.TileType])
            {
                return Main.tileSolidTop[tile.TileType];
            }
            return true;
        }
        return false;
    }

    public static bool IsTileFull(this Tile tile)
    {
        if (tile != null && tile.HasTile)
        {
            return Main.tileSolid[tile.TileType];
        }
        return false;
    }

    public static bool IsTileExposedToAir(int x, int y)
    {
        return IsTileExposedToAir(x, y, out float? angleToOpenAir);
    }

    public static bool IsTileExposedToAir(int x, int y, out float? angleToOpenAir)
    {
        angleToOpenAir = null;
        Tile val = ParanoidTileRetrieval(x - 1, y);
        if (!val.HasTile)
        {
            angleToOpenAir = MathHelper.Pi;
            return true;
        }
        val = ParanoidTileRetrieval(x + 1, y);
        if (!val.HasTile)
        {
            angleToOpenAir = 0f;
            return true;
        }
        val = ParanoidTileRetrieval(x, y - 1);
        if (!val.HasTile)
        {
            angleToOpenAir = MathHelper.Pi / 2f;
            return true;
        }
        val = ParanoidTileRetrieval(x, y + 1);
        if (!val.HasTile)
        {
            angleToOpenAir = -MathHelper.Pi / 2f;
            return true;
        }
        return false;
    }

    public static void PlatformHangOffset(int i, int j, ref int offsetY)
    {
        Tile tile = Main.tile[i, j];
        TileObjectData data = TileObjectData.GetTileData(tile);
        int num = i - tile.TileFrameX / 18 % data.Width;
        int topLeftY = j - tile.TileFrameY / 18 % data.Height;
        if (WorldGen.IsBelowANonHammeredPlatform(num, topLeftY))
        {
            offsetY -= 8;
        }
    }
}