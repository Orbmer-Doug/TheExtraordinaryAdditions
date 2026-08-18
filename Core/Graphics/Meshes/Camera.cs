using System;
using Terraria;

namespace TheExtraordinaryAdditions.Core.Graphics.Meshes;

/// <summary>
/// Defines an orbital camera for usage in viewing meshes
/// </summary>
public struct Camera()
{
    public const float OrbitSpeed = 0.01f;

    public Vector3 Target = new Vector3(0.0f, 0.0f, 0.0f);
    public float Radius = 50.0f;
    public float Azimuth = 0.0f;
    public float Elevation = MathHelper.PiOver2;
    public bool Dragging = false;
    public float LastX = 0f, LastY = 0f;

    public Vector3 Position()
    {
        // Clamp to prevent gimbal lock
        float elevation = MathHelper.Clamp(Elevation, 0.01f, MathHelper.Pi - 0.01f);
        return new Vector3(
            Radius * MathF.Sin(elevation) * MathF.Cos(Azimuth),
            Radius * MathF.Cos(elevation),
            Radius * MathF.Sin(elevation) * MathF.Sin(Azimuth)
        );
    }

    public void Update()
    {
        Target = new Vector3(0.0f, 0.0f, 0.0f);
    }

    public void ProcessMouseMove(float x, float y)
    {
        float dx = x - LastX;
        float dy = y - LastY;
        if (Dragging)
        {
            Azimuth += dx * OrbitSpeed;
            Elevation -= dy * OrbitSpeed;
            Elevation = MathHelper.Clamp(Elevation, 0.01f, MathHelper.Pi - 0.01f);
        }

        LastX = x;
        LastY = y;
        Update();
    }

    public void ProcessMouseButton()
    {
        Dragging = Main.mouseRight;
    }

    public void ProcessScroll(float yoffset)
    {
        Radius -= yoffset * 2f;
        if (Radius < 1.0f)
            Radius = 1.0f;
        Update();
    }
}
