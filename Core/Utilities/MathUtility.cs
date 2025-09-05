using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.GameContent.RGB;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.Utilities;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Utilities;
using TheExtraordinaryAdditions.Extraordinary.CrossCompatibility;
using static Terraria.Player;

namespace TheExtraordinaryAdditions.Core.Utilities;

[Flags]
public enum Direction2D : byte
{
    Up = 1,
    Down = 2,
    Left = 4,
    Right = 8,
    TopLeft = Up | Left,
    TopRight = Up | Right,
    BottomLeft = Down | Left,
    BottomRight = Down | Right,
}

public static partial class Utility
{
    public const float GoldenRatio = 1.618033989f;
    public const float InverseGoldenRatio = 0.618033989f;
    public const float PiOver3 = MathF.PI / 3f;

    /// <summary>
    /// Calculates an radially symmetric Gaussian falloff
    /// </summary>
    /// <param name="center">The center of the Gaussian (the peak)</param>
    /// <param name="point">The vector to sample from</param>
    /// <param name="amplitude">The maximum height of the Gaussian (the intensity)</param>
    /// <param name="sigma">Standard deviation in both X and Y directions (the max distance)<br></br> Smaller values make a sharper curve</param>
    /// <param name="minValue">The minimum value the Gaussian may calculate</param>
    /// <returns>The intensity at a <paramref name="point"/> from the <paramref name="center"/></returns>
    public static float GaussianFalloff2D(Vector2 center, Vector2 point, float amplitude, float sigma, float minValue = .0001f)
    {
        float distance = center.Distance(point);
        return MathF.Max(amplitude * MathF.Exp(-distance * distance / (2f * sigma * sigma)), minValue);
    }

    /// <summary>
    /// Calculates an elliptical Gaussian falloff
    /// </summary>
    /// <param name="center">The center of the Gaussian (the peak)</param>
    /// <param name="point">The vector to sample from</param>
    /// <param name="amplitude">The maximum height of the Gaussian (the intensity)</param>
    /// <param name="sigma">Standard deviation in both X and Y directions <br></br> Smaller values make a sharper curve in each direction</param>
    /// <param name="minValue">The minimum value the Gaussian may calculate</param>
    /// <returns>The intensity at a <paramref name="point"/> from the <paramref name="center"/></returns>
    public static float GaussianFalloff2D(Vector2 center, Vector2 point, float amplitude, Vector2 sigma, float minValue = .0001f)
    {
        float dx = point.X - center.X;
        float dy = point.Y - center.Y;
        return MathF.Max(amplitude * MathF.Exp(-((dx * dx) / (2f * (sigma.X * sigma.X)) + (dy * dy) / (2f * (sigma.Y * sigma.Y)))), minValue);
    }

    public static Vector2 ClampToCardinalDirection(Vector2 direction)
    {
        if (direction == Vector2.Zero)
            return Vector2.Zero;

        // Get the angle of the direction vector in radians
        float angle = direction.ToRotation();

        // Convert angle to range [0, 2pi)
        if (angle < 0)
            angle += MathHelper.TwoPi;

        // Round to the nearest 45 degrees
        float cardinalAngle = MathF.Round(angle / (MathHelper.Pi / 4)) * (MathHelper.Pi / 4);

        // Convert back to a normalized vector
        return cardinalAngle.ToRotationVector2();
    }

    /// <summary>
    /// 
    /// <code>
    ///       top(pi/2)
    ///         /   \
    /// sides(pi)   sides(0)
    ///         \   /
    ///     bottom(3pi/2)
    /// </code>
    /// 
    /// </summary>
    /// <param name="angle">An angle between [0, 2pi]</param>
    /// <param name="top">Value returned when at the top</param>
    /// <param name="sides">Value returned when at the sides</param>
    /// <param name="bottom">Value returned when at the bottom</param>
    /// <returns></returns>
    public static float GetCircularSectionValue(float angle, float top = 0f, float sides = .5f, float bottom = 1f, float rotation = 0f)
    {
        // Normalize angle to [0, 2pi]
        angle = MathHelper.WrapAngle(angle + rotation) % (MathHelper.TwoPi);
        if (angle < 0)
            angle += MathHelper.TwoPi;

        float piOver2 = MathHelper.PiOver2;
        float pi = MathHelper.Pi;
        float threePiOver2 = 3f * MathHelper.PiOver2;

        if (angle >= 0 && angle < piOver2)
        {
            float t = angle / piOver2;
            return MathHelper.Lerp(sides, top, t);
        }
        else if (angle >= piOver2 && angle < pi)
        {
            float t = (angle - piOver2) / piOver2;
            return MathHelper.Lerp(top, sides, t);
        }
        else if (angle >= pi && angle < threePiOver2)
        {
            float t = (angle - pi) / piOver2;
            return MathHelper.Lerp(sides, bottom, t);
        }
        else
        {
            float t = (angle - threePiOver2) / piOver2;
            return MathHelper.Lerp(bottom, sides, t);
        }
    }

    /// <summary>
    /// Converts a world coordinate to a valid screen position, accounting for gravity and zoom <br></br>
    /// Mainly used for shaders
    /// </summary>
    public static Vector2 GetTransformedScreenCoords(Vector2 position, bool invert = false, Player player = null)
    {
        Vector2 pos = Vector2.Transform(position - Main.screenPosition, invert ? Matrix.Invert(Main.GameViewMatrix.ZoomMatrix) : (Main.GameViewMatrix.ZoomMatrix));
        if ((player ?? Main.LocalPlayer).gravDir == -1f)
            pos.Y = Main.screenPosition.Y + Main.screenHeight - position.Y;

        return pos;
    }

    /// <summary>
    /// Checks if a target is within a cone of sight
    /// </summary>
    public static bool IsInFieldOfView(this Vector2 viewerPosition, float viewerRotation, Vector2 targetPosition, float viewAngle, float? maxDistance = null)
    {
        Vector2 directionToTarget = targetPosition - viewerPosition;
        float distanceSquared = directionToTarget.LengthSquared();

        if (distanceSquared < 0.0001f)
            return true;

        if (maxDistance != null)
        {
            if (distanceSquared > maxDistance * maxDistance)
                return false;
        }

        directionToTarget = directionToTarget.SafeNormalize(Vector2.Zero);
        Vector2 viewerDirection = viewerRotation.ToRotationVector2();

        float dotProduct = Vector2.Dot(viewerDirection, directionToTarget);
        float angleThreshold = (float)Math.Cos(viewAngle / 2f);

        return dotProduct >= angleThreshold;
    }

    public static Vector2 CalculateJointPosition(Vector2 start, Vector2 end, float limbLength, float secondLimbLength, bool flip)
    {
        float c = Vector2.Distance(start, end);
        float angle = (float)Math.Acos(Math.Clamp((c * c + limbLength * limbLength - secondLimbLength * secondLimbLength) / (c * limbLength * 2f), -1f, 1f)) * (flip ? -1 : 1);
        return start + (angle + start.AngleTo(end)).ToRotationVector2() * limbLength;
    }

    public static Vector2 ClampInRect(this Vector2 vector, Rectangle rect) => new(
        Math.Clamp(vector.X, rect.Left, rect.Right),
        Math.Clamp(vector.Y, rect.Top, rect.Bottom));

    public static Vector2 ClampInCircle(this Vector2 point, Vector2 center, float radius)
    {
        if (radius < 0) 
            return point;

        Vector2 direction = point - center;
        float distance = direction.Length();

        if (distance > radius)
            return center + Vector2.Normalize(direction) * radius;

        return point;
    }

    public static Vector2 ClampOutCircle(this Vector2 point, Vector2 center, float radius)
    {
        if (radius < 0)
            return point;

        Vector2 direction = point - center;
        float distance = direction.Length();

        if (distance < radius)
            return center + Vector2.Normalize(direction) * radius;

        // Point is inside or on the circle, no clamping needed
        return point;
    }

    public static Rectangle RectangleFromVectors(Vector2 topLeft, Vector2 bottomRight) => new(
        (int)Math.Min(topLeft.X, bottomRight.X),
        (int)Math.Min(topLeft.Y, bottomRight.Y),
        (int)Math.Abs(topLeft.X - bottomRight.X),
        (int)Math.Abs(topLeft.Y - bottomRight.Y));

    public static bool ContainsInvalidPoint(this ReadOnlySpan<Vector2> points)
    {
        for (int i = 0; i < points.Length; i++)
        {
            if (float.IsNaN(points[i].X) || float.IsNaN(points[i].Y) ||
            float.IsInfinity(points[i].X) || float.IsInfinity(points[i].Y))
                return true;
        }
        return false;
    }

    public static bool AllPointsEqual(this ReadOnlySpan<Vector2> points)
    {
        if (points.Length <= 1)
            return true; // 0 or 1 point is trivially "all equal"

        Vector2 first = points[0];
        for (int i = 1; i < points.Length; i++)
        {
            if (points[i] != first)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Generates a lightning bolt with a given amount of branches
    /// </summary>
    /// <param name="start">Start of the bolt</param>
    /// <param name="end">End of the bolt</param>
    /// <param name="numBranches">How many seperate branches to make</param>
    /// <param name="thickness">Overall thickness of the bolts</param>
    /// <param name="branchExtraDist">Adds on how much farther resulting branches should go</param>
    /// <param name="sway">How much to offset points</param>
    /// <param name="maxRot">The maximum amount of turn when making branches</param>
    /// <returns></returns>
    public static List<List<Line>> CreateLightningBranch(Vector2 start, Vector2 end, int numBranches, float thickness, float branchExtraDist = 0f, float sway = 50f, float maxRot = .3f)
    {
        List<List<Line>> bolts = [];

        var mainBolt = CreateBolt(start, end, thickness, sway);

        bolts.Add(mainBolt);

        Vector2 diff = end - start;

        // pick a bunch of random points between 0 and 1 and sort them
        float[] branchPoints = [.. Enumerable.Range(0, numBranches)
            .Select(x => Rand(0, 1f))
            .OrderBy(x => x)];

        List<Vector2> pos = [];
        foreach (List<Line> bolt in bolts)
        {
            foreach (Line line in bolt)
                pos.Add(line.a);
        }

        for (int i = 0; i < branchPoints.Length; i++)
        {
            Vector2 boltStart = pos[(int)(branchPoints[i] * (pos.Count - 1))];
            Vector2 boltEnd = (diff * (1 - branchPoints[i])).RotatedByRandom(maxRot) + boltStart;
            Vector2 dir = boltStart.SafeDirectionTo(boltEnd);
            boltEnd += dir * branchExtraDist;

            bolts.Add(CreateBolt(boltStart, boltEnd, thickness, sway));
        }

        return bolts;
    }

    /// <summary>
    /// Creates a simple bolt of electricity
    /// </summary>
    /// <param name="source">The start</param>
    /// <param name="dest">The end</param>
    /// <param name="thickness">Thickness of the bolt</param>
    /// <param name="sway">How much to offset points</param>
    /// <returns></returns>
    public static List<Line> CreateBolt(Vector2 source, Vector2 dest, float thickness, float sway = 50f, int segmentDensity = 4)
    {
        var results = new List<Line>();

        Vector2 tangent = dest - source;

        Vector2 normal = Vector2.Normalize(new Vector2(tangent.Y, -tangent.X));

        float length = tangent.Length();

        List<float> positions = [];

        positions.Add(0);

        for (int i = 0; i < length / segmentDensity; i++)
            positions.Add(Rand(0f, 1f));

        positions.Sort();

        float jaggedness = 1f / sway;

        Vector2 prevPoint = source;

        float prevDisplacement = 0;

        for (int i = 1; i < positions.Count; i++)
        {
            float pos = positions[i];

            // Used to prevent sharp angles by ensuring very close positions also have small perpendicular variation.
            float scale = length * jaggedness * (pos - positions[i - 1]);

            // Defines an envelope. Points near the middle of the bolt can be further from the central line.
            float envelope = pos > 0.95f ? 20f * (1f - pos) : 1f;

            float displacement = Rand(-sway, sway);

            displacement -= (displacement - prevDisplacement) * (1f - scale);

            displacement *= envelope;

            Vector2 point = source + pos * tangent + displacement * normal;

            results.Add(new Line(prevPoint, point, thickness));

            prevPoint = point;

            prevDisplacement = displacement;
        }

        results.Add(new Line(prevPoint, dest, thickness));

        return results;
    }

    public static List<Vector2> GetBoltPoints(Vector2 source, Vector2 dest, float sway = 50f, float segmentDensity = 4f)
    {
        const float EnvelopeThreshold = 0.95f;
        const float EnvelopeScale = 20f;

        var points = new List<Vector2>();
        Vector2 tangent = dest - source;
        Vector2 normal = Vector2.Normalize(new Vector2(tangent.Y, -tangent.X));
        float length = tangent.Length();

        int estimatedSegments = (int)(length / segmentDensity) + 2;
        var positions = new List<float>(estimatedSegments) { 0f };

        // Generate positions without sorting
        float step = 1f / estimatedSegments;
        float currentPos = 0f;
        while (currentPos < 1f)
        {
            currentPos += Rand(0.5f * step, 1.5f * step);
            if (currentPos < 1f)
                positions.Add(currentPos);
        }
        positions.Add(1f);

        float jaggedness = 1f / sway;
        Vector2 prevPoint = source;
        float prevDisplacement = 0f;

        points.Add(source);

        for (int i = 1; i < positions.Count; i++)
        {
            float pos = positions[i];
            float scale = (length * jaggedness) * (pos - positions[i - 1]);
            float envelope = pos > EnvelopeThreshold ? EnvelopeScale * (1f - pos) : 1f;

            float displacement = Rand(-sway, sway);
            displacement = MathHelper.Lerp(prevDisplacement, displacement, scale);
            displacement *= envelope;

            Vector2 point = source + pos * tangent + displacement * normal;
            points.Add(point);

            prevPoint = point;
            prevDisplacement = displacement;
        }

        return points;
    }

    public static List<List<Vector2>> GetLightningBranchPoints(Vector2 start, Vector2 end, int numBranches, float branchExtraDist = 0f, float sway = 50f, float maxRot = 0.3f)
    {
        List<List<Vector2>> bolts = [];

        var mainBolt = GetBoltPoints(start, end, sway);
        bolts.Add(mainBolt);

        Vector2 diff = end - start;
        float[] branchPoints = new float[numBranches];
        for (int i = 0; i < numBranches; i++)
            branchPoints[i] = Rand(0, 1f);
        Array.Sort(branchPoints);

        for (int i = 0; i < branchPoints.Length; i++)
        {
            float t = MathHelper.Clamp(branchPoints[i], 0.01f, 0.99f);
            Vector2 boltStart = Vector2.Lerp(start, end, t);
            Vector2 boltEnd = (diff * (1 - t)).RotatedBy(Rand(-maxRot, maxRot)) + boltStart;
            Vector2 dir = boltStart.SafeDirectionTo(boltEnd);
            boltEnd += dir * branchExtraDist;

            bolts.Add(GetBoltPoints(boltStart, boltEnd, sway));
        }

        return bolts;
    }

    static float Rand(float min, float max)
    {
        return (float)Main.rand.NextDouble() * (max - min) + min;
    }

    public sealed class Line(Vector2 a, Vector2 b, float thickness = 1f)
    {
        public Vector2 a = a;
        public Vector2 b = b;
        public float thickness = thickness;

        /// <summary>
        /// A default drawing method for a line
        /// </summary>
        /// <param name="color">The color of this line</param>
        public void Draw(Color color)
        {
            Texture2D cap = AssetRegistry.GetTexture(AdditionsTexture.BloomLineCap);
            Texture2D horiz = AssetRegistry.GetTexture(AdditionsTexture.BloomLineHoriz);

            Vector2 tangent = a.SafeDirectionTo(b) * a.Distance(b);
            float rotation = tangent.ToRotation();

            const float ImageThickness = 8;

            float thicknessScale = thickness / ImageThickness;

            Vector2 capOrigin = new(cap.Width, cap.Height / 2f);

            Vector2 middleOrigin = new(0, horiz.Height / 2f);

            Vector2 middleScale = new(a.Distance(b) / horiz.Width, thicknessScale);

            Main.spriteBatch.Draw(horiz, a - Main.screenPosition, null, color, rotation, middleOrigin, middleScale, SpriteEffects.None, 0f);

            Main.spriteBatch.Draw(cap, a - Main.screenPosition, null, color, rotation, capOrigin, thicknessScale, SpriteEffects.None, 0f);

            Main.spriteBatch.Draw(cap, b - Main.screenPosition, null, color, rotation + MathHelper.Pi, capOrigin, thicknessScale, SpriteEffects.None, 0f);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RotatedRectangle ToRotated(this Rectangle rect, float rot)
        => new(rect.X, rect.Y, rect.Width, rect.Height, rot);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rectangle FromRotated(this RotatedRectangle rect)
        => new(rect.X, rect.Y, rect.Width, rect.Height);
    public static Vector2[] Corners(this RotatedRectangle rect) => [rect.Position, rect.TopRight, rect.BottomRight, rect.BottomLeft];
    public static Vector2[] Sides(this RotatedRectangle rect) => [rect.Left, rect.Top, rect.Right, rect.Bottom];
    public static Vector2[] Corners(this Rectangle rect) => [rect.TopLeft(), rect.TopRight(), rect.BottomRight(), rect.BottomLeft()];

    #region Polars
    public static SystemVector2 PolarVector2(float radius, float theta) =>
        new SystemVector2((float)Math.Cos(theta), (float)Math.Sin(theta)) * radius;

    /// <summary>
    /// A circle
    /// </summary>
    /// <param name="theta">Subtract <see cref="MathHelper.PiOver2"/> to go up, add to go down</param>
    public static Vector2 PolarVector(float radius, float theta) =>
        new Vector2((float)Math.Cos(theta), (float)Math.Sin(theta)) * radius;

    /// <summary>
    /// A circle that could be oval
    /// </summary>
    public static Vector2 PolarVector(Vector2 radius, float theta) =>
        new Vector2((float)Math.Cos(theta), (float)Math.Sin(theta)) * radius;

    public static Vector2 NextVector2Ellipse(float width, float height, float rotation, Vector2? offset = null)
    {
        offset ??= Vector2.Zero;

        // Generate a random radius and angle in polar coordinates
        float randomAngle = RandomRotation();
        float randomRadius = (float)(Main.rand.NextDouble() * 0.5) + 0.5f; // Random radius between 0.5 and 1.0 for ellipse scaling

        // Convert polar coordinates to Cartesian coordinates for the unrotated ellipse
        float x = (float)(Math.Cos(randomAngle) * (width / 2) * randomRadius);
        float y = (float)(Math.Sin(randomAngle) * (height / 2) * randomRadius);

        // Rotate the point
        float rotatedX = x * (float)Math.Cos(rotation) - y * (float)Math.Sin(rotation);
        float rotatedY = x * (float)Math.Sin(rotation) + y * (float)Math.Cos(rotation);

        return new Vector2(offset.Value.X + rotatedX, offset.Value.Y + rotatedY);
    }

    public static Vector2 NextVector2EllipseEdge(float width, float height, float rotation, Vector2? offset = null)
    {
        offset ??= Vector2.Zero;
        return GetPointOnRotatedEllipse(width, height, rotation, RandomRotation(), offset);
    }

    public static Vector2 GetPointOnRotatedEllipse(float width, float height, float rotation, float theta, Vector2? offset = null)
    {
        offset ??= Vector2.Zero;

        // Calculate the unrotated ellipse point using parametric equations
        float x = width / 2 * (float)Math.Cos(theta);
        float y = height / 2 * (float)Math.Sin(theta);

        // Rotate the point
        float rotatedX = x * (float)Math.Cos(rotation) - y * (float)Math.Sin(rotation);
        float rotatedY = x * (float)Math.Sin(rotation) + y * (float)Math.Cos(rotation);

        return new Vector2(offset.Value.X + rotatedX, offset.Value.Y + rotatedY);
    }

    public static Vector2 GetPointOnLemniscate(float completion, float rotation, float a = 1f)
    {
        float theta = completion * MathHelper.TwoPi;

        // Parametric equations for a lemniscate
        float sinTheta = (float)Math.Sin(theta);
        float cosTheta = (float)Math.Cos(theta);
        float denominator = 1f + sinTheta * sinTheta;
        float x = a * cosTheta / denominator;
        float y = a * sinTheta * cosTheta / denominator;

        // Apply rotation using a 2D rotation matrix
        float rotatedX = x * (float)Math.Cos(rotation) - y * (float)Math.Sin(rotation);
        float rotatedY = x * (float)Math.Sin(rotation) + y * (float)Math.Cos(rotation);

        return new Vector2(rotatedX, rotatedY);
    }
    #endregion Polars

    public static Vector2 Lerp(this Vector2 value1, Vector2 value2, float amount) => Vector2.Lerp(value1, value2, amount);

    #region System Vectors
    public static bool HasNaNs(this SystemVector2 vec)
    {
        if (!float.IsNaN(vec.X))
            return float.IsNaN(vec.Y);

        return true;
    }

    public static SystemVector2 SafeNormalize(this SystemVector2 v, SystemVector2 defaultValue)
    {
        if (v == SystemVector2.Zero || v.HasNaNs())
            return defaultValue;

        return SystemVector2.Normalize(v);
    }

    public static SystemVector2 SafeDirectionTo(this in SystemVector2 from, in SystemVector2 to, SystemVector2? fallback = null)
    {
        if (!fallback.HasValue)
            fallback = SystemVector2.Zero;

        return SafeNormalize(to - from, fallback.Value);
    }

    public static float AngleTo(this in SystemVector2 from, in SystemVector2 to)
    {
        SystemVector2 v = to - from;
        return (float)Math.Atan2(v.Y, v.X);
    }

    public static float ToRotation(this in SystemVector2 v)
    {
        return (float)Math.Atan2(v.Y, v.X);
    }

    public static SystemVector2 RotatedBy(this in SystemVector2 v, float radians)
    {
        float cos = MathF.Cos(radians);
        float sin = MathF.Sin(radians);
        return new SystemVector2(
            v.X * cos - v.Y * sin,
            v.X * sin + v.Y * cos
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static SystemVector2 CatmullRom(in SystemVector2 p0, in SystemVector2 p1, in SystemVector2 p2, in SystemVector2 p3, float t)
    {
        SystemVector2 spline = new();

        float t2 = t * t;
        float t3 = t2 * t;

        spline.X = 0.5f * ((2.0f * p1.X) +
        (-p0.X + p2.X) * t +
        (2.0f * p0.X - 5.0f * p1.X + 4 * p2.X - p3.X) * t2 +
        (-p0.X + 3.0f * p1.X - 3.0f * p2.X + p3.X) * t3);

        spline.Y = 0.5f * ((2.0f * p1.Y) +
        (-p0.Y + p2.Y) * t +
        (2.0f * p0.Y - 5.0f * p1.Y + 4 * p2.Y - p3.Y) * t2 +
        (-p0.Y + 3.0f * p1.Y - 3.0f * p2.Y + p3.Y) * t3);

        return spline;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SystemVector2 ToNumerics(this in Vector2 v) => new(v.X, v.Y);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 FromNumerics(this in SystemVector2 v) => new(v.X, v.Y);
    #endregion System Vectors

    public static int AngleToXDirection(float angle) => MathF.Cos(angle).NonZeroSign();
    public static int AngleToYDirection(float angle) => MathF.Sin(angle).NonZeroSign();

    public static Vector2 ClampMagnitude(this Vector2 v, float min, float max)
    {
        Vector2 result = Utils.SafeNormalize(v, Vector2.UnitY) * MathHelper.Clamp(v.Length(), min, max);
        if (Utils.HasNaNs(result))
            return Vector2.UnitY * (0f - min);

        return result;
    }

    public static Vector2 ClampLength(this Vector2 v, float min, float max) =>
         v.SafeNormalize(Vector2.UnitY) * MathHelper.Clamp(v.Length(), min, max);

    public static bool FinalExtraUpdate(this Projectile proj) => proj.numUpdates == -1;

    public static Rectangle MouseHitbox => new((int)Main.LocalPlayer.Additions().mouseWorld.X, (int)Main.LocalPlayer.Additions().mouseWorld.Y, 14, 14);
    public static Rectangle MouseScreenHitbox => new((int)Main.LocalPlayer.Additions().mouseScreen.X, (int)Main.LocalPlayer.Additions().mouseScreen.Y, 14, 14);

    public static Vector2 ClampToWorld(Vector2 position, bool tilePos = false)
    {
        if (tilePos)
        {
            position.X = (int)MathHelper.Clamp(position.X, 0f, Main.maxTilesX);
            position.Y = (int)MathHelper.Clamp(position.Y, 0f, Main.maxTilesY);
        }
        else
        {
            position.X = (int)MathHelper.Clamp(position.X, 0f, Main.maxTilesX * 16);
            position.Y = (int)MathHelper.Clamp(position.Y, 0f, Main.maxTilesY * 16);
        }
        return position;
    }

    public static Point ClampToWorld(Point position, bool tilePos = false)
    {
        if (tilePos)
        {
            position.X = (int)MathHelper.Clamp(position.X, 0f, Main.maxTilesX);
            position.Y = (int)MathHelper.Clamp(position.Y, 0f, Main.maxTilesY);
        }
        else
        {
            position.X = (int)MathHelper.Clamp(position.X, 0f, Main.maxTilesX * 16);
            position.Y = (int)MathHelper.Clamp(position.Y, 0f, Main.maxTilesY * 16);
        }
        return position;
    }

    public static float UltrasmoothStep(float val1, float val2, float x)
    {
        x = MathHelper.Clamp(x, 0f, 1f);
        return MathHelper.SmoothStep(val1, val2, MathHelper.SmoothStep(val1, val2, x));
    }

    public static float InverseSmoothstep(float x) => .5f - MathF.Sin(MathF.Asin(1f - 2f * x) / 3f);

    public static void SetFrontHandBetter(this Player player, Player.CompositeArmStretchAmount stretch, float rotation) =>
        player.SetCompositeArmFront(true, stretch, (rotation - MathHelper.PiOver2) * player.gravDir + (player.gravDir == -1 ? MathHelper.Pi : 0f));
    public static void SetBackHandBetter(this Player player, Player.CompositeArmStretchAmount stretch, float rotation) =>
        player.SetCompositeArmBack(true, stretch, (rotation - MathHelper.PiOver2) * player.gravDir + (player.gravDir == -1 ? MathHelper.Pi : 0f));

    public static Vector2 GetFrontHandPositionImproved(this Player player)
    {
        CompositeArmData arm = player.compositeFrontArm;
        Vector2 position = Utils.Floor(player.GetFrontHandPosition(arm.stretch, (arm.rotation + player.fullRotation) * player.gravDir));
        if (player.gravDir == -1f)
            position.Y = player.position.Y + player.height + (player.position.Y - position.Y);

        return position;
    }

    public static Vector2 GetBackHandPositionImproved(this Player player)
    {
        CompositeArmData arm = player.compositeBackArm;
        Vector2 position = Utils.Floor(player.GetBackHandPosition(arm.stretch, (arm.rotation + player.fullRotation) * player.gravDir));
        if (player.gravDir == -1f)
            position.Y = player.position.Y + player.height + (player.position.Y - position.Y);

        return position;
    }

    public static bool BetweenNum(this float val, float start, float end, bool? equals = false) =>
     equals == true ? val >= start && val <= end : val > start && val < end;

    public static bool BetweenNum(this int val, int start, int end, bool? equals = false) =>
         equals == true ? val >= start && val <= end : val > start && val < end;

    public static float Convert01To010(float value) => MathF.Sin(MathHelper.Pi * MathHelper.Clamp(value, 0f, 1f));
    public static float Convert01To101(float value) => -Convert01To010(value) + 1;

    // Algorithm taken from http://web.archive.org/web/20060911055655/http://local.wasp.uwa.edu.au/~pbourke/geometry/lineline2d/
    public static bool LinesIntersect(Vector2 point1, Vector2 point2, Vector2 point3, Vector2 point4, out Vector2 intersectPoint)
    {
        intersectPoint = Vector2.Zero;

        double denominator = (point4.Y - point3.Y) * (point2.X - point1.X) - (point4.X - point3.X) * (point2.Y - point1.Y);

        double a = (point4.X - point3.X) * (point1.Y - point3.Y) - (point4.Y - point3.Y) * (point1.X - point3.X);
        double b = (point2.X - point1.X) * (point1.Y - point3.Y) - (point2.Y - point1.Y) * (point1.X - point3.X);

        if (denominator == 0)
        {
            if (a == 0 || b == 0) // Lines are coincident
            {
                intersectPoint = Vector2.Zero; // Possibly not the best fallback?
                return true;
            }
            else
            {
                return false; // Lines are parallel
            }
        }

        double ua = a / denominator;
        double ub = b / denominator;

        if (ua > 0 && ua < 1 && ub > 0 && ub < 1)
        {
            float x = (float)(point1.X + ua * (point2.X - point1.X));
            float y = (float)(point1.Y + ua * (point2.Y - point1.Y));
            intersectPoint = new Vector2(x, y);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Finds the closest point on a line segment to a given point
    /// </summary>
    /// <param name="point">The point to project onto the line segment</param>
    /// <param name="lineStart">The start of the line segment</param>
    /// <param name="lineEnd">The end of the line segment</param>
    /// <returns>The closest point on the line segment to the given point</returns>
    public static Vector2 ClosestPointOnLineSegment(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        Vector2 lineDir = lineEnd - lineStart;
        float lineLengthSquared = lineDir.LengthSquared();

        if (lineLengthSquared == 0f)
            return lineStart; // Line segment has zero length, return the start point

        // Project point onto the line
        Vector2 toPoint = point - lineStart;
        float t = Vector2.Dot(toPoint, lineDir) / lineLengthSquared;

        // Clamp t to stay within the line segment
        t = MathHelper.Clamp(t, 0f, 1f);

        // Return the closest point on the line segment
        return lineStart + t * lineDir;
    }

    /// <remarks>Due to terraria's updating, this should only be used in a update method</remarks>
    public static RotatedRectangle RotHitbox(this Entity entity, float rotation)
    {
        Point point = (entity.position + entity.velocity).ToPoint();
        return new Rectangle(point.X, point.Y, entity.width, entity.height).ToRotated(rotation);
    }

    /// <inheritdoc cref="RotHitbox(Entity, float)"></inheritdoc>
    public static RotatedRectangle RotHitbox(this Projectile projectile)
    {
        Point point = (projectile.position + projectile.velocity).ToPoint();
        return new Rectangle(point.X, point.Y, (int)(projectile.width * projectile.scale), (int)(projectile.height * projectile.scale)).ToRotated(projectile.rotation);
    }

    /// <inheritdoc cref="RotHitbox(Entity, float)"></inheritdoc>
    public static RotatedRectangle RotHitbox(this NPC npc)
    {
        Point point = (npc.position + npc.velocity).ToPoint();
        return new Rectangle(point.X, point.Y, npc.width, npc.height).ToRotated(npc.rotation);
    }

    /// <inheritdoc cref="RotHitbox(Entity, float)"></inheritdoc>
    public static RotatedRectangle RotHitbox(this Player player)
    {
        Point point = (player.position + player.velocity).ToPoint();
        return new Rectangle(point.X, point.Y, player.width, player.height).ToRotated(player.fullRotation);
    }

    public static RotatedRectangle BaseRotHitbox(this Entity entity, float rotation) => new Rectangle((int)entity.position.X, (int)entity.position.Y, entity.width, entity.height).ToRotated(rotation);
    public static RotatedRectangle BaseRotHitbox(this Projectile projectile) => new Rectangle((int)projectile.position.X, (int)projectile.position.Y, (int)(projectile.width * projectile.scale), (int)(projectile.height * projectile.scale)).ToRotated(projectile.rotation);
    public static RotatedRectangle BaseRotHitbox(this NPC npc) => new Rectangle((int)npc.position.X, (int)npc.position.Y, npc.width, npc.height).ToRotated(npc.rotation);
    public static RotatedRectangle BaseRotHitbox(this Player player) => new Rectangle((int)player.position.X, (int)player.position.Y, player.width, player.height).ToRotated(player.fullRotation);

    public static int MultiLerp(float t, params int[] ints)
    {
        t = Math.Clamp(t, 0f, 1f);

        if (t == 0f)
            return ints[0];
        if (t == 1f)
            return ints[^1];

        float scaledIndex = t * (ints.Length - 1);
        int lowerIndex = (int)scaledIndex;
        int upperIndex = lowerIndex + 1;

        // Interpolation factors
        int lowerValue = ints[lowerIndex];
        int upperValue = ints[upperIndex];
        int difference = upperValue - lowerValue;

        // Perform the interpolation
        return lowerValue + (int)(difference * (scaledIndex - lowerIndex));
    }

    public static Vector2 MultiLerp(float t, params Vector2[] points)
    {
        t = MathHelper.Clamp(t, 0f, 1f);

        // Calculate the total number of segments
        float segmentLength = 1f / (points.Length - 1);
        int segmentIndex = (int)(t / segmentLength);

        if (segmentIndex >= points.Length - 1)
            return points[^1];

        // Calculate the blend factor for the current segment
        float segmentT = (t - (segmentIndex * segmentLength)) / segmentLength;

        // Get the two points to interpolate between
        Vector2 start = points[segmentIndex];
        Vector2 end = points[segmentIndex + 1];

        return Vector2.Lerp(start, end, segmentT);
    }

    public static Vector2 ClosestOutOfList(Vector2 target, out int atIndex, ReadOnlySpan<Vector2> span)
    {
        Vector2 final = span[0];
        atIndex = 0;
        float closestDistanceSquared = Vector2.DistanceSquared(target, final);

        for (int i = 0; i < span.Length; i++)
        {
            Vector2 vector = span[i];
            float distanceSquared = Vector2.DistanceSquared(target, vector);
            if (distanceSquared < closestDistanceSquared)
            {
                closestDistanceSquared = distanceSquared;
                final = vector;
                atIndex = i;
            }
        }

        return final;
    }

    public static Vector2 ClosestOutOfList(Vector2 target, out int atIndex, params Vector2[] positions)
    {
        Vector2 final = positions[0];
        atIndex = 0;
        float closestDistanceSquared = Vector2.DistanceSquared(target, final);

        for (int i = 0; i < positions.Length; i++)
        {
            Vector2 vector = positions[i];
            float distanceSquared = Vector2.DistanceSquared(target, vector);
            if (distanceSquared < closestDistanceSquared)
            {
                closestDistanceSquared = distanceSquared;
                final = vector;
                atIndex = i;
            }
        }

        return final;
    }

    /// <param name="velocity">The velocity of this object</param>
    /// <param name="function">Put a desired periodic function here to define what to rotate by <br></br>Is already multiplied by PI</param>
    /// <param name="delayAmount">The length of a period, in frames</param>
    /// <param name="amplitude">How powerful</param>
    /// <param name="delay">Decrements on its own</param>
    /// <param name="time">Increments on its own</param>
    /// <returns>The rotated velocity</returns>
    public static Vector2 VelEqualTrig(this Vector2 velocity, Func<float, float> function, float delayAmount, float amplitude, ref float delay, ref float time)
    {
        time++;
        if (delay <= 0f)
            delay = delayAmount;

        if (delay > 0f)
        {
            delay--;

            float completionRatio = 1f - time / delayAmount - 1f;
            float rot = (float)function.Invoke(MathHelper.Pi * completionRatio) * amplitude;
            return Utils.RotatedBy(velocity.SafeNormalize(Vector2.UnitX), rot);
        }

        return velocity;
    }

    public static Vector2 GetArcVel(Vector2 startingPos, Vector2 targetPos, float gravity, float? minArcHeight = null, float? maxArcHeight = null, float? maxXvel = null, float? heightabovetarget = null)
    {
        Vector2 DistanceToTravel = targetPos - startingPos;
        float MaxHeight = DistanceToTravel.Y - (heightabovetarget ?? 0);

        if (minArcHeight != null)
            MaxHeight = Math.Min(MaxHeight, -(float)minArcHeight);

        if (maxArcHeight != null)
            MaxHeight = Math.Max(MaxHeight, -(float)maxArcHeight);

        float TravelTime;
        float neededYvel;

        if (MaxHeight <= 0)
        {
            neededYvel = -(float)Math.Sqrt(-2 * gravity * MaxHeight);
            TravelTime = (float)Math.Sqrt(-2 * MaxHeight / gravity) + (float)Math.Sqrt(2 * Math.Max(DistanceToTravel.Y - MaxHeight, 0) / gravity); // Time up, then time down
        }
        else
        {
            neededYvel = 0;
            TravelTime = (-neededYvel + (float)Math.Sqrt(Math.Pow(neededYvel, 2) - 4 * -DistanceToTravel.Y * gravity / 2)) / gravity; // Time down
        }

        if (maxXvel != null)
            return new Vector2(MathHelper.Clamp(DistanceToTravel.X / TravelTime, -(float)maxXvel, (float)maxXvel), neededYvel);

        return new Vector2(DistanceToTravel.X / TravelTime, neededYvel);
    }

    public static float InverseLerp(float from, float to, float x, bool clamped = true)
    {
        float inverse = (x - from) / (to - from);
        if (!clamped)
            return inverse;

        return MathHelper.Clamp(inverse, 0f, 1f);
    }

    public static Vector2 InverseLerp(Vector2 from, Vector2 to, float x)
    {
        Vector2 pos;
        pos.X = InverseLerp(from.X, to.X, x);
        pos.Y = InverseLerp(from.Y, to.Y, x);
        return pos;
    }

    public static float GetLerpBump(float from1, float to1, float from2, float to2, float x, bool clamp = true) =>
         Utils.GetLerpValue(from1, to1, x, clamp) * Utils.GetLerpValue(from2, to2, x, clamp);
    public static int NonZeroSign(this float x) => x >= 0f ? 1 : -1;

    public static Vector2[] ResetTrail(this Vector2[] array, Vector2 samplePos, int? newLength = null)
    {
        if (newLength.HasValue)
        {
            int num = newLength.Value;

            if (num != array.Length)
                Array.Resize(ref array, num);
        }

        for (int i = 0; i < array.Length; i++)
            array[i] = samplePos;

        return array;
    }

    public static Vector2[] CreateTrail(this Vector2[] array, Vector2 samplePos)
    {
        // Initialize the array
        if (array == null || array.Any(point => point == Vector2.Zero))
        {
            for (int i = 0; i < array.Length; i++)
                array[i] = samplePos;
        }

        // Update the array
        for (int j = array.Length - 1; j > 0; j--)
            array[j] = array[j - 1];
        array[0] = samplePos;

        return array;
    }

    public static List<Vector2> GetLaserControlPoints(this Vector2 start, Vector2 end, int samplesCount)
    {
        List<Vector2> controlPoints = [];
        for (int i = 0; i < samplesCount; i++)
            controlPoints.Add(Vector2.Lerp(start, end, i / (float)(samplesCount - 1f)));

        return controlPoints;
    }

    public static float AngleBetween(this Vector2 v1, Vector2 v2) =>
     (float)Math.Acos(Vector2.Dot(Utils.SafeNormalize(v1, Vector2.Zero), Utils.SafeNormalize(v2, Vector2.Zero)));

    public static float AngleBetween(this float angle, float otherAngle) => (otherAngle - angle + MathHelper.Pi).Modulo(MathHelper.TwoPi) - MathHelper.Pi;

    /// <summary>
    /// Smoothly interpolates between current and target angles
    /// </summary>
    /// <param name="smoothness">0-1 value, 0 is instant, 1 is very smooth</param>
    /// <param name="shiftSpeed">Base rotation speed in radians per frame</param>
    public static float SmoothAngleLerp(this float currentAngle, float targetAngle, float smoothness, float shiftSpeed)
    {
        // Normalize angles
        currentAngle = MathHelper.WrapAngle(currentAngle);
        targetAngle = MathHelper.WrapAngle(targetAngle);

        // Calculate shortest angular distance
        float difference = targetAngle - currentAngle;
        if (difference > MathHelper.Pi)
            difference -= MathHelper.TwoPi;
        else if (difference < -MathHelper.Pi)
            difference += MathHelper.TwoPi;

        // Calculate rotation amount with smoothness
        float smoothFactor = MathHelper.Clamp(smoothness, 0f, 1f);
        float effectiveSpeed = shiftSpeed * (1f - smoothFactor);

        // Apply velocity based interpolation
        float change = MathHelper.Clamp(
            difference * (1f - smoothFactor) + difference * smoothFactor * 2f,
            -effectiveSpeed,
            effectiveSpeed
        );

        // Apply the change and wrap the result
        float newAngle = MathHelper.WrapAngle(currentAngle + change);

        return newAngle;
    }

    public static SpriteEffects ToSpriteDirection(this int direction) => direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

    public static Vector2 SafeDirectionTo(this Entity entity, Vector2 destination, Vector2? fallback = null)
    {
        if (!fallback.HasValue)
            fallback = Vector2.Zero;

        return Utils.SafeNormalize(destination - entity.Center, fallback.Value);
    }

    public static Vector2 SafeDirectionTo(this Vector2 target, Vector2 destination) =>
            (destination - target).SafeNormalize(Vector2.Zero);

    public static void SmoothFlyNear(this Entity entity, Vector2 destination, float movementSharpnessInterpolant, float movementSmoothnessInterpolant)
    {
        // Calculate the ideal velocity. The closer movementSharpnessInterpolant is to 1, the more closely the entity will hover exactly at the destination
        // Lower, greater than zero values result in a greater tendency to hover in the general vicinity of the destination, rather than zipping straight towards it
        Vector2 idealVelocity = (destination - entity.Center) * MathHelper.Clamp(movementSharpnessInterpolant, 0.0001f, 1f);

        // Interpolate towards the ideal velocity. The closer movementSmoothnessInterpolant is to 1, the more opportunities the entity has for overshooting and more "curvy" motion.
        entity.velocity = Vector2.Lerp(entity.velocity, idealVelocity, MathHelper.Clamp(1f - movementSmoothnessInterpolant, 0.0001f, 1f));
    }

    public static void SmoothFlyNearWithSlowdownRadius(this Entity entity, Vector2 destination, float movementSharpnessInterpolant, float movementSmoothnessInterpolant, float slowdownRadius)
    {
        // Calculate the distance to the slowdown radius. If the entity is within the slowdown radius, the distance is registered as zero
        float distanceToSlowdownRadius = entity.Distance(destination) - slowdownRadius;
        if (distanceToSlowdownRadius < 0f)
            distanceToSlowdownRadius = 0f;

        // Determine the ideal speed based on the distance to the slowdown radius rather than the destination itself
        float idealSpeed = distanceToSlowdownRadius * MathHelper.Clamp(movementSharpnessInterpolant, 0.0001f, 1f);
        Vector2 idealVelocity = entity.Center.SafeDirectionTo(destination) * idealSpeed;

        // Same velocity interpolation behavior as SmoothFlyNear.
        entity.velocity = Vector2.Lerp(entity.velocity, idealVelocity, MathHelper.Clamp(1f - movementSmoothnessInterpolant, 0.0001f, 1f));
    }

    public static Vector2 RandAreaInEntity(this Entity entity) => entity.position + new Vector2(Main.rand.Next(0, entity.width), Main.rand.Next(0, entity.height));

    public static Vector2 RandomVelocity(float directionMult, float min, float max)
    {
        Vector2 velocity = new(Main.rand.NextFloat(-directionMult, directionMult), Main.rand.NextFloat(-directionMult, directionMult));
        while (velocity.X == 0f && velocity.Y == 0f)
            velocity = new Vector2(Utils.NextFloat(Main.rand, 0f - directionMult, directionMult), Utils.NextFloat(Main.rand, 0f - directionMult, directionMult));

        velocity.Normalize();
        velocity *= Main.rand.NextFloat(min, max);
        return velocity;
    }

    public static Vector2 NextVector2FromRectangleLimited(this UnifiedRandom r, Rectangle rect, float min, float max) => new(rect.X + r.NextFloat(min, max) * rect.Width, rect.Y + r.NextFloat(min, max) * rect.Height);
    public static Vector2 NextVector2CircularLimited(this UnifiedRandom r, float circleHalfWidth, float circleHalfHeight, float min, float max) => r.NextVector2Unit() * new Vector2(circleHalfWidth, circleHalfHeight) * r.NextFloat(min, max);
    public static byte NextByte(this UnifiedRandom r, byte min, byte max) => (byte)r.Next(min, max);
    public static T NextEnum<T>(this UnifiedRandom r) where T : Enum
    {
        T[] values = (T[])Enum.GetValues(typeof(T));
        return values[r.Next(values.Length)];
    }
    public static T NextFromSet<T>(this UnifiedRandom random, HashSet<T> objs) => objs.ToArray()[random.Next(objs.Count)];

    /// <summary>
    /// Samples a random value from a Gaussian distribution.
    /// </summary>
    /// <param name="r">The RNG to use for sampling.</param>
    /// <param name="standardDeviation">The standard deviation of the distribution.</param>
    /// <param name="mean">The mean of the distribution. Used for horizontally shifting the overall resulting graph.</param>
    public static float NextGaussian(this UnifiedRandom r, float standardDeviation = 1f, float mean = 0f)
    {
        // Refer to the following link for an explanation of why this works:
        // https://blog.cupcakephysics.com/computational%20physics/2015/05/10/the-box-muller-algorithm.html
        float randomAngle = RandomRotation();

        // An incredibly tiny value of 1e-6 is used as a safe lower bound for the interpolant, as a value of exactly zero will cause the
        // upcoming logarithm to short circuit and return an erroneous output of float.NegativeInfinity.
        // This situation is extremely unlikely, but better safe than sorry.

        float distributionInterpolant = r.NextFloat(1e-6f, 1f);

        return MathF.Sqrt(MathF.Log(distributionInterpolant) * -2f) * MathF.Cos(randomAngle) * standardDeviation + mean;
    }

    // When two periodic functions are summed, the resulting function is periodic if the ratio of the b/a is rational, given periodic functions f and g:
    // f(a * x) + g(b * x). However, if the ratio is irrational, then the result has no period.
    // This is desirable for somewhat random wavy fluctuations.
    // In this case, pi and e used, which are indeed irrational numbers.
    /// <summary>
    /// Calculates an aperiodic sine. This function only achieves this if <paramref name="a"/> and <paramref name="b"/> are irrational numbers.
    /// </summary>
    /// <param name="x">The input value.</param>
    /// <param name="a">The first irrational coefficient.</param>
    /// <param name="b">The second irrational coefficient.</param>
    public static float AperiodicSin(float x, float dx = 0f, float a = MathHelper.Pi, float b = MathHelper.E)
    {
        return (MathF.Sin(x * a + dx) + MathF.Sin(x * b + dx)) * 0.5f;
    }

    public static float RandomRotation() => Main.rand.NextFloat(MathHelper.TwoPi);
    public static Vector2 RandomRectangle(this Rectangle rect) => Main.rand.NextVector2FromRectangle(rect);
    public static Rectangle ToRectangle(this Vector2 vector, int width, int height) => new((int)vector.X - width / 2, (int)vector.Y - height / 2, width, height);

    /// <summary>
    /// Applies 2D FBM, an iterative process commonly use with things like Perlin noise to give a natural, "crisp" aesthetic to noise, rather than a blobby one.
    /// <br></br>
    /// The greater the amount of octaves, the more pronounced this effect is, but the more performance intensive it is.
    /// </summary>
    /// <param name="x">The X position to sample from.</param>
    /// <param name="y">The Y position to sample from.</param>
    /// <param name="seed">The RNG seed for the underlying noise calculations.</param>
    /// <param name="octaves">The amount of octaves. The greater this is, the more crisp the results are.</param>
    /// <param name="gain">The exponential factor between each iteration. Iterations have an intensity of g^n, where g is the gain and n is the iteration number.</param>
    /// <param name="lacunarity">The degree of self-similarity of the noise.</param>
    public static float FractalBrownianMotion(float x, float y, int seed, int octaves, float gain = 0.5f, float lacunarity = 2f)
    {
        float result = 0f;
        float frequency = 1f;
        float amplitude = 0.5f;

        // Offset the noise a bit based on the seed.
        x += seed * 0.00489937f % 10f;

        for (int i = 0; i < octaves; i++)
        {
            // Calculate -1 to 1 ranged noise from the input value.
            float noise = NoiseHelper.GetStaticNoise(new Vector2(x, y) * frequency) * 2f - 1f;

            result += noise * amplitude;
            amplitude *= gain;
            frequency *= lacunarity;
        }
        return result;
    }

    public static void DrawStar(Vector2 position, int dustType, float pointAmount = 5f, float mainSize = 1f, float dustDensity = 1f, float dustSize = 1f, float pointDepthMult = 1f, float pointDepthMultOffset = 0.5f, bool noGravity = false, float randomAmount = 0f, float rotationAmount = -1f)
    {
        float rot = (!(rotationAmount < 0f)) ? rotationAmount : RandomRotation();
        float density = 1f / dustDensity * 0.1f;
        for (float i = 0f; i < MathHelper.TwoPi; i += density)
        {
            float rand = 0f;
            if (randomAmount > 0f)
                rand = Utils.NextFloat(Main.rand, -0.01f, 0.01f) * randomAmount;

            float x = (float)Math.Cos(i + rand);
            float y = (float)Math.Sin(i + rand);
            float mult = Math.Abs(i * (pointAmount / 2f) % MathHelper.Pi - MathHelper.PiOver2) * pointDepthMult + pointDepthMultOffset;
            Vector2 vel = Utils.RotatedBy(new Vector2(x, y), (double)rot, default) * mult * mainSize;
            Dust.NewDustPerfect(position, dustType, vel, 0, default, dustSize).noGravity = noGravity;
        }
    }

    public static void DrawCircle(Vector2 position, int dustType, float mainSize = 1f, float RatioX = 1f, float RatioY = 1f, float dustDensity = 1f, float dustSize = 1f, float randomAmount = 0f, float rotationAmount = 0f, bool nogravity = false)
    {
        float rot = ((!(rotationAmount < 0f)) ? rotationAmount : Utils.NextFloat(Main.rand, 0f, MathHelper.Pi * 2f));
        float density = 1f / dustDensity * 0.1f;
        for (float i = 0f; i < MathHelper.TwoPi; i += density)
        {
            float rand = 0f;
            if (randomAmount > 0f)
            {
                rand = Utils.NextFloat(Main.rand, -0.01f, 0.01f) * randomAmount;
            }
            float x = (float)Math.Cos(i + rand) * RatioX;
            float y = (float)Math.Sin(i + rand) * RatioY;
            if (dustType == 222 || dustType == 130 || nogravity)
            {
                Dust.NewDustPerfect(position, dustType, (Vector2?)(Utils.RotatedBy(new Vector2(x, y), (double)rot, default) * mainSize), 0, default(Color), dustSize).noGravity = true;
            }
            else
            {
                Dust.NewDustPerfect(position, dustType, (Vector2?)(Utils.RotatedBy(new Vector2(x, y), (double)rot, default) * mainSize), 0, default(Color), dustSize);
            }
        }
    }

    public static float Modulo(this float dividend, float divisor)
    {
        return dividend - (float)Math.Floor(dividend / divisor) * divisor;
    }

    /// <summary>
    /// Approximates the derivative of a function at a given point based on a 
    /// </summary>
    /// <param name="fx">The function to take the derivative of.</param>
    /// <param name="x">The value to evaluate the derivative at.</param>
    public static double ApproximateDerivative(this Func<double, double> fx, double x)
    {
        double left = fx(x + 1e-7);
        double right = fx(x - 1e-7);
        return (left - right) * 5e6;
    }

    /// <summary>
    /// Searches for an approximate for a root of a given function.
    /// </summary>
    /// <param name="fx">The function to find the root for.</param>
    /// <param name="initialGuess">The initial guess for what the root could be.</param>
    /// <param name="iterations">The amount of iterations to perform. The higher this is, the more generally accurate the result will be.</param>
    public static double IterativelySearchForRoot(this Func<double, double> fx, double initialGuess, int iterations)
    {
        // This uses the Newton-Raphson method to iteratively get closer and closer to roots of a given function.
        // The exactly formula is as follows:
        // x = x - f(x) / f'(x)
        // In most circumstances repeating the above equation will result in closer and closer approximations to a root.
        // The exact reason as to why this intuitively works can be found at the following video:
        // https://www.youtube.com/watch?v=-RdOwhmqP5s
        double result = initialGuess;
        for (int i = 0; i < iterations; i++)
        {
            double derivative = fx.ApproximateDerivative(result);
            result -= fx(result) / derivative;
        }

        return result;
    }

    /// <summary>
    /// Easy shorthand for (sin(x) + 1) / 2, which has the useful property of having a range of 0 to 1 rather than -1 to 1.
    /// </summary>
    /// <param name="x">The input number.</param>
    public static float Sin01(float x) => MathF.Sin(x) * 0.5f + 0.5f;

    /// <summary>
    /// Easy shorthand for (cos(x) + 1) / 2, which has the useful property of having a range of 0 to 1 rather than -1 to 1.
    /// </summary>
    /// <param name="x">The input number.</param>
    public static float Cos01(float x) => MathF.Cos(x) * 0.5f + 0.5f;
    public static float QuadraticBump(float input) => input * (4 - input * 4);
    public static float InverseQuadraticBump(float input) => -input * (4 + input * 4);

    /// <summary>
    /// Interpolates between three <see cref="Vector2"/>-based points via a quadratic Bezier spline
    /// </summary>
    /// <param name="a">The first point</param>
    /// <param name="b">The second point</param>
    /// <param name="c">The third point</param>
    /// <param name="interpolant">A 0 - 1 completion ratio to sample points by</param>
    public static Vector2 QuadraticBezier(Vector2 a, Vector2 b, Vector2 c, float interpolant)
    {
        Vector2 firstTerm = (1f - interpolant).Squared() * a;
        Vector2 secondTerm = (2f - interpolant * 2f) * interpolant * b;
        Vector2 thirdTerm = interpolant.Squared() * c;

        return firstTerm + secondTerm + thirdTerm;
    }

    public static int SecondsToFrames(int seconds) => seconds * 60;
    public static uint SecondsToFrames(uint seconds) => seconds * 60;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int SecondsToFrames(float seconds) => (int)MathF.Round(seconds * 60f);
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int DistanceToTiles(float distance) => (int)MathF.Round(distance * 16f);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static float Cubed(this int input)
    {
        return (int)MathF.Pow(input, 3);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static float Squared(this int input)
    {
        return (int)MathF.Pow(input, 2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static float Quartic(this float input)
    {
        return MathF.Pow(input, 4);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static float Cubed(this float input)
    {
        return MathF.Pow(input, 3);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static float Squared(this float input)
    {
        return MathF.Pow(input, 2);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static double Quartic(this double input)
    {
        return Math.Pow(input, 4);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static double Cubed(this double input)
    {
        return Math.Pow(input, 3);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static double Squared(this double input)
    {
        return Math.Pow(input, 2);
    }

    public static void VelocityBasedRotation(this Projectile proj, float power = .03f) => proj.rotation += (Math.Abs(proj.velocity.X) + Math.Abs(proj.velocity.Y)) * power * proj.direction;
    public static float FacingUpRight(this Projectile p) => p.rotation = p.velocity.ToRotation() - MathHelper.PiOver4;
    public static float FacingUp(this Projectile p) => p.rotation = p.velocity.ToRotation() + MathHelper.PiOver2;
    public static float FacingDown(this Projectile p) => p.rotation = p.velocity.ToRotation() - MathHelper.PiOver2;
    public static float FacingRight(this Projectile p) => p.rotation = p.velocity.ToRotation();
    public static float FacingLeft(this Projectile p) => p.rotation = p.velocity.ToRotation() + (3f * MathHelper.Pi / 2f);
    
    public static float FacingDirectionLiteral(this Projectile p, bool flip = false)
    {
        float dir1 = -p.velocity.ToRotation();
        float dir2 = p.velocity.ToRotation();
        if (p.direction < 0)
        {
            return p.rotation = flip ? dir2 : dir1;
        }
        else
        {
            return p.rotation = flip ? dir1 : dir2;
        }
    }

    public static void ExpandHitboxBy(this Projectile projectile, int width, int height)
    {
        projectile.position = projectile.Center;
        projectile.width = width;
        projectile.height = height;
        projectile.position -= projectile.Size * 0.5f;
    }

    public static void ExpandHitboxBy(this Projectile projectile, int newSize)
    {
        projectile.ExpandHitboxBy(newSize, newSize);
    }

    public static void ExpandHitboxBy(this Projectile projectile, Vector2 newSize)
    {
        projectile.ExpandHitboxBy((int)newSize.X, (int)newSize.Y);
    }

    public static CompositeArmStretchAmount ToStretchAmount(this float interpolant)
    {
        if (!(interpolant < 0.25f))
        {
            if (!(interpolant < 0.5f))
            {
                if (!(interpolant < 0.75f))
                {
                    return CompositeArmStretchAmount.Full;
                }
                return CompositeArmStretchAmount.ThreeQuarters;
            }
            return CompositeArmStretchAmount.Quarter;
        }
        return CompositeArmStretchAmount.None;
    }

    // TODO: apply actual difficulty shenanigans from the better proj method, but first inspect if these methods are only used with SpawnProjectile so as to not divide twice
    /// <summary>
    /// Use to easily set a value across multiple difficulties
    /// </summary>
    /// <param name="applyDifficultyReduction">Automatically divides the value by 4 thanks to vanilla upscaling</param>
    /// <param name="normal">Applies to base game</param>
    /// <param name="expert">Applies to expert mode worlds</param>
    /// <param name="master">Applies to master mode worlds and Revengeance mode</param>
    /// <param name="ftw">Applies to FTW and Death mode</param>
    /// <param name="legendary">Applies to FTW worlds in master</param>
    /// <param name="gfb">Applies to the funny world</param>
    /// <returns></returns>
    public static float DifficultyBasedValue(float? normal = null, float? expert = null, float? master = null, float? ftw = null, float? legendary = null, float? gfb = null, bool applyDifficultyReduction = false)
    {
        if (Main.zenithWorld && gfb.HasValue)
        {
            return applyDifficultyReduction ? (gfb.Value / 4f) : gfb.Value;
        }
        if (Main.getGoodWorld && Main.masterMode && legendary.HasValue)
        {
            return applyDifficultyReduction ? (legendary.Value / 4f) : legendary.Value;
        }
        if ((Main.getGoodWorld || CommonCalamityVariables.DeathModeActive) && ftw.HasValue)
        {
            return applyDifficultyReduction ? (ftw.Value / 4f) : ftw.Value;
        }
        if ((Main.masterMode || CommonCalamityVariables.RevengeanceModeActive) && master.HasValue)
        {
            return applyDifficultyReduction ? (master.Value / 4f) : master.Value;
        }
        if (Main.expertMode && expert.HasValue)
        {
            return applyDifficultyReduction ? (expert.Value / 4f) : expert.Value;
        }
        return applyDifficultyReduction ? (normal.Value / 4) : normal.Value;
    }

    public static int DifficultyBasedValue(int? normal = null, int? expert = null, int? master = null, int? ftw = null, int? legendary = null, int? gfb = null, bool applyDifficultyReduction = false)
    {
        if (Main.zenithWorld && gfb.HasValue)
        {
            return applyDifficultyReduction ? (gfb.Value / 4) : gfb.Value;
        }
        if (Main.getGoodWorld && Main.masterMode && legendary.HasValue)
        {
            return applyDifficultyReduction ? (legendary.Value / 4) : legendary.Value;
        }
        if ((Main.getGoodWorld || CommonCalamityVariables.DeathModeActive) && ftw.HasValue)
        {
            return applyDifficultyReduction ? (ftw.Value / 4) : ftw.Value;
        }
        if ((Main.masterMode || CommonCalamityVariables.RevengeanceModeActive) && master.HasValue)
        {
            return applyDifficultyReduction ? (master.Value / 4) : master.Value;
        }
        if (Main.expertMode && expert.HasValue)
        {
            return applyDifficultyReduction ? (expert.Value / 4) : expert.Value;
        }
        return applyDifficultyReduction ? (normal.Value / 4) : normal.Value;
    }

    /// <summary>
    /// Defines a given <see cref="NPC"/>'s HP based on the current difficulty mode.
    /// </summary>
    /// <param name="npc">The NPC to set the HP for.</param>
    /// <param name="normalModeHP">HP value for normal mode</param>
    /// <param name="expertModeHP">HP value for expert mode</param>
    /// <param name="revengeanceModeHP">HP value for revengeance mode AND master mode</param>
    /// <param name="deathModeHP">Optional HP value for death mode.</param>
    public static void SetLifeMaxByMode(this NPC npc, int normalModeHP, int expertModeHP, int revengeanceModeHP, int? deathModeHP = null, int? gfbModeHP = null)
    {
        npc.lifeMax = normalModeHP;
        if (Main.expertMode)
            npc.lifeMax = expertModeHP;
        if (CommonCalamityVariables.RevengeanceModeActive || Main.masterMode)
            npc.lifeMax = revengeanceModeHP;
        if (deathModeHP.HasValue && CommonCalamityVariables.DeathModeActive)
            npc.lifeMax = deathModeHP.Value;
        if (gfbModeHP.HasValue && Main.zenithWorld)
            npc.lifeMax = gfbModeHP.Value;
    }

    public static int DamageSoftCap(double dmgInput, int cap)
    {
        if (dmgInput < cap)
        {
            return (int)dmgInput;
        }
        double cappedRatio = Math.Pow(dmgInput / cap, 0.5) / 1.25 + 0.2;
        return (int)(cap * cappedRatio);
    }

    public static float WrapAngle90Degrees(float theta)
    {
        if (theta > MathHelper.Pi)
        {
            theta -= MathHelper.Pi;
        }
        if (theta > MathHelper.PiOver2)
        {
            theta -= MathHelper.Pi;
        }
        if (theta < -MathHelper.PiOver2)
        {
            theta += MathHelper.Pi;
        }
        return theta;
    }

    public static float WrapAngle360(float theta)
    {
        theta = MathHelper.WrapAngle(theta);
        if (theta < 0f)
            theta += MathHelper.TwoPi;

        return theta;
    }

    /// <summary>
    /// Calculates matrices for usage by vertex shaders, notably in the context of primitive meshes.
    /// </summary>
    /// <param name="width">The width of the overall view.</param>
    /// <param name="height">The height of the overall view.</param>
    /// <param name="viewMatrix">The view matrix.</param>
    /// <param name="projectionMatrix">The projection matrix.</param>
    /// <param name="ui">Whether this is for UI. Controls whether gravity screen flipping is taken into account.</param>
    public static void CalculatePrimitiveMatrices(int width, int height, out Matrix viewMatrix, out Matrix projectionMatrix, bool ui = false)
    {
        Vector2 zoom = Main.GameViewMatrix.Zoom;
        if (ui)
            zoom = Vector2.One;

        Matrix zoomScaleMatrix = Matrix.CreateScale(zoom.X, zoom.Y, 1f);

        // Get a matrix that aims towards the Z axis (these calculations are relative to a 2D world).
        viewMatrix = Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.Up);

        // Offset the matrix to the appropriate position.
        viewMatrix *= Matrix.CreateTranslation(0f, -height, 0f);

        // Flip the matrix around 180 degrees.
        viewMatrix *= Matrix.CreateRotationZ(MathHelper.Pi);

        // Account for the inverted gravity effect.
        if (Main.LocalPlayer.gravDir == -1f && !ui)
            viewMatrix *= Matrix.CreateScale(1f, -1f, 1f) * Matrix.CreateTranslation(0f, height, 0f);

        // And account for the current zoom.
        viewMatrix *= zoomScaleMatrix;

        projectionMatrix = Matrix.CreateOrthographicOffCenter(0f, width * zoom.X, 0f, height * zoom.Y, 0f, 1f) * zoomScaleMatrix;
    }

    public static Matrix GetCustomSkyBackgroundMatrix()
    {
        Matrix transformationMatrix = Main.BackgroundViewMatrix.TransformationMatrix;
        Vector3 translationDirection = new(1f, Main.BackgroundViewMatrix.Effects.HasFlag(SpriteEffects.FlipVertically) ? -1f : 1f, 1f);

        transformationMatrix.Translation -= Main.BackgroundViewMatrix.ZoomMatrix.Translation * translationDirection;
        return transformationMatrix;
    }

    /// <summary>
    /// Converts world positions to 0-1 UV values relative to the screen. This is incredibly useful when supplying position data to screen shaders.
    /// </summary>
    /// <param name="worldPosition">The world position.</param>
    public static Vector2 WorldSpaceToScreenUV(Vector2 worldPosition)
    {
        // Calculate the coordinates relative to the raw screen size. This does not yet account for things like zoom.
        Vector2 baseUV = (worldPosition - Main.screenPosition) / Main.ScreenSize.ToVector2();

        // Once the above normalized coordinates are calculated, apply the game view matrix to the result to ensure that zoom is incorporated into the result.
        // In order to achieve this it is necessary to firstly anchor the coordinates so that <0, 0> is the origin and not <0.5, 0.5>, and then convert back to
        // the original anchor point after the transformation is complete.
        return Vector2.Transform(baseUV - Vector2.One * 0.5f, Main.GameViewMatrix.TransformationMatrix with { M41 = 0f, M42 = 0f }) + Vector2.One * 0.5f;
    }

    public static T[] FastUnion<T>(this T[] front, T[] back)
    {
        T[] combined = new T[front.Length + back.Length];

        Array.Copy(front, combined, front.Length);
        Array.Copy(back, 0, combined, front.Length, back.Length);

        return combined;
    }

    /// <summary>
    /// Solves a quadratic equation ax^2 + bx + c = 0 and returns the smallest positive root, if any
    /// </summary>
    public static float? SolveQuadratic(float a, float b, float c)
    {
        if (Math.Abs(a) < 0.0001f) // Treat as linear: bt + c = 0
        {
            if (Math.Abs(b) < 0.0001f)
                return c == 0f ? 0f : null; // c = 0 means already at target; else no solution
            float t = -c / b;
            return t > 0f ? t : null;
        }

        float discriminant = b * b - 4f * a * c;
        if (discriminant < 0f)
            return null;

        float sqrtD = (float)Math.Sqrt(discriminant);
        float t1 = (-b - sqrtD) / (2f * a);
        float t2 = (-b + sqrtD) / (2f * a);

        if (t1 > 0f && t2 > 0f)
            return Math.Min(t1, t2);
        if (t1 > 0f)
            return t1;
        if (t2 > 0f)
            return t2;
        return null;
    }
}