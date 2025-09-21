using System;
using System.Collections.Generic;
using System.Linq;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;
using static System.MathF;

namespace TheExtraordinaryAdditions.Core.Graphics;

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
        int segmentIndex = Math.Min((int)scaledT, segmentCount - 1);
        float localT = scaledT - segmentIndex;

        Vector2 p0 = segmentIndex == 0 ? points[0] : points[segmentIndex - 1]; // First point (or previous)
        Vector2 p1 = points[segmentIndex]; // Current point
        Vector2 p2 = points[segmentIndex + 1]; // Next point
        Vector2 p3 = segmentIndex == segmentCount - 1 ? points[segmentIndex + 1] : points[segmentIndex + 2]; // Next (or next-next)

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
                float t = (float)j / segments;
                splinePoints.Add(Vector2.CatmullRom(p0, p1, p2, p3, t));
            }
        }

        return splinePoints;
    }

    public delegate float InterpolationFunction(float interpolant);
    public record Curve(InterpolationFunction InFunction, InterpolationFunction OutFunction, InterpolationFunction InOutFunction);

    public static InterpolationFunction Bump(float from1, float to1, float from2, float to2) => new(interpolant => GetLerpBump(from1, to1, from2, to2, interpolant));
    public static InterpolationFunction BezierEase => new(interpolant => interpolant * interpolant / (2f * (interpolant * interpolant - interpolant) + 1f));
    public static InterpolationFunction SwoopEase => new(interpolant => 3.75f * (interpolant * interpolant * interpolant) - 8.5f * (interpolant * interpolant) + 5.75f * interpolant);
    public static InterpolationFunction InterpHermite(int amt = 3) => new(interpolant => interpolant * interpolant * (amt - (amt - 1) * interpolant));

    /// <param name="p0">Start</param>
    /// <param name="p1">Peak</param>
    /// <param name="m0">Tangent at <paramref name="p0"/></param>
    /// <param name="m1">Tangent at <paramref name="p1"/></param>
    public static InterpolationFunction Hermite(float p0, float p1, float m0, float m1) => new(interpolant =>
    (2f * interpolant.Cubed() - 3f * interpolant.Squared() + 1f) * p0 +
    (interpolant.Cubed() - 2f * interpolant.Squared() + interpolant) * m0 +
    (-2f * interpolant.Cubed() + 3f * interpolant.Squared()) * p1 +
    (interpolant.Cubed() - interpolant.Squared()) * m1);

    /// <summary>
    /// May help to use https://cubic-bezier.com/
    /// </summary>
    public static InterpolationFunction CubicBezier(float x1, float y1, float x2, float y2)
    {
        return new(t =>
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
        });
    }

    public static InterpolationFunction SineBump => new(Convert01To010);

    public static readonly Curve Sine = new(interpolant => 1f - Cos(interpolant * Pi / 2f),
        interpolant => Sin(interpolant * Pi / 2f),
        interpolant => -(Cos(interpolant * Pi) - 1f) / 2f);

    public static Curve MakePoly(float exponent)
    {
        return new(interpolant =>
        {
            return Pow(interpolant, exponent);
        }, interpolant =>
        {
            return 1f - Pow(1f - interpolant, exponent);
        }, interpolant =>
        {
            if (interpolant < 0.5f)
                return Pow(2f, exponent - 1f) * Pow(interpolant, exponent);
            return 1f - Pow(interpolant * -2f + 2f, exponent) * 0.5f;
        });
    }

    public static Curve Exp(float exponent = 2f)
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
        interpolant => interpolant < 0.5 ? 1f - Sqrt(1f - (2f * interpolant).Squared()) / 2f : (Sqrt(1f - (-2f * interpolant - 2f).Squared()) + 1f) / 2f);

    public static readonly Curve Back = new(interpolant => c3 * interpolant.Cubed() - c1 * interpolant.Squared(),
        interpolant => 1 + c3 * (interpolant - 1).Cubed() + c1 * (interpolant - 1).Squared(),
        interpolant => interpolant < 0.5 ? (2 * interpolant).Squared() * ((c2 + 1) * 2 * interpolant - c2) / 2
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
    public static float Evaluate(this InterpolationFunction interpol, float start, float end, float interpolant, bool clamp = true)
    {
        if (clamp)
            interpolant = Clamp(interpolant, 0f, 1f);
        return Lerp(start, end, interpol(interpolant));
    }

    /// <summary>
    /// Maps a value from one range to another using an interpolation function
    /// </summary>
    public static float Evaluate(this InterpolationFunction interpol, float fromValue, float fromMin, float fromMax, float toMin, float toMax, bool clamp = true)
    {
        float lerpValue = InverseLerp(fromMin, fromMax, fromValue, clamp);
        return interpol.Evaluate(toMin, toMax, lerpValue, clamp);
    }

    /// <summary>
    /// Maps a value through two ranges with a bump effect (multiplies results) using an interpolation function
    /// </summary>
    public static float EvaluateBump(this (InterpolationFunction first, InterpolationFunction second) curves, float fromValue,
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
        EvaluateBump((curve, curve), fromValue, fromMin1, fromMax1, toMin1, toMax1, fromMin2, fromMax2, toMin2, toMax2, clampInput, clampOutput);

    public class PiecewiseCurve
    {
        /// <summary>
        /// A piecewise curve that takes up part of a domain
        /// </summary>
        public readonly record struct CurveSegment(float From, float To, float Start, float End, InterpolationFunction Interpol);

        /// <summary>
        /// The list of <see cref="CurveSegment"/> that encompasses the whole 0-1 domain
        /// </summary>
        protected List<CurveSegment> segments = [];

        public PiecewiseCurve Add(float from, float to, float end, InterpolationFunction interpolant)
        {
            float start = segments.Count != 0 ? segments.Last().End : 0f;

            if (segments.Count > 0 && start < segments.Last().End)
                throw new ArgumentException("New segment's start must be after the previous segment's end.");

            segments.Add(new(from, to, start, end, interpolant));
            return this;
        }

        /// <summary>
        /// Adds a stall segment that holds a constant value for the specified duration
        /// </summary>
        /// <param name="value">The constant value to hold</param>
        /// <param name="end">Duration of the stall in frames</param>
        public PiecewiseCurve AddStall(float value, float end) => Add(value, value, end, _ => value);

        public float Evaluate(float interpolant)
        {
            interpolant = Clamp(interpolant, 0f, 1f);

            CurveSegment segmentToUse = segments.Find(s => interpolant.BetweenNum(s.Start, s.End, true));
            if (segmentToUse == default)
                throw new NullReferenceException("Couldn't find a valid curve segment!");

            float curveLocalInterpolant = InverseLerp(segmentToUse.Start, segmentToUse.End, interpolant);

            return segmentToUse.Interpol.Evaluate(segmentToUse.From, segmentToUse.To, curveLocalInterpolant);
        }
    }

    #endregion Evaluators

    #region Quaternion
    public class PiecewiseRotation
    {
        /// <summary>
        /// A piecewise rotation curve that takes up a part of the domain of a <see cref="Interpolant"/>, specifying the equivalent range and curvature in said domain
        /// </summary>
        protected readonly struct CurveSegment(Quaternion startingRotation, Quaternion endingRotation, float animationStart, float animationEnd, InterpolationFunction interpolant)
        {
            /// <summary>
            /// The starting output rotation value. This is what is outputted when the <see cref="Interpolant"/> is evaluated at <see cref="AnimationStart"/>
            /// </summary>
            internal readonly Quaternion StartingRotation = startingRotation;

            /// <summary>
            /// The ending output rotation value. This is what is outputted when the <see cref="Interpolant"/> is evaluated at <see cref="AnimationEnd"/>
            /// </summary>
            internal readonly Quaternion EndingRotation = endingRotation;

            /// <summary>
            /// The start of this curve segment's domain relative to the <see cref="Interpolant"/>
            /// </summary>
            internal readonly float AnimationStart = animationStart;

            /// <summary>
            /// The ending of this curve segment's domain relative to the <see cref="Interpolant"/>
            /// </summary>
            internal readonly float AnimationEnd = animationEnd;

            /// <summary>
            /// The easing curve that dictates the how the outputs vary between <see cref="StartingRotation"/> and <see cref="EndingRotation"/>
            /// </summary>
            internal readonly InterpolationFunction Interpolant = interpolant;
        }

        /// <summary>
        /// The list of <see cref="CurveSegment"/> that encompass the entire 0-1 domain of this function
        /// </summary>
        protected List<CurveSegment> segments = [];

        public PiecewiseRotation Add(InterpolationFunction interpolant, Quaternion endingRotation, float animationEnd, Quaternion? startingRotation = null)
        {
            float animationStart = segments.Count != 0 ? segments.Last().AnimationEnd : 0f;
            startingRotation ??= segments.Count != 0 ? segments.Last().EndingRotation : Quaternion.Identity;
            if (animationEnd <= 0f || animationEnd > 1f)
                throw new InvalidOperationException("A piecewise animation curve segment cannot have a domain outside of 0-1.");

            // Add the new segment
            segments.Add(new(startingRotation.Value, endingRotation, animationStart, animationEnd, interpolant));

            // Return the piecewise curve that called this method to allow method chaining
            return this;
        }

        public Quaternion Evaluate(float interpolant, bool takeOptimalRoute, int inversionDirection)
        {
            // Clamp the interpolant into the valid range
            interpolant = Clamp(interpolant, 0f, 1f);

            // Calculate the local interpolant relative to the segment that the base interpolant fits into
            CurveSegment segmentToUse = segments.Find(s => interpolant >= s.AnimationStart && interpolant <= s.AnimationEnd);
            float curveLocalInterpolant = InverseLerp(segmentToUse.AnimationStart, segmentToUse.AnimationEnd, interpolant);

            // Calculate the segment value based on the local interpolant
            float segmentInterpolant = segmentToUse.Interpolant.Evaluate(0f, 1f, curveLocalInterpolant);

            // Spherically interpolate piecemeal between the quaternions
            // Unlike a single Quaternion.Lerp, which would typically invert negative dot products, this has the ability to take un-optimal routes to the destination angle, which is desirable for things such as big swings
            Quaternion start = segmentToUse.StartingRotation;
            Quaternion end = segmentToUse.EndingRotation;

            start.Normalize();
            end.Normalize();
            float similarity = Quaternion.Dot(start, end);
            if (similarity.NonZeroSign() != inversionDirection && takeOptimalRoute)
            {
                similarity *= -1f;
                start *= -1f;
            }

            const float threshhold = 0.99999f;
            float angle = Acos(Clamp(similarity, -threshhold, threshhold));
            float cosecantAngle = 1f / Sin(angle);
            return (start * Sin((1f - segmentInterpolant) * angle) + end * Sin(segmentInterpolant * angle)) * cosecantAngle;
        }
    }

    public static float ZRotation(Quaternion Rotation) => Atan2((Rotation.W * Rotation.Z + Rotation.X * Rotation.Y) * 2f, 1f - (Rotation.Y.Squared() + Rotation.Z.Squared()) * 2f);

    /// <summary>
    /// Creates a new <see cref="Quaternion"/> from the specified angles
    /// </summary>
    /// <param name="horizontalDir">A signed direction of rotation. Makes rotation go counter-clockwise if at -1.</param>
    /// <param name="angle2D">A basic 2D angle like any other</param>
    /// <param name="angleSide">Effects the X and W of this quaternion. Normal is at 0, π (flipped), and 2π. Aligns with the X axis at π/2 and 3π/2. Flips other way after π/2 and back to normal after 3π/2</param>
    /// <returns>A quaternion composed of euler angles</returns>
    /// <remarks>Imagine it like a circle thats completion is represented by <paramref name="angle2D"/> that is rotated by <paramref name="angleSide"/> thats clockwise rotation is effected by <paramref name="horizontalDir"/></remarks>
    public static Quaternion EulerAnglesConversion(float horizontalDir, float angle2D, float angleSide = 0f)
    {
        float forwardRotationOffset = angle2D * horizontalDir + (horizontalDir == -1f ? PiOver2 : 0f);
        return Quaternion.CreateFromRotationMatrix(Matrix.CreateRotationZ(WrapAngle360(forwardRotationOffset)) * Matrix.CreateRotationX(angleSide));
    }

    public static void GetPrincipalAxes(in Quaternion quaternion, out float roll, out float pitch, out float yaw)
    {
        Matrix rotationMatrix = Matrix.CreateFromQuaternion(quaternion);
        yaw = (float)Math.Atan2(rotationMatrix.M21, rotationMatrix.M11);

        float m33 = rotationMatrix.M33;

        // Clamp M33 to avoid division by near-zero, preventing gimbal lock issues
        if (Math.Abs(m33) < 0.0001f)
            m33 = m33 >= 0 ? 0.0001f : -0.0001f;

        pitch = (float)Math.Atan2(-rotationMatrix.M23, m33);
        roll = (float)Math.Atan2(rotationMatrix.M13, m33);
    }

    // close enough, size is cut by √2/2 (~30% reduction) among j and k axis but im unsure how to formalize that
    public static RotatedRectangle GetProjectedHitbox(Vector2 center, Quaternion quaternion, Vector2 originalSize, float start = 0f)
    {
        // Create rotation matrix from quaternion
        Matrix rotationMatrix = Matrix.CreateFromQuaternion(quaternion);

        GetPrincipalAxes(quaternion, out float roll, out float pitch, out float yaw);
        yaw += start;

        float projectedWidth = originalSize.X * Abs(Cos(roll));
        float projectedHeight = originalSize.Y * Abs(Cos(pitch));
        projectedWidth = Math.Max(projectedWidth, 0.01f);
        projectedHeight = Math.Max(projectedHeight, 0.01f);

        bool isFlipped = rotationMatrix.M33 < 0;
        if (isFlipped)
            yaw = -yaw;

        // Create hitbox centered on the sprite's position
        Vector2 size = new(projectedWidth, projectedHeight);
        Vector2 topLeft = center - size / 2;
        return new RotatedRectangle(topLeft, size, yaw);
    }

    public static float Get2DRotationFromQuaternion(Quaternion quaternion, float startingRot = 0f)
    {
        // Convert the quaternion to a rotation matrix
        Matrix rotationMatrix = Matrix.CreateFromQuaternion(quaternion);

        // Extract the 2D rotation angle from the matrix
        // M11 and M21 represent the cosine and sine of the Z-rotation
        float angle = (float)Math.Atan2(rotationMatrix.M21, rotationMatrix.M11);
        return angle + startingRot;
    }

    /// <summary>
    /// Determines the inverse of a given quaternion.
    /// </summary>
    /// <param name="rotation">The quaternion to calculate the inverse of.</param>
    public static Quaternion Inverse(this Quaternion rotation)
    {
        float x = rotation.X;
        float y = rotation.Y;
        float z = rotation.Z;
        float w = rotation.W;
        float inversionFactor = 1f / (x.Squared() + y.Squared() + z.Squared() + w.Squared());
        return new Quaternion(x, -y, -z, -w) * inversionFactor;
    }

    /// <summary>
    /// Rotates a given vector by a given quaternion rotation.
    /// </summary>
    /// <param name="vector">The vector to rotate.</param>
    /// <param name="rotation">The quaternion to rotate by.</param>
    public static Vector3 RotatedBy(this Vector3 vector, Quaternion rotation)
    {
        return Vector3.Transform(Vector3.Transform(vector, rotation), rotation.Inverse());
    }

    public static Quaternion Normalized(this Quaternion quaternion)
    {
        quaternion.Normalize();
        return quaternion;
    }

    public static Quaternion SlerpUnoptimal(Quaternion q1, Quaternion q2, float t)
    {
        float dot = Quaternion.Dot(q1, q2);

        // Negate one quaternion if the dot product is negative
        if (dot < 0f)
        {
            q2 = -q2; // Negate q2 to take the longer path
            dot = -dot; // Update the dot product
        }

        const float THRESHOLD = 0.9995f;

        if (dot > THRESHOLD)
        {
            // Use linear interpolation for very close quaternions
            Quaternion result = Quaternion.Lerp(q1, q2, t);
            return Quaternion.Normalize(result);
        }

        // Calculate the angle and the coefficients
        float theta_0 = (float)Math.Acos(dot); // angle between input quaternions
        float theta = theta_0 * t;
        Quaternion q2_ = q2 - q1 * dot;
        q2_ = Quaternion.Normalize(q2_);

        return q1 * (float)Math.Cos(theta) + q2_ * (float)Math.Sin(theta);
    }

    #endregion Quaternion
}