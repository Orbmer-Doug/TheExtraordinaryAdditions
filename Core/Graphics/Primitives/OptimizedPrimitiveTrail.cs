using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;

namespace TheExtraordinaryAdditions.Core.Graphics.Primitives;

// An extremely optimized system for rendering primis, easily handling millions of veritices a second.
// Could potentially benefit from added usage of:
// Dynamic vertex/index buffers
// Trail batching
// Vectorization

// Original design based off the primitive trail system present in the You Boss, which was likely carried from Infernum.

/// <summary>
/// A readonly struct representing a 2D vertex optimized for trail rendering, containing position, color, and texture coordinates.
/// </summary>
[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct Vertex2D(SystemVector2 position, Color color, SystemVector2 texCoord) : IVertexType
{
    public static readonly VertexDeclaration VertexDeclaration2D = new(
        [
            new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
            new VertexElement(8, VertexElementFormat.Color, VertexElementUsage.Color, 0),
            new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
        ]);

    public static readonly VertexDeclaration VertexDeclaration = VertexDeclaration2D;

    VertexDeclaration IVertexType.VertexDeclaration
    {
        get => VertexDeclaration;
    }

    public readonly SystemVector2 position = position;
    public readonly Color color = color;
    public readonly SystemVector2 texCoord = texCoord;

    public override readonly string ToString()
    {
        return $"[Position at: {position}, Colored with: {color}, Coord of :{texCoord}]";
    }
}

/// <summary>
/// A class managing a dynamic buffer of trail points.
/// Provides a read-only span of valid points for rendering or processing.
/// </summary>
public sealed class TrailPoints
{
    private Vector2[] trailBuffer;
    private int count;
    public int Count => count;

    public ReadOnlySpan<Vector2> Points => trailBuffer.AsSpan(0, count);

    public TrailPoints(int max)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(max, nameof(max));

        trailBuffer = new Vector2[max];
        count = max;
    }

    /// <summary>
    /// Sets a single point at the specified index.
    /// </summary>
    /// <param name="index">The index of the point to set.</param>
    /// <param name="value">The Vector2 value to set.</param>
    public void SetPoint(int index, Vector2 value)
    {
        if (index < 0 || index >= trailBuffer.Length)
            throw new IndexOutOfRangeException("Index is out of range for the trail points.");
        trailBuffer[index] = value;
    }

    /// <summary>
    /// Sets all points at once from a list.
    /// </summary>
    /// <param name="newPoints">The list of points to set.</param>
    public void SetPoints(List<Vector2> newPoints)
    {
        if (newPoints.Count > trailBuffer.Length)
            trailBuffer = new Vector2[newPoints.Count];

        newPoints.CopyTo(0, trailBuffer, 0, newPoints.Count);
        count = newPoints.Count;
    }

    /// <summary>
    /// Sets all points at once from a span.
    /// </summary>
    /// <param name="newPoints">The span of points to set.</param>
    public void SetPoints(ReadOnlySpan<Vector2> newPoints)
    {
        if (newPoints.Length > trailBuffer.Length)
            trailBuffer = new Vector2[newPoints.Length];

        newPoints.CopyTo(trailBuffer);
        count = newPoints.Length;
    }

    public void Update(Vector2 newPosition)
    {
        CreateTrail(trailBuffer.AsSpan(), newPosition, trailBuffer.Length, ref count);
    }

    /// <summary>
    /// Updates a trail buffer with a new sample position, shifting existing points and maintaining a maximum size.
    /// </summary>
    /// <param name="trailBuffer">The preallocated array to store trail points.</param>
    /// <param name="samplePos">The new position to add to the trail.</param>
    /// <param name="max">The maximum number of points in the trail.</param>
    /// <param name="currentCount">The current number of valid points in the buffer (updated by reference).</param>
    /// <returns>A Span representing the updated trail points.</returns>
    public static Span<Vector2> CreateTrail(Span<Vector2> trailBuffer, Vector2 samplePos, int max, ref int currentCount)
    {
        if (trailBuffer.Length < max)
            throw new ArgumentException("trailBuffer must be at least as large as max!");

        // Initialize if invalid or empty
        if (currentCount <= 0 || trailBuffer[..currentCount].IndexOf(Vector2.Zero) >= 0)
        {
            currentCount = max;
            trailBuffer[..max].Fill(samplePos);
        }
        else
        {
            // Shift existing points right (discard oldest if at max)
            int shiftCount = Math.Min(currentCount, max - 1);
            if (shiftCount > 0)
                trailBuffer[..shiftCount].CopyTo(trailBuffer[1..]);

            // Insert new position at start
            trailBuffer[0] = samplePos;

            // Update count (cap at max)
            currentCount = Math.Min(currentCount + 1, max);
        }

        return trailBuffer[..currentCount];
    }

    /// <summary>
    /// Clears all points, resetting the count to zero.
    /// </summary>
    public void Clear()
    {
        if (trailBuffer.Length != 0)
        {
            count = 0;
            Array.Clear(trailBuffer);
        }
    }
}

/// <summary>
/// I suspect there to be better options but this is the only reliable thing I can think of to <br></br>
/// ensure a trail sometime somewhere is removed to prevent memory leak when whatever object that needed it is gone
/// </summary>
public sealed class TrailCleaner : ModSystem
{
    public static TrailCleaner Instance => ModContent.GetInstance<TrailCleaner>();
    public List<OptimizedPrimitiveTrail> trails = [];
    private int cleanCounter;

    public override void PostUpdateEverything()
    {
        if (trails.Count == 0)
            return;

        if (cleanCounter++ < 10)
            return;
        cleanCounter = 0;

        int writeIndex = 0;
        for (int readIndex = 0; readIndex < trails.Count; readIndex++)
        {
            OptimizedPrimitiveTrail trail = trails[readIndex];
            if (trail == null || trail.Disposed)
                continue;

            trail.FailedTicks--;
            if (trail.FailedTicks <= 0)
            {
                trail.Dispose();
                continue;
            }

            trails[writeIndex] = trail;
            writeIndex++;
        }

        if (writeIndex < trails.Count)
            trails.RemoveRange(writeIndex, trails.Count - writeIndex);
    }
}

/// <summary>
/// A highly optimized class for rendering trails
/// </summary>
public sealed class OptimizedPrimitiveTrail : IDisposable
{
    #region Public Delegates
    /// <summary>
    /// Delegate for a function that returns the trail width based on a completion ratio.
    /// </summary>
    /// <param name="completionRatio">A value from 0 to 1 indicating progress along the trail.</param>
    /// <returns>The width of the trail at the given completion ratio.</returns>
    public delegate float VertexWidthFunction(float completionRatio);

    /// <summary>
    /// Delegate for a function that returns the color based on texture coordinates.
    /// </summary>
    /// <param name="texCoord">A 0-1 vector (X for progress, Y for side) used to sample the color.</param>
    /// <param name="position">A vector for the world position this color is being sampled at.</param>
    /// <returns>The color at the specified texture coordinate.</returns>
    public delegate Color VertexColorFunction(SystemVector2 texCoord, Vector2 position);

    /// <summary>
    /// Delegate for a function that returns an offset vector based on a completion ratio.
    /// </summary>
    /// <param name="completionRatio">A value from 0 to 1 indicating progress along the trail.</param>
    /// <returns>The offset vector to apply at the given completion ratio.</returns>
    public delegate SystemVector2 VertexOffsetFunction(float completionRatio);
    #endregion

    #region Private Fields
    private SystemVector2[] _trailPointsBuffer;
    private Vertex2D[] _verticesBuffer;
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

    #region Public Constructor
    /// <summary>
    /// Initializes a new <see cref="OptimizedPrimitiveTrail"/>.
    /// </summary>
    public OptimizedPrimitiveTrail(VertexWidthFunction widthFunction, VertexColorFunction colorFunction,
    VertexOffsetFunction offsetFunction = null, int maxTrailPoints = 1024)
    {
        if (!Main.dedServ)
        {
            this.widthFunction = widthFunction ?? throw new ArgumentNullException(nameof(widthFunction));
            this.colorFunction = colorFunction ?? throw new ArgumentNullException(nameof(colorFunction));
            this.offsetFunction = offsetFunction;

            _maxTrailPoints = maxTrailPoints;
            _trailPointsBuffer = new SystemVector2[maxTrailPoints];
            _verticesBuffer = new Vertex2D[(maxTrailPoints) * 2];
            _indicesBuffer = new short[(maxTrailPoints - 1) * 6];

            PrecomputeIndices(maxTrailPoints);
        }

        TrailCleaner.Instance.trails.Add(this);
        FailedTicks = 10;
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Precomputes the triangle indices for the trail body to improve rendering efficiency.
    /// </summary>
    private void PrecomputeIndices(int pointCount)
    {
        Array.Clear(_indicesBuffer, 0, _indicesBuffer.Length);
        for (int i = 0; i < pointCount - 1; i++)
        {
            int start = i * 6;
            int connect = i * 2;
            _indicesBuffer[start] = (short)connect;
            _indicesBuffer[start + 1] = (short)(connect + 1);
            _indicesBuffer[start + 2] = (short)(connect + 2);
            _indicesBuffer[start + 3] = (short)(connect + 2);
            _indicesBuffer[start + 4] = (short)(connect + 1);
            _indicesBuffer[start + 5] = (short)(connect + 3);
        }
    }

    /// <summary>
    /// Ensures the internal buffers are large enough to handle the required number of trail points with the given tip.
    /// </summary>
    private void EnsureBuffers(int requiredTrailPoints)
    {
        if (requiredTrailPoints > _trailPointsBuffer.Length)
        {
            _trailPointsBuffer = new SystemVector2[requiredTrailPoints];
            _verticesBuffer = new Vertex2D[(requiredTrailPoints) * 2];
            _indicesBuffer = new short[(requiredTrailPoints - 1) * 6];
            PrecomputeIndices(requiredTrailPoints);
        }
        else
        {
            // Sometimes one of these goes null for some reason despite the trail being correctly created in the constructor???
            // idk its wildly inconsistent to replicate the problem so this will do for now
            _verticesBuffer ??= new Vertex2D[(requiredTrailPoints) * 2];
            _indicesBuffer ??= new short[(requiredTrailPoints - 1) * 6];
        }
    }

    /// <summary>
    /// Performs linear interpolation of trail points based on segment lengths for even distribution.
    /// </summary>
    private static int GetLinearTrailPoints(ReadOnlySpan<SystemVector2> originalPositions,
        Span<SystemVector2> trailPoints, int totalTrailPoints)
    {
        if (originalPositions.Length < 2)
        {
            if (originalPositions.Length == 1)
            {
                trailPoints[0] = originalPositions[0];
                return 1;
            }
            return 0;
        }

        // Ensure trailPoints is large enough
        if (trailPoints.Length < totalTrailPoints)
            throw new ArgumentException("trailPoints buffer must be at least totalTrailPoints in length.");

        // Calculate total length for even distribution
        float totalLength = 0f;
        float[] segmentLengths = new float[originalPositions.Length - 1];
        for (int i = 0; i < originalPositions.Length - 1; i++)
        {
            segmentLengths[i] = SystemVector2.Distance(originalPositions[i], originalPositions[i + 1]);
            totalLength += segmentLengths[i];
        }

        // Avoid division by zero
        if (totalLength == 0f)
        {
            for (int i = 0; i < totalTrailPoints; i++)
                trailPoints[i] = originalPositions[0];
            return totalTrailPoints;
        }

        float step = totalLength / (totalTrailPoints - 1);
        trailPoints[0] = originalPositions[0];
        int currentPoint = 1;
        float accumulatedLength = 0f;
        int segmentIndex = 0;

        // Use a while loop for distance-based point distribution
        while (currentPoint < totalTrailPoints && segmentIndex < segmentLengths.Length)
        {
            float targetLength = currentPoint * step;
            while (accumulatedLength + segmentLengths[segmentIndex] < targetLength && segmentIndex < segmentLengths.Length - 1)
            {
                accumulatedLength += segmentLengths[segmentIndex];
                segmentIndex++;
            }

            float t = (targetLength - accumulatedLength) / segmentLengths[segmentIndex];
            t = Math.Clamp(t, 0f, 1f); // Prevent extrapolation due to numerical errors
            trailPoints[currentPoint] = SystemVector2.Lerp(originalPositions[segmentIndex],
                originalPositions[segmentIndex + 1], t);
            currentPoint++;
        }

        // If we didn't generate enough points, fill the remainder with the last position
        if (currentPoint < totalTrailPoints)
        {
            SystemVector2 lastPoint = segmentIndex < originalPositions.Length - 1
                ? originalPositions[segmentIndex + 1]
                : originalPositions[^1];
            for (int i = currentPoint; i < totalTrailPoints; i++)
            {
                trailPoints[i] = lastPoint;
            }
        }

        return totalTrailPoints;
    }

    /// <summary>
    /// Performs Catmull-Rom spline interpolation for smooth trail points.
    /// </summary>
    private int GetSmoothTrailPoints(ReadOnlySpan<SystemVector2> originalPositions,
    Span<SystemVector2> trailPoints, int totalTrailPoints)
    {
        if (originalPositions.Length < 2)
            return originalPositions.Length;

        // Extend control points for Catmull-Rom
        SystemVector2 p0 = originalPositions[0] - (originalPositions[1] - originalPositions[0]);
        SystemVector2 pN = originalPositions[^1] + (originalPositions[^1] - originalPositions[^2]);
        Span<SystemVector2> controlPoints = stackalloc SystemVector2[originalPositions.Length + 2];
        controlPoints[0] = p0;
        originalPositions.CopyTo(controlPoints[1..]);
        controlPoints[^1] = pN;

        float tStep = (float)(originalPositions.Length - 1) / (totalTrailPoints - 1);
        for (int i = 0; i < totalTrailPoints; i++)
        {
            float t = i * tStep;
            int idx = (int)t;
            float u = t - idx;
            trailPoints[i] = CatmullRom(
                controlPoints[idx],
                controlPoints[idx + 1],
                controlPoints[idx + 2],
                idx + 3 < controlPoints.Length ? controlPoints[idx + 3] : controlPoints[^1],
                u
            );
        }

        return totalTrailPoints;
    }

    /// <summary>
    /// Converts trail points into vertex data for rendering, including width and color.
    /// </summary>
    /// <param name="trailPoints">The interpolated points to convert.</param>
    /// <param name="vertices">The output span to store vertex data.</param>
    /// <param name="directionOverride">An optional override for the direction vector (default is null).</param>
    /// <returns>The number of vertices written to <paramref name="vertices"/>.</returns>
    /// <remarks>
    /// Each pair of points generates two vertices (one per side), with normals calculated from the direction.
    /// </remarks>
    private int GetVerticesFromTrailPoints(ReadOnlySpan<SystemVector2> trailPoints,
        Span<Vertex2D> vertices, float? directionOverride = null)
    {
        if (trailPoints.Length < 2)
            return 0;

        for (int i = 0; i < trailPoints.Length; i++)
        {
            float completion = i / (float)(trailPoints.Length - 1);
            SystemVector2 direction;
            if (directionOverride == null)
            {
                if (i == 0)
                    direction = (trailPoints[1] - trailPoints[0]).SafeNormalize(SystemVector2.UnitX);
                else if (i == trailPoints.Length - 1)
                    direction = (trailPoints[i] - trailPoints[i - 1]).SafeNormalize(SystemVector2.UnitX);
                else
                    direction = (trailPoints[i + 1] - trailPoints[i] + (trailPoints[i] - trailPoints[i - 1])).SafeNormalize(SystemVector2.UnitX);
            }
            else
                direction = PolarVector2(1f, directionOverride.Value);

            SystemVector2 normal = new(-direction.Y, direction.X);
            float width = widthFunction(completion);
            SystemVector2 offset = offsetFunction != null ? offsetFunction(completion) : SystemVector2.Zero;
            SystemVector2 trailPoint = trailPoints[i];

            vertices[i * 2] = new Vertex2D(
                trailPoint + offset + normal * width * 0.5f,
                colorFunction(new SystemVector2(completion, 0), trailPoint.FromNumerics()),
                new SystemVector2(completion, 0)
            );
            vertices[i * 2 + 1] = new Vertex2D(
                trailPoint + offset - normal * width * 0.5f,
                colorFunction(new SystemVector2(completion, 1), trailPoint.FromNumerics()),
                new SystemVector2(completion, 1)
            );
        }

        return trailPoints.Length * 2;
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Directly draws a trail.
    /// </summary>
    /// <param name="effect">The shader to apply</param>
    /// <param name="originalPositions">The points</param>
    /// <param name="totalTrailPoints">Increase for smoothness</param>
    /// <param name="smooth">Set to true for greater smoothness</param>
    /// <param name="pixelated">Is this trail pixelated?</param>
    public void DrawTrail(ManagedShader effect, ReadOnlySpan<Vector2> originalPositions, int totalTrailPoints = -1, bool smooth = false, bool pixelated = true)
    {
        if (Main.dedServ)
            return;
        if (originalPositions == null || originalPositions.Length < 2 || Utility.ContainsInvalidPoint(originalPositions) || Utility.AllPointsEqual(originalPositions))
            return;

        int effectiveTotalPoints = totalTrailPoints > 0 ? totalTrailPoints : _maxTrailPoints;
        EnsureBuffers(effectiveTotalPoints);

        Span<SystemVector2> convertedPositions = stackalloc SystemVector2[originalPositions.Length];
        for (int i = 0; i < originalPositions.Length; i++)
            convertedPositions[i] = originalPositions[i].ToNumerics();

        // Clear potentially stale data
        Array.Clear(_trailPointsBuffer, 0, _trailPointsBuffer.Length);
        int pointCount = smooth
            ? GetSmoothTrailPoints(convertedPositions, _trailPointsBuffer.AsSpan(0, effectiveTotalPoints), effectiveTotalPoints)
            : GetLinearTrailPoints(convertedPositions, _trailPointsBuffer.AsSpan(0, effectiveTotalPoints), effectiveTotalPoints);

        if (pointCount < 2)
            return;

        // Clear potentially stale data
        Array.Clear(_verticesBuffer, 0, _verticesBuffer.Length);
        int trailVertexCount = GetVerticesFromTrailPoints(_trailPointsBuffer.AsSpan(0, pointCount),
            _verticesBuffer.AsSpan(0, (pointCount) * 2), null);

        if (trailVertexCount < 3)
            return;

        GraphicsDevice device = Main.instance.GraphicsDevice;

        // Save and set render states
        RasterizerState prevRasterizer = device.RasterizerState;
        Rectangle prevScissor = device.ScissorRectangle;
        BlendState prevBlendState = Main.instance.GraphicsDevice.BlendState;

        device.RasterizerState = CullOnlyScreen;
        device.ScissorRectangle = new Rectangle(0, 0, device.Viewport.Width, device.Viewport.Height);
        device.BlendState = BlendState.AlphaBlend;

        // Apply effect
        effect.Render(ManagedShader.DefaultPassName, pixelated);

        // Draw trail body
        int trailIndexCount = (pointCount - 1) * 6;
        device.DrawUserIndexedPrimitives(
            PrimitiveType.TriangleList,
            _verticesBuffer,
            0,
            trailVertexCount,
            _indicesBuffer,
            0,
            trailIndexCount / 3
        );
        FailedTicks = 10;

        // Restore render states
        device.RasterizerState = prevRasterizer;
        device.ScissorRectangle = prevScissor;
        device.BlendState = prevBlendState;
    }

    /// <summary>
    /// Retrieves the last (or first) trail point, optionally smoothed.
    /// </summary>
    public Vector2 GetEndPoint(ReadOnlySpan<Vector2> positions, bool getFirstPoint = false, bool smooth = false)
    {
        EnsureBuffers(_maxTrailPoints);

        Span<SystemVector2> convertedPositions = stackalloc SystemVector2[positions.Length];
        for (int i = 0; i < positions.Length; i++)
            convertedPositions[i] = positions[i].ToNumerics();

        Span<SystemVector2> tempPoints = stackalloc SystemVector2[_maxTrailPoints];
        int pointCount = smooth
            ? GetSmoothTrailPoints(convertedPositions, tempPoints, _maxTrailPoints)
            : GetLinearTrailPoints(convertedPositions, tempPoints, _maxTrailPoints);

        if (pointCount < 2)
            return convertedPositions[^1].FromNumerics();

        Span<Vertex2D> tempVertices = stackalloc Vertex2D[(pointCount) * 2];
        int vertexCount = GetVerticesFromTrailPoints(tempPoints[..pointCount], tempVertices);

        if (getFirstPoint)
        {
            if (pointCount < 2) // Not enough points to smooth
                return convertedPositions[0].FromNumerics(); // Just return the first input point

            // For a smooth start, average the first two vertices
            return ((tempVertices[0].position + tempVertices[1].position) * 0.5f).FromNumerics();
        }
        else
        {
            if (vertexCount < 2)
                return tempPoints[pointCount - 1].FromNumerics();

            return ((tempVertices[vertexCount - 1].position + tempVertices[vertexCount - 2].position) * 0.5f).FromNumerics();
        }
    }

    public void Dispose()
    {
        if (!Disposed)
        {
            _indicesBuffer = null;
            _verticesBuffer = null;

            Disposed = true;
            GC.SuppressFinalize(this);
        }
    }
    #endregion

    #region Public Static Methods
    /// <summary>
    /// Helps make a <see cref="VertexWidthFunction"/> that creates a pyriform shaped trail (commonly referred to as a meteor trail or pear)
    /// </summary>
    /// <param name="completionRatio">A value from 0 to 1 indicating progress along the trail.</param>
    /// <param name="width">The thickness of the trail.</param>
    /// <param name="power">The thickness of the base</param>
    /// <param name="hemisphereAmt">The percentage of how much of the trail the hemisphere should take.</param>
    /// <param name="taperAmt">The percentage of how much of the trail should taper off.</param>
    /// <returns></returns>
    public static float PyriformWidthFunct(float completionRatio, float width, float power = 2f, float hemisphereAmt = .3f, float taperAmt = .4f)
    {
        float tipInterpolant = MathF.Sqrt(1f - Animators.MakePoly(power).InFunction(InverseLerp(hemisphereAmt, 0f, completionRatio)));
        return width * InverseLerp(1f, taperAmt, completionRatio) * tipInterpolant;
    }

    /// <summary>
    /// Helps make a <see cref="VertexWidthFunction"/> that creates a hemispherical tip at the end of a trail
    /// </summary>
    /// <param name="completionRatio">A value from 0 to 1 indicating progress along the trail.</param>
    /// <param name="width">The thickness of the trail.</param>
    /// <param name="power">The thickness of the base</param>
    /// <param name="hemisphereAmt">The percentage of how much of the trail the hemisphere should take.</param>
    /// <returns></returns>
    public static float HemisphereWidthFunct(float completionRatio, float width, float power = 1f, float hemisphereAmt = .3f)
    {
        float tipInterpolant = MathF.Sqrt(1f - Animators.MakePoly(power).InFunction(InverseLerp(hemisphereAmt, 0f, completionRatio)));
        return width * tipInterpolant;
    }
    #endregion

    #region Destructor
    ~OptimizedPrimitiveTrail()
    {
        if (!Disposed)
        {
            _indicesBuffer = null;
            _verticesBuffer = null;
        }
    }
    #endregion

    #region Override Methods
    public override string ToString()
    {
        return $"Rendering {_trailPointsBuffer.Length} trail points, {_verticesBuffer} vertices, and {_indicesBuffer} indices. Is this trail disposed? {Disposed} \n";
    }
    #endregion
}