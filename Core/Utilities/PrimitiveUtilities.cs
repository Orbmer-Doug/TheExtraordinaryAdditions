using Microsoft.Xna.Framework.Graphics;
using Terraria;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using static Microsoft.Xna.Framework.MathHelper;

namespace TheExtraordinaryAdditions.Core.Utilities;

// Reminders: Attempt to refrain from
// branching in shaders,
// not disposing properly,
// recreating render targets (each frame),
// and unnecessary sb restarts (commonly each frame)
public static partial class Utility
{
    public static void LogDeviceInfo(this GraphicsDevice g)
    {
        ($"BlendFactor: {g.BlendFactor}" +
            $" BlendState: {g.BlendState}" +
            $" DepthStencilState: {g.DepthStencilState}" +
            $" DisplayMode: {g.DisplayMode}" +
            $" GraphicsDeviceStatus: {g.GraphicsDeviceStatus}" +
            $" GraphicsProfile: {g.GraphicsProfile}" +
            $" IsDisposed: {g.IsDisposed}" +
            $" RasterizerState: {g.RasterizerState}" +
            $" ScissorRectangle: {g.ScissorRectangle}" +
            $" Viewport: {g.Viewport}" +
            $" Exists? {g == null}").Log();
    }

    public static void UpdateBaseEffect(out Matrix effectWorld, out Matrix effectProjection, out Matrix effectView)
    {
        // Screen bounds.
        int height = Main.instance.GraphicsDevice.Viewport.Height;

        Vector2 zoom = Main.GameViewMatrix.Zoom;
        Matrix zoomScaleMatrix = Matrix.CreateScale(zoom.X, zoom.Y, 1f);

        // Get a matrix that aims towards the Z axis (these calculations are relative to a 2D world).
        effectView = Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.Up);

        // Offset the matrix to the appropriate position.
        effectView *= Matrix.CreateTranslation(0f, -height, 0f);

        // Flip the matrix around 180 degrees.
        effectView *= Matrix.CreateRotationZ(Pi);

        // Account for the inverted gravity effect.
        if (Main.LocalPlayer.gravDir == -1f)
            effectView *= Matrix.CreateScale(1f, -1f, 1f) * Matrix.CreateTranslation(0f, height, 0f);

        // And account for the current zoom.
        effectView *= zoomScaleMatrix;

        effectProjection = Matrix.CreateOrthographicOffCenter(0f, Main.screenWidth * zoom.X, 0f, Main.screenHeight * zoom.Y, 0f, 1f) * zoomScaleMatrix;
        effectWorld = Matrix.CreateTranslation(new Vector3(-Main.screenPosition, 0));
    }

    public static void UpdatePixelatedBaseEffect(out Matrix effectWorld, out Matrix effectProjection, out Matrix effectView)
    {
        effectWorld = Matrix.CreateTranslation(new Vector3(-Main.screenPosition, 0));
        effectProjection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1);
        effectView = Matrix.Identity;
    }

    public static readonly short[] TextureQuadIndices = [0, 1, 2, 2, 3, 0];
    public static Matrix CalculateTextureMatrix(Vector2 center, Quaternion quaternion, float size, float startRot = 0f, int horizontalDir = 1)
    {
        Matrix translation = Matrix.CreateTranslation(new Vector3(center.X - Main.screenPosition.X, center.Y - Main.screenPosition.Y, 0f));
        Matrix projection = Matrix.CreateOrthographicOffCenter(0f, Main.screenWidth, Main.screenHeight, 0f, -450f, 450f);
        Matrix view = translation * Main.GameViewMatrix.TransformationMatrix * projection;

        Matrix rotation = Matrix.CreateFromQuaternion(quaternion) * Matrix.CreateRotationZ(startRot);
        Matrix scale = Matrix.CreateScale(size);

        Matrix vertexMatrix = rotation * scale * view;
        if (horizontalDir == -1f)
            vertexMatrix = Matrix.CreateReflection(new Plane(Vector3.UnitX, 1f)) * Matrix.CreateRotationZ(PiOver2) * vertexMatrix;

        return vertexMatrix;
    }

    public static Matrix Calculate3DPrimitiveMatrix(Vector2 center, Quaternion quaternion, float size, float startRot = 0f, int horizontalDir = 1)
    {
        Vector2 zoom = Main.GameViewMatrix.Zoom;
        Matrix zoomScaleMatrix = Matrix.CreateScale(zoom.X, zoom.Y, 1f);

        Vector2 screenPos;

        // Convert world center to screen space, adjusting for gravity
        if (Main.LocalPlayer.gravDir == 1f)
        {
            // Normal gravity: Convert world to screen space directly
            screenPos = center - Main.screenPosition;
        }
        else
        {
            // Flipped gravity: Mirror the Y-coordinate across the screen height
            float screenY = Main.screenHeight / 2 - (center.Y - Main.screenPosition.Y);
            screenPos = new Vector2(center.X - Main.screenPosition.X, screenY);
        }
        screenPos = Vector2.Transform(screenPos, Matrix.Invert(Main.GameViewMatrix.ZoomMatrix));
        screenPos += Main.screenPosition - Main.screenLastPosition;
        
        Matrix translation = Matrix.CreateTranslation(new Vector3(screenPos.X, screenPos.Y, 0f));

        // Define orthographic projection matching screen dimensions
        Matrix projection = Matrix.CreateOrthographicOffCenter(0f, Main.screenWidth, Main.screenHeight, 0f, -450f, 450f) * Matrix.Invert(zoomScaleMatrix);

        // Model transformations in object space
        Matrix rotation = Matrix.CreateFromQuaternion(quaternion) * Matrix.CreateRotationZ(startRot);

        Matrix scale = Matrix.CreateScale(size);

        // Combine transformations: model -> screen space -> view effects -> projection
        Matrix vertexMatrix = rotation * scale * translation * Main.GameViewMatrix.TransformationMatrix * projection;

        // Apply horizontal flip if needed
        if (horizontalDir == -1)
        {
            vertexMatrix = Matrix.CreateReflection(new Plane(Vector3.UnitX, 1f)) * Matrix.CreateRotationZ(PiOver2) * vertexMatrix;
        }

        return vertexMatrix;
    }

    public static VertexPositionColorTexture[] GenerateQuadClockwise(Vector2 quadArea, Color? color = null, bool center = false)
    {
        Vector3 topLeftPos;
        Vector3 topRightPos;
        Vector3 bottomRightPos;
        Vector3 bottomLeftPos;

        if (center)
        {
            float halfW = quadArea.X / 2f;
            float halfH = quadArea.Y / 2f;
            topLeftPos = new(-halfW, -halfH, 0f);
            topRightPos = new(halfW, -halfH, 0f);
            bottomRightPos = new(halfW, halfH, 0f);
            bottomLeftPos = new(-halfW, halfH, 0f);
        }
        else
        {
            topLeftPos = new(0f, -quadArea.Y, 0f);
            topRightPos = new(quadArea.X, -quadArea.Y, 0f);
            bottomRightPos = new(quadArea.X, 0f, 0f);
            bottomLeftPos = new(0f, 0f, 0f);
        }

        Color col = color ?? Color.White;
        VertexPositionColorTexture topLeft = new(topLeftPos, col, new Vector2(0.01f, 0.01f));
        VertexPositionColorTexture topRight = new(topRightPos, col, new Vector2(0.99f, 0.01f));
        VertexPositionColorTexture bottomRight = new(bottomRightPos, col, new Vector2(0.99f, 0.99f));
        VertexPositionColorTexture bottomLeft = new(bottomLeftPos, col, new Vector2(0.01f, 0.99f));
        return [topLeft, topRight, bottomRight, bottomLeft];
    }

    /// <summary>
    /// Renders a 2D texture in a 3D plane using dark evil wizard magic
    /// </summary>
    /// <param name="texture">The texture</param>
    /// <param name="pos">The position of this. <see cref="Main.screenPosition"/> is already subtracted.</param>
    /// <param name="rotation">The quaternion to use to define 3D rotation</param>
    /// <param name="scale">The scale of this.</param>
    /// <param name="start">The starting 2D rotation for the quaternion</param>
    public static void DrawTextureIn3D(Texture2D texture, Vector2 pos, Quaternion rotation, float scale, float start, Color? color = null, bool center = false, int horizontalDir = 1)
    {
        VertexPositionColorTexture[] quad = GenerateQuadClockwise(texture.Size(), color, center);
        ManagedShader projectionShader = AssetRegistry.GetShader("PrimitiveProjection");
        projectionShader.TrySetParameter("vertexMatrix", CalculateTextureMatrix(pos, rotation, scale, start, horizontalDir));
        projectionShader.Render();

        GraphicsDevice gd = Main.instance.GraphicsDevice;

        RasterizerState prevRast = gd.RasterizerState;
        SamplerState prevState = gd.SamplerStates[1];
        Texture prevTex = gd.Textures[1];

        gd.RasterizerState = RasterizerState.CullNone;
        gd.SamplerStates[1] = SamplerState.PointClamp;
        gd.Textures[1] = texture;

        gd.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, quad, 0, quad.Length, TextureQuadIndices, 0, TextureQuadIndices.Length / 3);

        gd.RasterizerState = prevRast;
        gd.SamplerStates[1] = prevState;
        gd.Textures[1] = prevTex;
    }
}