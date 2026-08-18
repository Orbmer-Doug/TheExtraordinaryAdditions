using System;
using System.Collections.Generic;
using Terraria;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Core.DataStructures;

/// <summary>
/// Describes a rotatable 2D-rectangle
/// </summary>
public struct RotatedRectangle
{
    #region Constructors

    /// <summary>
    /// Creates a rotated rectangle
    /// </summary>
    /// <param name="pos">The position of the rectangle</param>
    /// <param name="size">The size of the rectangle</param>
    /// <param name="rotation">The rotation in radians</param>
    /// <param name="pivot">The [0, 1] value that determines where in the rectangle to rotate around</param>
    /// <param name="adjustLocal">If the pivots local space should always rotate around <paramref name="pos"/></param>
    public RotatedRectangle(Vector2 pos, Vector2 size, float rotation, Vector2 pivot, bool adjustLocal = true)
    {
        if (adjustLocal)
            pos -= size * pivot;
        X = (int) pos.X;
        Y = (int) pos.Y;
        Width = (int) size.X;
        Height = (int) size.Y;
        Rotation = rotation;
        Pivot = pivot;
    }

    /// <summary>
    /// Creates a line
    /// </summary>
    /// <param name="width">Width of this line</param>
    /// <param name="start">Will be <see cref="Bottom"/></param>
    /// <param name="end">Will be <see cref="Top"/></param>
    public RotatedRectangle(float width, Vector2 start, Vector2 end)
    {
        Width = (int) width;
        Height = (int) start.Distance(end);
        Vector2 pivot = new Vector2(.5f, 1f);
        Vector2 pos = start - new Vector2(Width, Height) * pivot;
        X = (int) pos.X;
        Y = (int) pos.Y;
        Rotation = start.AngleTo(end) + MathHelper.PiOver2;
        Pivot = pivot;
    }

    #endregion Constructors

    #region Public Fields

    public int X;
    public int Y;
    public int Width;
    public int Height;
    public float Rotation;
    public Vector2 Pivot;

    #endregion Public Fields

    #region Private Helpers

    public readonly Vector2 PivotPoint => new Vector2(X, Y) + Pivot * Size;

    private readonly Vector2 RotateFromPivot(Vector2 localPoint)
    {
        Vector2 offset = localPoint - Pivot * Size;
        return PivotPoint
               + PolarVector(offset.X, Rotation)
               + PolarVector(offset.Y, Rotation + MathHelper.PiOver2);
    }

    #endregion Private Helpers

    #region Public Properties

    public readonly Vector2 Size => new(Width, Height);

    public readonly Vector2 Center => RotateFromPivot(new Vector2(Width / 2f, Height / 2f));

    public readonly Vector2 Position => RotateFromPivot(Vector2.Zero);

    public readonly Vector2 TopRight => RotateFromPivot(new Vector2(Width, 0));

    public readonly Vector2 Bottom => RotateFromPivot(new Vector2(Width / 2f, Height));

    public readonly Vector2 BottomLeft => RotateFromPivot(new Vector2(0, Height));

    public readonly Vector2 BottomRight => RotateFromPivot(new Vector2(Width, Height));

    public readonly Vector2 Top => RotateFromPivot(new Vector2(Width / 2f, 0));

    public readonly Vector2 Left => RotateFromPivot(new Vector2(0, Height / 2f));

    public readonly Vector2 Right => RotateFromPivot(new Vector2(Width, Height / 2f));

    public readonly Rectangle BaseRect => new(X, Y, Width, Height);

    #endregion Public Properties

    #region Public Methods

    #region Positioning

    public void SetCenter(Vector2 position)
    {
        Vector2 currentCenter = Center;
        Vector2 offset = position - currentCenter;
        X = (int) (X + offset.X);
        Y = (int) (Y + offset.Y);
    }

    public void SetTopRight(Vector2 position)
    {
        Vector2 currentTopRight = TopRight;
        Vector2 offset = position - currentTopRight;
        X = (int) (X + offset.X);
        Y = (int) (Y + offset.Y);
    }

    public void SetBottomLeft(Vector2 position)
    {
        Vector2 currentBottomLeft = BottomLeft;
        Vector2 offset = position - currentBottomLeft;
        X = (int) (X + offset.X);
        Y = (int) (Y + offset.Y);
    }

    public void SetBottomRight(Vector2 position)
    {
        Vector2 currentBottomRight = BottomRight;
        Vector2 offset = position - currentBottomRight;
        X = (int) (X + offset.X);
        Y = (int) (Y + offset.Y);
    }

    public void SetTop(Vector2 position)
    {
        Vector2 currentTop = Top;
        Vector2 offset = position - currentTop;
        X = (int) (X + offset.X);
        Y = (int) (Y + offset.Y);
    }

    public void SetBottom(Vector2 position)
    {
        Vector2 currentBottom = Bottom;
        Vector2 offset = position - currentBottom;
        X = (int) (X + offset.X);
        Y = (int) (Y + offset.Y);
    }

    public void SetLeft(Vector2 position)
    {
        Vector2 currentLeft = Left;
        Vector2 offset = position - currentLeft;
        X = (int) (X + offset.X);
        Y = (int) (Y + offset.Y);
    }

    public void SetRight(Vector2 position)
    {
        Vector2 currentRight = Right;
        Vector2 offset = position - currentRight;
        X = (int) (X + offset.X);
        Y = (int) (Y + offset.Y);
    }

    #endregion Positioning Methods

    public Vector2 ClampPoint(Vector2 point)
    {
        // Transform point to local space
        Vector2 translated = point - Center;
        Vector2 localPoint = translated.RotatedBy(-Rotation);

        // Clamp to rectangle bounds in local space
        float halfWidth = Width / 2f;
        float halfHeight = Height / 2f;
        Vector2 clampedLocal = new(
            Math.Clamp(localPoint.X, -halfWidth, halfWidth),
            Math.Clamp(localPoint.Y, -halfHeight, halfHeight)
        );

        // Transform back to world space
        Vector2 rotated = clampedLocal.RotatedBy(Rotation);

        return rotated + Center;
    }

    public (Vector2 start, Vector2 end)? GetIntersectionLine(Vector2 start, Vector2 end)
    {
        Vector2 direction = end - start;
        float length = direction.Length();

        // There's no line segment
        if (length == 0)
            return null;

        Vector2 center = Center;

        // Translate points relative to the rectangles center and rotate points to align the rectangle with the axes
        Vector2 startLocal = center.DirectionTo(start).RotatedBy(-Rotation);
        Vector2 endLocal = center.DirectionTo(end).RotatedBy(-Rotation);

        // Compute local direction and length
        Vector2 dirLocal = startLocal.DirectionTo(endLocal);
        float lengthLocal = dirLocal.Length();
        dirLocal /= lengthLocal;

        // Define the axis-aligned bounds of the rectangle in local space
        float minX = -Width / 2f;
        float maxX = Width / 2f;
        float minY = -Height / 2f;
        float maxY = Height / 2f;

        // Liang-Barsky algorithm in local space
        float tEnter = float.NegativeInfinity;
        float tExit = float.PositiveInfinity;

        switch (dirLocal.X)
        {
            // Check X constraints
            case > 0:
                tEnter = Math.Max(tEnter, (minX - startLocal.X) / dirLocal.X);
                tExit = Math.Min(tExit, (maxX - startLocal.X) / dirLocal.X);
                break;
            case < 0:
                tEnter = Math.Max(tEnter, (maxX - startLocal.X) / dirLocal.X);
                tExit = Math.Min(tExit, (minX - startLocal.X) / dirLocal.X);
                break;
            default:
            {
                if (startLocal.X < minX || startLocal.X > maxX)
                    return null;
                break;
            }
        }

        switch (dirLocal.Y)
        {
            // Check Y constraints
            case > 0:
                tEnter = Math.Max(tEnter, (minY - startLocal.Y) / dirLocal.Y);
                tExit = Math.Min(tExit, (maxY - startLocal.Y) / dirLocal.Y);
                break;
            case < 0:
                tEnter = Math.Max(tEnter, (maxY - startLocal.Y) / dirLocal.Y);
                tExit = Math.Min(tExit, (minY - startLocal.Y) / dirLocal.Y);
                break;
            default:
            {
                if (startLocal.Y < minY || startLocal.Y > maxY)
                    return null;
                break;
            }
        }

        // Clip to the line segment's range [0, LLocal]
        float tStart = Math.Max(0, tEnter);
        float tEnd = Math.Min(lengthLocal, tExit);

        if (!(tStart <= tEnd))
            return null; // No intersection

        // Compute intersection points in local space
        Vector2 intersectionStart = startLocal + tStart * dirLocal;
        Vector2 intersectionEnd = startLocal + tEnd * dirLocal;

        // Transform points back to world space (rotate and translate)
        Vector2 startWorld = intersectionStart.RotatedBy(Rotation) + center;
        Vector2 endWorld = intersectionEnd.RotatedBy(Rotation) + center;

        return (startWorld, endWorld);
    }

    public Vector2 GetClosestPoint(Vector2 point, bool sidesOnly = false)
    {
        // Transform point to local space
        Vector2 translated = point - Center;
        Vector2 localPoint = translated.RotatedBy(-Rotation);

        float halfWidth = Width / 2f;
        float halfHeight = Height / 2f;

        // Check if point is inside
        bool isInside = localPoint.X >= -halfWidth && localPoint.X <= halfWidth &&
                        localPoint.Y >= -halfHeight && localPoint.Y <= halfHeight;

        if (isInside && !sidesOnly)
            return point;

        // Compute projections to each side
        Vector2 projLeft = new(-halfWidth, Math.Clamp(localPoint.Y, -halfHeight, halfHeight));
        Vector2 projRight = new(halfWidth, Math.Clamp(localPoint.Y, -halfHeight, halfHeight));
        Vector2 projTop = new(Math.Clamp(localPoint.X, -halfWidth, halfWidth), -halfHeight);
        Vector2 projBottom = new(Math.Clamp(localPoint.X, -halfWidth, halfWidth), halfHeight);

        // Compute distances squared
        float distLeft = (localPoint - projLeft).LengthSquared();
        float distRight = (localPoint - projRight).LengthSquared();
        float distTop = (localPoint - projTop).LengthSquared();
        float distBottom = (localPoint - projBottom).LengthSquared();

        // Find the minimum distance
        Vector2 closestLocal = projLeft;
        float minDist = distLeft;
        if (distRight < minDist)
        {
            minDist = distRight;
            closestLocal = projRight;
        }

        if (distTop < minDist)
        {
            minDist = distTop;
            closestLocal = projTop;
        }

        if (distBottom < minDist)
        {
            closestLocal = projBottom;
        }

        // Transform back to world space
        Vector2 rotated = closestLocal.RotatedBy(Rotation);
        Vector2 closestWorld = rotated + Center;

        return closestWorld;
    }

    /// <summary>
    /// Is this <see cref="RotatedRectangle"/> intersecting another <see cref="RotatedRectangle"/>?
    /// </summary>
    /// <param name="other">The other rectangle</param>
    /// <returns>Whether or not they intersect</returns>
    public bool Intersects(RotatedRectangle other)
    {
        Vector2[] thisCorners = [Position, TopRight, BottomLeft, BottomRight];
        Vector2[] otherCorners = [other.Position, other.TopRight, other.BottomLeft, other.BottomRight];
        return IsIntersecting(thisCorners, otherCorners);
    }

    /// <summary>
    /// Is this <see cref="RotatedRectangle"/> intersecting a <see cref="Rectangle"/>?
    /// </summary>
    /// <param name="other">The other rectangle</param>
    /// <returns>Whether or not they intersect</returns>
    public bool Intersects(Rectangle other)
    {
        Vector2[] thisCorners = [Position, TopRight, BottomLeft, BottomRight];
        Vector2[] otherCorners = [other.TopLeft(), other.TopRight(), other.BottomLeft(), other.BottomRight()];
        return IsIntersecting(thisCorners, otherCorners);
    }

    /// <summary>
    /// Gets the intersection points out of another <see cref="RotatedRectangle"/>
    /// </summary>
    /// <param name="other">The other rectangle to lookout for</param>
    /// <returns>The found points, in any</returns>
    public List<Vector2> GetIntersectionPoints(RotatedRectangle other)
    {
        List<Vector2> intersectionPoints = [];
        Vector2[] otherCorners = [other.Position, other.TopRight, other.BottomRight, other.BottomLeft];
        for (int i = 0; i < 4; i++)
        {
            Vector2 start = otherCorners[i];
            Vector2 end = otherCorners[(i + 1) % 4];
            if (LinesIntersect(Position, BottomLeft, start, end, out Vector2 point))
                intersectionPoints.Add(point);
            if (LinesIntersect(Position, TopRight, start, end, out point))
                intersectionPoints.Add(point);
            if (LinesIntersect(TopRight, BottomRight, start, end, out point))
                intersectionPoints.Add(point);
            if (LinesIntersect(BottomLeft, BottomRight, start, end, out point))
                intersectionPoints.Add(point);
        }

        return intersectionPoints;
    }

    /// <summary>
    /// Performs a safe way of getting intersection points from another <see cref="RotatedRectangle"/>
    /// </summary>
    /// <param name="other">The other rectangel to look for</param>
    /// <param name="points">The found points, if any</param>
    /// <returns>Whether or not any points were found</returns>
    public bool TryGetIntersectionPoints(RotatedRectangle other, out List<Vector2> points)
    {
        List<Vector2> list = GetIntersectionPoints(other);
        if (list == null || list.Count == 0)
        {
            points = [];
            return false;
        }

        points = list;
        return true;
    }

    /// <summary>
    /// Determines if this <see cref="RotatedRectangle"/> is intersecting solid tiles
    /// </summary>
    /// <param name="sampleIncrement">How precise the calculation should be. The lower the number the more accurate.</param>
    /// <param name="acceptTopSurfaces">Account for things like platforms?</param>
    /// <returns>Whether or not a intersection happened</returns>
    public bool SolidCollision(float sampleIncrement = 1f, bool acceptTopSurfaces = false)
    {
        Vector2[] corners = [Position, TopRight, BottomRight, BottomLeft];

        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 startPoint = corners[i];
            Vector2 endPoint = corners[(i + 1) % corners.Length];

            float distance = Vector2.Distance(startPoint, endPoint);
            int sampleCount = (int) (distance / sampleIncrement);
            Vector2 direction = startPoint.SafeDirectionTo(endPoint);

            for (int j = 0; j <= sampleCount; j++)
            {
                Vector2 samplePoint = startPoint + direction * sampleIncrement * j;

                // Convert sample point to tile coordinates
                Point tilePoint = ClampToWorld(samplePoint.ToTileCoordinates(), true);

                // Check if the tile is solid
                Tile tile = Main.tile[tilePoint.X, tilePoint.Y];
                bool solid = tile != null && tile.HasTile && Main.tileSolid[tile.TileType] &&
                             !Main.tileSolidTop[tile.TileType];
                if (acceptTopSurfaces)
                    solid |= Main.tileSolidTop[tile.TileType] && tile.TileFrameY == 0;
                if (solid)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines if this <see cref="RotatedRectangle"/> is intersecting liquid of any kind
    /// </summary>
    /// <param name="sampleIncrement">How precise the calculation should be. The lower the number the more accurate.</param>
    /// <returns>Whether or not a intersection happened</returns>
    public bool LiquidCollision(float sampleIncrement = 1f)
    {
        Vector2[] corners = [Position, TopRight, BottomRight, BottomLeft];

        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 startPoint = corners[i];
            Vector2 endPoint = corners[(i + 1) % corners.Length];

            float distance = Vector2.Distance(startPoint, endPoint);
            int sampleCount = (int) (distance / sampleIncrement);
            Vector2 direction = startPoint.SafeDirectionTo(endPoint);

            for (int j = 0; j <= sampleCount; j++)
            {
                Vector2 samplePoint = startPoint + direction * sampleIncrement * j;

                // Convert sample point to tile coordinates
                Point tilePoint = ClampToWorld(samplePoint.ToTileCoordinates(), true);

                // Check if the tile is solid
                Tile tile = Main.tile[tilePoint.X, tilePoint.Y];
                if (tile.LiquidAmount <= 0)
                    continue;

                float completion = 1f - InverseLerp(0f, byte.MaxValue, tile.LiquidAmount);
                Vector2 liquidPosition = new(tilePoint.X * 16, tilePoint.Y * 16 + 16 * completion);

                // Check if the sample point is below the liquid height
                if (samplePoint.Y >= liquidPosition.Y)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets a random point from this <see cref="RotatedRectangle"/>
    /// </summary>
    public Vector2 RandomPoint(bool edge = false)
    {
        if (edge)
        {
            return Main.rand.Next(4) switch
            {
                0 => Vector2.Lerp(Position, TopRight, Main.rand.NextFloat()),
                1 => Vector2.Lerp(TopRight, BottomRight, Main.rand.NextFloat()),
                2 => Vector2.Lerp(BottomRight, BottomLeft, Main.rand.NextFloat()),
                3 => Vector2.Lerp(BottomLeft, Position, Main.rand.NextFloat()),
                _ => Vector2.Zero
            };
        }

        Vector2 randLeft = Vector2.Lerp(Position, BottomLeft, Main.rand.NextFloat());
        Vector2 randRight = Vector2.Lerp(TopRight, BottomRight, Main.rand.NextFloat());
        return Vector2.Lerp(randLeft, randRight, Main.rand.NextFloat());
    }

    #endregion Public Methods

    #region Overrides

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(X, Y, Width, Height, Pivot);
    }

    public override readonly string ToString()
    {
        return
            $"[Position: {Position}, Width: {Width}, Height: {Height}, Current Rotation: {Rotation:F2}, Pivot: {Pivot}]";
    }

    #endregion
}
