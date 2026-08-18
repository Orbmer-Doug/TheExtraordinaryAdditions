using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;

namespace TheExtraordinaryAdditions.Core.Graphics.Meshes;

/// <summary>
/// A readonly struct representing a 3D vertex optimized for primitive meshes.
/// </summary>
[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct VertexPositionColor3D(Vector3 position, Color color) : IVertexType
{
    public static readonly VertexDeclaration VertexDeclaration3D = new(
    [
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0),
    ]);

    public static readonly VertexDeclaration VertexDeclaration = VertexDeclaration3D;

    VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

    public readonly Vector3 Position = position;
    public readonly Color Color = color;
}

/// <summary>
/// Per-instance data uploaded to the GPU each frame.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct InstancePositionColor3D(Vector3 position, Color color)
{
    public static readonly VertexDeclaration Declaration = new(
    [
        // TEXCOORD1 in the shader - world-space
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 1),
        // COLOR1 in the shader
        new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 1),
    ]);

    public readonly Vector3 Position = position;
    public readonly Color Color = color;
}
