using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.GameContent.RGB;
using Terraria.Utilities;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics.Systems;

namespace TheExtraordinaryAdditions.Core.Utilities;

// TODO: split up methods with large optional parameters
public static class MathUtils
{
    #region Constants

    // just as a reminder...
    // at one time never realized importance of delta time in games because i didn't know terraria happened to be capped at 60fps
    public const int FramesPerSecond = 60;

    public const float GoldenRatio = 1.618033989f;
    public const float InverseGoldenRatio = 0.618033989f;
    public const float PiOver3 = MathF.PI / 3f;
    public const float ThreePIOver4 = MathHelper.Pi * 3 / 4;
    public const float ThreePIOver2 = MathHelper.Pi * 3 / 2;

    #endregion

    public static int SecondsToFrames(int x) => x * FramesPerSecond;
    public static int SecondsToFrames(float x) => (int) MathF.Round(x * FramesPerSecond);
    public static bool WithinBounds(this int index, int cap) => index >= 0 && index < cap;
    public static float Convert01To010(float value) => MathF.Sin(MathHelper.Pi * MathHelper.Clamp(value, 0f, 1f));
    public static float Convert01To101(float value) => -Convert01To010(value) + 1;

    public static float InverseLerp(float from, float to, float x, bool clamped = true)
    {
        float inverse = (x - from) / (to - from);
        return !clamped ? inverse : MathHelper.Clamp(inverse, 0f, 1f);
    }
    
    public static float GetLerpBump(float from1, float to1, float from2, float to2, float x, bool clamp = true) =>
        InverseLerp(from1, to1, x, clamp) * InverseLerp(from2, to2, x, clamp);

    public static float Clamp01(float val)
    {
        return val switch
        {
            > 1f => 1f,
            < 0f => 0f,
            _ => val
        };
    }

    public static int NonZeroSign(this float x) => x >= 0f ? 1 : -1;

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
    public static float GetCircularSectionValue(float angle, float top = 0f, float sides = .5f, float bottom = 1f,
        float rotation = 0f)
    {
        // Normalize angle to [0, 2pi]
        angle = MathHelper.WrapAngle(angle + rotation) % MathHelper.TwoPi;
        if (angle < 0)
            angle += MathHelper.TwoPi;

        const float piOver2 = MathHelper.PiOver2;
        const float pi = MathHelper.Pi;
        const float threePiOver2 = 3f * MathHelper.PiOver2;

        switch (angle)
        {
            case >= 0 and < piOver2:
            {
                float t = angle / piOver2;
                return MathHelper.Lerp(sides, top, t);
            }
            case >= piOver2 and < pi:
            {
                float t = (angle - piOver2) / piOver2;
                return MathHelper.Lerp(top, sides, t);
            }
            case >= pi and < threePiOver2:
            {
                float t = (angle - pi) / piOver2;
                return MathHelper.Lerp(sides, bottom, t);
            }
            default:
            {
                float t = (angle - threePiOver2) / piOver2;
                return MathHelper.Lerp(bottom, sides, t);
            }
        }
    }

    public static Vector2 ClampToWorld(Vector2 position, bool tilePos = false)
    {
        if (tilePos)
        {
            position.X = (int) MathHelper.Clamp(position.X, 0f, Main.maxTilesX);
            position.Y = (int) MathHelper.Clamp(position.Y, 0f, Main.maxTilesY);
        }
        else
        {
            position.X = (int) MathHelper.Clamp(position.X, 0f, Main.maxTilesX * 16);
            position.Y = (int) MathHelper.Clamp(position.Y, 0f, Main.maxTilesY * 16);
        }

        return position;
    }

    public static Point ClampToWorld(Point position, bool tilePos = false)
    {
        if (tilePos)
        {
            position.X = (int) MathHelper.Clamp(position.X, 0f, Main.maxTilesX);
            position.Y = (int) MathHelper.Clamp(position.Y, 0f, Main.maxTilesY);
        }
        else
        {
            position.X = (int) MathHelper.Clamp(position.X, 0f, Main.maxTilesX * 16);
            position.Y = (int) MathHelper.Clamp(position.Y, 0f, Main.maxTilesY * 16);
        }

        return position;
    }

    public static int MultiLerp(float t, params int[] ints)
    {
        t = Math.Clamp(t, 0f, 1f);

        if (t == 0f)
            return ints[0];
        if (t == 1f)
            return ints[^1];

        float scaledIndex = t * (ints.Length - 1);
        int lowerIndex = (int) scaledIndex;
        int upperIndex = lowerIndex + 1;

        // Interpolation factors
        int lowerValue = ints[lowerIndex];
        int upperValue = ints[upperIndex];
        int difference = upperValue - lowerValue;

        // Perform the interpolation
        return lowerValue + (int) (difference * (scaledIndex - lowerIndex));
    }

    #region Balancing

    public static int FixDamageFromDifficulty(int damage, bool opposite = false)
    {
        float damageJankCorrectionFactor = 1f / 2f;
        if (Main.expertMode)
            damageJankCorrectionFactor = 1f / 4f;
        if (Main.masterMode)
            damageJankCorrectionFactor = 1f / 6f;
        return (int) (damage * damageJankCorrectionFactor);
    }

    public static int DifficultyBasedValue(int normal, int? expert = null, int? master = null, int? ftw = null,
        int? legendary = null, int? gfb = null)
    {
        int val = normal;
        if (expert.HasValue && Main.expertMode)
            val = expert.Value;
        if (master.HasValue && Main.masterMode)
            val = master.Value;
        if (ftw.HasValue && Main.getGoodWorld)
            val = ftw.Value;
        if (legendary.HasValue && Main.getGoodWorld && Main.masterMode)
            val = legendary.Value;
        if (gfb.HasValue && Main.zenithWorld)
            val = gfb.Value;
        return val;
    }

    public static float DifficultyBasedValue(float normal, float? expert = null, float? master = null,
        float? ftw = null, float? legendary = null, float? gfb = null)
    {
        float val = normal;
        if (expert.HasValue && Main.expertMode)
            val = expert.Value;
        if (master.HasValue && Main.masterMode)
            val = master.Value;
        if (ftw.HasValue && Main.getGoodWorld)
            val = ftw.Value;
        if (legendary.HasValue && Main.getGoodWorld && Main.masterMode)
            val = legendary.Value;
        if (gfb.HasValue && Main.zenithWorld)
            val = gfb.Value;
        return val;
    }

    /// <summary>
    /// Defines a given <see cref="NPC"/>'s HP based on the current difficulty mode
    /// </summary>
    public static void SetLifeMaxByMode(this NPC npc, int normalModeHP, int expertModeHP, int revengeanceModeHP,
        int? deathModeHP = null, int? gfbModeHP = null)
    {
        npc.lifeMax = normalModeHP;
        if (Main.expertMode)
            npc.lifeMax = expertModeHP;
        if (Main.masterMode)
            npc.lifeMax = revengeanceModeHP;
        if (deathModeHP.HasValue)
            npc.lifeMax = deathModeHP.Value;
        if (gfbModeHP.HasValue && Main.zenithWorld)
            npc.lifeMax = gfbModeHP.Value;
    }

    public static int DamageSoftCap(double dmgInput, int cap)
    {
        if (dmgInput < cap)
            return (int) dmgInput;

        double cappedRatio = Math.Pow(dmgInput / cap, 0.5) / 1.25 + 0.2;
        return (int) (cap * cappedRatio);
    }

    #endregion

    #region Vectors

    extension(Vector2 start)
    {
        public Vector2 Perp(bool ccw = false) => ccw ? new Vector2(-start.Y, start.X) : new Vector2(start.Y, -start.X);

        public Vector2 Lerp(Vector2 end, float t) => Vector2.Lerp(start, end, t);

        public List<Vector2> GetLaserControlPoints(Vector2 end, int samplesCount)
        {
            List<Vector2> controlPoints = [];
            for (int i = 0; i < samplesCount; i++)
                controlPoints.Add(Vector2.Lerp(start, end, i / (samplesCount - 1f)));

            return controlPoints;
        }

        public float AngleBetween(Vector2 v2) =>
            (float) Math.Acos(Vector2.Dot(start.SafeNormalize(Vector2.Zero), v2.SafeNormalize(Vector2.Zero)));

        public Vector2 SafeDirectionTo(Vector2 destination) =>
            (destination - start).SafeNormalize(Vector2.Zero);

        public Rectangle ToRectangle(int width, int height) =>
            new((int) start.X - width / 2, (int) start.Y - height / 2, width, height);

        public Vector2 ClampOutCircle(Vector2 center, float radius)
        {
            if (radius < 0)
                return start;

            Vector2 direction = start - center;
            float distance = direction.Length();

            if (distance < radius)
                return center + Vector2.Normalize(direction) * radius;

            // Point is inside or on the circle, no clamping needed
            return start;
        }

        public Vector2 ClampInRect(Rectangle rect) => new(
            Math.Clamp(start.X, rect.Left, rect.Right),
            Math.Clamp(start.Y, rect.Top, rect.Bottom));

        public Vector2 ClampInCircle(Vector2 center, float radius)
        {
            if (radius < 0)
                return start;

            Vector2 direction = start - center;
            float distance = direction.Length();

            if (distance > radius)
                return center + Vector2.Normalize(direction) * radius;

            return start;
        }

        /// <summary>
        /// Checks if a target is within a cone of sight
        /// </summary>
        public bool IsInFieldOfView(float viewerRotation, Vector2 targetPosition,
            float viewAngle, float? maxDistance = null)
        {
            Vector2 directionToTarget = targetPosition - start;
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
            float angleThreshold = (float) Math.Cos(viewAngle / 2f);

            return dotProduct >= angleThreshold;
        }

        public Vector2 ClampLength(float min, float max) =>
            start.SafeNormalize(Vector2.UnitY) * MathHelper.Clamp(start.Length(), min, max);

        /// <param name="function">Put a desired periodic function here to define what to rotate by <br></br>Is already multiplied by PI</param>
        /// <param name="delayAmount">The length of a period, in frames</param>
        /// <param name="amplitude">How powerful</param>
        /// <param name="delay">Decrements on its own</param>
        /// <param name="time">Increments on its own</param>
        /// <returns>The rotated velocity</returns>
        public Vector2 VelEqualTrig(Func<float, float> function, float delayAmount,
            float amplitude, ref float delay, ref float time)
        {
            time++;
            if (delay <= 0f)
                delay = delayAmount;

            if (delay > 0f)
            {
                delay--;

                float completionRatio = 1f - time / delayAmount - 1f;
                float rot = function.Invoke(MathHelper.Pi * completionRatio) * amplitude;
                return start.SafeNormalize(Vector2.UnitX).RotatedBy(rot);
            }

            return start;
        }
    }

    public static Vector2 MultiLerp(float t, params Vector2[] points)
    {
        t = MathHelper.Clamp(t, 0f, 1f);

        // Calculate the total number of segments
        float segmentLength = 1f / (points.Length - 1);
        int segmentIndex = (int) (t / segmentLength);

        if (segmentIndex >= points.Length - 1)
            return points[^1];

        // Calculate the blend factor for the current segment
        float segmentT = (t - segmentIndex * segmentLength) / segmentLength;

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

    public static Vector2 ClosestPointOnLineSegment(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        Vector2 lineDir = lineEnd - lineStart;
        float lineLengthSquared = lineDir.LengthSquared();

        if (lineLengthSquared == 0f)
            return lineStart; // Line segment has zero length, return the start point

        // Project point onto the line
        Vector2 toPoint = point - lineStart;
        float t = Vector2.Dot(toPoint, lineDir) / lineLengthSquared;
        t = MathHelper.Clamp(t, 0f, 1f);

        return lineStart + t * lineDir;
    }

    public static Vector2 ClosestPointOnCircle(Vector2 point, Vector2 circlePoint, float radius,
        bool clampToEdges = true)
    {
        Vector2 dir = point - circlePoint;
        float distanceSqr = dir.LengthSquared();
        if (!clampToEdges && distanceSqr <= radius.Squared())
            return point;

        return circlePoint + dir.SafeNormalize(Vector2.UnitX) * radius;
    }

    public static float Wedge(Vector2 vec1, Vector2 vec2) => vec1.X * vec2.Y - vec1.Y * vec2.X;

    public static Vector2 CalculateJointPosition(Vector2 start, Vector2 end, float limbLength, float secondLimbLength,
        bool flip)
    {
        float c = Vector2.Distance(start, end);
        float angle =
            (float) Math.Acos(Math.Clamp(
                (c * c + limbLength * limbLength - secondLimbLength * secondLimbLength) / (c * limbLength * 2f), -1f,
                1f)) * (flip ? -1 : 1);
        return start + (angle + start.AngleTo(end)).ToRotationVector2() * limbLength;
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

    #region Numerics Vectors

    extension(SystemVector2 vec)
    {
        public bool HasNaNs()
        {
            return float.IsNaN(vec.X) || float.IsNaN(vec.Y);
        }

        public SystemVector2 SafeNormalize(SystemVector2 defaultValue)
        {
            if (vec == SystemVector2.Zero || vec.HasNaNs())
                return defaultValue;

            return SystemVector2.Normalize(vec);
        }
    }

    extension(in SystemVector2 from)
    {
        public SystemVector2 SafeDirectionTo(in SystemVector2 to,
            SystemVector2? fallback = null)
        {
            fallback ??= SystemVector2.Zero;
            return (to - from).SafeNormalize(fallback.Value);
        }

        public float AngleTo(in SystemVector2 to)
        {
            SystemVector2 v = to - from;
            return (float) Math.Atan2(v.Y, v.X);
        }

        public float ToRotation()
        {
            return (float) Math.Atan2(from.Y, from.X);
        }

        public SystemVector2 RotatedBy(float radians)
        {
            float cos = MathF.Cos(radians);
            float sin = MathF.Sin(radians);
            return new SystemVector2(
                from.X * cos - from.Y * sin,
                from.X * sin + from.Y * cos
            );
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static SystemVector2 CatmullRom(in SystemVector2 p0, in SystemVector2 p1, in SystemVector2 p2,
        in SystemVector2 p3, float t)
    {
        SystemVector2 spline = new();

        float t2 = t * t;
        float t3 = t2 * t;

        spline.X = 0.5f * (2.0f * p1.X +
                           (-p0.X + p2.X) * t +
                           (2.0f * p0.X - 5.0f * p1.X + 4 * p2.X - p3.X) * t2 +
                           (-p0.X + 3.0f * p1.X - 3.0f * p2.X + p3.X) * t3);

        spline.Y = 0.5f * (2.0f * p1.Y +
                           (-p0.Y + p2.Y) * t +
                           (2.0f * p0.Y - 5.0f * p1.Y + 4 * p2.Y - p3.Y) * t2 +
                           (-p0.Y + 3.0f * p1.Y - 3.0f * p2.Y + p3.Y) * t3);

        return spline;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SystemVector2 ToNumerics(this in Vector2 v) => new(v.X, v.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 FromNumerics(this in SystemVector2 v) => new(v.X, v.Y);

    #region 3D

    extension(SystemVector3 vec)
    {
        public bool HasNaNs()
        {
            return float.IsNaN(vec.X) || float.IsNaN(vec.Y);
        }

        public SystemVector3 SafeNormalize(SystemVector3 defaultValue)
        {
            if (vec == SystemVector3.Zero || vec.HasNaNs())
                return defaultValue;

            return SystemVector3.Normalize(vec);
        }
    }

    public static SystemVector3 SafeDirectionTo(this in SystemVector3 from, in SystemVector3 to,
        SystemVector3? fallback = null)
    {
        fallback ??= SystemVector3.Zero;
        return (to - from).SafeNormalize(fallback.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static SystemVector3 CatmullRom(in SystemVector3 p0, in SystemVector3 p1, in SystemVector3 p2,
        in SystemVector3 p3, float t)
    {
        SystemVector3 spline = new();

        float t2 = t * t;
        float t3 = t2 * t;

        spline.X = 0.5f * (2.0f * p1.X +
                           (-p0.X + p2.X) * t +
                           (2.0f * p0.X - 5.0f * p1.X + 4 * p2.X - p3.X) * t2 +
                           (-p0.X + 3.0f * p1.X - 3.0f * p2.X + p3.X) * t3);

        spline.Y = 0.5f * (2.0f * p1.Y +
                           (-p0.Y + p2.Y) * t +
                           (2.0f * p0.Y - 5.0f * p1.Y + 4 * p2.Y - p3.Y) * t2 +
                           (-p0.Y + 3.0f * p1.Y - 3.0f * p2.Y + p3.Y) * t3);

        spline.Z = 0.5f * (2.0f * p1.Z +
                           (-p0.Z + p2.Z) * t +
                           (2.0f * p0.Z - 5.0f * p1.Z + 4 * p2.Z - p3.Z) * t2 +
                           (-p0.Z + 3.0f * p1.Z - 3.0f * p2.Z + p3.Z) * t3);

        return spline;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SystemVector3 ToNumerics(this in Vector3 v) => new(v.X, v.Y, v.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 FromNumerics(this in SystemVector3 v) => new(v.X, v.Y, v.Z);

    #endregion

    #endregion

    #region Lightning

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
    public static List<List<Line>> CreateLightningBranch(Vector2 start, Vector2 end, int numBranches, float thickness,
        float branchExtraDist = 0f, float sway = 50f, float maxRot = .3f)
    {
        List<List<Line>> bolts = [];

        List<Line> mainBolt = CreateBolt(start, end, thickness, sway);

        bolts.Add(mainBolt);

        Vector2 diff = end - start;

        // pick a bunch of random points between 0 and 1 and sort them
        float[] branchPoints =
        [
            .. Enumerable.Range(0, numBranches)
                .Select(x => Main.rand.NextFloat(0, 1f))
                .OrderBy(x => x)
        ];

        List<Vector2> pos = [];
        foreach (List<Line> bolt in bolts)
        {
            foreach (Line line in bolt)
                pos.Add(line.A);
        }

        for (int i = 0; i < branchPoints.Length; i++)
        {
            Vector2 boltStart = pos[(int) (branchPoints[i] * (pos.Count - 1))];
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
    public static List<Line> CreateBolt(Vector2 source, Vector2 dest, float thickness, float sway = 50f,
        int segmentDensity = 4)
    {
        List<Line> results = [];

        Vector2 tangent = dest - source;

        Vector2 normal = Vector2.Normalize(new Vector2(tangent.Y, -tangent.X));

        float length = tangent.Length();

        List<float> positions =
        [
            0
        ];

        for (int i = 0; i < length / segmentDensity; i++)
            positions.Add(Main.rand.NextFloat(0f, 1f));

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

            float displacement = Main.rand.NextFloat(-sway, sway);

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
        const float envelopeThreshold = 0.95f;
        const float envelopeScale = 20f;

        List<Vector2> points = [];
        Vector2 tangent = dest - source;
        Vector2 normal = Vector2.Normalize(new Vector2(tangent.Y, -tangent.X));
        float length = tangent.Length();

        int estimatedSegments = (int) (length / segmentDensity) + 2;
        List<float> positions = new List<float>(estimatedSegments) { 0f };

        // Generate positions without sorting
        float step = 1f / estimatedSegments;
        float currentPos = 0f;
        while (currentPos < 1f)
        {
            currentPos += Main.rand.NextFloat(0.5f * step, 1.5f * step);
            if (currentPos < 1f)
                positions.Add(currentPos);
        }

        positions.Add(1f);

        float jaggedness = 1f / sway;
        float prevDisplacement = 0f;

        points.Add(source);

        for (int i = 1; i < positions.Count; i++)
        {
            float pos = positions[i];
            float scale = length * jaggedness * (pos - positions[i - 1]);
            float envelope = pos > envelopeThreshold ? envelopeScale * (1f - pos) : 1f;

            float displacement = Main.rand.NextFloat(-sway, sway);
            displacement = MathHelper.Lerp(prevDisplacement, displacement, scale);
            displacement *= envelope;

            Vector2 point = source + pos * tangent + displacement * normal;
            points.Add(point);

            prevDisplacement = displacement;
        }

        return points;
    }

    public static List<List<Vector2>> GetLightningBranchPoints(Vector2 start, Vector2 end, int numBranches,
        float branchExtraDist = 0f, float sway = 50f, float maxRot = 0.3f)
    {
        List<List<Vector2>> bolts = [];

        List<Vector2> mainBolt = GetBoltPoints(start, end, sway);
        bolts.Add(mainBolt);

        Vector2 diff = end - start;
        float[] branchPoints = new float[numBranches];
        for (int i = 0; i < numBranches; i++)
            branchPoints[i] = Main.rand.NextFloat(0, 1f);
        Array.Sort(branchPoints);

        for (int i = 0; i < branchPoints.Length; i++)
        {
            float t = MathHelper.Clamp(branchPoints[i], 0.01f, 0.99f);
            Vector2 boltStart = Vector2.Lerp(start, end, t);
            Vector2 boltEnd = (diff * (1 - t)).RotatedBy(Main.rand.NextFloat(-maxRot, maxRot)) + boltStart;
            Vector2 dir = boltStart.SafeDirectionTo(boltEnd);
            boltEnd += dir * branchExtraDist;

            bolts.Add(GetBoltPoints(boltStart, boltEnd, sway));
        }

        return bolts;
    }

    public readonly struct Line(Vector2 a, Vector2 b, float thickness = 1f)
    {
        public readonly Vector2 A = a;
        public readonly Vector2 B = b;
        public readonly float Thickness = thickness;

        public void Draw(Color color, float widthInterpol = 1f)
        {
            Texture2D cap = AssetRegistry.GennedTextures.BloomLineCap;
            Texture2D horiz = AssetRegistry.GennedTextures.BloomLineHoriz;

            Vector2 tangent = A.SafeDirectionTo(B) * A.Distance(B);
            float rotation = tangent.ToRotation();

            const float imageThickness = 8;

            float thicknessScale = Thickness * widthInterpol / imageThickness;

            Vector2 capOrigin = new(cap.Width, cap.Height / 2f);

            Vector2 middleOrigin = new(0, horiz.Height / 2f);

            Vector2 middleScale = new(A.Distance(B) / horiz.Width, thicknessScale);

            Main.spriteBatch.Draw(horiz, A - Main.screenPosition, null, color, rotation, middleOrigin, middleScale,
                SpriteEffects.None, 0f);

            Main.spriteBatch.Draw(cap, A - Main.screenPosition, null, color, rotation, capOrigin, thicknessScale,
                SpriteEffects.None, 0f);

            Main.spriteBatch.Draw(cap, B - Main.screenPosition, null, color, rotation + MathHelper.Pi, capOrigin,
                thicknessScale, SpriteEffects.None, 0f);
        }

        public void DrawPixelated(PixelationLayer layer, BlendState blend, Color color, float widthInterpol = 1f)
        {
            Texture2D cap = AssetRegistry.GennedTextures.BloomLineCap;
            Texture2D horiz = AssetRegistry.GennedTextures.BloomLineHoriz;

            Vector2 tangent = A.SafeDirectionTo(B) * A.Distance(B);
            float rotation = tangent.ToRotation();

            const float imageThickness = 8;

            float thicknessScale = Thickness * widthInterpol / imageThickness;

            Vector2 capOrigin = new(cap.Width, cap.Height / 2f);

            Vector2 middleOrigin = new(0, horiz.Height / 2f);

            Vector2 middleScale = new(A.Distance(B) / horiz.Width, thicknessScale);

            SpriteBatch.DrawAltPixelated(layer, blend, horiz, A - Main.screenPosition, null, color, rotation,
                middleOrigin, middleScale);

            SpriteBatch.DrawAltPixelated(layer, blend, cap, A - Main.screenPosition, null, color, rotation, capOrigin,
                thicknessScale);

            SpriteBatch.DrawAltPixelated(layer, blend, cap, B - Main.screenPosition, null, color,
                rotation + MathHelper.Pi, capOrigin,
                thicknessScale);
        }
    }

    #endregion

    #endregion

    #region Rectangles

    public static Rectangle MouseHitbox => new((int) Main.LocalPlayer.AdditionsMouse().MouseWorld.X,
        (int) Main.LocalPlayer.AdditionsMouse().MouseWorld.Y, 14, 14);

    public static Rectangle MouseScreenHitbox => new((int) Main.LocalPlayer.AdditionsMouse().MouseScreen.X,
        (int) Main.LocalPlayer.AdditionsMouse().MouseScreen.Y, 14, 14);

    public static Rectangle RectangleFromPoints(Vector2 topLeft, Vector2 bottomRight) => new(
        (int) Math.Min(topLeft.X, bottomRight.X),
        (int) Math.Min(topLeft.Y, bottomRight.Y),
        (int) Math.Abs(topLeft.X - bottomRight.X),
        (int) Math.Abs(topLeft.Y - bottomRight.Y));

    public static Vector2[] Corners(this Rectangle rect) =>
        [rect.TopLeft(), rect.TopRight(), rect.BottomRight(), rect.BottomLeft()];

    public static RotatedRectangle ToRotated(this Rectangle rect, float rot, Vector2? pivot = null)
        => new(new(rect.X, rect.Y), new(rect.Width, rect.Height), rot, pivot ?? Vector2.One / 2);

    /// <remarks>Due to terraria's updating, this should only be used in a update method</remarks>
    public static RotatedRectangle RotHitbox(this Entity entity, float rotation, Vector2? pivot = null)
    {
        Point point = (entity.Center + entity.velocity).ToPoint();
        return new Rectangle(point.X, point.Y, entity.width, entity.height).ToRotated(rotation,
            pivot ?? Vector2.One / 2);
    }

    /// <inheritdoc cref="RotHitbox(Entity, float, Vector2?)"></inheritdoc>
    public static RotatedRectangle RotHitbox(this Projectile projectile, Vector2? pivot = null)
    {
        Point point = (projectile.Center + projectile.velocity).ToPoint();
        return new Rectangle(point.X, point.Y, (int) (projectile.width * projectile.scale),
            (int) (projectile.height * projectile.scale)).ToRotated(projectile.rotation, pivot ?? Vector2.One / 2);
    }

    /// <inheritdoc cref="RotHitbox(Entity, float, Vector2?)"></inheritdoc>
    public static RotatedRectangle RotHitbox(this NPC npc, Vector2? pivot = null)
    {
        Point point = (npc.Center + npc.velocity).ToPoint();
        return new Rectangle(point.X, point.Y, npc.width, npc.height).ToRotated(npc.rotation, pivot ?? Vector2.One / 2);
    }

    /// <inheritdoc cref="RotHitbox(Entity, float, Vector2?)"></inheritdoc>
    public static RotatedRectangle RotHitbox(this Player player, Vector2? pivot = null)
    {
        Point point = (player.Center + player.velocity).ToPoint();
        return new Rectangle(point.X, point.Y, player.width, player.height).ToRotated(player.fullRotation,
            pivot ?? Vector2.One / 2);
    }

    public static RotatedRectangle BaseRotHitbox(this Projectile projectile, Vector2? pivot = null) =>
        new Rectangle((int) projectile.Center.X, (int) projectile.Center.Y,
                (int) (projectile.width * projectile.scale), (int) (projectile.height * projectile.scale))
            .ToRotated(projectile.rotation, pivot ?? Vector2.One / 2);

    public static RotatedRectangle BaseRotHitbox(this Entity entity, float rotation, Vector2? pivot = null) =>
        new Rectangle((int) entity.Center.X, (int) entity.Center.Y, entity.width, entity.height)
            .ToRotated(rotation, pivot ?? Vector2.One / 2);

    public static RotatedRectangle BaseRotHitbox(this NPC npc, Vector2? pivot = null) =>
        new Rectangle((int) npc.Center.X, (int) npc.Center.Y, npc.width, npc.height).ToRotated(npc.rotation,
            pivot ?? Vector2.One / 2);

    public static RotatedRectangle BaseRotHitbox(this Player player, Vector2? pivot = null) =>
        new Rectangle((int) player.Center.X, (int) player.Center.Y, player.width, player.height).ToRotated(
            player.fullRotation, pivot ?? Vector2.One / 2);

    #endregion

    #region Polars

    public static SystemVector2 PolarVector2(float radius, float theta) =>
        new SystemVector2((float) Math.Cos(theta), (float) Math.Sin(theta)) * radius;

    /// <summary>
    /// A circle
    /// </summary>
    /// <param name="theta">Subtract <see cref="MathHelper.PiOver2"/> to go up, add to go down</param>
    public static Vector2 PolarVector(float radius, float theta) =>
        new Vector2((float) Math.Cos(theta), (float) Math.Sin(theta)) * radius;

    /// <summary>
    /// A circle that could be oval
    /// </summary>
    public static Vector2 PolarVector(Vector2 radius, float theta) =>
        new Vector2((float) Math.Cos(theta), (float) Math.Sin(theta)) * radius;

    public static Vector2 NextVector2Ellipse(float width, float height, float rotation, Vector2? offset = null)
    {
        offset ??= Vector2.Zero;

        // Generate a random radius and angle in polar coordinates
        float randomAngle = RandomRotation();
        float randomRadius =
            (float) (Main.rand.NextDouble() * 0.5) + 0.5f; // Random radius between 0.5 and 1.0 for ellipse scaling

        // Convert polar coordinates to Cartesian coordinates for the unrotated ellipse
        float x = (float) (Math.Cos(randomAngle) * (width / 2) * randomRadius);
        float y = (float) (Math.Sin(randomAngle) * (height / 2) * randomRadius);

        // Rotate the point
        float rotatedX = x * (float) Math.Cos(rotation) - y * (float) Math.Sin(rotation);
        float rotatedY = x * (float) Math.Sin(rotation) + y * (float) Math.Cos(rotation);

        return new Vector2(offset.Value.X + rotatedX, offset.Value.Y + rotatedY);
    }

    public static Vector2 NextVector2EllipseEdge(float width, float height, float rotation, Vector2? offset = null)
    {
        offset ??= Vector2.Zero;
        return GetPointOnRotatedEllipse(width, height, rotation, RandomRotation(), offset);
    }

    public static Vector2 GetPointOnRotatedEllipse(float width, float height, float rotation, float theta,
        Vector2? offset = null)
    {
        offset ??= Vector2.Zero;

        // Calculate the unrotated ellipse point using parametric equations
        float x = width / 2 * (float) Math.Cos(theta);
        float y = height / 2 * (float) Math.Sin(theta);

        // Rotate the point
        float rotatedX = x * (float) Math.Cos(rotation) - y * (float) Math.Sin(rotation);
        float rotatedY = x * (float) Math.Sin(rotation) + y * (float) Math.Cos(rotation);

        return new Vector2(offset.Value.X + rotatedX, offset.Value.Y + rotatedY);
    }

    public static Vector2 GetPointOnLemniscate(float completion, float rotation, float a = 1f)
    {
        float theta = completion * MathHelper.TwoPi;

        // Parametric equations for a lemniscate
        float sinTheta = (float) Math.Sin(theta);
        float cosTheta = (float) Math.Cos(theta);
        float denominator = 1f + sinTheta * sinTheta;
        float x = a * cosTheta / denominator;
        float y = a * sinTheta * cosTheta / denominator;

        // Apply rotation using a 2D rotation matrix
        float rotatedX = x * (float) Math.Cos(rotation) - y * (float) Math.Sin(rotation);
        float rotatedY = x * (float) Math.Sin(rotation) + y * (float) Math.Cos(rotation);

        return new Vector2(rotatedX, rotatedY);
    }

    #endregion Polars

    #region Angles

    public static int AngleToXDirection(float angle) => MathF.Cos(angle).NonZeroSign();
    public static int AngleToYDirection(float angle) => MathF.Sin(angle).NonZeroSign();

    public static float WrapAngle360(float theta)
    {
        theta = MathHelper.WrapAngle(theta);
        if (theta < 0f)
            theta += MathHelper.TwoPi;

        return theta;
    }

    extension(float angle)
    {
        public float AngleBetween(float otherAngle) =>
            (otherAngle - angle + MathHelper.Pi).Modulo(MathHelper.TwoPi) - MathHelper.Pi;

        /// <summary>
        /// Smoothly interpolates between current and target angles
        /// </summary>
        /// <param name="smoothness">0-1 value, 0 is instant, 1 is very smooth</param>
        /// <param name="shiftSpeed">Base rotation speed in radians per frame</param>
        public float SmoothAngleLerp(float targetAngle, float smoothness, float shiftSpeed)
        {
            // Normalize angles
            angle = MathHelper.WrapAngle(angle);
            targetAngle = MathHelper.WrapAngle(targetAngle);

            // Calculate shortest angular distance
            float difference = targetAngle - angle;
            switch (difference)
            {
                case > MathHelper.Pi:
                    difference -= MathHelper.TwoPi;
                    break;
                case < -MathHelper.Pi:
                    difference += MathHelper.TwoPi;
                    break;
            }

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
            float newAngle = MathHelper.WrapAngle(angle + change);

            return newAngle;
        }

        public float Modulo(float divisor)
        {
            return angle - (float) Math.Floor(angle / divisor) * divisor;
        }
    }

    #endregion

    #region Powers

    extension(int input)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public float Cubed()
        {
            return (int) MathF.Pow(input, 3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public float Squared()
        {
            return (int) MathF.Pow(input, 2);
        }
    }

    extension(float input)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public float Quartic()
        {
            return MathF.Pow(input, 4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public float Cubed()
        {
            return MathF.Pow(input, 3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public float Squared()
        {
            return MathF.Pow(input, 2);
        }
    }

    extension(double input)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public double Quartic()
        {
            return Math.Pow(input, 4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public double Cubed()
        {
            return Math.Pow(input, 3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public double Squared()
        {
            return Math.Pow(input, 2);
        }
    }

    #endregion

    #region Random

    extension(Entity entity)
    {
        public Vector2 RandAreaInEntity() => entity.position +
                                             new Vector2(Main.rand.Next(0, entity.width),
                                                 Main.rand.Next(0, entity.height));
    }

    public static float RandomRotation() => Main.rand.NextFloat(MathHelper.TwoPi);
    public static Vector2 RandomRectangle(this Rectangle rect) => Main.rand.NextVector2FromRectangle(rect);

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
    public static float FractalBrownianMotion(float x, float y, int seed, int octaves, float gain = 0.5f,
        float lacunarity = 2f)
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


    public static Vector2 RandomVelocity(float directionMult, float min, float max)
    {
        Vector2 velocity = new(Main.rand.NextFloat(-directionMult, directionMult),
            Main.rand.NextFloat(-directionMult, directionMult));
        while (velocity.X == 0f && velocity.Y == 0f)
            velocity = new Vector2(Main.rand.NextFloat(0f - directionMult, directionMult),
                Main.rand.NextFloat(0f - directionMult, directionMult));

        velocity.Normalize();
        velocity *= Main.rand.NextFloat(min, max);
        return velocity;
    }

    /// <param name="r">The RNG to use for sampling.</param>
    extension(UnifiedRandom r)
    {
        public Vector2 NextVector2FromRectangleLimited(Rectangle rect, float min, float max)
            => new(rect.X + r.NextFloat(min, max) * rect.Width, rect.Y + r.NextFloat(min, max) * rect.Height);

        public Vector2 NextVector2CircularLimited(float circleHalfWidth,
            float circleHalfHeight, float min, float max)
            => r.NextVector2Unit() * new Vector2(circleHalfWidth, circleHalfHeight) * r.NextFloat(min, max);

        public byte NextByte(byte min, byte max) => (byte) r.Next(min, max);

        public T NextEnum<T>() where T : Enum
        {
            T[] values = (T[]) Enum.GetValues(typeof(T));
            return values[r.Next(values.Length)];
        }

        public T NextFromSet<T>(HashSet<T> objs) =>
            objs.ToArray()[r.Next(objs.Count)];

        /// <summary>
        /// Samples a random value from a Gaussian distribution.
        /// </summary>
        /// <param name="standardDeviation">The standard deviation of the distribution.</param>
        /// <param name="mean">The mean of the distribution. Used for horizontally shifting the overall resulting graph.</param>
        public float NextGaussian(float standardDeviation = 1f, float mean = 0f)
        {
            // Refer to the following link for an explanation of why this works:
            // https://blog.cupcakephysics.com/computational%20physics/2015/05/10/the-box-muller-algorithm.html
            float randomAngle = RandomRotation();

            // An incredibly tiny value of 1e-6 is used as a safe lower bound for the interpolant, as a value of exactly zero will cause the
            // upcoming logarithm to short circuit and return an erroneous output of float.NegativeInfinity.
            // This situation is extremely unlikely, but better safe than sorry.

            float distributionInterpolant = r.NextFloat(1e-6f, 1f);

            return MathF.Sqrt(MathF.Log(distributionInterpolant) * -2f) * MathF.Cos(randomAngle) * standardDeviation +
                   mean;
        }
    }

    public static float GaussianDistribution(float x, float standardDeviation, float mean = 0f)
    {
        const float sqrt2Pi = 2.5066283f;

        float correctionCoefficient = 1f / (standardDeviation * sqrt2Pi);
        float exponent = ((x - mean) / standardDeviation).Squared() * -0.5f;
        return correctionCoefficient * MathF.Exp(exponent);
    }

    public static Vector3 RandomInSphere(float radius, float minPercent, float maxPercent)
    {
        float theta = RandomRotation();
        float phi = (float) Math.Acos(2.0 * Main.rand.NextDouble() - 1.0);

        float x = (float) (Math.Sin(phi) * Math.Cos(theta));
        float y = (float) (Math.Sin(phi) * Math.Sin(theta));
        float z = (float) Math.Cos(phi);

        float t = (float) Math.Pow(Main.rand.NextDouble(), 1.0 / 3.0);
        float r = radius * MathHelper.Lerp(minPercent, maxPercent, t);

        return new Vector3(x, y, z) * r;
    }

    #endregion

    #region Trails

    extension(ReadOnlySpan<Vector2> points)
    {
        public bool ContainsZeroedPoint()
        {
            for (int i = 0; i < points.Length; i++)
            {
                if (points[i] == Vector2.Zero)
                    return true;
            }

            return false;
        }

        public bool ContainsInvalidPoint()
        {
            for (int i = 0; i < points.Length; i++)
            {
                if (float.IsNaN(points[i].X) || float.IsNaN(points[i].Y) ||
                    float.IsInfinity(points[i].X) || float.IsInfinity(points[i].Y))
                    return true;
            }

            return false;
        }

        public bool AllPointsEqual()
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
    }

    #endregion

    #region Graphical

    /// <summary>
    /// Converts world positions to 0-1 UV values relative to the screen. This is incredibly useful when supplying position data to screen shaders.
    /// </summary>
    public static Vector2 WorldSpaceToScreenUV(Vector2 worldPosition)
    {
        // Calculate the coordinates relative to the raw screen size. This does not yet account for things like zoom.
        Vector2 baseUV = (worldPosition - Main.screenPosition) / Main.ScreenSize.ToVector2();

        // Once the above normalized coordinates are calculated, apply the game view matrix to the result to ensure that zoom is incorporated into the result.
        // In order to achieve this it is necessary to firstly anchor the coordinates so that <0, 0> is the origin and not <0.5, 0.5>, and then convert back to
        // the original anchor point after the transformation is complete.
        return Vector2.Transform(baseUV - Vector2.One * 0.5f,
            Main.GameViewMatrix.TransformationMatrix with { M41 = 0f, M42 = 0f }) + Vector2.One * 0.5f;
    }

    /// <summary>
    /// Converts a world coordinate to a valid screen position, accounting for gravity and zoom
    /// </summary>
    public static Vector2 GetTransformedScreenCoords(Vector2 position, bool invert = false, Player player = null)
    {
        if (Main.dedServ)
            return Vector2.Zero;

        Vector2 pos = Vector2.Transform(position - Main.screenPosition,
            invert ? Matrix.Invert(Main.GameViewMatrix.ZoomMatrix) : Main.GameViewMatrix.ZoomMatrix);
        if ((int) (player ?? Main.LocalPlayer).gravDir == -1)
            pos.Y = Main.screenPosition.Y + Main.screenHeight - position.Y;

        return pos;
    }

    #endregion

    #region 3D

    public static Vector3 SphericalToCartesian(float r, float theta, float phi)
    {
        float x = r * MathF.Sin(theta) * MathF.Cos(phi);
        float y = r * MathF.Cos(theta);
        float z = r * MathF.Sin(theta) * MathF.Sin(phi);
        return new Vector3(x, y, z);
    }

    #endregion

    #region General

    /// <summary>
    /// Calculates an aperiodic sine
    /// </summary>
    /// <param name="x">The input value.</param>
    /// <param name="a">The first irrational coefficient.</param>
    /// <param name="b">The second irrational coefficient.</param>
    public static float AperiodicSin(float x, float dx = 0f, float a = MathHelper.Pi, float b = MathHelper.E)
    {
        return (MathF.Sin(x * a + dx) + MathF.Sin(x * b + dx)) * 0.5f;
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

    /// <summary>
    /// Calculates an radially symmetric Gaussian falloff
    /// </summary>
    /// <param name="center">The center of the Gaussian (the peak)</param>
    /// <param name="point">The vector to sample from</param>
    /// <param name="amplitude">The maximum height of the Gaussian (the intensity)</param>
    /// <param name="sigma">Standard deviation in both X and Y directions (the max distance)<br></br> Smaller values make a sharper curve</param>
    /// <param name="minValue">The minimum value the Gaussian may calculate</param>
    /// <returns>The intensity at a <paramref name="point"/> from the <paramref name="center"/></returns>
    public static float GaussianFalloff2D(Vector2 center, Vector2 point, float amplitude, float sigma,
        float minValue = .0001f)
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
    public static float GaussianFalloff2D(Vector2 center, Vector2 point, float amplitude, Vector2 sigma,
        float minValue = .0001f)
    {
        float dx = point.X - center.X;
        float dy = point.Y - center.Y;
        return MathF.Max(
            amplitude * MathF.Exp(-(dx * dx / (2f * (sigma.X * sigma.X)) + dy * dy / (2f * (sigma.Y * sigma.Y)))),
            minValue);
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

        float sqrtD = (float) Math.Sqrt(discriminant);
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

    /// <param name="fx">The function to take the derivative of.</param>
    extension(Func<double, double> fx)
    {
        /// <summary>
        /// Approximates the derivative of a function at a given point based on a 
        /// </summary>
        /// <param name="x">The value to evaluate the derivative at.</param>
        public double ApproximateDerivative(double x)
        {
            double left = fx(x + 1e-7);
            double right = fx(x - 1e-7);
            return (left - right) * 5e6;
        }

        /// <summary>
        /// Searches for an approximate for a root of a given function.
        /// </summary>
        /// <param name="initialGuess">The initial guess for what the root could be.</param>
        /// <param name="iterations">The amount of iterations to perform. The higher this is, the more generally accurate the result will be.</param>
        public double IterativelySearchForRoot(double initialGuess, int iterations)
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
    }

    #endregion

    #region Complex Numbers

    // Derived from Math.NET.Numerics: https://numerics.mathdotnet.com/
    private static readonly double[] GammaDk =
    [
        2.4857408913875355E-05,
        1.0514237858172197,
        -3.4568709722201625,
        4.512277094668948,
        -2.9828522532357664,
        1.056397115771267,
        -0.19542877319164587,
        0.01709705434044412,
        -0.0005719261174043057,
        4.633994733599057E-06,
        -2.7199490848860772E-09
    ];

    public static double Gamma(double z)
    {
        if (z < 0.5)
        {
            double num = GammaDk[0];
            for (int index = 1; index <= 10; ++index)
                num += GammaDk[index] / (index - z);
            return Math.PI / (Math.Sin(Math.PI * z) * num * 1.8603827342052657 *
                              Math.Pow((0.5 - z + 10.900511) / Math.E, 0.5 - z));
        }

        double num1 = GammaDk[0];
        for (int index = 1; index <= 10; ++index)
            num1 += GammaDk[index] / (z + index - 1.0);
        return num1 * 1.8603827342052657 * Math.Pow((z - 0.5 + 10.900511) / Math.E, z - 0.5);
    }

    #endregion
}
