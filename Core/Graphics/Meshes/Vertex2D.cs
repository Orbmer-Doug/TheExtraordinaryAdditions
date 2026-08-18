using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;

namespace TheExtraordinaryAdditions.Core.Graphics.Meshes;

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

    VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

    public readonly SystemVector2 position = position;
    public readonly Color color = color;
    public readonly SystemVector2 texCoord = texCoord;

    public override string ToString()
    {
        return $"[Position at: {position}, Colored with: {color}, Coord of :{texCoord}]";
    }
}
