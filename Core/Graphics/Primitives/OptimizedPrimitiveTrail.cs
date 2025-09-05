using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;
using static TheExtraordinaryAdditions.Core.Graphics.Primitives.OptimizedPrimitiveTrail;

namespace TheExtraordinaryAdditions.Core.Graphics.Primitives;

// A prime design, is the best possible trailing system I can create.
// Over months I would come back to this system and try to make it better, ending up failing in the end.
// Now, I musn't hide the true creation of this system.
// With numerous long hours of comprehending code I finally gave in to asking many types of AI.
// It ended with Grok, in which I then fixed up the rest of the system.

// Original was the primitive trail system present in the You Boss, which was likely copied from Infernum's.

// Thereotically, the amount of trail points can be up to 3.5x the amount of pixels likely on your screen (2,073,600), but the games unplayable at that point (but it doesn't crash!)
// Up to 172,000 trail points (double the max) in my experience were enough to make the game stutter (thats ~61,919,940 indices or ~20,640,000 vertices being rendered per second!)

// Either way it's absurd how fast it is despite the possible optimizations of:
// Dynamic Vertex/Index buffers (though this gets real complex real fast, and it is particulary annoying to deal with)
// Trail batching
// Actually combining the tip and trail points (without creating artifacts)

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
/// A class managing a dynamic buffer of trail points with automatic updating and shifting.
/// Provides a read-only span of valid points for rendering or processing.
/// </summary>
/// <remarks>
/// This class is designed for real-time trail updates, shifting older points out as new ones are added.
/// </remarks>
public class TrailPoints
{
    private readonly Vector2[] _trailBuffer;
    private int _trailCount;

    // Public property to access the current trail points as a ReadOnlySpan
    public ReadOnlySpan<Vector2> Points => _trailBuffer.AsSpan(0, _trailCount);

    public TrailPoints(int max)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(max, nameof(max));

        _trailBuffer = new Vector2[max];
        _trailCount = 0;
    }

    public void Update(Vector2 newPosition)
    {
        CreateTrail(_trailBuffer.AsSpan(), newPosition, _trailBuffer.Length, ref _trailCount);
    }

    public void Clear()
    {
        if (_trailBuffer.Length != 0)
        {
            _trailCount = 0;
            Array.Clear(_trailBuffer);
        }
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
        //if (samplePos == Vector2.Zero)
        //  throw new ArgumentException("samplePos cannot be zero!");

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
}

/// <summary>
/// A class for manually managing a trail's points with a fixed capacity.
/// </summary>
/// <remarks>
/// This class assumes the initial capacity is fully utilized unless overwritten.
/// It resizes the buffer only when new data exceeds the current capacity.
/// </remarks>
public class ManualTrailPoints
{
    private Vector2[] points;
    private int count;

    /// <summary>
    /// Initializes a new instance with a fixed capacity.
    /// </summary>
    /// <param name="capacity">The number of points the trail can hold.</param>
    public ManualTrailPoints(int capacity)
    {
        points = new Vector2[capacity];
        count = capacity; // Assume full capacity is used unless set otherwise
    }

    /// <summary>
    /// Sets all points at once from a list.
    /// </summary>
    /// <param name="newPoints">The list of points to set.</param>
    public void SetPoints(List<Vector2> newPoints)
    {
        if (newPoints.Count > points.Length)
        {
            points = new Vector2[newPoints.Count];
        }
        newPoints.CopyTo(0, points, 0, newPoints.Count);
        count = newPoints.Count;
    }

    /// <summary>
    /// Sets all points at once from a span.
    /// </summary>
    /// <param name="newPoints">The span of points to set.</param>
    public void SetPoints(ReadOnlySpan<Vector2> newPoints)
    {
        if (newPoints.Length > points.Length)
        {
            points = new Vector2[newPoints.Length];
        }
        newPoints.CopyTo(points);
        count = newPoints.Length;
    }

    /// <summary>
    /// Sets a single point at the specified index.
    /// </summary>
    /// <param name="index">The index of the point to set.</param>
    /// <param name="value">The Vector2 value to set.</param>
    public void SetPoint(int index, Vector2 value)
    {
        if (index < 0 || index >= points.Length)
            throw new IndexOutOfRangeException("Index is out of range for the trail points.");
        points[index] = value;
    }

    /// <summary>
    /// Gets the points as a read-only span for rendering.
    /// </summary>
    public ReadOnlySpan<Vector2> Points => points.AsSpan(0, count);

    /// <summary>
    /// Gets the number of points currently in use.
    /// </summary>
    public int Count => count;

    /// <summary>
    /// Ensures the internal buffer can hold at least the required capacity.
    /// </summary>
    /// <param name="requiredCapacity">The minimum capacity needed.</param>
    public void EnsureCapacity(int requiredCapacity)
    {
        if (requiredCapacity > points.Length)
        {
            int newCapacity = Math.Max(requiredCapacity, points.Length * 2); // Double capacity to reduce future resizes
            Array.Resize(ref points, newCapacity);
        }
    }

    /// <summary>
    /// Clears all points, resetting the count to zero.
    /// </summary>
    public void Clear()
    {
        Array.Clear(points, 0, points.Length);
        count = 0;
    }
}

/// <summary>
/// Defines the contract for a trail tip implementation, which provides additional geometry at the end of a trail.
/// Tips are responsible for calculating their own vertex and index counts, as well as generating mesh data.
/// </summary>
/// <remarks>
/// Implementations should ensure that <see cref="ExtraVertices"/> and <see cref="ExtraIndices"/> are consistent with
/// the geometry generated in <see cref="GenerateMesh"/>. The method operates on pre-allocated spans to avoid
/// unnecessary allocations, and buffer overflow checks are recommended.
/// </remarks>
public interface ITrailTip
{
    int ExtraVertices { get; }
    int ExtraIndices { get; }
    void GenerateMesh(SystemVector2 trailTipPosition, SystemVector2 trailTipNormal, int startFromIndex,
        Span<Vertex2D> vertices, Span<short> indices, VertexWidthFunction trailWidthFunction, VertexColorFunction trailColorFunction);
}

public class NoTip : ITrailTip
{
    public int ExtraVertices => 0;
    public int ExtraIndices => 0;

    public void GenerateMesh(SystemVector2 trailTipPosition, SystemVector2 trailTipNormal, int startFromIndex, Span<Vertex2D> vertices, Span<short> indices, VertexWidthFunction trailWidthFunction, VertexColorFunction trailColorFunction)
    {
    }
}

public class TriangularTip(float length) : ITrailTip
{
    private readonly float length = length;

    public int ExtraVertices => 3;
    public int ExtraIndices => 3;

    /// <summary>
    /// Generates a triangular mesh at the trail's end, using the trail's width and color functions.
    /// </summary>
    public void GenerateMesh(SystemVector2 trailTipPosition, SystemVector2 trailTipNormal, int startFromIndex, Span<Vertex2D> vertices, Span<short> indices, VertexWidthFunction trailWidthFunction, VertexColorFunction trailColorFunction)
    {
        if (vertices.Length < ExtraVertices || indices.Length < ExtraIndices)
            return;

        SystemVector2 normalPerp = trailTipNormal.RotatedBy(PiOver2);
        float width = trailWidthFunction?.Invoke(1f) ?? 1f;
        SystemVector2 a = trailTipPosition + normalPerp * width;
        SystemVector2 b = trailTipPosition - normalPerp * width;
        SystemVector2 c = trailTipPosition + trailTipNormal * length;

        SystemVector2 texCoordA = SystemVector2.UnitX;
        SystemVector2 texCoordB = SystemVector2.One;
        SystemVector2 texCoordC = new(1f, 0.5f);

        Color colorA = trailColorFunction?.Invoke(texCoordA, a.FromNumerics()) ?? Color.White;
        Color colorB = trailColorFunction?.Invoke(texCoordB, b.FromNumerics()) ?? Color.White;
        Color colorC = trailColorFunction?.Invoke(texCoordC, c.FromNumerics()) ?? Color.White;

        vertices[0] = new Vertex2D(a, colorA, texCoordA);
        vertices[1] = new Vertex2D(b, colorB, texCoordB);
        vertices[2] = new Vertex2D(c, colorC, texCoordC);

        indices[0] = (short)startFromIndex;
        indices[1] = (short)(startFromIndex + 1);
        indices[2] = (short)(startFromIndex + 2);
    }
}

// TODO: Is there a way to make this more seamless for non-uniform shaders?
public class RoundedTip : ITrailTip
{
    private readonly int triCount;
    private readonly SystemVector2? centerTexCoord;
    private readonly float colorStart;
    public int ExtraVertices => 1 + 2 * triCount;
    public int ExtraIndices => 6 * triCount;

    /// <summary>
    /// Initializes a new <see cref="RoundedTip"/> with the specified triangle count.
    /// </summary>
    /// <param name="triCount">The number of triangles per layer (must be at least 2 for a valid shape).</param>
    /// <param name="centerTexCoord">An optional modifier for where the tips color sampling should take place.</param>
    public RoundedTip(int triCount = 2, SystemVector2? centerTexCoord = null, float colorStart = 0f)
    {
        this.triCount = triCount;
        this.centerTexCoord = centerTexCoord;
        this.colorStart = colorStart;
        if (triCount < 2)
            throw new ArgumentException($"Parameter {nameof(triCount)} cannot be less than 2.");
    }

    /// <summary>
    /// Generates a rounded mesh at the trail's end, using two concentric hemispherical layers.
    /// </summary>
    public void GenerateMesh(SystemVector2 trailTipPosition, SystemVector2 tipNormal, int startFromIndex, Span<Vertex2D> vertices, Span<short> indices, VertexWidthFunction trailWidthFunction, VertexColorFunction trailColorFunction)
    {
        // Calculate required space including the starting index
        if (startFromIndex + ExtraVertices > vertices.Length || ExtraIndices > indices.Length)
        {
            $"Buffer overflow: startFromIndex={startFromIndex}, ExtraVertices={ExtraVertices}, vertices.Length={vertices.Length}, ExtraIndices={ExtraIndices}, indices.Length={indices.Length}".Log();
            return;
        }

        // Calculate radius based on the trail's width
        float radius = trailWidthFunction?.Invoke(colorStart) ?? 10f;

        SystemVector2 fanCenterTexCoord = centerTexCoord ?? new(0f, 0.5f);
        Color centerColor = (trailColorFunction?.Invoke(fanCenterTexCoord, trailTipPosition.FromNumerics()) ?? Color.White) * 0.75f;
        vertices[startFromIndex] = new Vertex2D(trailTipPosition, centerColor, fanCenterTexCoord);
        tipNormal = (tipNormal.FromNumerics().SafeNormalize(Vector2.UnitX)).ToNumerics();

        int vertexIndex = startFromIndex + 1; // Start after the center vertex
        int indexCount = 0;
        int triCount = (ExtraVertices - 1) / 2;

        // First layer (outer hemisphere)
        float firstLayerRadius = radius * .5f;
        for (int k = 0; k < triCount; k++)
        {
            float rotationFactor = k / (float)(triCount - 1);
            float angle = -Pi / 2 + rotationFactor * Pi;
            SystemVector2 offset = tipNormal.RotatedBy(angle) * firstLayerRadius;
            SystemVector2 circlePoint = trailTipPosition + offset;
            SystemVector2 texCoord = new(rotationFactor, 1f);
            Color vertexColor = (trailColorFunction?.Invoke(texCoord, trailTipPosition.FromNumerics()) ?? Color.White) * (0.85f + rotationFactor * 0.35f);

            vertices[vertexIndex++] = new Vertex2D(circlePoint, vertexColor, texCoord);

            // Write indices for the triangle clockwise (fan around the center)
            int nextVertex = startFromIndex + 1 + (k + 1) % triCount;
            indices[indexCount++] = (short)startFromIndex; // Center
            indices[indexCount++] = (short)(startFromIndex + 1 + k); // Current vertex
            indices[indexCount++] = (short)nextVertex; // Next vertex
        }

        // Second layer (inner hemisphere)
        float secondLayerRadius = radius * .3f;
        for (int k = 0; k < triCount; k++)
        {
            float rotationFactor = k / (float)(triCount - 1);
            float angle = -Pi / 2 + rotationFactor * Pi;
            SystemVector2 offset = tipNormal.RotatedBy(-angle) * secondLayerRadius;
            SystemVector2 circlePoint = trailTipPosition + offset;
            SystemVector2 texCoord = new(rotationFactor, 0f);
            Color vertexColor = (trailColorFunction?.Invoke(texCoord, trailTipPosition.FromNumerics()) ?? Color.White) * (0.85f + rotationFactor * 0.25f);

            vertices[vertexIndex++] = new Vertex2D(circlePoint, vertexColor, texCoord);

            // Write indices for the triangle counter-clockwise (fan around the center)
            int nextVertex = startFromIndex + 1 + triCount + (k + 1) % triCount;
            indices[indexCount++] = (short)startFromIndex; // Center
            indices[indexCount++] = (short)nextVertex; // Next vertex
            indices[indexCount++] = (short)(startFromIndex + 1 + triCount + k); // Current vertex
        }

        // Don't exceed buffer limits
        if (indexCount > indices.Length)
        {
            $"Index overflow: indexCount={indexCount}, indices.Length={indices.Length}".Log();
        }
    }
}

/// <summary>
/// I suspect there to be better options but this is the only reliable thing I can think of to
/// ensure a trail sometime somewhere is removed to prevent memory leak when whatever object that needed it is gone <br></br>
/// So in this case we are just gonna do a little SLR
/// </summary>
public class TrailManager : ModSystem
{
    public static TrailManager Instance => ModContent.GetInstance<TrailManager>();
    public List<OptimizedPrimitiveTrail> trails = [];
    public int cleanCounter;
    public override void PostUpdateEverything()
    {
        if (Main.dedServ || trails.Count == 0)
            return;

        // Only run ~12 times a second to reduce overhead
        cleanCounter++;
        if (cleanCounter < 5)
            return;
        cleanCounter = 0;

        var toRemove = new List<int>();

        var span = (ReadOnlySpan<OptimizedPrimitiveTrail>)CollectionsMarshal.AsSpan(trails);
        for (int i = 0; i < span.Length; i++)
        {
            var trail = span[i];
            if (trail == null || trail._disposed)
            {
                toRemove.Add(i);
                continue;
            }

            trail.failedTicks--;
            if (trail.failedTicks <= 0)
            {
                trail.Dispose();
                toRemove.Add(i);
            }
        }

        // Remove trails in reverse order to avoid index shifting
        for (int i = toRemove.Count - 1; i >= 0; i--)
        {
            trails.RemoveAt(toRemove[i]);
        }
    }
}

/// <summary>
/// A highly optimized class for rendering trails with optional tips
/// </summary>
public class OptimizedPrimitiveTrail : IDisposable
{
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

    internal readonly VertexWidthFunction _widthFunction;
    internal readonly VertexColorFunction _colorFunction;
    internal readonly VertexOffsetFunction _offsetFunction;

    private SystemVector2[] _trailPointsBuffer;
    private Vertex2D[] _verticesBuffer;
    private short[] _indicesBuffer;

    // Separate buffers for the tip
    private Vertex2D[] _tipVerticesBuffer;
    private short[] _tipIndicesBuffer;

    private readonly int _maxTrailPoints;

    public bool _disposed;
    public int failedTicks;

    /// <summary>
    /// Initializes a new <see cref="OptimizedPrimitiveTrail"/> with a tip.
    /// </summary>
    public OptimizedPrimitiveTrail(ITrailTip tip, VertexWidthFunction widthFunction, VertexColorFunction colorFunction,
        VertexOffsetFunction offsetFunction = null, int maxTrailPoints = 1024)
    {
        if (Main.dedServ)
            return;

        _widthFunction = widthFunction ?? throw new ArgumentNullException(nameof(widthFunction));
        _colorFunction = colorFunction ?? throw new ArgumentNullException(nameof(colorFunction));
        _offsetFunction = offsetFunction;

        _maxTrailPoints = maxTrailPoints;
        _trailPointsBuffer = new SystemVector2[maxTrailPoints];
        _verticesBuffer = new Vertex2D[(maxTrailPoints) * 2 + tip.ExtraVertices];
        _indicesBuffer = new short[(maxTrailPoints - 1) * 6 + tip.ExtraIndices];
        _tipVerticesBuffer = new Vertex2D[tip.ExtraVertices];
        _tipIndicesBuffer = new short[tip.ExtraIndices];

        PrecomputeIndices(maxTrailPoints);
        Register();
    }

    /// <summary>
    /// Initializes a new <see cref="OptimizedPrimitiveTrail"/> without a tip.
    /// </summary>
    public OptimizedPrimitiveTrail(VertexWidthFunction widthFunction, VertexColorFunction colorFunction,
    VertexOffsetFunction offsetFunction = null, int maxTrailPoints = 1024)
    {
        if (Main.dedServ)
            return;

        _widthFunction = widthFunction ?? throw new ArgumentNullException(nameof(widthFunction));
        _colorFunction = colorFunction ?? throw new ArgumentNullException(nameof(colorFunction));
        _offsetFunction = offsetFunction;

        _maxTrailPoints = maxTrailPoints;
        _trailPointsBuffer = new SystemVector2[maxTrailPoints];
        _verticesBuffer = new Vertex2D[(maxTrailPoints) * 2];
        _indicesBuffer = new short[(maxTrailPoints - 1) * 6];

        PrecomputeIndices(maxTrailPoints);
        Register();
    }

    private void Register()
    {
        TrailManager.Instance.trails.Add(this);
        failedTicks = 10;
    }

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
    private void EnsureBuffers(int requiredTrailPoints, ITrailTip tip)
    {
        if (requiredTrailPoints > _trailPointsBuffer.Length)
        {
            _trailPointsBuffer = new SystemVector2[requiredTrailPoints];
            _verticesBuffer = new Vertex2D[(requiredTrailPoints) * 2 + tip.ExtraVertices];
            _indicesBuffer = new short[(requiredTrailPoints - 1) * 6 + tip.ExtraIndices];
            PrecomputeIndices(requiredTrailPoints);
        }
        else
        {
            // Sometimes one of these goes null for some reason despite the trail being correctly created in the constructor???
            // idk its wildly inconsistent to replicate the problem so this will do for now
            _verticesBuffer ??= new Vertex2D[(requiredTrailPoints) * 2 + tip.ExtraVertices];
            _indicesBuffer ??= new short[(requiredTrailPoints - 1) * 6 + tip.ExtraIndices];
            _tipVerticesBuffer ??= new Vertex2D[tip.ExtraVertices];
            _tipIndicesBuffer ??= new short[tip.ExtraIndices];
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
            float width = _widthFunction(completion);
            SystemVector2 offset = _offsetFunction != null ? _offsetFunction(completion) : SystemVector2.Zero;
            SystemVector2 trailPoint = trailPoints[i];

            vertices[i * 2] = new Vertex2D(
                trailPoint + offset + normal * width * 0.5f,
                _colorFunction(new SystemVector2(completion, 0), trailPoint.FromNumerics()),
                new SystemVector2(completion, 0)
            );
            vertices[i * 2 + 1] = new Vertex2D(
                trailPoint + offset - normal * width * 0.5f,
                _colorFunction(new SystemVector2(completion, 1), trailPoint.FromNumerics()),
                new SystemVector2(completion, 1)
            );
        }

        return trailPoints.Length * 2;
    }

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

        if (this == null)
            throw new NullReferenceException("This primitive trail cannot be null!");

        if (originalPositions == null || originalPositions.Length < 2 || Utility.ContainsInvalidPoint(originalPositions) || Utility.AllPointsEqual(originalPositions))
            return;

        int effectiveTotalPoints = totalTrailPoints > 0 ? totalTrailPoints : _maxTrailPoints;
        EnsureBuffers(effectiveTotalPoints, new NoTip());

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
        failedTicks = 10;

        // Restore render states
        device.RasterizerState = prevRasterizer;
        device.ScissorRectangle = prevScissor;
        device.BlendState = prevBlendState;
    }

    /// <summary>
    /// Directly draws a trail with a specified tip.
    /// </summary>
    /// 
    /// <inheritdoc cref="DrawTrail(ManagedShader, ReadOnlySpan{Vector2}, int, bool, bool)"></inheritdoc>
    /// <param name="tip">What tip to use</param>
    /// <param name="isPrepended">Is this trails positions added from the front?</param>
    /// <param name="positionOverride">Override where the tip is</param>
    /// <param name="directionOverride">Override where the tip is pointing</param>
    /// 
    /// <remarks>
    /// This method uses separate buffers for the tip to avoid artifacts from index merging.
    /// The performance impact is minimal due to the trail's optimization, but merging indices/vertices
    /// remains a challenge. If anyone has a solution let me know!
    /// </remarks>
    public void DrawTippedTrail(ManagedShader effect, ReadOnlySpan<Vector2> originalPositions, ITrailTip tip, bool isPrepended = false, int totalTrailPoints = -1, bool smooth = false, Vector2 directionOverride = default, Vector2 positionOverride = default, bool pixelated = true)
    {
        if (Main.dedServ)
            return;

        if (this == null)
            throw new NullReferenceException("This primitive trail cannot be null!");

        if (originalPositions == null || originalPositions.Length < 2 || Utility.ContainsInvalidPoint(originalPositions) || Utility.AllPointsEqual(originalPositions))
            return;

        int effectiveTotalPoints = totalTrailPoints > 0 ? totalTrailPoints : _maxTrailPoints;
        EnsureBuffers(effectiveTotalPoints, tip);

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

        // Calculate trail end and normal
        SystemVector2 effectiveTipPos = (positionOverride != default ? positionOverride : GetLastTrailPoint(originalPositions, isPrepended, tip, totalTrailPoints, smooth)).ToNumerics();

        SystemVector2 firstCenter = (_verticesBuffer[0].position + _verticesBuffer[1].position) / 2;
        SystemVector2 secondCenter = (_verticesBuffer[2].position + _verticesBuffer[3].position) / 2;
        SystemVector2 direction = isPrepended ? secondCenter.SafeDirectionTo(firstCenter) : firstCenter.SafeDirectionTo(secondCenter);

        // Generate tip geometry into its own buffers
        tip.GenerateMesh(effectiveTipPos, direction, 0, _tipVerticesBuffer.AsSpan(0, tip.ExtraVertices), _tipIndicesBuffer.AsSpan(0, tip.ExtraIndices), _widthFunction, _colorFunction);
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

        // Draw tip
        device.DrawUserIndexedPrimitives(
            PrimitiveType.TriangleList,
            _tipVerticesBuffer,
            0,
            tip.ExtraVertices,
            _tipIndicesBuffer,
            0,
            tip.ExtraIndices / 3
        );
        failedTicks = 10;

        // Restore render states
        device.RasterizerState = prevRasterizer;
        device.ScissorRectangle = prevScissor;
        device.BlendState = prevBlendState;
    }

    /// <summary>
    /// Retrieves the last (or first) trail point, optionally smoothed, for use with tips or other logic.
    /// </summary>
    public Vector2 GetLastTrailPoint(ReadOnlySpan<Vector2> positions, bool getFirstPoint = true, ITrailTip tip = null, int totalTrailPoints = -1, bool smooth = false)
    {
        int effectiveTotalPoints = totalTrailPoints > 0 ? totalTrailPoints : _maxTrailPoints;
        EnsureBuffers(effectiveTotalPoints, tip ?? new NoTip());

        Span<SystemVector2> convertedPositions = stackalloc SystemVector2[positions.Length];
        for (int i = 0; i < positions.Length; i++)
            convertedPositions[i] = positions[i].ToNumerics();

        Span<SystemVector2> tempPoints = stackalloc SystemVector2[effectiveTotalPoints];
        int pointCount = smooth
            ? GetSmoothTrailPoints(convertedPositions, tempPoints, effectiveTotalPoints)
            : GetLinearTrailPoints(convertedPositions, tempPoints, effectiveTotalPoints);

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
        if (!_disposed)
        {
            _indicesBuffer = null;
            _verticesBuffer = null;
            _tipVerticesBuffer = null;
            _tipIndicesBuffer = null;

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }

    ~OptimizedPrimitiveTrail()
    {
        if (!_disposed)
        {
            "An OptimizedPrimitiveTrail was not disposed properly, performing cleanup in finalizer".Warn();
            _indicesBuffer = null;
            _verticesBuffer = null;
            _tipVerticesBuffer = null;
            _tipIndicesBuffer = null;
        }
    }

    public override string ToString()
    {
        return $"Rendering {_trailPointsBuffer.Length} trail points, {_verticesBuffer} vertices, and {_indicesBuffer} indices. Is this trail disposed? {_disposed} \n" +
            $"{((_tipVerticesBuffer == null || _tipIndicesBuffer == null) ? "This trail has no tip" : $"Rendering {_tipVerticesBuffer.Length} tip vertices and {_tipIndicesBuffer.Length} tip indices")}";
    }
}