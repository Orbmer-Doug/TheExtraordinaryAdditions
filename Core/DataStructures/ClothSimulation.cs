using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Terraria;
using Terraria.Graphics;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Core.DataStructures;

// resulted from a 5 minute coding adventure
// Adapted from: https://pikuma.com/blog/verlet-integration-2d-cloth-physics-simulation
// Rebuilds vertices in a way that allows for tearing, though im unsure how to make an interesting application for it
// Especially given I cant seem to find a way to give it tile, slope, water, and entity collision effectively

public class ClothSimulation : IDisposable
{
    private record struct Point
    {
        public Vector2 Position { get; set; }
        public Vector2 PreviousPosition { get; set; }
        public Vector2 InitialPosition { get; set; }
        public bool IsPinned { get; set; }
        public List<int> StickIndices { get; init; }
        public bool IsSelected { get; set; }

        public Point(Vector2 position, bool isPinned)
        {
            Position = position;
            PreviousPosition = position;
            InitialPosition = position;
            IsPinned = isPinned;
            StickIndices = new List<int>(2);
            IsSelected = false;
        }
    }

    private record struct Stick(int P0Index, int P1Index, float Length)
    {
        public bool IsActive { get; set; } = true;
        public bool IsSelected { get; set; } = false;
    }

    private record struct Triangle(int V0, int V1, int V2, int[] StickIndices)
    {
        public bool IsActive { get; set; } = true;
    }

    private readonly List<Point> _points = new();
    private readonly List<Stick> _sticks = new();
    private readonly List<Triangle> _triangles = new();
    private readonly int _gridWidth;
    private readonly int _gridHeight;
    private readonly Vector2[] _initialOffsets;
    private short[] _indices;
    private Vector2 _mousePosition;
    private Vector2 _previousMousePosition;
    private bool _leftButtonDown;
    private bool _rightButtonDown;
    private float _cursorSize = 30f;
    private readonly float _hysteresisFactor = 1.1f;
    private static DynamicIndexBuffer Indices;
    private static DynamicVertexBuffer Vertices;

    public ClothSimulation(Vector2 initialPosition, int gridWidth, int gridHeight, float spacing)
    {
        _gridWidth = gridWidth;
        _gridHeight = gridHeight;

        // Initialize cloth grid and store initial offsets for pinned points
        _points.Capacity = gridWidth * gridHeight;
        _initialOffsets = new Vector2[gridWidth];
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                Vector2 pos = new Vector2(x * spacing, y * spacing);
                bool isPinned = y == 0;
                _points.Add(new Point(pos + initialPosition, isPinned));
                if (isPinned)
                {
                    _initialOffsets[x] = pos; // Offset relative to initialPosition
                }
            }
        }

        // Preallocate sticks and triangles lists
        _sticks.Capacity = (gridWidth - 1) * gridHeight + gridWidth * (gridHeight - 1);
        _triangles.Capacity = (gridWidth - 1) * (gridHeight - 1) * 2;

        // Create horizontal sticks and track stick indices
        Dictionary<(int, int), int> stickIndexMap = new();
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth - 1; x++)
            {
                int index = y * gridWidth + x;
                Stick stick = new Stick(index, index + 1, spacing);
                stickIndexMap[(index, index + 1)] = _sticks.Count;
                _sticks.Add(stick);
                _points[index].StickIndices.Add(_sticks.Count - 1);
                _points[index + 1].StickIndices.Add(_sticks.Count - 1);
            }
        }

        // Create vertical sticks
        for (int y = 0; y < gridHeight - 1; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                int index = y * gridWidth + x;
                Stick stick = new Stick(index, index + gridWidth, spacing);
                stickIndexMap[(index, index + gridWidth)] = _sticks.Count;
                _sticks.Add(stick);
                _points[index].StickIndices.Add(_sticks.Count - 1);
                _points[index + gridWidth].StickIndices.Add(_sticks.Count - 1);
            }
        }

        // Generate triangles and initial indices
        _indices = new short[(gridWidth - 1) * (gridHeight - 1) * 6];
        int indexCount = 0;
        for (int y = 0; y < gridHeight - 1; y++)
        {
            for (int x = 0; x < gridWidth - 1; x++)
            {
                int topLeft = y * gridWidth + x;
                int topRight = topLeft + 1;
                int bottomLeft = topLeft + gridWidth;
                int bottomRight = bottomLeft + 1;

                // First triangle (topLeft, bottomLeft, topRight)
                int[] stickIndices1 = new int[3];
                stickIndices1[0] = stickIndexMap.GetValueOrDefault((topLeft, bottomLeft), -1);
                stickIndices1[1] = stickIndexMap.GetValueOrDefault((bottomLeft, topRight), -1);
                stickIndices1[2] = stickIndexMap.GetValueOrDefault((topLeft, topRight), -1);
                _triangles.Add(new Triangle(topLeft, bottomLeft, topRight, stickIndices1));

                // Second triangle (topRight, bottomLeft, bottomRight)
                int[] stickIndices2 = new int[3];
                stickIndices2[0] = stickIndexMap.GetValueOrDefault((topRight, bottomLeft), -1);
                stickIndices2[1] = stickIndexMap.GetValueOrDefault((bottomLeft, bottomRight), -1);
                stickIndices2[2] = stickIndexMap.GetValueOrDefault((topRight, bottomRight), -1);
                _triangles.Add(new Triangle(topRight, bottomLeft, bottomRight, stickIndices2));

                // Initial indices
                _indices[indexCount++] = (short)topLeft;
                _indices[indexCount++] = (short)bottomLeft;
                _indices[indexCount++] = (short)topRight;
                _indices[indexCount++] = (short)topRight;
                _indices[indexCount++] = (short)bottomLeft;
                _indices[indexCount++] = (short)bottomRight;
            }
        }

        Main.QueueMainThreadAction(() =>
        {
            Indices = new(Main.instance.GraphicsDevice, IndexElementSize.SixteenBits, _indices.Length, BufferUsage.None);
            Vertices = new(Main.instance.GraphicsDevice, typeof(VertexPositionColorTexture), _gridWidth * _gridHeight, BufferUsage.WriteOnly);
        });
    }

    public void Update(Vector2 position, float radians = 0f, Vector2? pivot = null, Vector2? velocity = null)
    {
        Vector2 anchor = position; // Anchor is in world coordinates
        pivot ??= anchor;
        velocity ??= Vector2.Zero;

        float deltaTime = TimeSystem.LogicDeltaTime;
        float drag = 0.01f;
        Vector2 gravity = new Vector2(0, 980f); // 9.8m/s^2
        float elasticity = 10f;

        // Apply velocity to non-pinned points
        Parallel.For(0, _points.Count, i =>
        {
            Point point = _points[i];
            if (!point.IsPinned)
            {
                point.Position += velocity.Value;
                point.PreviousPosition += velocity.Value;
                _points[i] = point;
            }
        });

        // Update pinned points to follow anchor with rotation around pivot
        for (int x = 0; x < _gridWidth; x++)
        {
            int index = x; // Top row (y=0)
            Point point = _points[index];
            if (point.IsPinned)
            {
                // Rotate offset relative to pivot
                Vector2 offset = _initialOffsets[x];
                Vector2 relativeToPivot = offset - (pivot.Value - anchor);
                float cosTheta = MathF.Cos(radians);
                float sinTheta = MathF.Sin(radians);
                Vector2 rotatedOffset = new Vector2(
                    relativeToPivot.X * cosTheta - relativeToPivot.Y * sinTheta,
                    relativeToPivot.X * sinTheta + relativeToPivot.Y * cosTheta
                );
                point.InitialPosition = anchor + rotatedOffset + (pivot.Value - anchor);
                point.Position = point.InitialPosition;
                point.PreviousPosition = point.InitialPosition; // Prevent velocity
                _points[index] = point;
            }
        }

        // Update mouse input
        MouseState mouseState = Mouse.GetState();
        _previousMousePosition = _mousePosition;
        _mousePosition = new Vector2(mouseState.X, mouseState.Y) + Main.screenPosition;
        _leftButtonDown = mouseState.LeftButton == ButtonState.Pressed;
        _rightButtonDown = mouseState.RightButton == ButtonState.Pressed;

        // Reset selection states
        for (int i = 0; i < _points.Count; i++)
        {
            Point point = _points[i];
            point.IsSelected = false;
            _points[i] = point;
        }
        for (int i = 0; i < _sticks.Count; i++)
        {
            if (_sticks[i].IsActive)
            {
                _sticks[i] = _sticks[i] with { IsSelected = false };
            }
        }

        // Update points in parallel
        Parallel.For(0, _points.Count, i =>
        {
            Point point = _points[i];
            if (point.IsPinned)
                return;

            // Check if point is selected by mouse with hysteresis
            Vector2 mouseDir = point.Position - _mousePosition;
            float distSquared = mouseDir.LengthSquared();
            float threshold = point.IsSelected ? _cursorSize * _cursorSize * _hysteresisFactor : _cursorSize * _cursorSize;
            point.IsSelected = distSquared < threshold;

            // Update sticks selection state if either point is selected
            foreach (int stickIndex in point.StickIndices)
            {
                if (_sticks[stickIndex].IsActive)
                {
                    lock (_sticks)
                    {
                        _sticks[stickIndex] = _sticks[stickIndex] with { IsSelected = true };
                    }
                }
            }

            // Handle mouse dragging
            if (_leftButtonDown && point.IsSelected && !_previousMousePosition.Equals(_mousePosition))
            {
                Vector2 difference = _mousePosition - _previousMousePosition;
                difference.X = MathHelper.Clamp(difference.X, -elasticity, elasticity);
                difference.Y = MathHelper.Clamp(difference.Y, -elasticity, elasticity);
                point.PreviousPosition = point.Position - difference;
            }

            Player p = Main.LocalPlayer;
            if (!point.IsPinned && p.Hitbox.Intersects(_points[i].Position.ToRectangle(4, 4)))
            {
                Vector2 difference = p.position - p.oldPosition;
                difference.X = MathHelper.Clamp(difference.X, -elasticity, elasticity);
                difference.Y = MathHelper.Clamp(difference.Y, -elasticity, elasticity);
                point.PreviousPosition = point.Position - difference;
            }

            // Handle tearing and update triangle activity
            if (_rightButtonDown && point.IsSelected)
            {
                foreach (int stickIndex in point.StickIndices)
                {
                    if (_sticks[stickIndex].IsActive)
                    {
                        lock (_sticks)
                        {
                            _sticks[stickIndex] = _sticks[stickIndex] with { IsActive = false };
                        }
                        lock (_triangles)
                        {
                            for (int t = 0; t < _triangles.Count; t++)
                            {
                                Triangle triangle = _triangles[t];
                                if (triangle.IsActive && triangle.StickIndices.Contains(stickIndex))
                                {
                                    _triangles[t] = triangle with { IsActive = false };
                                }
                            }
                        }
                    }
                }
            }

            // Verlet integration
            Vector2 newPos = point.Position + (point.Position - point.PreviousPosition) * (1f - drag) +
                         gravity * (1f - drag) * deltaTime * deltaTime;
            point.PreviousPosition = point.Position;
            point.Position = newPos;

            _points[i] = point;
        });

        // Update sticks (constraints) sequentially
        for (int i = 0; i < _sticks.Count; i++)
        {
            Stick stick = _sticks[i];
            if (!stick.IsActive)
                continue;

            Point p0 = _points[stick.P0Index];
            Point p1 = _points[stick.P1Index];
            Vector2 diff = p0.Position - p1.Position;
            float dist = diff.Length();
            float diffFactor = dist > 0 ? (stick.Length - dist) / dist : 0f;
            Vector2 offset = diff * diffFactor * 0.5f;

            if (!p0.IsPinned)
            {
                p0.Position += offset;
                _points[stick.P0Index] = p0;
            }
            if (!p1.IsPinned)
            {
                p1.Position -= offset;
                _points[stick.P1Index] = p1;
            }
        }
    }

    public void Draw(Texture2D texture)
    {
        void draw()
        {
            // Generate vertices
            VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[_points.Count];
            for (int i = 0; i < _points.Count; i++)
            {
                Point point = _points[i];
                vertices[i] = new VertexPositionColorTexture
                {
                    Position = new Vector3(point.Position, 0f),
                    Color = point.IsSelected ? Color.Red : Color.White,
                    TextureCoordinate = new Vector2((float)(i % _gridWidth) / (_gridWidth - 1), (float)(i / _gridWidth) / (_gridHeight - 1))
                };
            }

            // Rebuild indices from active triangles
            List<short> activeIndices = new List<short>(_triangles.Count * 3);
            foreach (Triangle triangle in _triangles)
            {
                if (triangle.IsActive)
                {
                    activeIndices.Add((short)triangle.V0);
                    activeIndices.Add((short)triangle.V1);
                    activeIndices.Add((short)triangle.V2);
                }
            }
            _indices = activeIndices.ToArray();

            if (_indices.Length <= 0)
                return;

            Indices.SetData(_indices);
            Vertices.SetData(vertices);

            GraphicsDevice gd = Main.instance.GraphicsDevice;

            UpdatePixelatedBaseEffect(out Matrix offset, out Matrix projection, out Matrix view);

            ManagedShader clothShader = AssetRegistry.GetShader("ClothShader");
            clothShader.TrySetParameter("size", new Vector2(_gridWidth, _gridHeight));
            clothShader.TrySetParameter("transformMatrix", offset * view * projection);
            clothShader.SetTexture(texture, 1, SamplerState.PointClamp);
            clothShader.Effect.CurrentTechnique.Passes[ManagedShader.DefaultPassName].Apply();

            RasterizerState prevRasterizer = gd.RasterizerState;
            BlendState prevBlend = gd.BlendState;
            gd.RasterizerState = RasterizerState.CullNone;
            gd.BlendState = BlendState.AlphaBlend;
            gd.SetVertexBuffer(Vertices);
            gd.Indices = Indices;
            gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertices.Length, 0, _indices.Length / 3);
            gd.RasterizerState = prevRasterizer;
            gd.BlendState = prevBlend;
            gd.SetVertexBuffer(null);
            gd.Indices = null;
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderPlayers);
    }

    private bool _disposed;
    public void Dispose()
    {
        if (_disposed)
            return;

        Main.QueueMainThreadAction(() =>
        {
            Indices?.Dispose();
            Vertices?.Dispose();
        });

        _disposed = true;
    }
}

public class ClothSimulationTiles : IDisposable
{
    public record struct Point
    {
        public Vector2 Position { get; set; }
        public Vector2 PreviousPosition { get; set; }
        public Vector2 InitialPosition { get; set; }
        public bool IsPinned { get; set; }
        public List<int> StickIndices { get; init; }

        public Point(Vector2 position, bool isPinned)
        {
            Position = position;
            PreviousPosition = position;
            InitialPosition = position;
            IsPinned = isPinned;
            StickIndices = new List<int>(2);
        }
    }

    public record struct Stick(int P0Index, int P1Index, float Length)
    {
        public bool IsActive { get; set; } = true;
    }

    private record struct Triangle(int V0, int V1, int V2, int[] StickIndices)
    {
        public bool IsActive { get; set; } = true;
    }

    public readonly List<Point> Points = new();
    public readonly List<Stick> Sticks = new();
    private readonly List<Triangle> _triangles = new();
    public readonly int GridWidth;
    public readonly int GridHeight;
    public float Spacing;
    public Vector2 Position;
    private short[] _indices;
    private static DynamicIndexBuffer Indices;
    private static DynamicVertexBuffer Vertices;

    public ClothSimulationTiles(int gridWidth, int gridHeight, float spacing)
    {
        if (Main.dedServ)
            return;

        GridWidth = gridWidth;
        GridHeight = gridHeight;
        Spacing = spacing;

        // Initialize cloth grid
        Points.Capacity = gridWidth * gridHeight;
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                Vector2 pos = new Vector2(x * Spacing * 0.8f, y * Spacing);
                bool isPinned = x == 0; // Pin to left edge
                Points.Add(new Point(pos, isPinned));
            }
        }

        // Preallocate sticks and triangles
        Sticks.Capacity = (gridWidth - 1) * gridHeight + gridWidth * (gridHeight - 1);
        _triangles.Capacity = (gridWidth - 1) * (gridHeight - 1) * 2;

        // Create horizontal sticks
        Dictionary<(int, int), int> stickIndexMap = new();
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth - 1; x++)
            {
                int index = y * gridWidth + x;
                Stick stick = new Stick(index, index + 1, Spacing * 0.8f);
                stickIndexMap[(index, index + 1)] = Sticks.Count;
                Sticks.Add(stick);
                Points[index].StickIndices.Add(Sticks.Count - 1);
                Points[index + 1].StickIndices.Add(Sticks.Count - 1);
            }
        }

        // Create vertical sticks
        for (int y = 0; y < gridHeight - 1; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                int index = y * gridWidth + x;
                Stick stick = new Stick(index, index + gridWidth, Spacing);
                stickIndexMap[(index, index + gridWidth)] = Sticks.Count;
                Sticks.Add(stick);
                Points[index].StickIndices.Add(Sticks.Count - 1);
                Points[index + gridWidth].StickIndices.Add(Sticks.Count - 1);
            }
        }

        // Generate triangles and initial indices
        _indices = new short[(gridWidth - 1) * (gridHeight - 1) * 6];
        int indexCount = 0;
        for (int y = 0; y < gridHeight - 1; y++)
        {
            for (int x = 0; x < gridWidth - 1; x++)
            {
                int topLeft = y * gridWidth + x;
                int topRight = topLeft + 1;
                int bottomLeft = topLeft + gridWidth;
                int bottomRight = bottomLeft + 1;

                int[] stickIndices1 = new int[3];
                stickIndices1[0] = stickIndexMap.GetValueOrDefault((topLeft, bottomLeft), -1);
                stickIndices1[1] = stickIndexMap.GetValueOrDefault((bottomLeft, topRight), -1);
                stickIndices1[2] = stickIndexMap.GetValueOrDefault((topLeft, topRight), -1);
                _triangles.Add(new Triangle(topLeft, bottomLeft, topRight, stickIndices1));

                int[] stickIndices2 = new int[3];
                stickIndices2[0] = stickIndexMap.GetValueOrDefault((topRight, bottomLeft), -1);
                stickIndices2[1] = stickIndexMap.GetValueOrDefault((bottomLeft, bottomRight), -1);
                stickIndices2[2] = stickIndexMap.GetValueOrDefault((topRight, bottomRight), -1);
                _triangles.Add(new Triangle(topRight, bottomLeft, bottomRight, stickIndices2));

                _indices[indexCount++] = (short)topLeft;
                _indices[indexCount++] = (short)bottomLeft;
                _indices[indexCount++] = (short)topRight;
                _indices[indexCount++] = (short)topRight;
                _indices[indexCount++] = (short)bottomLeft;
                _indices[indexCount++] = (short)bottomRight;
            }
        }

        Main.QueueMainThreadAction(() =>
        {
            Indices = new(Main.instance.GraphicsDevice, IndexElementSize.SixteenBits, _indices.Length, BufferUsage.None);
            Vertices = new(Main.instance.GraphicsDevice, typeof(VertexPositionColorTexture), GridWidth * GridHeight, BufferUsage.WriteOnly);
        });
    }

    public void Update(Vector2 entityPos, float radians = 0f, Vector2? pivot = null, Vector2? velocity = null)
    {
        Vector2 anchor = Position;
        pivot ??= anchor;
        velocity ??= Vector2.Zero;

        float deltaTime = TimeSystem.LogicDeltaTime;
        float drag = 0.01f;

        // Apply velocity and offset to all points
        Parallel.For(0, Points.Count, i =>
        {
            Point point = Points[i];
            Vector2 localPos = point.Position - anchor; // Relative to anchor
            if (!point.IsPinned)
            {
                point.Position = localPos + velocity.Value + anchor;
                point.PreviousPosition = localPos + velocity.Value + anchor;
            }
            else
            {
                point.Position = anchor + localPos; // Keep pinned points at anchor
                point.PreviousPosition = point.Position;
            }
            Points[i] = point;
        });

        // Verlet integration
        Parallel.For(0, Points.Count, i =>
        {
            Point point = Points[i];
            if (point.IsPinned)
                return;

            foreach (Player p in Main.ActivePlayers)
            {
                if (p.Hitbox.Intersects((point.Position + entityPos).ToRectangle((int)Spacing, (int)Spacing)))
                {
                    Vector2 difference = p.position - p.oldPosition;
                    difference.X = MathHelper.Clamp(difference.X, -2f, 2f);
                    difference.Y = MathHelper.Clamp(difference.Y, -2f, 2f);
                    point.PreviousPosition = point.Position - difference;
                }
            }

            float interpol = 120f * MathHelper.Clamp(MathF.Abs(Main.windSpeedCurrent), 0f, 1f);
            Vector2 windForce = new Vector2(
                x: ((MathF.Cos(Main.GlobalTimeWrappedHourly * 25f + point.Position.X * 0.4f + point.Position.Y * 0.09f) * 0.5f + 0.076f)
                + (Main.windSpeedCurrent * 9f * (float)Main.dayRate)) * interpol * .5f,
                 y: MathF.Cos(Main.GlobalTimeWrappedHourly * -4.6f + point.Position.X * 0.03f) * interpol * 9f);

            Vector2 localPos = point.Position - anchor;
            Vector2 newLocalPos = localPos + (localPos - (point.PreviousPosition - anchor)) * (1f - drag) +
                                windForce * (1f - drag) * deltaTime * deltaTime;
            point.PreviousPosition = point.Position;
            point.Position = newLocalPos + anchor;

            Points[i] = point;
        });

        // Update constraints
        for (int iter = 0; iter < 1; iter++)
        {
            for (int i = 0; i < Sticks.Count; i++)
            {
                Stick stick = Sticks[i];
                if (!stick.IsActive)
                    continue;

                Point p0 = Points[stick.P0Index];
                Point p1 = Points[stick.P1Index];
                Vector2 diff = (p0.Position - anchor) - (p1.Position - anchor); // Local difference
                float dist = diff.Length();
                float diffFactor = dist > 0 ? (stick.Length - dist) / dist : 0f;
                Vector2 offset = diff * diffFactor * 0.5f;

                if (!p0.IsPinned)
                {
                    p0.Position = (p0.Position - anchor) + offset + anchor;
                    Points[stick.P0Index] = p0;
                }
                if (!p1.IsPinned)
                {
                    p1.Position = (p1.Position - anchor) - offset + anchor;
                    Points[stick.P1Index] = p1;
                }
            }
        }
    }

    public void Draw(Texture2D texture, Vector2 drawOffset, VertexColors colors)
    {
        if (Main.dedServ)
            return;

        Position = drawOffset;

        // Generate vertices in world coordinates
        VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[Points.Count];
        for (int i = 0; i < Points.Count; i++)
        {
            Point point = Points[i];
            int x = i % GridWidth;
            int y = i / GridWidth;

            Color colorTop = Color.Lerp(colors.TopLeftColor, colors.TopRightColor, x / (float)GridWidth);
            Color colorBottom = Color.Lerp(colors.BottomLeftColor, colors.BottomRightColor, x / (float)GridWidth);

            vertices[i] = new VertexPositionColorTexture
            {
                Position = new Vector3(point.Position.X, point.Position.Y, 0f),
                Color = Color.Lerp(colorTop, colorBottom, y / (float)GridHeight),
                TextureCoordinate = new Vector2((float)x / (GridWidth - 1), (float)y / (GridHeight - 1))
            };
        }

        // Rebuild indices from active triangles
        List<short> activeIndices = new List<short>(_triangles.Count * 3);
        foreach (Triangle triangle in _triangles)
        {
            if (triangle.IsActive)
            {
                activeIndices.Add((short)triangle.V0);
                activeIndices.Add((short)triangle.V1);
                activeIndices.Add((short)triangle.V2);
            }
        }
        _indices = activeIndices.ToArray();

        if (_indices.Length <= 0)
            return;

        Indices.SetData(_indices);
        Vertices.SetData(vertices);

        GraphicsDevice gd = Main.instance.GraphicsDevice;

        Matrix world = Matrix.CreateTranslation(drawOffset.X, drawOffset.Y, 0f);
        Matrix projection = Matrix.CreateOrthographicOffCenter(0f, Main.instance.GraphicsDevice.Viewport.Width, Main.instance.GraphicsDevice.Viewport.Height, 0f, -1f, 1f);

        ManagedShader clothShader = AssetRegistry.GetShader("ClothShader");
        clothShader.TrySetParameter("size", new Vector2(GridWidth / 2, GridHeight));
        clothShader.TrySetParameter("transformMatrix", world * projection);
        clothShader.SetTexture(texture, 1, SamplerState.PointClamp);
        clothShader.Effect.CurrentTechnique.Passes[ManagedShader.DefaultPassName].Apply();

        RasterizerState prevRasterizer = gd.RasterizerState;
        BlendState prevBlend = gd.BlendState;
        Viewport prevViewport = gd.Viewport;
        Rectangle prevScissor = gd.ScissorRectangle;
        gd.RasterizerState = RasterizerState.CullNone;
        gd.BlendState = BlendState.AlphaBlend;
        gd.SetVertexBuffer(Vertices);
        gd.Indices = Indices;
        gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertices.Length, 0, _indices.Length / 3);
        gd.RasterizerState = prevRasterizer;
        gd.BlendState = prevBlend;
        gd.Viewport = prevViewport;
        gd.ScissorRectangle = prevScissor;
        gd.SetVertexBuffer(null);
        gd.Indices = null;
    }

    private bool _disposed;
    public void Dispose()
    {
        if (_disposed)
            return;

        Main.QueueMainThreadAction(() =>
        {
            Indices?.Dispose();
            Vertices?.Dispose();
        });

        _disposed = true;
    }
}

/* Wireframe
foreach (Stick stick in _sticks)
{
    if (!stick.IsActive)
        continue;
    SystemVector2 start = _points[stick.P0Index].Position;
    SystemVector2 end = _points[stick.P1Index].Position;
    Color color = stick.IsSelected ? Color.Red : Color.Black;
    SystemVector2 direction = end - start;
    float length = direction.Length();
    if (length > 0)
    {
        float angle = direction.ToRotation();
        Main.spriteBatch.Draw(AssetRegistry.GetTexture(AdditionsTexture.Pixel), start, null, color, angle, SystemVector2.Zero,
                         new SystemVector2(length, 1f), SpriteEffects.None, 0f);
    }
}
*/

/* Test Projectile
public class qwerFlag : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 50000;
    }
    public override void SetDefaults()
    {
        Projectile.timeLeft = int.MaxValue;
        Projectile.Size = new(100);
        Projectile.hostile = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
    }

    public ClothSimulation sim;
    public override void AI()
    {
        Projectile.Center.SuperQuickDust();
        Projectile.Center = Main.LocalPlayer.Center - Vector2.UnitY * 200f;
        Projectile.velocity = Vector2.Zero;
        Vector2 target = new(Main.spawnTileX * 16, Main.spawnTileY * 16 - 500);

        if (sim == null)
        {
            sim = new(target, 40, 40, 12f);
        }

        sim?.Update(target, 0f);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        sim?.Draw(AssetRegistry.GetTexture(AdditionsTexture.Background_CloudedCrater));
        return false;
    }
}
*/