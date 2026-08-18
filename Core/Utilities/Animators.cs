using System;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.Xna.Framework.MathHelper;
using static System.MathF;

namespace TheExtraordinaryAdditions.Core.Utilities;

public static class Animators
{
    #region Dark Evil Wizard Numbers

    private const float c1 = 1.70158f;
    private const float c2 = c1 * 1.525f;
    private const float c3 = c1 + 1;

    private const float c4 = 2f * Pi / 3f;
    private const float c5 = 2f * Pi / 4.5f;

    private const float n1 = 7.5625f;
    private const float d1 = 2.75f;

    #endregion Dark Evil Wizard Numbers

    #region Easing/Curve Definitions and Functions

    public static Vector2 CatmullRomSpline(List<Vector2> points, float t)
    {
        if (points == null || points.Count < 2)
            return Vector2.Zero;

        int segmentCount = points.Count - 1;
        if (segmentCount == 0)
            return points[0];

        // Scale t to select the correct segment
        float scaledT = t * segmentCount;
        int segmentIndex = Math.Min((int) scaledT, segmentCount - 1);
        float localT = scaledT - segmentIndex;

        Vector2 p0 = segmentIndex == 0 ? points[0] : points[segmentIndex - 1]; // First point (or previous)
        Vector2 p1 = points[segmentIndex]; // Current point
        Vector2 p2 = points[segmentIndex + 1]; // Next point
        Vector2 p3 = segmentIndex == segmentCount - 1
            ? points[segmentIndex + 1]
            : points[segmentIndex + 2]; // Next (or next-next)

        return Vector2.CatmullRom(p0, p1, p2, p3, localT);
    }

    public static List<Vector2> CatmullRomSpline(List<Vector2> points, int segments)
    {
        if (points == null || points.Count < 2)
            return [];

        List<Vector2> splinePoints = [];

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 p0 = (i == 0) ? points[i] : points[i - 1]; // First point (or previous)
            Vector2 p1 = points[i]; // Current point
            Vector2 p2 = points[i + 1]; // Next point
            Vector2 p3 = (i == points.Count - 2) ? points[i + 1] : points[i + 2]; // Next (or next-next)

            for (int j = 0; j < segments; j++)
            {
                float t = (float) j / segments;
                splinePoints.Add(Vector2.CatmullRom(p0, p1, p2, p3, t));
            }
        }

        return splinePoints;
    }

    public delegate float InterpolationFunction(float interpolant);

    public record Curve(
        InterpolationFunction InFunction,
        InterpolationFunction OutFunction,
        InterpolationFunction InOutFunction);

    public static InterpolationFunction Bump(float from1, float to1, float from2, float to2) =>
        interpolant => GetLerpBump(from1, to1, from2, to2, interpolant);

    public static InterpolationFunction BezierEase => interpolant =>
        interpolant * interpolant / (2f * (interpolant * interpolant - interpolant) + 1f);

    public static InterpolationFunction SwoopEase => interpolant =>
        3.75f * (interpolant * interpolant * interpolant) - 8.5f * (interpolant * interpolant) + 5.75f * interpolant;

    public static InterpolationFunction InterpHermite(int amt = 3) =>
        interpolant => interpolant * interpolant * (amt - (amt - 1) * interpolant);

    /// <param name="p0">Start</param>
    /// <param name="p1">Peak</param>
    /// <param name="m0">Tangent at <paramref name="p0"/></param>
    /// <param name="m1">Tangent at <paramref name="p1"/></param>
    public static InterpolationFunction Hermite(float p0, float p1, float m0, float m1) => interpolant =>
        (2f * interpolant.Cubed() - 3f * interpolant.Squared() + 1f) * p0 +
        (interpolant.Cubed() - 2f * interpolant.Squared() + interpolant) * m0 +
        (-2f * interpolant.Cubed() + 3f * interpolant.Squared()) * p1 +
        (interpolant.Cubed() - interpolant.Squared()) * m1;

    /// <summary>
    /// May help to use https://cubic-bezier.com/
    /// </summary>
    public static InterpolationFunction CubicBezier(float x1, float y1, float x2, float y2)
    {
        return t =>
        {
            Vector2 _p0 = new(0f, 0f); // Start point
            Vector2 _p1 = new(x1, y1); // First control point
            Vector2 _p2 = new(x2, y2); // Second control point
            Vector2 _p3 = new(1f, 1f); // End point

            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            // Cubic Bézier formula: B(t) = (1-t)^3 * P0 + 3(1-t)^2 * t * P1 + 3(1-t) * t^2 * P2 + t^3 * P3
            Vector2 point = uuu * _p0 +
                            3f * uu * t * _p1 +
                            3f * u * tt * _p2 +
                            ttt * _p3;

            return point.Y;
        };
    }

    public static InterpolationFunction SineBump => Convert01To010;

    public static readonly Curve Sine = new(interpolant => 1f - Cos(interpolant * Pi / 2f),
        interpolant => Sin(interpolant * Pi / 2f),
        interpolant => -(Cos(interpolant * Pi) - 1f) / 2f);

    public static Curve MakePoly(float exponent)
    {
        return new(interpolant => Pow(interpolant, exponent),
            interpolant => 1f - Pow(1f - interpolant, exponent), interpolant =>
            {
                if (interpolant < 0.5f)
                    return Pow(2f, exponent - 1f) * Pow(interpolant, exponent);
                return 1f - Pow(interpolant * -2f + 2f, exponent) * 0.5f;
            });
    }

    public static Curve Expo(float exponent = 2f)
    {
        return new(interpolant =>
        {
            if (interpolant == 0f)
                return 0f;

            return Pow(exponent, 10f * interpolant - 10f);
        }, interpolant =>
        {
            if (interpolant == 1f)
                return 1f;

            return 1f - Pow(exponent, -10f * interpolant);
        }, interpolant =>
        {
            if (interpolant == 0f)
                return 0f;
            if (interpolant == 1f)
                return 1f;
            if (interpolant <= .5f)
                return Pow(exponent, (20f * interpolant) - 10f) / 2f;

            return (2f - Pow(exponent, (-20f * interpolant) + 10f)) / 2f;
        });
    }

    public static readonly Curve Circ = new(interpolant => 1f - Sqrt(1f - interpolant.Squared()),
        interpolant => Sqrt(1f - (interpolant - 1f).Squared()),
        interpolant => interpolant < 0.5
            ? 1f - Sqrt(1f - (2f * interpolant).Squared()) / 2f
            : (Sqrt(1f - (-2f * interpolant - 2f).Squared()) + 1f) / 2f);

    public static readonly Curve Back = new(interpolant => c3 * interpolant.Cubed() - c1 * interpolant.Squared(),
        interpolant => 1 + c3 * (interpolant - 1).Cubed() + c1 * (interpolant - 1).Squared(),
        interpolant => interpolant < 0.5
            ? (2 * interpolant).Squared() * ((c2 + 1) * 2 * interpolant - c2) / 2
            : ((2 * interpolant - 2).Squared() * ((c2 + 1) * (interpolant * 2 - 2) + c2) + 2) / 2);

    public static readonly Curve Elastic = new(
        interpolant => -Pow(2, 10 * interpolant - 10) * Sin((interpolant * 10f - 10.75f) * c4),
        interpolant => Pow(2, -10 * interpolant) * Sin((interpolant * 10f - 0.75f) * c4) + 1,
        interpolant => interpolant < 0.5
            ? -(Pow(2, 20 * interpolant - 10) * Sin((20 * interpolant - 11.125f) * c5)) / 2
            : (Pow(2, -20 * interpolant + 10) * Sin((20 * interpolant - 11.125f) * c5)) / 2 + 1);

    private static float BounceOutFunction(float interpolant) => interpolant < 1 / d1 ? n1 * interpolant.Squared()
        : interpolant < 2 / d1 ? n1 * (interpolant - 1.5f / d1) * interpolant + 0.75f
        : interpolant < 2.5 / d1 ? n1 * (interpolant - 2.25f / d1) * interpolant + 0.9375f
        : n1 * (interpolant - 2.625f / d1) * interpolant + 0.984375f;

    public static readonly Curve Bounce = new(interpolant => 1 - BounceOutFunction(1 - interpolant),
        BounceOutFunction,
        interpolant => interpolant < 0.5
            ? (1 - BounceOutFunction(1 - 2 * interpolant)) / 2
            : (1 + BounceOutFunction(2 * interpolant - 1)) / 2);

    #endregion Easing/Curve Definitions and Functions

    #region Evaluators

    /// <summary>
    /// Evaluates an interpolation function at a given interpolant, scaling from start to end
    /// </summary>
    public static float Evaluate(this InterpolationFunction interpol, float start, float end, float interpolant,
        bool clamp = true)
    {
        if (clamp)
            interpolant = Clamp(interpolant, 0f, 1f);
        return Lerp(start, end, interpol(interpolant));
    }

    /// <summary>
    /// Maps a value from one range to another using an interpolation function
    /// </summary>
    public static float Evaluate(this InterpolationFunction interpol, float fromValue, float fromMin, float fromMax,
        float toMin, float toMax, bool clamp = true)
    {
        float lerpValue = InverseLerp(fromMin, fromMax, fromValue, clamp);
        return interpol.Evaluate(toMin, toMax, lerpValue, clamp);
    }

    /// <summary>
    /// Maps a value through two ranges with a bump effect (multiplies results) using an interpolation function
    /// </summary>
    public static float EvaluateBump(this (InterpolationFunction first, InterpolationFunction second) curves,
        float fromValue,
        float fromMin1, float fromMax1, float toMin1, float toMax1,
        float fromMin2, float fromMax2, float toMin2, float toMax2,
        bool clampInput = true, bool clampOutput = true)
    {
        float lerp1 = curves.first.Evaluate(fromValue, fromMin1, fromMax1, toMin1, toMax1, clampInput);
        float lerp2 = curves.second.Evaluate(fromValue, fromMin2, fromMax2, toMin2, toMax2, clampInput);
        float result = lerp1 * lerp2;

        if (clampOutput)
        {
            float minResult = Math.Min(toMin1 * toMin2, toMax1 * toMax2);
            float maxResult = Math.Max(toMin1 * toMax2, toMax1 * toMin2);
            result = Clamp(result, minResult, maxResult);
        }

        return result;
    }

    /// <summary>
    /// <inheritdoc cref="EvaluateBump(ValueTuple{InterpolationFunction, InterpolationFunction}, float, float, float, float, float, float, float, float, float, bool)"></inheritdoc>
    /// </summary>
    public static float EvaluateBump(this InterpolationFunction curve, float fromValue,
        float fromMin1, float fromMax1, float toMin1, float toMax1,
        float fromMin2, float fromMax2, float toMin2, float toMax2,
        bool clampInput = true, bool clampOutput = true) =>
        EvaluateBump((curve, curve), fromValue, fromMin1, fromMax1, toMin1, toMax1, fromMin2, fromMax2, toMin2, toMax2,
            clampInput, clampOutput);

    public sealed class PiecewiseCurve
    {
        /// <summary>
        /// A piecewise curve that takes up part of a domain
        /// </summary>
        private readonly struct CurveSegment(float from, float to, float start, float end, InterpolationFunction funct)
        {
            internal readonly float From = from;
            internal readonly float To = to;
            internal readonly float Start = start;
            internal readonly float End = end;
            internal readonly InterpolationFunction Funct = funct;
        }

        private readonly List<CurveSegment> segments = [];

        public PiecewiseCurve Add(float from, float to, float end, InterpolationFunction funct)
        {
            ArgumentNullException.ThrowIfNull(funct, nameof(InterpolationFunction));

            float start = segments.Count != 0 ? segments[^1].End : 0;
            if (segments.Count > 0 && start < segments[^1].End)
                throw new ArgumentException("New segments start must be after the previous segments end");
            segments.Add(new CurveSegment(from, to, start, end, funct));
            return this;
        }

        public PiecewiseCurve AddStall(float value, float end) => Add(value, value, end, _ => value);

        public float Evaluate(float interpolant, bool clamped = true)
        {
            if (segments.Count == 0)
                throw new Exception("At least one segment must be added before evaluating");

            if (clamped)
                interpolant = Clamp01(interpolant);

            CurveSegment use = default;
            for (int i = segments.Count - 1; i >= 0; --i)
            {
                if (interpolant >= segments[i].Start && interpolant <= segments[i].End)
                    use = segments[i];
            }

            float localInterpol = InverseLerp(use.Start, use.End, interpolant);
            return Lerp(use.From, use.To, use.Funct!(localInterpol));
        }
    }

    #endregion Evaluators
}
