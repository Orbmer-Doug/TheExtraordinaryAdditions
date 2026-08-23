using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Terraria;
using static Microsoft.Xna.Framework.MathHelper;
using static System.MathF;

namespace TheExtraordinaryAdditions.Core.Utilities;

public static class QuaternionUtils
{
    public static Quaternion LookAt(Vector3 from, Vector3 to, Vector3 up)
    {
        Vector3 forward = Vector3.Normalize(to - from);
        Matrix rotMatrix = Matrix.CreateWorld(Vector3.Zero, forward, up);
        return Quaternion.CreateFromRotationMatrix(rotMatrix);
    }

    /// <param name="q">The quaternion</param>
    extension(Quaternion q)
    {
        /// <summary>
        /// Vector (imaginary) parts magnitude
        /// </summary>
        public float VectorNorm() => Sqrt(q.X * q.X + q.Y * q.Y + q.Z * q.Z);

        /// <summary>  
        ///     Maps a unit quaternion to a pure quaternion in tangent space
        /// </summary>
        /// <returns>A pure quaternion</returns>
        public Quaternion Log()
        {
            float w = Clamp(q.W, -1f, 1f);
            float theta = Acos(w); // half-angle  
            float sinTheta = Sin(theta);

            if (sinTheta < 1e-6f)
                // Near identity - axis is degenerate, return near-zero pure quaternion  
                return new Quaternion(q.X, q.Y, q.Z, 0);

            float scale = theta / sinTheta;
            return new Quaternion(q.X * scale, q.Y * scale, q.Z * scale, 0);
        }

        /// <summary>  
        ///     Maps a pure quaternion from tangent space back onto the unit sphere
        /// </summary>
        /// <returns>A unit quaternion</returns>
        public Quaternion Exp()
        {
            // q should be pure (w = 0) — we treat its xyz as a 3D vector  
            float theta = q.VectorNorm();
            float sinTheta = Sin(theta);

            if (theta < 1e-6f)
                // Near zero - exp(0) = identity
                return new Quaternion(0f, 0f, 0f, 1f);

            float scale = sinTheta / theta;
            return new Quaternion(q.X * scale, q.Y * scale, q.Z * scale, Cos(theta));
        }

        /// <summary>  
        /// Converts a quaternion into a 4x4 matrix
        /// </summary>
        /// <returns>A 4x4 rotation matrix</returns>
        public Matrix ToMatrix4x4()
        {
            float xx = q.X * q.X, yy = q.Y * q.Y, zz = q.Z * q.Z;
            float xy = q.X * q.Y, xz = q.X * q.Z, yz = q.Y * q.Z;
            float wx = q.W * q.X, wy = q.W * q.Y, wz = q.W * q.Z;

            return new Matrix(
                1 - 2 * (yy + zz), 2 * (xy - wz), 2 * (xz + wy), 0,
                2 * (xy + wz), 1 - 2 * (xx + zz), 2 * (yz - wx), 0,
                2 * (xz - wy), 2 * (yz + wx), 1 - 2 * (xx + yy), 0,
                0, 0, 0, 1
            );
        }

        /// <summary>  
        /// Converts a quaternion into euler angles (pitch, yaw, roll)
        /// </summary>
        /// <returns>A <see cref="Vector3"/> comprised of euler angles </returns>
        public Vector3 ToEulerAngles()
        {
            // Returns (pitch, yaw, roll) in radians  
            float pitch = Atan2(2f * (q.W * q.X + q.Y * q.Z),
                1f - 2f * (q.X * q.X + q.Y * q.Y));

            float sinp = 2f * (q.W * q.Y - q.Z * q.X);
            float yaw = Abs(sinp) >= 1f
                ? CopySign(PI / 2f, sinp) // gimbal lock clamp  
                : Asin(sinp);

            float roll = Atan2(2f * (q.W * q.Z + q.X * q.Y),
                1f - 2f * (q.Y * q.Y + q.Z * q.Z));
            return new Vector3(pitch, yaw, roll);
        }


        /// <summary>  
        ///     Rotates a 3D vector
        /// </summary>
        /// <param name="v">The vector to rotate</param>
        /// <returns>A <see cref="Vector3" /> rotated in the direction of <paramref name="q" /> </returns>
        public Vector3 Rotate(Vector3 v)
        {
            // Embed v as a pure quaternion  
            Quaternion pureV = new(v.X, v.Y, v.Z, 0);

            // Sandwich product
            // Result will always have w = 0  
            Quaternion result = q * pureV * Quaternion.Conjugate(q);

            return new Vector3(result.X, result.Y, result.Z);
        }

        /// <summary>  
        ///     Rotates a 3D vector with Rodrigues' rotation
        /// </summary>
        /// <param name="v">The vector to rotate</param>
        /// <returns>A <see cref="Vector3" /> rotated in the direction of <paramref name="q" /> </returns>
        public Vector3 RotateFast(Vector3 v)
        {
            Vector3 qv = new(q.X, q.Y, q.Z);
            Vector3 t = Vector3.Cross(qv, v) * 2f;
            return v + t * q.W + Vector3.Cross(qv, t);
        }

        // Extract local direction axes from a quaternion
        public Vector3 Right() => new(
            1f - 2f * (q.Y * q.Y + q.Z * q.Z),
            2f * (q.X * q.Y + q.W * q.Z),
            2f * (q.X * q.Z - q.W * q.Y)
        );

        public Vector3 Up() => new(
            2f * (q.X * q.Y - q.W * q.Z),
            1f - 2f * (q.X * q.X + q.Z * q.Z),
            2f * (q.Y * q.Z + q.W * q.X)
        );

        public Vector3 Forward() => new(
            2f * (q.X * q.Z + q.W * q.Y),
            2f * (q.Y * q.Z - q.W * q.X),
            1f - 2f * (q.X * q.X + q.Y * q.Y)
        );

        #region Decomposition

        /// <summary>  
        ///     Extracts the radians around the Z axis (roll)
        /// </summary>        
        public float AngleAroundZ()
        {
            float sinAngle = 2f * (q.X * q.Y + q.W * q.Z);
            float cosAngle = 1f - 2f * (q.Y * q.Y + q.Z * q.Z);
            return Atan2(sinAngle, cosAngle);
        }

        /// <summary>  
        ///     Extracts the radians around the Y axis (yaw)
        /// </summary>        
        public float AngleAroundY()
        {
            float sinAngle = 2f * (q.X * q.Z + q.W * q.Y);
            float cosAngle = 1f - 2f * (q.Y * q.Y + q.Z * q.Z);
            return Atan2(sinAngle, cosAngle);
        }

        /// <summary>  
        ///     Extracts the radians around the X axis (pitch)
        /// </summary>        
        public float AngleAroundX()
        {
            float sinAngle = 2f * (q.Y * q.Z + q.W * q.X);
            float cosAngle = 1f - 2f * (q.X * q.X + q.Z * q.Z);
            return Atan2(sinAngle, cosAngle);
        }

        #endregion
    }

    /// <summary>
    /// Creates a new <see cref="Quaternion"/> in local-space from the specified angles
    /// </summary>
    /// <param name="angle2D">Horizontal sweep angle, measured clockwise from the top of the unit circle (top = 0).</param>
    /// <param name="angleSide">The lateral tilt. Flat at 0 and 2pi, toward the screen at pi/2, flipped but flat at pi, away from screen at 3pi/2.</param>
    /// <param name="clockwise">Counter-clockwise if false.</param>
    /// <remarks>Imagine it like a circle thats completion is represented by <paramref name="angle2D"/> that is rotated orthogonally by <paramref name="angleSide"/></remarks>
    public static Quaternion CreateFromPolarAngles(float angle2D, float angleSide = 0f, bool clockwise = true)
    {
        int dirInt = clockwise.ToDirectionInt();
        // Adding pi makes it mirror correctly
        float forwardRotationOffset = angle2D * dirInt + (clockwise ? 0f : Pi);

        Quaternion aroundZ = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, forwardRotationOffset);
        Quaternion aroundX = Quaternion.CreateFromAxisAngle(Vector3.UnitX, angleSide * dirInt);

        return Quaternion.Concatenate(aroundZ, aroundX);
    }
}

public static class QuaternionAnimators
{
    /// <summary>  
    /// Smoothly shifts a quaternion towards a target quaternion
    /// </summary>
    /// <param name="from">The quaternion to shift</param>
    /// <param name="to">The target quaternion</param>
    /// <param name="maxRadiansDelta">How many radians per frame to tilt <paramref name="from"/> towards <paramref name="to"/></param>
    /// <returns>The shifted quaternion</returns>    
    public static Quaternion RotateTowards(Quaternion from, Quaternion to, float maxRadiansDelta)
    {
        float angle = Acos(Clamp(Abs(Quaternion.Dot(from, to)), 0f, 1f)) * 2f;
        if (angle < 1e-6f)
            return to;

        float t = MathF.Min(1f, maxRadiansDelta / angle);
        return from.Slerp(to, t);
    }

    public static Quaternion IntegrateAngularVelocity(Quaternion q, Vector3 omega, float dt)
    {
        // omega is axis * angularSpeed in radians/sec  
        float angle = omega.Length() * dt;
        if (angle < 1e-6f)
            return q;

        Vector3 axis = Vector3.Normalize(omega);
        return Quaternion.Normalize(q * Quaternion.CreateFromAxisAngle(axis, angle));
    }

    /// <summary>  
    /// Compute the intermediate control point fo keyframe q_i, given its neighbors q_prev and q_next
    /// </summary>
    /// <returns>A control point for use in a spline segment</returns>
    /// <remarks>Call this once per keyframe when setting up the spline</remarks>    
    public static Quaternion ComputeControlPoint(
        Quaternion qPrev,
        Quaternion qCurr,
        Quaternion qNext,
        float dtPrev,
        float dtNext)
    {
        Quaternion qCurrInv = Quaternion.Conjugate(qCurr);

        Quaternion logToNext = (qCurrInv * qNext).Log();
        Quaternion logToPrev = (qCurrInv * qPrev).Log();

        // Weight each log term by the opposite interval
        float total = dtPrev + dtNext;
        float wNext = dtPrev / total; // weight for logToNext  
        float wPrev = dtNext / total; // weight for logToPrev  

        Quaternion avgLog = new(
            -(logToNext.X * wNext + logToPrev.X * wPrev) / 2f,
            -(logToNext.Y * wNext + logToPrev.Y * wPrev) / 2f,
            -(logToNext.Z * wNext + logToPrev.Z * wPrev) / 2f,
            0
        );

        return Quaternion.Normalize(qCurr * avgLog.Exp());
    }

    /// <summary>  
    /// Evaluates a spherical quadrangle
    /// </summary>
    /// <param name="seg">The singular spline segment to evaluate</param>
    /// <param name="t">A value between [0, 1] that blends between 2 quaternions</param>
    /// <returns></returns>    
    public static Quaternion EvaluateSquad(SplineSegment seg, float t)
    {
        Quaternion slerpKeyframes = seg.q1.Slerp(seg.q2, t);
        Quaternion slerpControlPoints = seg.s1.Slerp(seg.s2, t);
        float blend = 2f * t * (1f - t);
        return slerpKeyframes.Slerp(slerpControlPoints, blend);
    }

    /// <summary>  
    /// Precompute all segments from a list of keyframes
    /// </summary>
    /// <param name="keyframes">The specific quaternion keyframes</param>
    /// <returns>An array of spline segments</returns>
    /// <exception cref="ArgumentException">Need at least 2 keyframes</exception>
    /// <remarks>Call this once when you set up the animation, not every frame</remarks>    
    public static SplineSegment[] BuildSpline(KeyFrame[] keyframes)
    {
        int n = keyframes.Length;
        if (n < 2)
            throw new ArgumentException("Need at least 2 keyframes");

        // Validate timestamps are strictly ascending  
        for (int i = 1; i < n; i++)
            if (keyframes[i].Time <= keyframes[i - 1].Time)
                throw new ArgumentException(
                    $"Keyframe times must be strictly ascending. " +
                    $"Keyframe {i} (t={keyframes[i].Time}) <= keyframe {i - 1} (t={keyframes[i - 1].Time})");

        SplineSegment[] segments = new SplineSegment[n - 1];
        Quaternion[] controls = new Quaternion[n];

        // Interior control points — weighted by adjacent interval lengths  
        for (int i = 1; i < n - 1; i++)
        {
            float dtPrev = keyframes[i].Time - keyframes[i - 1].Time;
            float dtNext = keyframes[i + 1].Time - keyframes[i].Time;

            controls[i] = ComputeControlPoint(
                keyframes[i - 1].Rotation,
                keyframes[i].Rotation,
                keyframes[i + 1].Rotation,
                dtPrev,
                dtNext
            );
        }

        // Endpoints have no neighbor on one side — no curvature correction  
        controls[0] = keyframes[0].Rotation;
        controls[n - 1] = keyframes[n - 1].Rotation;

        for (int i = 0; i < n - 1; i++)
            segments[i] = new SplineSegment(
                keyframes[i].Rotation, keyframes[i + 1].Rotation,
                controls[i], controls[i + 1],
                keyframes[i].Time, keyframes[i + 1].Time
            );

        return segments;
    }

    /// <summary>  
    /// Evaluates multiple spline segments
    /// </summary>    
    public static Quaternion EvaluateSpline(SplineSegment[] segments, float time)
    {
        if (segments.Length == 0)
            return Quaternion.Identity;

        // Clamp to the spline's full time range  
        float startTime = segments[0].TimeStart;
        float endTime = segments[^1].TimeEnd;
        time = Clamp(time, startTime, endTime);

        // Binary search for the correct segment  
        int lo = 0, hi = segments.Length - 1;
        while (lo < hi)
        {
            int mid = (lo + hi) / 2;
            if (segments[mid].TimeEnd < time)
                lo = mid + 1;
            else
                hi = mid;
        }

        SplineSegment seg = segments[lo];

        // Map absolute time to [0, 1] within this segment  
        float localT = InverseLerp(seg.TimeStart, seg.TimeEnd, time);

        return EvaluateSquad(seg, localT);
    }

    /// <param name="quaternion1">The first quaternion</param>
    extension(Quaternion quaternion1)
    {
        /// <summary>Interpolates between two quaternions, using spherical linear interpolation</summary>
        /// <param name="quaternion2">The second quaternion</param>
        /// <param name="t">The relative weight of the second quaternion in the interpolation</param>
        /// <returns>The interpolated quaternion</returns>        
        public Quaternion Slerp(Quaternion quaternion2, float t)
        {
            float dot = Quaternion.Dot(quaternion1, quaternion2);

            // If dot < 0, q2 is on the far side of the sphere from q1  
            // Negate it to ensure we take the short arc
            if (dot < 0f)
            {
                quaternion2 = -quaternion2;
                dot = -dot;
            }

            // Clamp to prevent acos going NaN from floating point drift  
            dot = Clamp(dot, -1f, 1f);

            // For very small angles, fall back to NLERP (normalized LERP)  
            // The two converge at small angles and NLERP avoids divide-by-near-zero
            if (dot > 0.9995f)
                return quaternion1 + (quaternion2 + -quaternion1) * t;

            float omega = Acos(dot); // angle between q1 and q2  
            float sinOmega = Sin(omega);

            float w1 = Sin((1f - t) * omega) / sinOmega;
            float w2 = Sin(t * omega) / sinOmega;

            return Quaternion.Normalize(quaternion1 * w1 + quaternion2 * w2);
        }

        /// <summary>
        /// Interpolates between two quaternions, using spherical linear interpolation.
        /// </summary>
        /// <param name="quaternion2">The second quaternion</param>
        /// <param name="t">The relative weight of the second quaternion in the interpolation</param>
        /// <returns>The interpolated quaternion</returns>        
        public Quaternion SlerpLong(Quaternion quaternion2, float t)
        {
            float dot = Quaternion.Dot(quaternion1, quaternion2);

            // If dot > 0, we are one the short arc  
            // Negate it to ensure we take the long arc
            if (dot > 0f)
            {
                quaternion2 = -quaternion2;
                dot = -dot;
            }

            // Clamp to prevent acos going NaN from floating point drift  
            dot = Clamp(dot, -1f, 1f);

            // For very small angles, fall back to NLERP (normalized LERP)  
            // The two converge at small angles and NLERP avoids divide-by-near-zero
            if (dot < -0.9995f)
                return quaternion1 + (quaternion2 + -quaternion1) * t;

            float omega = Acos(dot); // angle between q1 and q2  
            float sinOmega = Sin(omega);

            float w1 = Sin((1f - t) * omega) / sinOmega;
            float w2 = Sin(t * omega) / sinOmega;

            return Quaternion.Normalize(quaternion1 * w1 + quaternion2 * w2);
        }

        /// <summary>  
        ///     Performs a linear interpolation between two quaternions based on a value that specifies the weighting of the second quaternion
        /// </summary>
        /// <param name="quaternion2">The second quaternion</param>
        /// <param name="amount">The relative weight of <paramref name="quaternion2" /> in the interpolation</param>
        /// <returns>The interpolated quaternion</returns>        
        public Quaternion Lerp(Quaternion quaternion2, float amount)
        {
            Vector128<float> left = quaternion2.AsVector128();
            Vector128<float> vector128 = Vector128.ConditionalSelect(
                Vector128.GreaterThanOrEqual(Vector128.Create(Quaternion.Dot(quaternion1, quaternion2)),
                    Vector128<float>.Zero), left, -left);

            Vector128<float> q1 = quaternion1.AsVector128();
            Vector128<float> oneMinus = Vector128.Create(1f - amount);
            Vector128<float> scaled2 = vector128 * amount;

            return Quaternion.Normalize((q1 * oneMinus + scaled2).AsQuaternion());
        }
    }
}

public readonly struct KeyFrame(Quaternion rotation, float time)
{
    public readonly Quaternion Rotation = rotation;
    public readonly float Time = time;
}

[StructLayout(LayoutKind.Auto)]
public readonly struct SplineSegment(
    Quaternion q1,
    Quaternion q2,
    Quaternion s1,
    Quaternion s2,
    float timeStart,
    float timeEnd)
{
    // keyframes  
    public readonly Quaternion q1 = q1;
    public readonly Quaternion q2 = q2;

    // control points  
    public readonly Quaternion s1 = s1;
    public readonly Quaternion s2 = s2;

    public readonly float TimeStart = timeStart;
    public readonly float TimeEnd = timeEnd;
}

public sealed class PiecewiseRotation
{
    /// <summary>  
    ///     The list of <see cref="CurveSegment" /> that encompass the entire 0-1 domain of this function
    /// </summary>
    private readonly List<CurveSegment> _segments = [];

    /// <summary>  
    ///     Adds a curve segment to this piecewise rotations collection of segments
    /// </summary>        /// <returns><see langword="this"/></returns>
    /// <exception cref="InvalidOperationException"><paramref name="animationEnd"/> must be between [0, 1] and must be greater than the start</exception>        
    public PiecewiseRotation Add(InterpolationFunction interpolant, Quaternion endingRotation,
        float animationEnd,
        Quaternion? startingRotation = null, bool optimalRoute = true)
    {
        float animationStart = _segments.Count != 0 ? _segments.Last().AnimationEnd : 0f;
        startingRotation ??= _segments.Count != 0 ? _segments.Last().EndingRotation : Quaternion.Identity;
        if (animationEnd is < 0f or > 1f)
            throw new InvalidOperationException(
                "A piecewise animation curve segment cannot have a domain outside of 0 - 1!");
        if (animationEnd <= animationStart)
            throw new InvalidOperationException(
                "A piecewise animation curve segments end must be greater than its start!");

        // Add the new segment  
        _segments.Add(new(startingRotation.Value, endingRotation, animationStart, animationEnd, interpolant,
            optimalRoute));

        // Return the piecewise curve that called this method to allow method chaining  
        return this;
    }

    public Quaternion Evaluate(float interpolant)
    {
        // Clamp the interpolant into the valid range  
        interpolant = Clamp(interpolant, 0f, 1f);

        // Calculate the local interpolant relative to the segment that the base interpolant fits into  
        CurveSegment segmentToUse = _segments.FindLast(s => interpolant >= s.AnimationStart);
        if (segmentToUse == default)
            throw new NullReferenceException("Couldn't find a valid curve segment!");
        float curveLocalInterpolant =
            InverseLerp(segmentToUse.AnimationStart, segmentToUse.AnimationEnd, interpolant);

        // Calculate the segment value based on the local interpolant  
        float segmentInterpolant = segmentToUse.Interpolant.Evaluate(0f, 1f, curveLocalInterpolant);

        // Spherically interpolate piecemeal between the quaternions
        Quaternion start = Quaternion.Normalize(segmentToUse.StartingRotation);
        Quaternion end = Quaternion.Normalize(segmentToUse.EndingRotation);
        return segmentToUse.OptimalRoute
            ? start.Slerp(end, segmentInterpolant)
            : start.SlerpLong(end, segmentInterpolant);
    }

    private readonly record struct CurveSegment(
        Quaternion StartingRotation,
        Quaternion EndingRotation,
        float AnimationStart,
        float AnimationEnd,
        InterpolationFunction Interpolant,
        bool OptimalRoute);
}

public static class VectorExtensions
{
    public static Vector128<float> AsVector128(this Quaternion value)
    {
        return Unsafe.BitCast<Quaternion, Vector128<float>>(value);
    }

    public static Quaternion AsQuaternion(this Vector128<float> value)
    {
        return Unsafe.BitCast<Vector128<float>, Quaternion>(value);
    }

    public static Quaternion AsQuaternion(this Vector4 value)
    {
        return Unsafe.BitCast<Vector4, Quaternion>(value);
    }
}
