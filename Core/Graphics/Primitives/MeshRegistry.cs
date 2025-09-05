using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace TheExtraordinaryAdditions.Core.Graphics.Primitives;

// TODO: either asterlin uses it or we kablooey
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct VertexPositionColorNormalTexture(Vector3 position, Color color, Vector2 textureCoordinates, Vector3 normal) : IVertexType
{
    VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

    /// <summary>
    /// The position of this vertex.
    /// </summary>
    public readonly Vector3 Position = position;

    /// <summary>
    /// The color of this vertex.
    /// </summary>
    public readonly Color Color = color;

    /// <summary>
    /// The texture coordinate associated with the vertex.
    /// </summary>
    public readonly Vector2 TextureCoords = textureCoordinates;

    /// <summary>
    /// The normal vector of this vertex.
    /// </summary>
    public readonly Vector3 Normal = normal;

    /// <summary>
    /// The vertex's unmanaged declaration.
    /// </summary>
    public static readonly VertexDeclaration VertexDeclaration;

    static VertexPositionColorNormalTexture()
    {
        VertexDeclaration = new VertexDeclaration(
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0),
            new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(24, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0)
        );
    }
}

public class MeshRegistry
{
    private static Dictionary<string, MeshData> _meshes = [];

    /// <summary>
    /// Registers a cylinder mesh with the given parameters.
    /// </summary>
    public static void RegisterCylinder(string key, float radius, float height, int segments, Color color)
    {
        GenerateCylinder(radius, height, segments, color, out var vertices, out var indices);
        _meshes[key] = new MeshData(vertices, indices);
    }

    /// <summary>
    /// Retrieves a mesh by key. Returns true if found, false otherwise.
    /// </summary>
    public static bool TryGetMesh(string key, out MeshData mesh)
    {
        return _meshes.TryGetValue(key, out mesh);
    }

    /// <summary>
    /// Clears the mesh registry.
    /// </summary>
    public static void Clear()
    {
        _meshes.Clear();
    }


    /// <summary>
    /// Generates a cylindrical mesh with customizable radius, height, and number of segments.
    /// </summary>
    /// <param name="radius">The radius of the cylinder (half the width).</param>
    /// <param name="height">The height of the cylinder.</param>
    /// <param name="segments">Number of segments around the cylinder's circumference (minimum 3).</param>
    /// <param name="color">The color of the vertices (default is white).</param>
    /// <param name="vertices">Output array of vertices for the cylinder.</param>
    /// <param name="indices">Output array of indices defining the triangles.</param>
    public static void GenerateCylinder(float radius, float height, int segments, Color color,
        out VertexPositionColorNormalTexture[] vertices, out short[] indices)
    {
        // Ensure segments is at least 3 to form a valid cylinder
        segments = Math.Max(3, segments);

        // Total vertices:
        // - segments for the top circle
        // - segments for the bottom circle
        // - 1 center vertex for the top cap
        // - 1 center vertex for the bottom cap
        int vertexCount = segments * 2 + 2;

        // Total triangles:
        // - Side: segments quads, each quad = 2 triangles (segments * 2 triangles)
        // - Top cap: segments triangles
        // - Bottom cap: segments triangles
        int triangleCount = (segments * 2) + segments + segments;
        int indexCount = triangleCount * 3;

        vertices = new VertexPositionColorNormalTexture[vertexCount];
        indices = new short[indexCount];

        // Step 1: Generate vertices for the top and bottom circles
        float halfHeight = height / 2f;
        int vertexIndex = 0;

        // Top circle vertices (at y = +halfHeight)
        for (int i = 0; i < segments; i++)
        {
            float angle = i * MathHelper.TwoPi / segments;
            float x = radius * MathF.Cos(angle);
            float z = radius * MathF.Sin(angle);
            Vector3 position = new Vector3(x, halfHeight, z);
            Vector3 normal = new Vector3(x / radius, 0, z / radius); // Normal points outward for the side
            Vector2 texCoord = new Vector2((float)i / segments, 0); // UV: u = angle, v = 0 at top

            vertices[vertexIndex] = new VertexPositionColorNormalTexture(position, color, texCoord, normal);
            vertexIndex++;
        }

        // Bottom circle vertices (at y = -halfHeight)
        for (int i = 0; i < segments; i++)
        {
            float angle = i * MathHelper.TwoPi / segments;
            float x = radius * MathF.Cos(angle);
            float z = radius * MathF.Sin(angle);
            Vector3 position = new Vector3(x, -halfHeight, z);
            Vector3 normal = new Vector3(x / radius, 0, z / radius); // Normal points outward for the side
            Vector2 texCoord = new Vector2((float)i / segments, 1); // UV: u = angle, v = 1 at bottom

            vertices[vertexIndex] = new VertexPositionColorNormalTexture(position, color, texCoord, normal);
            vertexIndex++;
        }

        // Top cap center vertex
        vertices[vertexIndex] = new VertexPositionColorNormalTexture(
            new Vector3(0, halfHeight, 0),
            color,
            new Vector2(0.5f, 0.5f), // Center of texture
            Vector3.Up // Normal points up for the top cap
        );
        int topCapCenterIndex = vertexIndex;
        vertexIndex++;

        // Bottom cap center vertex
        vertices[vertexIndex] = new VertexPositionColorNormalTexture(
            new Vector3(0, -halfHeight, 0),
            color,
            new Vector2(0.5f, 0.5f), // Center of texture
            Vector3.Down // Normal points down for the bottom cap
        );
        int bottomCapCenterIndex = vertexIndex;

        // Step 2: Generate indices for the side, top cap, and bottom cap
        int index = 0;

        // Side of the cylinder (quads between top and bottom circles)
        for (int i = 0; i < segments; i++)
        {
            int topLeft = i;
            int topRight = (i + 1) % segments;
            int bottomLeft = i + segments;
            int bottomRight = (i + 1) % segments + segments;

            // First triangle: topLeft -> bottomLeft -> topRight
            indices[index++] = (short)topLeft;
            indices[index++] = (short)bottomLeft;
            indices[index++] = (short)topRight;

            // Second triangle: topRight -> bottomLeft -> bottomRight
            indices[index++] = (short)topRight;
            indices[index++] = (short)bottomLeft;
            indices[index++] = (short)bottomRight;
        }

        // Top cap (fan of triangles from center to top circle)
        for (int i = 0; i < segments; i++)
        {
            int topVertex = i;
            int nextTopVertex = (i + 1) % segments;

            indices[index++] = (short)topCapCenterIndex;
            indices[index++] = (short)topVertex;
            indices[index++] = (short)nextTopVertex;
        }

        // Bottom cap (fan of triangles from center to bottom circle)
        for (int i = 0; i < segments; i++)
        {
            int bottomVertex = i + segments;
            int nextBottomVertex = (i + 1) % segments + segments;

            // Reverse winding order for bottom cap (since normal points down)
            indices[index++] = (short)bottomCapCenterIndex;
            indices[index++] = (short)nextBottomVertex;
            indices[index++] = (short)bottomVertex;
        }
    }

}

/// <summary>
/// A struct to hold a mesh's vertices and indices for easy storage in a registry.
/// </summary>
public struct MeshData(VertexPositionColorNormalTexture[] vertices, short[] indices)
{
    public VertexPositionColorNormalTexture[] Vertices = vertices;
    public short[] Indices = indices;
}