using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace TheExtraordinaryAdditions.Core.Utilities;

public static partial class Utility
{
    #region Raycasting
    [Flags]
    public enum CollisionTarget : byte
    {
        None = 0,
        Tiles = 1 << 1,
        Liquid = 1 << 2,
        NPCs = 1 << 3,
        Players = 1 << 4,
    }

    public static Vector2 LaserCollision(Vector2 start, Vector2 end, CollisionTarget targets, float thickness = 0f, int numRays = 5)
    {
        List<(Vector2 point, CollisionTarget targetType, bool noTileCollide)> intersections = new List<(Vector2, CollisionTarget, bool)>();
        float maxDistanceSquared = (end - start).LengthSquared();

        // Collect tile intersections
        if (targets.HasFlag(CollisionTarget.Tiles))
        {
            Vector2? tilePoint = RaytraceTiles(start, end, false, thickness, numRays);
            if (tilePoint.HasValue)
            {
                float distanceSquared = (tilePoint.Value - start).LengthSquared();
                if (distanceSquared <= maxDistanceSquared)
                {
                    intersections.Add((tilePoint.Value, CollisionTarget.Tiles, false));
                }
            }
        }

        // Collect liquid intersections
        if (targets.HasFlag(CollisionTarget.Liquid))
        {
            Vector2? liquidPoint = RaytraceLiquid(start, end, thickness, numRays);
            if (liquidPoint.HasValue)
            {
                float distanceSquared = (liquidPoint.Value - start).LengthSquared();
                if (distanceSquared <= maxDistanceSquared)
                {
                    intersections.Add((liquidPoint.Value, CollisionTarget.Liquid, false));
                }
            }
        }

        // Collect NPC intersections
        if (targets.HasFlag(CollisionTarget.NPCs))
        {
            Vector2? npcPoint = RaytraceNPCs(start, end, thickness, numRays);
            if (npcPoint.HasValue)
            {
                float distanceSquared = (npcPoint.Value - start).LengthSquared();
                if (distanceSquared <= maxDistanceSquared)
                {
                    // Determine if the NPC at the intersection point can pass tiles
                    bool noTileCollide = false;
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (npc.Hitbox.Contains((int)npcPoint.Value.X, (int)npcPoint.Value.Y))
                        {
                            noTileCollide = npc.noTileCollide;
                            break;
                        }
                    }
                    intersections.Add((npcPoint.Value, CollisionTarget.NPCs, noTileCollide));
                }
            }
        }

        // Collect player intersections
        if (targets.HasFlag(CollisionTarget.Players))
        {
            Vector2? playerPoint = RaytracePlayers(start, end, thickness, numRays);
            if (playerPoint.HasValue)
            {
                float distanceSquared = (playerPoint.Value - start).LengthSquared();
                if (distanceSquared <= maxDistanceSquared)
                {
                    intersections.Add((playerPoint.Value, CollisionTarget.Players, false));
                }
            }
        }

        // If no intersections, return the end point
        if (intersections.Count == 0)
            return end;

        // Find the closest tile intersection, if any
        float closestTileDistanceSquared = float.MaxValue;
        Vector2? closestTilePoint = null;
        foreach (var intersection in intersections)
        {
            if (intersection.targetType == CollisionTarget.Tiles)
            {
                float distanceSquared = (intersection.point - start).LengthSquared();
                if (distanceSquared < closestTileDistanceSquared)
                {
                    closestTileDistanceSquared = distanceSquared;
                    closestTilePoint = intersection.point;
                }
            }
        }

        // Find the closest valid intersection
        Vector2 closestPoint = end;
        float closestDistanceSquared = maxDistanceSquared;
        foreach (var intersection in intersections)
        {
            float distanceSquared = (intersection.point - start).LengthSquared();

            // For NPCs that don't collide with tiles, only consider them if they are closer than the tile
            if (intersection.targetType == CollisionTarget.NPCs && !intersection.noTileCollide)
            {
                if (closestTilePoint.HasValue && distanceSquared > closestTileDistanceSquared)
                    continue; // Skip grounded NPCs behind tiles
            }
            if (distanceSquared < closestDistanceSquared)
            {
                closestDistanceSquared = distanceSquared;
                closestPoint = intersection.point;
            }
        }

        return closestPoint;
    }

    public static Vector2 LaserCollision(Vector2 start, Vector2 end, CollisionTarget targets, out CollisionTarget hitTarget, float thickness = 0f, int numRays = 5)
    {
        List<(Vector2 point, CollisionTarget targetType, bool noTileCollide)> intersections = new List<(Vector2, CollisionTarget, bool)>();
        float maxDistanceSquared = (end - start).LengthSquared();

        // Collect tile intersections
        if (targets.HasFlag(CollisionTarget.Tiles))
        {
            Vector2? tilePoint = RaytraceTiles(start, end, false, thickness, numRays);
            if (tilePoint.HasValue)
            {
                float distanceSquared = (tilePoint.Value - start).LengthSquared();
                if (distanceSquared <= maxDistanceSquared)
                {
                    intersections.Add((tilePoint.Value, CollisionTarget.Tiles, false));
                }
            }
        }

        // Collect liquid intersections
        if (targets.HasFlag(CollisionTarget.Liquid))
        {
            Vector2? liquidPoint = RaytraceLiquid(start, end, thickness, numRays);
            if (liquidPoint.HasValue)
            {
                float distanceSquared = (liquidPoint.Value - start).LengthSquared();
                if (distanceSquared <= maxDistanceSquared)
                {
                    intersections.Add((liquidPoint.Value, CollisionTarget.Liquid, false));
                }
            }
        }

        // Collect NPC intersections
        if (targets.HasFlag(CollisionTarget.NPCs))
        {
            Vector2? npcPoint = RaytraceNPCs(start, end, thickness, numRays);
            if (npcPoint.HasValue)
            {
                float distanceSquared = (npcPoint.Value - start).LengthSquared();
                if (distanceSquared <= maxDistanceSquared)
                {
                    // Determine if the NPC at the intersection point can pass tiles
                    bool noTileCollide = false;
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (npc.Hitbox.Contains((int)npcPoint.Value.X, (int)npcPoint.Value.Y))
                        {
                            noTileCollide = npc.noTileCollide;
                            break;
                        }
                    }
                    intersections.Add((npcPoint.Value, CollisionTarget.NPCs, noTileCollide));
                }
            }
        }

        // Collect player intersections
        if (targets.HasFlag(CollisionTarget.Players))
        {
            Vector2? playerPoint = RaytracePlayers(start, end, thickness, numRays);
            if (playerPoint.HasValue)
            {
                float distanceSquared = (playerPoint.Value - start).LengthSquared();
                if (distanceSquared <= maxDistanceSquared)
                {
                    intersections.Add((playerPoint.Value, CollisionTarget.Players, false));
                }
            }
        }

        // If no intersections, return the end point
        if (intersections.Count == 0)
        {
            hitTarget = CollisionTarget.None;
            return end;
        }

        // Find the closest tile intersection, if any
        float closestTileDistanceSquared = float.MaxValue;
        Vector2? closestTilePoint = null;
        foreach (var intersection in intersections)
        {
            if (intersection.targetType == CollisionTarget.Tiles)
            {
                float distanceSquared = (intersection.point - start).LengthSquared();
                if (distanceSquared < closestTileDistanceSquared)
                {
                    closestTileDistanceSquared = distanceSquared;
                    closestTilePoint = intersection.point;
                }
            }
        }

        // Find the closest valid intersection
        Vector2 closestPoint = end;
        float closestDistanceSquared = maxDistanceSquared;
        CollisionTarget hit = CollisionTarget.None;
        foreach (var intersection in intersections)
        {
            float distanceSquared = (intersection.point - start).LengthSquared();

            // For NPCs that don't collide with tiles, only consider them if they are closer than the tile
            if (intersection.targetType == CollisionTarget.NPCs && !intersection.noTileCollide)
            {
                if (closestTilePoint.HasValue && distanceSquared > closestTileDistanceSquared)
                    continue; // Skip grounded NPCs behind tiles
            }
            if (distanceSquared < closestDistanceSquared)
            {
                closestDistanceSquared = distanceSquared;
                closestPoint = intersection.point;
                hit = intersection.targetType;
            }
        }

        hitTarget = hit;
        return closestPoint;
    }

    /// <summary>
    /// Raycast a line along tiles
    /// </summary>
    /// <param name="start">The start of the line</param>
    /// <param name="end">The end of the line</param>
    /// <param name="topSurfaces">Should this method accept platforms, workbenches, etc.?</param>
    /// <param name="thickness">How many pixels wide should the line be?</param>
    /// <param name="numRays">The more rays the more precise</param>
    /// <returns>The point at which the created line hit a tile in any state</returns>
    public static Vector2? RaytraceTiles(Vector2 start, Vector2 end, bool topSurfaces = false, float thickness = 0f, int numRays = 5)
    {
        if (thickness <= 0 || numRays <= 1)
        {
            // Calculate direction and length of the ray
            Vector2 direction = end - start;
            float length = direction.Length();
            if (length <= 0)
                return null; // No movement, no collision
            direction.Normalize();

            // Convert start and end to tile coordinates
            int startX = (int)(start.X / 16f);
            int startY = (int)(start.Y / 16f);
            int endX = (int)(end.X / 16f);
            int endY = (int)(end.Y / 16f);

            // DDA setup: determine step direction and distance per tile
            int stepX = direction.X > 0 ? 1 : (direction.X < 0 ? -1 : 0);
            int stepY = direction.Y > 0 ? 1 : (direction.Y < 0 ? -1 : 0);
            float tDeltaX = direction.X != 0 ? 16f / Math.Abs(direction.X) : float.MaxValue;
            float tDeltaY = direction.Y != 0 ? 16f / Math.Abs(direction.Y) : float.MaxValue;

            // Calculate initial tMax for X and Y (distance to next tile boundary)
            float tMaxX = direction.X != 0 ? ((direction.X > 0 ? startX + 1 : startX) * 16f - start.X) / direction.X : float.MaxValue;
            float tMaxY = direction.Y != 0 ? ((direction.Y > 0 ? startY + 1 : startY) * 16f - start.Y) / direction.Y : float.MaxValue;

            // Current tile position
            int x = startX;
            int y = startY;

            Vector2? collisionPoint = null;
            float closestDistance = float.MaxValue;

            // Continue until we exceed the ray length or find a collision
            while (true)
            {
                // Check if we've gone beyond the ray's length
                float t = Math.Min(tMaxX, tMaxY);
                if (t > length)
                    break;

                // Stay in bounds
                if (!WorldGen.InWorld(x, y, 0))
                    break;

                Tile tile = ParanoidTileRetrieval(x, y);
                if (tile.HasUnactuatedTile)
                {
                    bool solid = Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
                    if (topSurfaces)
                        solid |= (Main.tileSolidTop[tile.TileType] && tile.TileFrameY == 0) || TileID.Sets.Platforms[tile.TileType];

                    if (solid)
                    {
                        // Define the tiles bounding box in world coordinates
                        float tileLeft = x * 16f;
                        float tileRight = (x + 1) * 16f;
                        float tileTop = y * 16f;
                        float tileBottom = (y + 1) * 16f;

                        // Adjust for half blocks
                        if (tile.IsHalfBlock)
                            tileTop += 8f;

                        // Adjust for platforms
                        if (TileID.Sets.Platforms[tile.TileType])
                            tileBottom -= 8f;

                        // Define base corner points
                        Vector2 topLeft = new(tileLeft, tileTop);
                        Vector2 topRight = new(tileRight, tileTop);
                        Vector2 bottomLeft = new(tileLeft, tileBottom);
                        Vector2 bottomRight = new(tileRight, tileBottom);

                        // Adjust for slopes
                        Vector2 slopeStart = topLeft;
                        Vector2 slopeEnd = bottomRight;
                        if (tile.Slope != SlopeType.Solid && !tile.IsHalfBlock)
                        {
                            switch (tile.Slope)
                            {
                                case SlopeType.SlopeDownLeft:
                                    slopeStart = topLeft;
                                    slopeEnd = bottomRight;
                                    break;
                                case SlopeType.SlopeDownRight:
                                    slopeStart = topRight;
                                    slopeEnd = bottomLeft;
                                    break;
                                case SlopeType.SlopeUpLeft:
                                    slopeStart = bottomLeft;
                                    slopeEnd = topRight;
                                    break;
                                case SlopeType.SlopeUpRight:
                                    slopeStart = bottomRight;
                                    slopeEnd = topLeft;
                                    break;
                            }
                        }

                        // Define edges to check
                        List<Vector2[]> edges = new();
                        if (tile.Slope != SlopeType.Solid && !tile.IsHalfBlock)
                        {
                            // Prioritize slope edge
                            edges.Add(new[] { slopeStart, slopeEnd });
                        }
                        else
                        {
                            edges.Add(new[] { topLeft, topRight });      // Top edge
                            edges.Add(new[] { bottomLeft, bottomRight }); // Bottom edge
                            edges.Add(new[] { topLeft, bottomLeft });    // Left edge
                            edges.Add(new[] { topRight, bottomRight });  // Right edge
                        }

                        // Check each edge for intersection with the ray
                        for (int j = 0; j < edges.Count; j++)
                        {
                            Vector2 edgeStart = edges[j][0];
                            Vector2 edgeEnd = edges[j][1];

                            // Ray segment for this step
                            Vector2 rayStart = start;
                            Vector2 rayEnd = start + direction * Math.Min(t, length);

                            // Check for intersection between ray and edge
                            if (LinesIntersect(rayStart, rayEnd, edgeStart, edgeEnd, out Vector2 intersection))
                            {
                                // Validate intersection
                                bool isValid = true;
                                if (tile.Slope != SlopeType.Solid && !tile.IsHalfBlock)
                                {
                                    // Project intersection onto slope line
                                    float tSlope = Vector2.Dot(intersection - slopeStart, slopeEnd - slopeStart) / Vector2.Dot(slopeEnd - slopeStart, slopeEnd - slopeStart);
                                    if (tSlope < 0 || tSlope > 1)
                                        isValid = false;
                                    else
                                    {
                                        float slopeY = slopeStart.Y + tSlope * (slopeEnd.Y - slopeStart.Y);
                                        if ((tile.Slope == SlopeType.SlopeDownLeft || tile.Slope == SlopeType.SlopeDownRight) && intersection.Y > slopeY + 0.1f)
                                            isValid = false;
                                        else if ((tile.Slope == SlopeType.SlopeUpLeft || tile.Slope == SlopeType.SlopeUpRight) && intersection.Y < slopeY - 0.1f)
                                            isValid = false;
                                    }
                                }
                                else if (tile.IsHalfBlock || TileID.Sets.Platforms[tile.TileType])
                                {
                                    // Ensure intersection is within the solid region for half blocks/platforms
                                    if (intersection.Y < tileTop - 0.1f || intersection.Y > tileBottom + 0.1f)
                                        isValid = false;
                                }

                                if (isValid)
                                {
                                    float distance = Vector2.Distance(start, intersection);
                                    if (distance < closestDistance)
                                    {
                                        closestDistance = distance;
                                        collisionPoint = intersection;
                                    }
                                }
                            }
                        }

                        if (collisionPoint.HasValue)
                            break;
                    }
                }

                // Move to the next tile
                if (tMaxX < tMaxY)
                {
                    x += stepX;
                    tMaxX += tDeltaX;
                }
                else
                {
                    y += stepY;
                    tMaxY += tDeltaY;
                }

                // Stop if we've reached or passed the end tile
                if ((stepX > 0 && x > endX) || (stepX < 0 && x < endX) ||
                    (stepY > 0 && y > endY) || (stepY < 0 && y < endY))
                    break;
            }

            return collisionPoint;
        }
        else
        {
            Vector2 direction = (end - start);
            float length = direction.Length();
            if (length <= 0)
                return null;
            direction /= length; // Normalize
            Vector2 perp = new Vector2(-direction.Y, direction.X);

            List<(Vector2 point, float t)> intersections = new List<(Vector2, float)>();
            for (int k = 0; k < numRays; k++)
            {
                float offsetFactor = (k / (numRays - 1f)) - 0.5f; // -0.5 to 0.5
                Vector2 offset = offsetFactor * thickness * perp;
                Vector2 startOffset = start + offset;
                Vector2 endOffset = end + offset;
                Vector2? intersect = RaytraceTiles(startOffset, endOffset, topSurfaces, 0f, 1);
                if (intersect.HasValue)
                {
                    // Project the intersection point onto the main ray
                    Vector2 intersection = intersect.Value;
                    Vector2 toIntersection = intersection - start;
                    float t = Vector2.Dot(toIntersection, direction); // Distance along the main ray

                    // Ensure the projected point is within the rays length
                    if (t >= 0 && t <= length)
                    {
                        intersections.Add((intersection, t));
                    }
                }
            }

            if (intersections.Count == 0)
                return null;

            // Find the intersection with the smallest t (earliest along the main ray)
            float bestT = float.MaxValue;
            Vector2? bestPoint = null;

            foreach (var (point, t) in intersections)
            {
                if (t < bestT)
                {
                    bestT = t;
                    bestPoint = point;
                }
            }

            if (bestPoint.HasValue)
            {
                // Return the point on the main ray at distance bestT
                return start + direction * bestT;
            }

            return null;
        }
    }

    /// <summary>
    /// Raycast a line along the surface of liquids
    /// </summary>
    /// <param name="start">The start of the line</param>
    /// <param name="end">The end of the line</param>
    /// <param name="thickness">How many pixels wide should the line be?</param>
    /// <param name="numRays">The more rays the more precise</param>
    /// <returns>The point at which the created line hit the surface of liquid</returns>
    public static Vector2? RaytraceLiquid(Vector2 start, Vector2 end, float thickness = 0f, int numRays = 5)
    {
        if (thickness <= 0 || numRays <= 1)
        {
            // Calculate direction and length of the ray
            Vector2 direction = end - start;
            float length = direction.Length();
            if (length <= 0)
                return null; // No movement, no collision
            direction.Normalize();

            // Convert start and end to tile coordinates
            int startX = (int)(start.X / 16f);
            int startY = (int)(start.Y / 16f);
            int endX = (int)(end.X / 16f);
            int endY = (int)(end.Y / 16f);

            // DDA setup: determine step direction and distance per tile
            int stepX = direction.X > 0 ? 1 : (direction.X < 0 ? -1 : 0);
            int stepY = direction.Y > 0 ? 1 : (direction.Y < 0 ? -1 : 0);
            float tDeltaX = direction.X != 0 ? 16f / Math.Abs(direction.X) : float.MaxValue;
            float tDeltaY = direction.Y != 0 ? 16f / Math.Abs(direction.Y) : float.MaxValue;

            // Calculate initial tMax for X and Y (distance to next tile boundary)
            float tMaxX = direction.X != 0 ? ((direction.X > 0 ? startX + 1 : startX) * 16f - start.X) / direction.X : float.MaxValue;
            float tMaxY = direction.Y != 0 ? ((direction.Y > 0 ? startY + 1 : startY) * 16f - start.Y) / direction.Y : float.MaxValue;

            // Current tile position
            int x = startX;
            int y = startY;

            Vector2? collisionPoint = null;
            float closestDistance = float.MaxValue;

            // Continue until we exceed the ray length or find a collision
            while (true)
            {
                // Check if we've gone beyond the rays length
                float t = Math.Min(tMaxX, tMaxY);
                if (t > length)
                    break;

                // Check if we're out of bounds
                if (!WorldGen.InWorld(x, y, 0))
                    break;

                // Check if there is liquid above
                if (Framing.GetTileSafely(x, y - 1).LiquidAmount > 0)
                    break;

                Tile tile = ParanoidTileRetrieval(x, y);
                if (tile.LiquidAmount > 0)
                {
                    // Calculate the liquid surface level
                    float completion = 1f - InverseLerp(0f, byte.MaxValue, tile.LiquidAmount);
                    float liquidY = y * 16f + (16f * completion);

                    // Define the tiles bounding box in world coordinates
                    float tileLeft = x * 16f;
                    float tileRight = (x + 1) * 16f;

                    // Define the liquid surface as a horizontal edge
                    Vector2 liquidLeft = new(tileLeft, liquidY);
                    Vector2 liquidRight = new(tileRight, liquidY);

                    // Ray segment for this step
                    Vector2 rayStart = start;
                    Vector2 rayEnd = start + direction * Math.Min(t, length);

                    // Check for intersection between ray and liquid surface
                    if (LinesIntersect(rayStart, rayEnd, liquidLeft, liquidRight, out Vector2 intersection))
                    {
                        float distance = Vector2.Distance(start, intersection);
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            collisionPoint = intersection;
                        }
                    }

                    if (collisionPoint.HasValue)
                        break;
                }

                // Move to the next tile
                if (tMaxX < tMaxY)
                {
                    x += stepX;
                    tMaxX += tDeltaX;
                }
                else
                {
                    y += stepY;
                    tMaxY += tDeltaY;
                }

                // Stop if we've reached or passed the end tile
                if ((stepX > 0 && x > endX) || (stepX < 0 && x < endX) ||
                    (stepY > 0 && y > endY) || (stepY < 0 && y < endY))
                    break;
            }

            return collisionPoint;
        }
        else
        {
            Vector2 direction = (end - start);
            float length = direction.Length();
            if (length <= 0)
                return null;
            direction /= length; // Normalize
            Vector2 perp = new Vector2(-direction.Y, direction.X);

            List<(Vector2 point, float t)> intersections = new List<(Vector2, float)>();
            for (int k = 0; k < numRays; k++)
            {
                float offsetFactor = (k / (numRays - 1f)) - 0.5f; // -0.5 to 0.5
                Vector2 offset = offsetFactor * thickness * perp;
                Vector2 startOffset = start + offset;
                Vector2 endOffset = end + offset;
                Vector2? intersect = RaytraceLiquid(startOffset, endOffset, 0f, 1);
                if (intersect.HasValue)
                {
                    // Project the intersection point onto the main ray
                    Vector2 intersection = intersect.Value;
                    Vector2 toIntersection = intersection - start;
                    float t = Vector2.Dot(toIntersection, direction); // Distance along the main ray

                    // Ensure the projected point is within the rays length
                    if (t >= 0 && t <= length)
                    {
                        intersections.Add((intersection, t));
                    }
                }
            }

            if (intersections.Count == 0)
                return null;

            // Find the intersection with the smallest t (earliest along the main ray)
            float bestT = float.MaxValue;
            Vector2? bestPoint = null;

            foreach (var (point, t) in intersections)
            {
                if (t < bestT)
                {
                    bestT = t;
                    bestPoint = point;
                }
            }

            if (bestPoint.HasValue)
            {
                // Return the point on the main ray at distance bestT
                return start + direction * bestT;
            }

            return null;
        }
    }

    /// <summary>
    /// Raycast a line along NPCs
    /// </summary>
    /// <param name="start">The start of the line</param>
    /// <param name="end">The end of the line</param>
    /// <param name="thickness">How many pixels wide should the line be?</param>
    /// <param name="numRays">The more rays the more precise</param>
    /// <param name="requireHome">Do NPCs need to pass <see cref="NPCTargeting.CanHomeInto(NPC, bool, bool)"/>?</param>
    /// <returns>The point at which the created line hit a NPC's hitbox</returns>
    public static Vector2? RaytraceNPCs(Vector2 start, Vector2 end, float thickness = 0f, int numRays = 5, bool requireHome = true)
    {
        if (thickness <= 0 || numRays <= 1)
        {
            Vector2? closestIntersection = null;
            float closestDistanceSquared = float.MaxValue;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!(!requireHome || npc.CanHomeInto()) || !IsAABBInRayRange(start, end, npc.Hitbox))
                    continue;

                // Inside the NPC
                if (npc.Hitbox.Contains(start.ToPoint()))
                    return start;

                // Define the four sides of the NPCs hitbox
                Vector2[] corners = new Vector2[]
                {
                    npc.TopLeft,
                    npc.TopRight,
                    npc.BottomRight,
                    npc.BottomLeft
                };

                // Check all four sides of the hitbox
                for (int i = 0; i < 4; i++)
                {
                    Vector2 p1 = corners[i];
                    Vector2 p2 = corners[(i + 1) % 4];

                    if (LinesIntersect(start, end, p1, p2, out Vector2 intersectPoint))
                    {
                        float distanceSquared = (intersectPoint - start).LengthSquared();
                        if (distanceSquared < closestDistanceSquared)
                        {
                            closestDistanceSquared = distanceSquared;
                            closestIntersection = intersectPoint;
                        }
                    }
                }
            }

            return closestIntersection;
        }
        else
        {
            Vector2 direction = (end - start);
            float length = direction.Length();
            if (length <= 0)
                return null;
            direction /= length; // Normalize
            Vector2 perp = new Vector2(-direction.Y, direction.X);

            List<(Vector2 point, float t)> intersections = new List<(Vector2, float)>();
            for (int k = 0; k < numRays; k++)
            {
                float offsetFactor = (k / (numRays - 1f)) - 0.5f; // -0.5 to 0.5
                Vector2 offset = offsetFactor * thickness * perp;
                Vector2 startOffset = start + offset;
                Vector2 endOffset = end + offset;
                Vector2? intersect = RaytraceNPCs(startOffset, endOffset, 0f, 1, requireHome);
                if (intersect.HasValue)
                {
                    // Project the intersection point onto the main ray (start to end)
                    Vector2 intersection = intersect.Value;
                    Vector2 toIntersection = intersection - start;
                    float t = Vector2.Dot(toIntersection, direction); // Distance along the main ray

                    // Ensure the projected point is within the rays length
                    if (t >= 0 && t <= length)
                    {
                        intersections.Add((intersection, t));
                    }
                }
            }

            if (intersections.Count == 0)
                return null;

            // Find the intersection with the smallest t (earliest along the main ray)
            float bestT = float.MaxValue;
            Vector2? bestPoint = null;

            foreach (var (point, t) in intersections)
            {
                if (t < bestT)
                {
                    bestT = t;
                    bestPoint = point;
                }
            }

            if (bestPoint.HasValue)
            {
                // Return the point on the main ray at distance bestT
                return start + direction * bestT;
            }

            return null;
        }
    }

    /// <summary>
    /// Raycast a line along players
    /// </summary>
    /// <param name="start">The start of the line</param>
    /// <param name="end">The end of the line</param>
    /// <param name="thickness">How many pixels wide should the line be?</param>
    /// <param name="numRays">The more rays the more precise</param>
    /// <returns>The point at which the created line hit a players hitbox</returns>
    public static Vector2? RaytracePlayers(Vector2 start, Vector2 end, float thickness = 0f, int numRays = 5)
    {
        if (thickness <= 0 || numRays <= 1)
        {
            Vector2? closestIntersection = null;
            float closestDistanceSquared = float.MaxValue;

            foreach (Player player in Main.ActivePlayers)
            {
                if (!IsAABBInRayRange(start, end, player.Hitbox))
                    continue;

                // Inside the player
                if (player.Hitbox.Contains(start.ToPoint()))
                    return start;

                // Define the four sides of the players hitbox
                Vector2[] corners = new Vector2[]
                {
                    player.TopLeft,
                    player.TopRight,
                    player.BottomRight,
                    player.BottomLeft
                };

                // Check all four sides of the hitbox
                for (int i = 0; i < 4; i++)
                {
                    Vector2 p1 = corners[i];
                    Vector2 p2 = corners[(i + 1) % 4];

                    if (LinesIntersect(start, end, p1, p2, out Vector2 intersectPoint))
                    {
                        float distanceSquared = (intersectPoint - start).LengthSquared();
                        if (distanceSquared < closestDistanceSquared)
                        {
                            closestDistanceSquared = distanceSquared;
                            closestIntersection = intersectPoint;
                        }
                    }
                }
            }

            return closestIntersection;
        }
        else
        {
            Vector2 direction = (end - start);
            float length = direction.Length();
            if (length <= 0)
                return null;
            direction /= length; // Normalize
            Vector2 perp = new Vector2(-direction.Y, direction.X);

            List<(Vector2 point, float t)> intersections = new List<(Vector2, float)>();
            for (int k = 0; k < numRays; k++)
            {
                float offsetFactor = (k / (numRays - 1f)) - 0.5f; // -0.5 to 0.5
                Vector2 offset = offsetFactor * thickness * perp;
                Vector2 startOffset = start + offset;
                Vector2 endOffset = end + offset;
                Vector2? intersect = RaytracePlayers(startOffset, endOffset, 0f, 1);
                if (intersect.HasValue)
                {
                    // Project the intersection point onto the main ray (start to end)
                    Vector2 intersection = intersect.Value;
                    Vector2 toIntersection = intersection - start;
                    float t = Vector2.Dot(toIntersection, direction); // Distance along the main ray

                    // Ensure the projected point is within the rays length
                    if (t >= 0 && t <= length)
                    {
                        intersections.Add((intersection, t));
                    }
                }
            }

            if (intersections.Count == 0)
                return null;

            // Find the intersection with the smallest t (earliest along the main ray)
            float bestT = float.MaxValue;
            Vector2? bestPoint = null;

            foreach (var (point, t) in intersections)
            {
                if (t < bestT)
                {
                    bestT = t;
                    bestPoint = point;
                }
            }

            if (bestPoint.HasValue)
            {
                // Return the point on the main ray at distance bestT
                return start + direction * bestT;
            }

            return null;
        }
    }

    /// <summary>
    /// Checks if a given hitbox is within the bounding box of a line <br></br>
    /// Particularly useful for only checking necessary targets 
    /// </summary>
    /// <param name="start">The start of the line</param>
    /// <param name="end">The end of the line</param>
    /// <param name="hitbox">The hitbox to check for</param>
    public static bool IsAABBInRayRange(Vector2 start, Vector2 end, Rectangle hitbox)
    {
        // Rays AABB
        float rayMinX = Math.Min(start.X, end.X);
        float rayMaxX = Math.Max(start.X, end.X);
        float rayMinY = Math.Min(start.Y, end.Y);
        float rayMaxY = Math.Max(start.Y, end.Y);

        // Objects AABB
        float objMinX = hitbox.Left;
        float objMaxX = hitbox.Right;
        float objMinY = hitbox.Top;
        float objMaxY = hitbox.Bottom;

        // Check for AABB overlap
        if (rayMaxX < objMinX || rayMinX > objMaxX || rayMaxY < objMinY || rayMinY > objMaxY)
            return false;

        // Check if the closest point on the AABB is within the maximum distance
        float closestX = Math.Clamp(start.X, objMinX, objMaxX);
        float closestY = Math.Clamp(start.Y, objMinY, objMaxY);
        float distanceSquared = (start.X - closestX) * (start.X - closestX) + (start.Y - closestY) * (start.Y - closestY);
        return distanceSquared <= (end - start).LengthSquared();
    }

    public static bool RaytraceTo(int x0, int y0, int x1, int y1, bool ignoreHalfTiles = false)
    {
        // Bresenham's algorithm
        int horizontalDistance = Math.Abs(x1 - x0); // Delta X
        int verticalDistance = Math.Abs(y1 - y0); // Delta Y
        int horizontalIncrement = (x1 > x0) ? 1 : -1; // S1
        int verticalIncrement = (y1 > y0) ? 1 : -1; // S2

        int x = x0;
        int y = y0;
        int E = horizontalDistance - verticalDistance;

        while (true)
        {
            if (Main.tile[x, y].IsTileSolid() && (!ignoreHalfTiles || !Main.tile[x, y].IsHalfBlock))
                return false;

            if (x == x1 && y == y1)
                return true;

            int E2 = E * 2;
            if (E2 >= -verticalDistance)
            {
                if (x == x1)
                    return true;
                E -= verticalDistance;
                x += horizontalIncrement;
            }
            if (E2 <= horizontalDistance)
            {
                if (y == y1)
                    return true;

                E += horizontalDistance;
                y += verticalIncrement;
            }
        }
    }

    public static Point? RaytraceToFirstSolid(Vector2 pos1, Vector2 pos2)
    {
        Point point1 = ToSafeTileCoordinates(pos1);
        Point point2 = ToSafeTileCoordinates(pos2);
        return RaytraceToFirstSolid(point1, point2);
    }

    public static Point? RaytraceToFirstSolid(Point pos1, Point pos2)
    {
        return RaytraceToFirstSolid(pos1.X, pos1.Y, pos2.X, pos2.Y);
    }

    public static Point? RaytraceToFirstSolid(int x0, int y0, int x1, int y1)
    {
        //Bresenham's algorithm
        int horizontalDistance = Math.Abs(x1 - x0); //Delta X
        int verticalDistance = Math.Abs(y1 - y0); //Delta Y
        int horizontalIncrement = (x1 > x0) ? 1 : -1; //S1
        int verticalIncrement = (y1 > y0) ? 1 : -1; //S2

        int x = x0;
        int y = y0;
        int i = 1 + horizontalDistance + verticalDistance;
        int E = horizontalDistance - verticalDistance;
        horizontalDistance *= 2;
        verticalDistance *= 2;

        while (i > 0)
        {
            if (IsTileSolidOrPlatform(Main.tile[x, y]))
                return new Point(x, y);

            if (E > 0)
            {
                x += horizontalIncrement;
                E -= verticalDistance;
            }
            else
            {
                y += verticalIncrement;
                E += horizontalDistance;
            }
            i--;
        }
        return null;
    }
    #endregion

    /// <summary>
    /// Determines if two polygons are intersecting using the Separating Axis Theorem
    /// </summary>
    /// <param name="polygon1">The set of vertices for this polygon</param>
    /// <param name="polygon2">The set of vertices for the other polygon</param>
    /// <returns>Whether or not they intersected</returns>
    public static bool IsIntersecting(Vector2[] polygon1, Vector2[] polygon2)
    {
        // Check edges of the first polygon
        int poly1Count = polygon1.Length;
        for (int i = 0; i < poly1Count; i++)
        {
            Vector2 edge = polygon1[(i + 1) % poly1Count] - polygon1[i];
            Vector2 axis = new(-edge.Y, edge.X); // Normal vector

            if (!IsOverlappingOnAxis(polygon1, polygon2, axis))
                return false; // Found a separating axis
        }

        // Check edges of the second polygon
        int poly2Count = polygon2.Length;
        for (int i = 0; i < poly2Count; i++)
        {
            Vector2 edge = polygon2[(i + 1) % poly2Count] - polygon2[i];
            Vector2 axis = new(-edge.Y, edge.X); // Normal vector

            if (!IsOverlappingOnAxis(polygon1, polygon2, axis))
                return false; // Found a separating axis
        }

        return true; // No separating axis found, they intersect
    }

    private static bool IsOverlappingOnAxis(Vector2[] polygon1, Vector2[] polygon2, Vector2 axis)
    {
        // Project rectangle onto the axis
        (float firstMin, float firstMax) = ProjectVertices(polygon1, axis);

        // Project triangle onto the axis
        (float secondMin, float secondMax) = ProjectVertices(polygon2, axis);

        // Check for overlap
        return firstMax >= secondMin && secondMax >= firstMin;
    }

    private static (float, float) ProjectVertices(Vector2[] vertices, Vector2 axis)
    {
        float min = float.MaxValue;
        float max = float.MinValue;

        foreach (var vertex in vertices)
        {
            float projection = Vector2.Dot(vertex, axis);
            min = Math.Min(min, projection);
            max = Math.Max(max, projection);
        }

        return (min, max);
    }

    public static Vector2 ResolveCollision(ref Rectangle rect, RotatedRectangle rotatedRect, Vector2 velocity, out bool collisionOccurred, int iterations = 4)
    {
        collisionOccurred = false;
        iterations = Math.Max(1, Math.Min(iterations, 10));

        Vector2 newPosition = new Vector2(rect.X, rect.Y) + velocity;
        Rectangle testRect = new((int)newPosition.X, (int)newPosition.Y, rect.Width, rect.Height);

        Vector2[] rotatedCorners = [rotatedRect.Top, rotatedRect.TopRight, rotatedRect.BottomRight, rotatedRect.BottomLeft];
        float minX = Math.Min(rotatedCorners[0].X, Math.Min(rotatedCorners[1].X, Math.Min(rotatedCorners[2].X, rotatedCorners[3].X)));
        float minY = Math.Min(rotatedCorners[0].Y, Math.Min(rotatedCorners[1].Y, Math.Min(rotatedCorners[2].Y, rotatedCorners[3].Y)));
        float maxX = Math.Max(rotatedCorners[0].X, Math.Max(rotatedCorners[1].X, Math.Max(rotatedCorners[2].X, rotatedCorners[3].X)));
        float maxY = Math.Max(rotatedCorners[0].Y, Math.Max(rotatedCorners[1].Y, Math.Max(rotatedCorners[2].Y, rotatedCorners[3].Y)));

        if (testRect.Right < minX || testRect.Left > maxX || testRect.Bottom < minY || testRect.Top > maxY)
        {
            rect.X = (int)newPosition.X;
            rect.Y = (int)newPosition.Y;
            return velocity;
        }

        Rectangle initialRect = new(rect.X, rect.Y, rect.Width, rect.Height);
        bool wasColliding = GetSeparationInfo(initialRect, rotatedRect, out _, out _);

        if (!GetSeparationInfo(testRect, rotatedRect, out _, out _))
        {
            rect.X = (int)newPosition.X;
            rect.Y = (int)newPosition.Y;
            return velocity;
        }

        Vector2 resolvedVelocity = velocity;
        Vector2 step = velocity / iterations;
        Vector2 originalDirection = Vector2.Normalize(velocity.Length() > 0 ? velocity : Vector2.UnitX);
        float originalSpeed = velocity.Length();

        for (int i = 0; i < iterations; i++)
        {
            Vector2 currentPos = new Vector2(rect.X, rect.Y) + (step * i);
            testRect = new Rectangle((int)currentPos.X, (int)currentPos.Y, rect.Width, rect.Height);

            if (testRect.Right < minX || testRect.Left > maxX || testRect.Bottom < minY || testRect.Top > maxY)
                continue;

            if (GetSeparationInfo(testRect, rotatedRect, out Vector2 penetration, out Vector2 normal))
            {
                if (!wasColliding && i == 0 || (i > 0 && !GetSeparationInfo(
                    new Rectangle((int)(currentPos.X - step.X), (int)(currentPos.Y - step.Y), rect.Width, rect.Height),
                    rotatedRect, out _, out _)))
                {
                    collisionOccurred = true;
                }

                if (penetration == Vector2.Zero)
                    break;

                Vector2 slideVelocity = resolvedVelocity - Vector2.Dot(resolvedVelocity, normal) * normal;
                float slideMagnitude = slideVelocity.Length();

                if (slideMagnitude > 0.001f)
                    slideVelocity = Vector2.Normalize(slideVelocity) * Math.Min(originalSpeed, slideMagnitude);
                else
                    slideVelocity = originalDirection * originalSpeed * 0.5f;

                float alignment = Vector2.Dot(Vector2.Normalize(slideVelocity), originalDirection);
                if (alignment < 0.1f && alignment > -0.1f)
                    slideVelocity = Vector2.Lerp(slideVelocity, originalDirection * originalSpeed, 0.3f);

                currentPos -= penetration;
                resolvedVelocity = slideVelocity;
            }
        }

        rect.X = (int)(newPosition.X + resolvedVelocity.X);
        rect.Y = (int)(newPosition.Y + resolvedVelocity.Y);
        return resolvedVelocity;
    }

    private static bool GetSeparationInfo(Rectangle rect, RotatedRectangle rotatedRect, out Vector2 penetrationVector, out Vector2 normal)
    {
        penetrationVector = Vector2.Zero;
        normal = Vector2.Zero;

        Vector2 C = new(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);
        Vector2 rotatedC = rotatedRect.Center;
        Vector2 U = rotatedRect.Rotation.ToRotationVector2();
        Vector2 V = new(-U.Y, U.X);

        float minOverlap = float.MaxValue;
        Vector2 minAxis = Vector2.Zero;

        // Axis Vector2.UnitX
        float rectMinX = rect.X;
        float rectMaxX = rect.X + rect.Width;
        float r_rotatedX = (rotatedRect.Width / 2f) * Math.Abs(U.X) + (rotatedRect.Height / 2f) * Math.Abs(V.X);
        float rotatedMinX = rotatedC.X - r_rotatedX;
        float rotatedMaxX = rotatedC.X + r_rotatedX;
        float overlapX = Math.Min(rectMaxX, rotatedMaxX) - Math.Max(rectMinX, rotatedMinX);
        if (overlapX <= 0)
            return false;

        if (overlapX < minOverlap)
        {
            minOverlap = overlapX;
            minAxis = Vector2.UnitX;
        }

        // Axis Vector2.UnitY
        float rectMinY = rect.Y;
        float rectMaxY = rect.Y + rect.Height;
        float r_rotatedY = (rotatedRect.Width / 2f) * Math.Abs(U.Y) + (rotatedRect.Height / 2f) * Math.Abs(V.Y);
        float rotatedMinY = rotatedC.Y - r_rotatedY;
        float rotatedMaxY = rotatedC.Y + r_rotatedY;
        float overlapY = Math.Min(rectMaxY, rotatedMaxY) - Math.Max(rectMinY, rotatedMinY);
        if (overlapY <= 0)
            return false;

        if (overlapY < minOverlap)
        {
            minOverlap = overlapY;
            minAxis = Vector2.UnitY;
        }

        // Axis U
        float r_rectU = (rect.Width / 2f) * Math.Abs(U.X) + (rect.Height / 2f) * Math.Abs(U.Y);
        float rectProjU = Vector2.Dot(C, U);
        float rectMinU = rectProjU - r_rectU;
        float rectMaxU = rectProjU + r_rectU;
        float rotatedProjU = Vector2.Dot(rotatedC, U);
        float rotatedMinU = rotatedProjU - (rotatedRect.Width / 2f);
        float rotatedMaxU = rotatedProjU + (rotatedRect.Width / 2f);
        float overlapU = Math.Min(rectMaxU, rotatedMaxU) - Math.Max(rectMinU, rotatedMinU);
        if (overlapU <= 0)
            return false;

        if (overlapU < minOverlap)
        {
            minOverlap = overlapU;
            minAxis = U;
        }

        // Axis V
        float r_rectV = (rect.Width / 2f) * Math.Abs(V.X) + (rect.Height / 2f) * Math.Abs(V.Y);
        float rectProjV = Vector2.Dot(C, V);
        float rectMinV = rectProjV - r_rectV;
        float rectMaxV = rectProjV + r_rectV;
        float rotatedProjV = Vector2.Dot(rotatedC, V);
        float rotatedMinV = rotatedProjV - (rotatedRect.Height / 2f);
        float rotatedMaxV = rotatedProjV + (rotatedRect.Height / 2f);
        float overlapV = Math.Min(rectMaxV, rotatedMaxV) - Math.Max(rectMinV, rotatedMinV);
        if (overlapV <= 0)
            return false;

        if (overlapV < minOverlap)
        {
            minOverlap = overlapV;
            minAxis = V;
        }

        Vector2 direction = C - rotatedC;
        if (Vector2.Dot(direction, minAxis) < 0)
            minAxis = -minAxis;

        normal = minAxis;
        penetrationVector = minAxis * minOverlap;
        return true;
    }

    public static bool IsPointInTriangle(Vector2 point, Vector2 v0, Vector2 v1, Vector2 v2)
    {
        // Compute vectors from v0 to v1, v2, and the point
        Vector2 edge0 = v1 - v0;
        Vector2 edge1 = v2 - v0;
        Vector2 toPoint = point - v0;

        // Compute dot products for barycentric coordinates
        float dot00 = Vector2.Dot(edge0, edge0);
        float dot01 = Vector2.Dot(edge0, edge1);
        float dot11 = Vector2.Dot(edge1, edge1);
        float dot0p = Vector2.Dot(edge0, toPoint);
        float dot1p = Vector2.Dot(edge1, toPoint);

        // Compute barycentric coordinates
        float denom = dot00 * dot11 - dot01 * dot01;
        if (denom == 0)
            return false;

        float u = (dot11 * dot0p - dot01 * dot1p) / denom;
        float v = (dot00 * dot1p - dot01 * dot0p) / denom;

        // Check if point is inside (u >= 0, v >= 0, u + v <= 1)
        return (u >= 0) && (v >= 0) && (u + v <= 1);
    }

    public static bool IsRectangleIntersectingTriangle(Rectangle rect, Vector2 v0, Vector2 v1, Vector2 v2)
    {
        // Check if any triangle vertex is inside the rectangle
        if (rect.Contains((int)v0.X, (int)v0.Y) ||
            rect.Contains((int)v1.X, (int)v1.Y) ||
            rect.Contains((int)v2.X, (int)v2.Y))
            return true;

        // Check if any rectangle vertex is inside the triangle
        Vector2[] rectCorners =
        [
            new Vector2(rect.Left, rect.Top),
            new Vector2(rect.Right, rect.Top),
            new Vector2(rect.Right, rect.Bottom),
            new Vector2(rect.Left, rect.Bottom)
        ];

        foreach (Vector2 corner in rectCorners)
        {
            if (IsPointInTriangle(corner, v0, v1, v2))
                return true;
        }

        // Check if any triangle edge intersects any rectangle edge
        Vector2[] triangleEdges = [v0, v1, v1, v2, v2, v0];
        Vector2[] rectEdges =
        [
            rectCorners[0], rectCorners[1], // Top
            rectCorners[1], rectCorners[2], // Right
            rectCorners[2], rectCorners[3], // Bottom
            rectCorners[3], rectCorners[0]  // Left
        ];

        for (int i = 0; i < 3; i++)
        {
            Vector2 tStart = triangleEdges[i * 2];
            Vector2 tEnd = triangleEdges[i * 2 + 1];
            for (int j = 0; j < 4; j++)
            {
                Vector2 rStart = rectEdges[j * 2];
                Vector2 rEnd = rectEdges[j * 2 + 1];
                if (LinesIntersect(tStart, tEnd, rStart, rEnd, out _))
                    return true;
            }
        }

        return false;
    }

    public static (Vector2 start, Vector2 end)? GetIntersectionPoints(Vector2 A, Vector2 B, Rectangle rect)
    {
        // Calculate direction and length
        Vector2 D = B - A;
        float L = D.Length();

        // Avoid division by zero; if A and B are the same point, there's no line segment
        if (L == 0)
            return null;

        // Normalize direction
        D /= L;

        // Rectangle bounds
        float minX = rect.X;
        float maxX = rect.X + rect.Width;
        float minY = rect.Y;
        float maxY = rect.Y + rect.Height;

        // Initialize entering and exiting parameters
        float tEnter = float.NegativeInfinity;
        float tExit = float.PositiveInfinity;

        // Check X constraints
        if (D.X > 0)
        {
            tEnter = Math.Max(tEnter, (minX - A.X) / D.X); // Entering x >= minX
            tExit = Math.Min(tExit, (maxX - A.X) / D.X);   // Exiting x <= maxX
        }
        else if (D.X < 0)
        {
            tEnter = Math.Max(tEnter, (maxX - A.X) / D.X); // Entering x <= maxX
            tExit = Math.Min(tExit, (minX - A.X) / D.X);   // Exiting x >= minX
        }
        else // D.X == 0
        {
            // Parallel and outside
            if (A.X < minX || A.X > maxX)
                return null;
        }

        // Check Y constraints (Y increases downward in FNA)
        if (D.Y > 0)
        {
            tEnter = Math.Max(tEnter, (minY - A.Y) / D.Y); // Entering y >= minY
            tExit = Math.Min(tExit, (maxY - A.Y) / D.Y);   // Exiting y <= maxY
        }
        else if (D.Y < 0)
        {
            tEnter = Math.Max(tEnter, (maxY - A.Y) / D.Y); // Entering y <= maxY
            tExit = Math.Min(tExit, (minY - A.Y) / D.Y);   // Exiting y >= minY
        }
        else // D.Y == 0
        {
            // Parallel and outside
            if (A.Y < minY || A.Y > maxY)
                return null;
        }

        // Clip to the line segment's range [0, L]
        float tStart = Math.Max(0, tEnter);
        float tEnd = Math.Min(L, tExit);

        // If tStart <= tEnd, there is an intersection
        if (tStart <= tEnd)
        {
            Vector2 start = A + tStart * D;
            Vector2 end = A + tEnd * D;
            return (start, end);
        }

        return null; // No intersection
    }

    public static bool CheckLinearCollision(Vector2 point1, Vector2 point2, Rectangle hitbox, out Vector2 start, out Vector2 end)
    {
        (Vector2 start, Vector2 end)? points = GetIntersectionPoints(point1, point2, hitbox);
        if (points.HasValue)
        {
            start = points.Value.start;
            end = points.Value.end;
            return true;
        }

        start = end = Vector2.Zero;
        return false;
    }

    public static bool LineCollision(this Rectangle target, Vector2 start, Vector2 end, float width)
    {
        float _ = 0f;
        return Collision.CheckAABBvLineCollision(target.TopLeft(), target.Size(), start, end, width, ref _);
    }

    public static bool CollisionFromPoints(this Rectangle target, List<Vector2> list, int width)
    {
        if (list == null || list.Count <= 0)
            return false;

        for (int i = 0; i < list.Count; i++)
        {
            Vector2 point = list[i];
            if (new Rectangle((int)point.X - width / 2, (int)point.Y - width / 2, width, width).Intersects(target))
                return true;
        }

        return false;
    }

    public static bool CollisionFromPoints(this Rectangle target, ReadOnlySpan<Vector2> span, Func<float, float> width)
    {
        if (span == null || span.Length <= 0)
            return false;

        for (int i = 0; i < span.Length; i++)
        {
            Vector2 point = span[i];
            int size = (int)width(InverseLerp(0f, span.Length, i));
            if (new Rectangle((int)point.X - size / 2, (int)point.Y - size / 2, size, size).Intersects(target))
                return true;
        }

        return false;
    }

    public static bool CircularHitboxCollision(Vector2 center, float radius, Rectangle targetHitbox)
    {
        float closestX = MathHelper.Clamp(center.X, targetHitbox.Left, targetHitbox.Right);
        float closestY = MathHelper.Clamp(center.Y, targetHitbox.Top, targetHitbox.Bottom);

        Vector2 closestPoint = new(closestX, closestY);
        float distanceSquared = Vector2.DistanceSquared(center, closestPoint);

        return distanceSquared <= radius * radius;
    }

    public static bool EllipseCollision(Rectangle target, float ellipseWidth, float ellipseHeight, float ellipseRotation, Vector2 ellipseCenter)
    {
        // Rectangle center and half-extents
        Vector2 rectCenter = new(target.Center.X, target.Center.Y);
        Vector2 rectHalfExtents = new(target.Width / 2f, target.Height / 2f);

        // Translate ellipse center relative to rectangle center (simplifies math)
        Vector2 translatedEllipseCenter = ellipseCenter - rectCenter;

        // Ellipse half-width and half-height
        float a = ellipseWidth / 2f; // Half of major axis
        float b = ellipseHeight / 2f; // Half of minor axis

        // Ellipse rotation matrix components
        float cosRot = (float)Math.Cos(ellipseRotation);
        float sinRot = (float)Math.Sin(ellipseRotation);

        // Test axes: rectangle's X and Y, and ellipse's major and minor axes
        Vector2[] axes =
        [
            new Vector2(1f, 0f), // Rectangle X-axis
            new Vector2(0f, 1f), // Rectangle Y-axis
            new Vector2(cosRot, sinRot), // Ellipse major axis
            new Vector2(-sinRot, cosRot) // Ellipse minor axis
        ];

        foreach (Vector2 axis in axes)
        {
            // Project rectangle onto axis
            float rectProj = rectHalfExtents.X * Math.Abs(axis.X) + rectHalfExtents.Y * Math.Abs(axis.Y);

            // Project ellipse onto axis
            float ellipseProj = a * Math.Abs(axis.X * cosRot + axis.Y * sinRot) +
                               b * Math.Abs(axis.X * -sinRot + axis.Y * cosRot);

            // Project ellipse center onto axis
            float centerProj = Math.Abs(Vector2.Dot(translatedEllipseCenter, axis));

            // Check for separation
            if (centerProj > rectProj + ellipseProj)
                return false;
        }

        return true; // No separating axis found, shapes intersect
    }
}