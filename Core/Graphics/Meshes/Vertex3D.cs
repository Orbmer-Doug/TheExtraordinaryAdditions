using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;

namespace TheExtraordinaryAdditions.Core.Graphics.Meshes;

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct Vertex3D(SystemVector3 position, Color color, SystemVector2 texCoord) : IVertexType
{
    public static readonly VertexDeclaration VertexDeclaration3D = new(
    [
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0),
        new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
    ]);

    public static readonly VertexDeclaration VertexDeclaration = VertexDeclaration3D;

    VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

    public readonly SystemVector3 position = position;
    public readonly Color color = color;
    public readonly SystemVector2 texCoord = texCoord;

    public override string ToString() =>
        $"[Position at: {position}, Colored with: {color}, Coord of: {texCoord}]";
}

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct Vertex3DTex(SystemVector3 position, Color color, SystemVector3 texCoord) : IVertexType
{
    public static readonly VertexDeclaration VertexDeclaration3D = new(
    [
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0),
        new VertexElement(16, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0)
    ]);

    public static readonly VertexDeclaration VertexDeclaration = VertexDeclaration3D;

    VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

    public readonly SystemVector3 position = position;
    public readonly Color color = color;
    public readonly SystemVector3 texCoord = texCoord;

    public override string ToString() =>
        $"[Position at: {position}, Colored with: {color}, Coord of: {texCoord}]";
}
