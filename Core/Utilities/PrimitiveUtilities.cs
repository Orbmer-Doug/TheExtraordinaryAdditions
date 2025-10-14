using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using static Microsoft.Xna.Framework.MathHelper;

namespace TheExtraordinaryAdditions.Core.Utilities;

public static partial class Utility
{
    public static readonly short[] TextureQuadIndices = [0, 1, 2, 2, 3, 0];
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

    #region 2D
    public static void Get2DMatrices(out Matrix effectWorld, out Matrix effectProjection, out Matrix effectView)
    {
        // Screen bounds
        int height = Main.instance.GraphicsDevice.Viewport.Height;

        Vector2 zoom = Main.GameViewMatrix.Zoom;
        Matrix zoomScaleMatrix = Matrix.CreateScale(zoom.X, zoom.Y, 1f);

        // Get a matrix that aims towards the Z axis (these calculations are relative to a 2D world)
        effectView = Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.Up);

        // Offset the matrix to the appropriate position
        effectView *= Matrix.CreateTranslation(0f, -height, 0f);

        // Flip the matrix around 180 degrees
        effectView *= Matrix.CreateRotationZ(Pi);

        // Account for the inverted gravity effect
        if (Main.LocalPlayer.gravDir == -1f)
            effectView *= Matrix.CreateScale(1f, -1f, 1f) * Matrix.CreateTranslation(0f, height, 0f);

        // And account for the current zoom
        effectView *= zoomScaleMatrix;

        effectProjection = Matrix.CreateOrthographicOffCenter(0f, Main.screenWidth * zoom.X, 0f, Main.screenHeight * zoom.Y, 0f, 1f) * zoomScaleMatrix;
        effectWorld = Matrix.CreateTranslation(new Vector3(-Main.screenPosition, 0));
    }

    public static void GetPixelated2DMatrices(out Matrix effectWorld, out Matrix effectProjection, out Matrix effectView)
    {
        effectWorld = Matrix.CreateTranslation(new Vector3(-Main.screenPosition, 0));
        effectProjection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1);
        effectView = Matrix.Identity;
    }

    public static Matrix GetCustomSkyBackgroundMatrix()
    {
        Matrix transformationMatrix = Main.BackgroundViewMatrix.TransformationMatrix;
        Vector3 translationDirection = new(1f, Main.BackgroundViewMatrix.Effects.HasFlag(SpriteEffects.FlipVertically) ? -1f : 1f, 1f);

        transformationMatrix.Translation -= Main.BackgroundViewMatrix.ZoomMatrix.Translation * translationDirection;
        return transformationMatrix;
    }
    #endregion

    #region 3D
    public static short[] GenerateCylinderIndices(int baseVertexOffset, int widthSegments, int heightSegments)
    {
        int numIndices = widthSegments * heightSegments * 6 * 2;
        short[] indices = new short[numIndices];
        int vertexOffset = baseVertexOffset;
        int indexOffset = 0;

        for (int side = 0; side < 2; side++)
        {
            for (int i = 0; i < heightSegments; i++)
            {
                for (int j = 0; j < widthSegments; j++)
                {
                    int upperLeft = vertexOffset + i * (widthSegments + 1) + j;
                    int upperRight = upperLeft + 1;
                    int lowerLeft = upperLeft + widthSegments + 1;
                    int lowerRight = lowerLeft + 1;

                    indices[indexOffset + (i * widthSegments + j) * 6 + 0] = (short)upperLeft;
                    indices[indexOffset + (i * widthSegments + j) * 6 + 1] = (short)lowerRight;
                    indices[indexOffset + (i * widthSegments + j) * 6 + 2] = (short)lowerLeft;
                    indices[indexOffset + (i * widthSegments + j) * 6 + 3] = (short)upperLeft;
                    indices[indexOffset + (i * widthSegments + j) * 6 + 4] = (short)upperRight;
                    indices[indexOffset + (i * widthSegments + j) * 6 + 5] = (short)lowerRight;
                }
            }

            vertexOffset += (widthSegments + 1) * (heightSegments + 1);
            indexOffset += widthSegments * heightSegments * 6;
        }

        return indices;
    }

    public static void FillCylinderVertices(float thickness, Matrix rotationMatrix, Color baseColor, Vector3 start, Vector3 end, Span<VertexPositionColorTexture> vertices, int widthSegments, int heightSegments, float widthFactor = 1f, int vertexStartOffset = 0)
    {
        float widthStep = 1f / widthSegments;
        float heightStep = 1f / heightSegments;

        Vector3 direction = Vector3.Normalize(end - start);
        float length = Vector3.Distance(start, end);

        int vertexOffset = vertexStartOffset;

        for (int side = 0; side < 2; side++)
        {
            float cylinderOffsetAngle = side == 0 ? MathHelper.Pi : 0f;

            for (int i = 0; i <= heightSegments; i++)
            {
                float heightInterpolant = i * heightStep;
                Vector3 cylinderPoint = Vector3.Lerp(start, end, heightInterpolant);

                float width;
                const float percentageFromEnd = 0.2f;
                const float transitionPoint = 1f - percentageFromEnd;

                if (heightInterpolant <= transitionPoint)
                    width = thickness;
                else
                {
                    float term = (heightInterpolant - 1f + percentageFromEnd) / percentageFromEnd;
                    width = thickness * (float)Math.Sqrt(1f - term * term * term);
                }
                width *= widthFactor;

                for (int j = 0; j <= widthSegments; j++)
                {
                    float angle = MathHelper.Pi * j * widthStep + cylinderOffsetAngle;
                    Vector3 baseOffset = new(0f, MathF.Sin(angle), MathF.Cos(angle));
                    Vector3 orthogonalOffset = Vector3.Transform(baseOffset, rotationMatrix) * width;
                    Vector3 finalPoint = cylinderPoint + orthogonalOffset;
                    vertices[vertexOffset + i * (widthSegments + 1) + j] = new VertexPositionColorTexture(
                        new(finalPoint.X, finalPoint.Y, finalPoint.Z),
                        baseColor,
                        new Vector2(heightInterpolant, j * widthStep));
                }
            }

            vertexOffset += (widthSegments + 1) * (heightSegments + 1);
        }
    }

    public static Matrix Get3DPerspectivePrimitiveMatrix(float cameraDist = 2000f, float fov = 50f)
    {
        int width = Main.screenWidth;
        int height = Main.screenHeight;
        Vector2 zoom = Main.GameViewMatrix.Zoom;
        Matrix zoomScaleMatrix = Matrix.CreateScale(zoom.X, zoom.Y, 1f);

        // Set up the camera
        Matrix effectView = Matrix.CreateLookAt(
            new Vector3(width / 2f, -height / 2f, -cameraDist), // Camera position
            new Vector3(width / 2f, -height / 2f, 0f), // Look at screen center
            Vector3.Up);

        effectView *= Matrix.CreateTranslation(0f, -height, 0f); // Adjust for Y being down
        effectView *= Matrix.CreateRotationZ(Pi); // Flip to match orientation
        effectView *= zoomScaleMatrix;

        Matrix effectProjection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.ToRadians(fov),
            (float)width / height, // Aspect ratio
            1f, 3000f); // Near and far planes

        // Position in world
        Matrix effectWorld = Matrix.CreateTranslation(-Main.screenPosition.X, -Main.screenPosition.Y, 0f);
        return effectWorld * effectView * effectProjection;
    }

    public static Matrix Get3DOrthoPrimitiveMatrix(Vector2 center, Quaternion quaternion, float size, float startRot = 0f, int horizontalDir = 1)
    {
        Matrix mainZoom = Main.GameViewMatrix.ZoomMatrix;
        Matrix invertedZoom = Matrix.Invert(Matrix.CreateScale(Main.GameViewMatrix.Zoom.X, Main.GameViewMatrix.Zoom.Y, 1f));

        Vector2 screenPos = center - Main.screenPosition; // Convert to screen position
        screenPos = Vector2.Transform(screenPos, Matrix.Invert(mainZoom)); // Account for zoom

        Matrix translation = Matrix.CreateTranslation(new Vector3(screenPos.X, screenPos.Y, 0f)) * mainZoom; // zoom again...
        Matrix projection = Matrix.CreateOrthographicOffCenter(0f, Main.screenWidth, Main.screenHeight, 0f, -450f, 450f) * invertedZoom; // AGAIN

        // Apply rotation, scaling, and flipping
        Matrix rotation = Matrix.CreateFromQuaternion(quaternion) * Matrix.CreateRotationZ(startRot);
        Matrix scale = Matrix.CreateScale(size);
        Matrix vertexMatrix = rotation * scale * translation * projection;
        if (horizontalDir == -1)
            vertexMatrix = Matrix.CreateReflection(new Plane(Vector3.UnitX, 1f)) * Matrix.CreateRotationZ(PiOver2) * vertexMatrix;

        return vertexMatrix;
    }

    public static Matrix Get3DTextureMatrix(Vector2 center, Quaternion quaternion, float size, float startRot = 0f, int horizontalDir = 1)
    {
        Matrix translation = Matrix.CreateTranslation(new Vector3(center.X - Main.screenPosition.X, center.Y - Main.screenPosition.Y, 0f));
        Matrix projection = Matrix.CreateOrthographicOffCenter(0f, Main.screenWidth, Main.screenHeight, 0f, -450f, 450f);
        Matrix view = translation * Main.GameViewMatrix.TransformationMatrix * projection;

        // Apply rotation, scaling, and flipping
        Matrix rotation = Matrix.CreateFromQuaternion(quaternion) * Matrix.CreateRotationZ(startRot);
        Matrix scale = Matrix.CreateScale(size);
        Matrix vertexMatrix = rotation * scale * view;
        if (horizontalDir == -1f)
            vertexMatrix = Matrix.CreateReflection(new Plane(Vector3.UnitX, 1f)) * Matrix.CreateRotationZ(PiOver2) * vertexMatrix;

        return vertexMatrix;
    }

    public static Matrix Get3DPerspectiveTextureMatrix(Vector2 center, Quaternion quaternion, float startRot, float size, int horizontalDir = 1, float cameraDist = 1000f, float fov = 50f)
    {
        int width = Main.screenWidth;
        int height = Main.screenHeight;
        Vector2 zoom = Main.GameViewMatrix.Zoom;
        Matrix zoomScaleMatrix = Matrix.CreateScale(zoom.X, zoom.Y, 1f);

        // Set up the camera
        Matrix view = Matrix.CreateLookAt(
            new Vector3(width / 2f, -height / 2f, -cameraDist), // Camera position
            new Vector3(width / 2f, -height / 2f, 0f), // Look at screen center
            Vector3.Up);

        view *= Matrix.CreateTranslation(0f, -height, 0f); // Adjust for Y being down
        view *= Matrix.CreateRotationZ(Pi); // Flip to match orientation
        if (Main.LocalPlayer.gravDir == -1f)
            view *= Matrix.CreateScale(1f, -1f, 1f);
        view *= zoomScaleMatrix;

        Matrix projection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.ToRadians(fov),
            (float)width / height, // Aspect ratio
            1f, 3000f); // Near and far planes

        // Position in world
        Matrix world = Matrix.CreateTranslation(new Vector3(center.X - Main.screenPosition.X, center.Y - Main.screenPosition.Y, 0f));

        // Apply rotation, scaling, and flipping
        Matrix rotation = Matrix.CreateFromQuaternion(quaternion) * Matrix.CreateRotationZ(startRot);
        Matrix scale = Matrix.CreateScale(size);
        Matrix objectMatrix = rotation * scale;
        if (horizontalDir == -1f)
            objectMatrix = Matrix.CreateReflection(new Plane(Vector3.UnitX, 1f)) * Matrix.CreateRotationZ(MathHelper.PiOver2) * objectMatrix;

        return objectMatrix * world * view * projection;
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

    public static VertexPositionColorTexture[] GenerateQuadClockwise(Vector2 quadArea, Vector3 position, Quaternion rotation, Color? color = null, bool center = false)
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

        Matrix rotationMatrix = Matrix.CreateFromQuaternion(rotation);
        Matrix translationMatrix = Matrix.CreateTranslation(position);

        topLeftPos = Vector3.Transform(topLeftPos, rotationMatrix * translationMatrix);
        topRightPos = Vector3.Transform(topRightPos, rotationMatrix * translationMatrix);
        bottomRightPos = Vector3.Transform(bottomRightPos, rotationMatrix * translationMatrix);
        bottomLeftPos = Vector3.Transform(bottomLeftPos, rotationMatrix * translationMatrix);

        Color col = color ?? Color.White;
        return new[]
        {
            new VertexPositionColorTexture(topLeftPos, col, new Vector2(0.01f, 0.01f)),
            new VertexPositionColorTexture(topRightPos, col, new Vector2(0.99f, 0.01f)),
            new VertexPositionColorTexture(bottomRightPos, col, new Vector2(0.99f, 0.99f)),
            new VertexPositionColorTexture(bottomLeftPos, col, new Vector2(0.01f, 0.99f))
        };
    }

    /// <summary>
    /// Renders a 2D texture in a 3D plane using dark evil wizard magic
    /// </summary>
    /// <param name="texture">The texture</param>
    /// <param name="pos">The position of this in world space</param>
    /// <param name="rotation">The quaternion to use to define 3D rotation</param>
    /// <param name="scale">The scale of this</param>
    /// <param name="start">The starting 2D rotation for the quaternion</param>
    public static void DrawTextureIn3D(Texture2D texture, Vector2 pos, Quaternion rotation, float scale, float start, Color? color = null, bool center = false, int horizontalDir = 1)
    {
        VertexPositionColorTexture[] quad = GenerateQuadClockwise(texture.Size(), color, center);
        ManagedShader projectionShader = AssetRegistry.GetShader("PrimitiveProjection");
        projectionShader.TrySetParameter("vertexMatrix", Get3DTextureMatrix(pos, rotation, scale, start, horizontalDir));
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
    #endregion
}