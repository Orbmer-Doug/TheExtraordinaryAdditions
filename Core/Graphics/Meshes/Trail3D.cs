using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics.Resources;

namespace TheExtraordinaryAdditions.Core.Graphics.Meshes;

public readonly struct TrailSample3D(Vector3 position, Vector3 normal, float opacity)
{
    public readonly Vector3 Position = position;
    public readonly Vector3 Normal = normal;
    public readonly float Opacity = opacity;
}

/// <summary>
/// Manages a fixed-size shift buffer of <see cref="TrailSample3D"/> values. <br />
/// The newest sample is always inserted at index 0; older samples shift right.
/// </summary>
public sealed class TrailPoints3D
{
    private TrailSample3D[] _buffer;

    public int Count { get; private set; }

    public ReadOnlySpan<TrailSample3D> Points => _buffer.AsSpan(0, Count);

    /// <summary>
    /// The alpha of the most recently pushed sample, or 1 if the buffer is empty
    /// </summary>
    public float LeadingOpacity => Count > 0 ? _buffer[0].Opacity : 1f;

    public TrailPoints3D(int max)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(max);
        _buffer = new TrailSample3D[max];
        Count = max;
    }

    public void Update(TrailSample3D newSample)
    {
        // If the buffer has never been filled, seed every slot with the opening sample
        bool uninitialised = Count <= 0;
        if (!uninitialised)
        {
            // Treat a buffer full of default (zero) positions as uninitialised too
            uninitialised = true;
            for (int i = 0; i < Count; i++)
            {
                if (_buffer[i].Position == Vector3.Zero)
                    continue;

                uninitialised = false;
                break;
            }
        }

        if (uninitialised)
        {
            Count = _buffer.Length;
            _buffer.AsSpan(0, Count).Fill(newSample);
            return;
        }

        int shiftCount = Math.Min(Count, _buffer.Length - 1);
        if (shiftCount > 0)
            _buffer.AsSpan(0, shiftCount).CopyTo(_buffer.AsSpan(1));

        _buffer[0] = newSample;
        Count = Math.Min(Count + 1, _buffer.Length);
    }

    public void SetSample(int index, TrailSample3D value)
    {
        if ((uint) index >= (uint) _buffer.Length)
            throw new IndexOutOfRangeException("Index is out of range for the trail points.");
        _buffer[index] = value;
    }

    public void SetSamples(List<TrailSample3D> newSamples)
    {
        if (newSamples.Count > _buffer.Length)
            _buffer = new TrailSample3D[newSamples.Count];

        newSamples.CopyTo(0, _buffer, 0, newSamples.Count);
        Count = newSamples.Count;
    }

    public void SetSamples(ReadOnlySpan<TrailSample3D> newSamples)
    {
        if (newSamples.Length > _buffer.Length)
            _buffer = new TrailSample3D[newSamples.Length];

        newSamples.CopyTo(_buffer);
        Count = newSamples.Length;
    }

    public void Clear()
    {
        if (_buffer.Length == 0)
            return;

        Count = 0;
        Array.Clear(_buffer);
    }
}

/// <summary>
/// I suspect there to be better options but this is the only reliable thing I can think of to
/// ensure a trail sometime somewhere is removed to prevent memory leak when whatever object that needed it is gone
/// </summary>
public sealed class TrailCleaner3D : ModSystem
{
    public static TrailCleaner3D Instance => ModContent.GetInstance<TrailCleaner3D>();
    public List<Trail3D> Trails = [];
    private int cleanCounter;

    public override void PostUpdateEverything()
    {
        if (Trails.Count == 0)
            return;

        if (cleanCounter++ < 10)
            return;
        cleanCounter = 0;

        int writeIndex = 0;
        for (int readIndex = 0; readIndex < Trails.Count; readIndex++)
        {
            Trail3D trail = Trails[readIndex];
            if (trail == null || trail.Disposed)
                continue;

            trail.FailedTicks--;
            if (trail.FailedTicks <= 0)
            {
                trail.Dispose();
                continue;
            }

            Trails[writeIndex] = trail;
            writeIndex++;
        }

        if (writeIndex < Trails.Count)
            Trails.RemoveRange(writeIndex, Trails.Count - writeIndex);
    }
}

/// <summary>
/// Renders a 3D ribbon trail
/// </summary>
public sealed class Trail3D : IDisposable
{
    #region Public Delegates

    /// <inheritdoc cref="Trail.VertexWidthFunction"/>
    public delegate float VertexWidthFunction(float completionRatio);

    /// <summary>
    /// Returns the vertex color given UV coordinates and the world-space position being sampled
    /// </summary>
    /// <param name="texCoord">0-1 vector; X is progress along the trail, Y is side (0 = one edge, 1 = other).</param>
    public delegate Color VertexColorFunction(SystemVector2 texCoord);

    /// <summary>
    /// Returns a world-space offset applied to every vertex at the given completion ratio
    /// </summary>
    public delegate SystemVector3 VertexOffsetFunction(float completionRatio);

    #endregion

    #region Private Fields

    private TrailSample3D[] _sampleBuffer;
    private Vertex3D[] _verticesBuffer;
    private short[] _indicesBuffer;
    private readonly int _maxTrailPoints;

    #endregion

    #region Internal Fields

    internal readonly VertexWidthFunction widthFunction;
    internal readonly VertexColorFunction colorFunction;
    internal readonly VertexOffsetFunction offsetFunction;

    #endregion

    #region Public Fields

    public bool Disposed;
    public int FailedTicks;

    #endregion

    #region Constructor

    /// <summary>
    /// Initialises a new <see cref="Trail3D"/> and registers it
    /// </summary>
    /// <param name="widthFunction">Determines ribbon half-width at each completion ratio</param>
    /// <param name="colorFunction">Determines vertex color from UV</param>
    /// <param name="offsetFunction">Optional per-vertex world-space offset</param>
    /// <param name="maxTrailPoints">Maximum number of interpolated ribbon cross-sections</param>
    public Trail3D(VertexWidthFunction widthFunction, VertexColorFunction colorFunction,
        VertexOffsetFunction offsetFunction = null, int maxTrailPoints = 1024)
    {
        if (!Main.dedServ)
        {
            this.widthFunction = widthFunction ?? throw new ArgumentNullException(nameof(widthFunction));
            this.colorFunction = colorFunction ?? throw new ArgumentNullException(nameof(colorFunction));
            this.offsetFunction = offsetFunction;

            _maxTrailPoints = maxTrailPoints;
            _sampleBuffer = new TrailSample3D[maxTrailPoints];
            _verticesBuffer = new Vertex3D[maxTrailPoints * 2];
            _indicesBuffer = new short[(maxTrailPoints - 1) * 6];

            PrecomputeIndices(maxTrailPoints);
        }

        TrailCleaner3D.Instance.Trails.Add(this);
        FailedTicks = 10;
    }

    #endregion

    #region Private Methods

    private void PrecomputeIndices(int pointCount)
    {
        Array.Clear(_indicesBuffer, 0, _indicesBuffer.Length);
        for (int i = 0; i < pointCount - 1; i++)
        {
            int start = i * 6;
            int connect = i * 2;
            _indicesBuffer[start] = (short) connect;
            _indicesBuffer[start + 1] = (short) (connect + 1);
            _indicesBuffer[start + 2] = (short) (connect + 2);
            _indicesBuffer[start + 3] = (short) (connect + 2);
            _indicesBuffer[start + 4] = (short) (connect + 1);
            _indicesBuffer[start + 5] = (short) (connect + 3);
        }
    }

    private void EnsureBuffers(int requiredPoints)
    {
        if (requiredPoints > _sampleBuffer.Length)
        {
            _sampleBuffer = new TrailSample3D[requiredPoints];
            _verticesBuffer = new Vertex3D[requiredPoints * 2];
            _indicesBuffer = new short[(requiredPoints - 1) * 6];
            PrecomputeIndices(requiredPoints);
        }
        else
        {
            _verticesBuffer ??= new Vertex3D[_sampleBuffer.Length * 2];
            _indicesBuffer ??= new short[(_sampleBuffer.Length - 1) * 6];
        }
    }

    private static SystemVector3 SlerpNormal(SystemVector3 a, SystemVector3 b, float t)
    {
        float dot = Math.Clamp(SystemVector3.Dot(a, b), -1f, 1f);

        if (dot > 0.9995f)
            return SystemVector3.Normalize(SystemVector3.Lerp(a, b, t));

        float theta = MathF.Acos(dot);
        float sinTheta = MathF.Sin(theta);
        float wa = MathF.Sin((1f - t) * theta) / sinTheta;
        float wb = MathF.Sin(t * theta) / sinTheta;
        return SystemVector3.Normalize(a * wa + b * wb);
    }

    /// <summary>
    /// Distributes <paramref name="totalPoints"/> samples evenly along the polyline
    /// defined by <paramref name="originals"/> using arc-length parameterisation.
    /// </summary>
    private static int GetLinearTrailPoints3D(ReadOnlySpan<TrailSample3D> originals,
        Span<TrailSample3D> output, int totalPoints)
    {
        if (originals.Length < 2)
        {
            if (originals.Length != 1)
                return 0;

            output[0] = originals[0];
            return 1;
        }

        if (output.Length < totalPoints)
            throw new ArgumentException("Output buffer must be at least totalPoints in length");

        // Arc-length accumulation
        float totalLength = 0f;
        float[] segmentLengths = new float[originals.Length - 1];
        for (int i = 0; i < originals.Length - 1; i++)
        {
            segmentLengths[i] = SystemVector3.Distance(originals[i].Position.ToNumerics(),
                originals[i + 1].Position.ToNumerics());
            totalLength += segmentLengths[i];
        }

        if (totalLength == 0f)
        {
            for (int i = 0; i < totalPoints; i++)
                output[i] = originals[0];
            return totalPoints;
        }

        float step = totalLength / (totalPoints - 1);
        output[0] = originals[0];
        int currentPoint = 1;
        float accumulatedLen = 0f;
        int segIdx = 0;

        while (currentPoint < totalPoints && segIdx < segmentLengths.Length)
        {
            float targetLen = currentPoint * step;

            while (accumulatedLen + segmentLengths[segIdx] < targetLen &&
                   segIdx < segmentLengths.Length - 1)
            {
                accumulatedLen += segmentLengths[segIdx];
                segIdx++;
            }

            float t = Math.Clamp(
                (targetLen - accumulatedLen) / segmentLengths[segIdx], 0f, 1f);

            SystemVector3 pos = SystemVector3.Lerp(originals[segIdx].Position.ToNumerics(),
                originals[segIdx + 1].Position.ToNumerics(), t);
            SystemVector3 normal = SlerpNormal(originals[segIdx].Normal.ToNumerics(),
                originals[segIdx + 1].Normal.ToNumerics(), t);
            output[currentPoint++] =
                new TrailSample3D(pos.FromNumerics(), normal.FromNumerics(), originals[segIdx].Opacity);
        }

        // Fill any remainder with the last known position
        if (currentPoint < totalPoints)
        {
            TrailSample3D last = segIdx < originals.Length - 1
                ? originals[segIdx + 1]
                : originals[^1];
            for (int i = currentPoint; i < totalPoints; i++)
                output[i] = last;
        }

        return totalPoints;
    }

    private static int GetSmoothTrailPoints3D(ReadOnlySpan<TrailSample3D> originals,
        Span<TrailSample3D> output, int totalPoints)
    {
        if (originals.Length < 2)
            return originals.Length;

        // Ghost control points so the spline reaches the first and last recorded samples
        SystemVector3 p0 = originals[0].Position.ToNumerics() -
                           (originals[1].Position.ToNumerics() - originals[0].Position.ToNumerics());
        SystemVector3 pN = originals[^1].Position.ToNumerics() +
                           (originals[^1].Position.ToNumerics() - originals[^2].Position.ToNumerics());

        Span<SystemVector3> ctrlPos = stackalloc SystemVector3[originals.Length + 2];
        ctrlPos[0] = p0;
        ctrlPos[^1] = pN;
        for (int i = 0; i < originals.Length; i++)
            ctrlPos[i + 1] = originals[i].Position.ToNumerics();

        float tStep = (float) (originals.Length - 1) / (totalPoints - 1);

        for (int i = 0; i < totalPoints; i++)
        {
            float t = i * tStep;
            int idx = (int) t;
            float u = t - idx;

            // Catmull-Rom position through the control point array
            SystemVector3 pos = CatmullRom(
                ctrlPos[idx],
                ctrlPos[idx + 1],
                ctrlPos[idx + 2],
                idx + 3 < ctrlPos.Length ? ctrlPos[idx + 3] : ctrlPos[^1],
                u
            );

            // For the normals we slerp directly between the two bracketing recorded samples
            // (idx and idx+1, clamped to the originals range) using the same local t
            int srcA = Math.Clamp(idx, 0, originals.Length - 1);
            int srcB = Math.Clamp(idx + 1, 0, originals.Length - 1);
            SystemVector3 normal = SlerpNormal(
                originals[srcA].Normal.ToNumerics(),
                originals[srcB].Normal.ToNumerics(),
                u
            );

            output[i] = new TrailSample3D(pos.FromNumerics(), normal.FromNumerics(), originals[idx].Opacity);
        }

        return totalPoints;
    }

    /// <summary>
    /// Converts a span of interpolated <see cref="TrailSample3D"/> values into paired
    /// <see cref="Vertex3D"/> ribbon edges.
    /// <para>
    /// The ribbon half-width vector at each point is <c>cross(tangent, bladeNormal) * halfWidth</c>,
    /// so the ribbon lies in the plane defined by the blade face rather than always facing the camera.
    /// </para>
    /// </summary>
    private int GetVerticesFromTrailPoints3D(ReadOnlySpan<TrailSample3D> samples,
        Span<Vertex3D> vertices)
    {
        if (samples.Length < 2)
            return 0;

        for (int i = 0; i < samples.Length; i++)
        {
            float completion = i / (float) (samples.Length - 1);

            // Tangent: central difference in the interior, forward/backward at the ends
            SystemVector3 tangent;
            if (i == 0)
                tangent = (samples[1].Position.ToNumerics() - samples[0].Position.ToNumerics())
                    .SafeNormalize(SystemVector3.UnitX);
            else if (i == samples.Length - 1)
                tangent = (samples[i].Position.ToNumerics() - samples[i - 1].Position.ToNumerics())
                    .SafeNormalize(SystemVector3.UnitX);
            else
                tangent = (samples[i + 1].Position.ToNumerics() - samples[i].Position.ToNumerics() +
                           (samples[i].Position.ToNumerics() - samples[i - 1].Position.ToNumerics()))
                    .SafeNormalize(SystemVector3.UnitX);

            SystemVector3 normal = samples[i].Normal.ToNumerics();

            // The ribbon edge direction lies perpendicular to the tangent within the plane. This gives a vector along the width axis.
            SystemVector3 edgeDir = SystemVector3.Normalize(
                SystemVector3.Cross(tangent, normal));

            // Degenerate cross product (tangent and normal are parallel)
            if (float.IsNaN(edgeDir.X))
                edgeDir = SystemVector3.Normalize(SystemVector3.Cross(tangent, SystemVector3.UnitY));

            float halfWidth = widthFunction(completion) * 0.5f;
            SystemVector3 offset = offsetFunction?.Invoke(completion) ?? SystemVector3.Zero;
            SystemVector3 basePos = samples[i].Position.ToNumerics() + offset;

            SystemVector2 uv0 = new(completion, 0f);
            SystemVector2 uv1 = new(completion, 1f);

            float alpha = samples[i].Opacity;
            vertices[i * 2] = new Vertex3D(basePos + edgeDir * halfWidth,
                colorFunction(uv0) * alpha, uv0);
            vertices[i * 2 + 1] = new Vertex3D(basePos - edgeDir * halfWidth,
                colorFunction(uv1) * alpha, uv1);
        }

        return samples.Length * 2;
    }

    /// <summary>
    /// Compacts <paramref name="samples"/> in-place, removing any sample whose position
    /// lies within <paramref name="minDistance"/> world units of the previously retained sample.
    ///
    /// The first and last samples are always kept so the ribbon always starts and ends at the correct positions.
    /// </summary>
    /// <returns>The number of samples remaining after compaction.</returns>
    private static int RemoveProximateSamples(Span<TrailSample3D> samples, float minDistance)
    {
        if (samples.Length < 2)
            return samples.Length;

        float minDistSq = minDistance * minDistance;
        int writeHead = 1; // slot 0 is always kept

        for (int i = 1; i < samples.Length - 1; i++)
        {
            float distSq = SystemVector3.DistanceSquared(
                samples[i].Position.ToNumerics(),
                samples[writeHead - 1].Position.ToNumerics());

            if (distSq >= minDistSq)
                samples[writeHead++] = samples[i];
        }

        // Always retain the tail sample
        samples[writeHead++] = samples[^1];
        return writeHead;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Draws the ribbon trail.
    /// </summary>
    /// <param name="effect">The shader to apply. Its vertex function must accept 3D positions</param>
    /// <param name="originalSamples">The recorded trail samples, newest first</param>
    /// <param name="totalTrailPoints">
    /// Number of interpolated cross-sections. Higher values give a smoother ribbon.
    /// Defaults to the value supplied at construction time.
    /// </param>
    /// <param name="smooth">Use catmull-rom smoothing?</param>
    /// <param name="matrix">The combined world-view-projection matrix passed to the shader</param>
    public void DrawTrail(ManagedShader effect, ReadOnlySpan<TrailSample3D> originalSamples,
        int totalTrailPoints = -1, bool smooth = false, Matrix? matrix = null, float minSampleDistance = 6f)
    {
        if (Main.dedServ)
            return;
        if (originalSamples.Length < 2)
            return;

        int effectiveTotalPoints = totalTrailPoints > 0 ? totalTrailPoints : _maxTrailPoints;
        EnsureBuffers(effectiveTotalPoints);

        // Convert recorded samples into Numerics types for the interpolation passes.
        Span<TrailSample3D> converted = stackalloc TrailSample3D[originalSamples.Length];
        for (int i = 0; i < originalSamples.Length; i++)
            converted[i] = new TrailSample3D(
                originalSamples[i].Position,
                originalSamples[i].Normal,
                originalSamples[i].Opacity);

        Array.Clear(_sampleBuffer, 0, _sampleBuffer.Length);

        int pointCount = smooth
            ? GetSmoothTrailPoints3D(converted, _sampleBuffer.AsSpan(0, effectiveTotalPoints), effectiveTotalPoints)
            : GetLinearTrailPoints3D(converted, _sampleBuffer.AsSpan(0, effectiveTotalPoints), effectiveTotalPoints);

        if (pointCount < 2)
            return;

        // Remove interpolated samples that are too close together in world space
        // This eliminates degenerate zero-length ribbon segments that appear as clumps (which most often appear at animation inflection points)
        pointCount = RemoveProximateSamples(
            _sampleBuffer.AsSpan(0, pointCount),
            minSampleDistance);

        if (pointCount < 2)
            return;

        Array.Clear(_verticesBuffer, 0, _verticesBuffer.Length);
        int vertexCount = GetVerticesFromTrailPoints3D(
            _sampleBuffer.AsSpan(0, pointCount),
            _verticesBuffer.AsSpan(0, pointCount * 2));

        if (vertexCount < 3)
            return;

        GraphicsDevice device = Main.instance.GraphicsDevice;

        RasterizerState prevRasterizer = device.RasterizerState;
        Rectangle prevScissor = device.ScissorRectangle;
        BlendState prevBlendState = device.BlendState;

        device.RasterizerState = CullOnlyScreen;
        device.ScissorRectangle = new Rectangle(0, 0, device.Viewport.Width, device.Viewport.Height);
        device.BlendState = BlendState.AlphaBlend;

        effect.Render(transformMatrix: matrix);

        int indexCount = (pointCount - 1) * 6;
        device.DrawUserIndexedPrimitives(
            PrimitiveType.TriangleList,
            _verticesBuffer,
            0,
            vertexCount,
            _indicesBuffer,
            0,
            indexCount / 3
        );

        FailedTicks = 10;

        device.RasterizerState = prevRasterizer;
        device.ScissorRectangle = prevScissor;
        device.BlendState = prevBlendState;
    }

    public void Dispose()
    {
        if (Disposed)
            return;

        _indicesBuffer = null;
        _verticesBuffer = null;

        Disposed = true;
        GC.SuppressFinalize(this);
    }

    #endregion

    #region Destructor

    ~Trail3D()
    {
        if (Disposed)
            return;

        _indicesBuffer = null;
        _verticesBuffer = null;
    }

    #endregion

    #region Override Methods

    public override string ToString() =>
        $"Rendering {_sampleBuffer.Length} trail samples, {_verticesBuffer?.Length} vertices, " +
        $"{_indicesBuffer?.Length} indices. Disposed: {Disposed}";

    #endregion
}
