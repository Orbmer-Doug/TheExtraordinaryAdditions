global using RotatedRectangle = TheExtraordinaryAdditions.Core.DataStructures.RotatedRectangle;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Terraria;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Core.DataStructures;

/// <summary>
/// Describes a rotatable 2D-rectangle.
/// </summary>
public struct RotatedRectangle
{
    #region Constructors
    public RotatedRectangle(int x, int y, int width, int height, float rotation)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        Rotation = rotation;
    }

    public RotatedRectangle(Vector2 pos, Vector2 size, float rotation)
    {
        X = (int)pos.X;
        Y = (int)pos.Y;
        Width = (int)size.X;
        Height = (int)size.Y;
        Rotation = rotation;
    }

    /// <param name="start">Will be <see cref="Bottom"/></param>
    /// <param name="end">Will be <see cref="Top"/></param>
    public RotatedRectangle(float height, Vector2 start, Vector2 end)
    {
        Width = (int)height;
        Height = (int)start.Distance(end);
        Rotation = start.AngleTo(end) + MathHelper.PiOver2;
        Vector2 rot = PolarVector(Height / 2f, Rotation - MathHelper.PiOver2);
        X = (int)(start.X - Width / 2f + rot.X);
        Y = (int)(start.Y - Height / 2f + rot.Y);
    }
    #endregion Constructors

    #region Public Fields
    public int X;
    public int Y;
    public int Width;
    public int Height;
    public float Rotation;
    #endregion Public Fields

    #region Public Properties
    public readonly Vector2 Size
    {
        get
        {
            return new(Width, Height);
        }
    }

    public Vector2 Center
    {
        get
        {
            return new Vector2(X, Y) + Size / 2;
        }
    }

    public Vector2 Position
    {
        get
        {
            return Center + PolarVector(Width / 2, Rotation + MathHelper.Pi) + PolarVector(Height / 2, Rotation - MathHelper.PiOver2);
        }
    }

    public Vector2 Top
    {
        get
        {
            return Center + PolarVector(Height / 2, Rotation - MathHelper.PiOver2);
        }
    }

    public Vector2 TopRight
    {
        get
        {
            return Center + PolarVector(Height / 2, Rotation - MathHelper.PiOver2) + PolarVector(Width / 2, Rotation);
        }
    }

    public Vector2 Bottom
    {
        get
        {
            return Center + PolarVector(Height / 2, Rotation + MathHelper.PiOver2);
        }
    }

    public Vector2 BottomLeft
    {
        get
        {
            return Center + PolarVector(Height / 2, Rotation + MathHelper.PiOver2) + PolarVector(Width / 2, Rotation + MathHelper.Pi);
        }
    }

    public Vector2 BottomRight
    {
        get
        {
            return Center + PolarVector(Height / 2, Rotation + MathHelper.PiOver2) + PolarVector(Width / 2, Rotation);
        }
    }

    public Vector2 Left
    {
        get
        {
            return Center + PolarVector(Width / 2, Rotation + MathHelper.Pi);
        }
    }

    public Vector2 Right
    {
        get
        {
            return Center + PolarVector(Width / 2, Rotation);
        }
    }

    #endregion Public Properties

    #region Public Methods
    #region Positioning
    public void SetCenter(Vector2 position)
    {
        Vector2 currentCenter = Center;
        Vector2 offset = position - currentCenter;
        X = (int)(X + offset.X);
        Y = (int)(Y + offset.Y);
    }

    public void SetTopRight(Vector2 position)
    {
        Vector2 currentTopRight = TopRight;
        Vector2 offset = position - currentTopRight;
        X = (int)(X + offset.X);
        Y = (int)(Y + offset.Y);
    }

    public void SetBottomLeft(Vector2 position)
    {
        Vector2 currentBottomLeft = BottomLeft;
        Vector2 offset = position - currentBottomLeft;
        X = (int)(X + offset.X);
        Y = (int)(Y + offset.Y);
    }

    public void SetBottomRight(Vector2 position)
    {
        Vector2 currentBottomRight = BottomRight;
        Vector2 offset = position - currentBottomRight;
        X = (int)(X + offset.X);
        Y = (int)(Y + offset.Y);
    }

    public void SetTop(Vector2 position)
    {
        Vector2 currentTop = Top;
        Vector2 offset = position - currentTop;
        X = (int)(X + offset.X);
        Y = (int)(Y + offset.Y);
    }

    public void SetBottom(Vector2 position)
    {
        Vector2 currentBottom = Bottom;
        Vector2 offset = position - currentBottom;
        X = (int)(X + offset.X);
        Y = (int)(Y + offset.Y);
    }

    public void SetLeft(Vector2 position)
    {
        Vector2 currentLeft = Left;
        Vector2 offset = position - currentLeft;
        X = (int)(X + offset.X);
        Y = (int)(Y + offset.Y);
    }

    public void SetRight(Vector2 position)
    {
        Vector2 currentRight = Right;
        Vector2 offset = position - currentRight;
        X = (int)(X + offset.X);
        Y = (int)(Y + offset.Y);
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

    public (Vector2 start, Vector2 end)? GetIntersectionLine(Vector2 A, Vector2 B)
    {
        // Calculate direction and length
        Vector2 D = B - A;
        float L = D.Length();

        // Avoid division by zero; if A and B are the same point, there's no line segment
        if (L == 0)
            return null;

        // Transform the line into the rectangle's local space (unrotate and translate)
        Vector2 center = Center;

        // Translate points relative to the rectangle's center
        Vector2 ATranslated = A - center;
        Vector2 BTranslated = B - center;

        // Rotate points to align the rectangle with the axes
        Vector2 ALocal = ATranslated.RotatedBy(-Rotation);
        Vector2 BLocal = BTranslated.RotatedBy(-Rotation);

        // Compute local direction and length
        Vector2 DLocal = BLocal - ALocal;
        float LLocal = DLocal.Length();
        if (LLocal == 0)
            return null; // Shouldn't happen since L != 0
        DLocal /= LLocal;

        // Define the axis-aligned bounds of the rectangle in local space
        float minX = -Width / 2f;
        float maxX = Width / 2f;
        float minY = -Height / 2f;
        float maxY = Height / 2f;

        // Apply Liang-Barsky algorithm in local space
        float tEnter = float.NegativeInfinity;
        float tExit = float.PositiveInfinity;

        // Check X constraints
        if (DLocal.X > 0)
        {
            tEnter = Math.Max(tEnter, (minX - ALocal.X) / DLocal.X);
            tExit = Math.Min(tExit, (maxX - ALocal.X) / DLocal.X);
        }
        else if (DLocal.X < 0)
        {
            tEnter = Math.Max(tEnter, (maxX - ALocal.X) / DLocal.X);
            tExit = Math.Min(tExit, (minX - ALocal.X) / DLocal.X);
        }
        else // DLocal.X == 0
        {
            if (ALocal.X < minX || ALocal.X > maxX) 
                return null;
        }

        // Check Y constraints
        if (DLocal.Y > 0)
        {
            tEnter = Math.Max(tEnter, (minY - ALocal.Y) / DLocal.Y);
            tExit = Math.Min(tExit, (maxY - ALocal.Y) / DLocal.Y);
        }
        else if (DLocal.Y < 0)
        {
            tEnter = Math.Max(tEnter, (maxY - ALocal.Y) / DLocal.Y);
            tExit = Math.Min(tExit, (minY - ALocal.Y) / DLocal.Y);
        }
        else // DLocal.Y == 0
        {
            if (ALocal.Y < minY || ALocal.Y > maxY) 
                return null;
        }

        // Clip to the line segment's range [0, LLocal]
        float tStart = Math.Max(0, tEnter);
        float tEnd = Math.Min(LLocal, tExit);

        // If tStart <= tEnd, there is an intersection
        if (tStart <= tEnd)
        {
            // Compute intersection points in local space
            Vector2 startLocal = ALocal + tStart * DLocal;
            Vector2 endLocal = ALocal + tEnd * DLocal;

            // Transform points back to world space (rotate and translate)
            Vector2 startWorld = startLocal.RotatedBy(Rotation) + center;
            Vector2 endWorld = endLocal.RotatedBy(Rotation) + center;

            return (startWorld, endWorld);
        }

        return null; // No intersection
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
        else
        {
            points = list;
            return true;
        }
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
            int sampleCount = (int)(distance / sampleIncrement);
            Vector2 direction = Vector2.Normalize(endPoint - startPoint);

            for (int j = 0; j <= sampleCount; j++)
            {
                Vector2 samplePoint = startPoint + direction * sampleIncrement * j;

                // Convert sample point to tile coordinates
                Point tilePoint = ClampToWorld(samplePoint.ToTileCoordinates(), true);

                // Check if the tile is solid
                Tile tile = Main.tile[tilePoint.X, tilePoint.Y];
                bool solid = tile != null && tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
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
            int sampleCount = (int)(distance / sampleIncrement);
            Vector2 direction = Vector2.Normalize(endPoint - startPoint);

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
                Vector2 liquidPosition = new(tilePoint.X * 16, tilePoint.Y * 16 + (16 * completion));

                // Check if the sample point is below the liquid height
                if (samplePoint.Y >= liquidPosition.Y)
                    return true;
            }
        }

        return false;
    }

    public override readonly bool Equals(object obj)
    {
        return (obj is RotatedRectangle rectangle) && this == rectangle;
    }

    public override readonly int GetHashCode()
    {
        return (X ^ Y ^ Width ^ Height);
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
        else
        {
            Vector2 randLeft = Vector2.Lerp(Position, BottomLeft, Main.rand.NextFloat());
            Vector2 randRight = Vector2.Lerp(TopRight, BottomRight, Main.rand.NextFloat());
            return Vector2.Lerp(randLeft, randRight, Main.rand.NextFloat());
        }
    }

    public override readonly string ToString()
    {
        return $"Position: {new Vector2(X, Y)}, Width: {Width}, Height: {Height}, Size: {new Vector2(Width, Height)}, Current Rotation: {Rotation}";
    }
    #endregion Public Methods

    #region Public Static Methods
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RotatedRectangle Max(RotatedRectangle value1, RotatedRectangle value2)
    {
        return new RotatedRectangle(
            (value1.X > value2.X) ? value1.X : value2.X,
            (value1.Y > value2.Y) ? value1.Y : value2.Y,
            (value1.Width > value2.Width) ? value1.Width : value2.Width,
            (value1.Height > value2.Height) ? value1.Height : value2.Height,
            (value1.Rotation > value2.Rotation) ? value1.Rotation : value2.Rotation
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RotatedRectangle Min(RotatedRectangle value1, RotatedRectangle value2)
    {
        return new RotatedRectangle(
            (value1.X < value2.X) ? value1.X : value2.X,
            (value1.Y < value2.Y) ? value1.Y : value2.Y,
            (value1.Width < value2.Width) ? value1.Width : value2.Width,
            (value1.Height < value2.Height) ? value1.Height : value2.Height,
            (value1.Rotation < value2.Rotation) ? value1.Rotation : value2.Rotation
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RotatedRectangle Clamp(RotatedRectangle value, RotatedRectangle min, RotatedRectangle max)
    {
        return Min(Max(value, min), max);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(RotatedRectangle a, RotatedRectangle b)
    {
        return ((a.X == b.X) &&
                (a.Y == b.Y) &&
                (a.Width == b.Width) &&
                (a.Height == b.Height));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(RotatedRectangle a, RotatedRectangle b)
    {
        return !(a == b);
    }
    #endregion Public Static Methods
}