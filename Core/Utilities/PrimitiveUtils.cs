using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using TheExtraordinaryAdditions.Core.Graphics.Meshes;
using TheExtraordinaryAdditions.Core.Graphics.Resources;
using static Microsoft.Xna.Framework.MathHelper;

namespace TheExtraordinaryAdditions.Core.Utilities;

public static class PrimitiveUtils
{
    public static RasterizerState CullOnlyScreen
    {
        get
        {
            if (field is not null)
                return field;

            field = RasterizerState.CullNone;
            field.ScissorTestEnable = true;

            return field;
        }
    }
    
    extension(Main)
    {
        public static Vector2 ScreenDelta => Main.screenLastPosition - Main.screenPosition;
    }

    #region Matrices

    public static Matrix GetOrthographicMeshMatrix(bool zoom = true, int planeLength = 1000)
    {
        Matrix final = Matrix.CreateTranslation(new Vector3(-Main.screenPosition, 0));
        if (zoom)
            final = Matrix.Multiply(final, Main.GameViewMatrix.ZoomMatrix);
        final = Matrix.Multiply(final,
            Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -planeLength, planeLength));
        return final;
    }

    public static Matrix GetPerspectiveMeshMatrix(float cameraDist = 2000f, float fov = 50f, float nearPlaneDist = 1f,
        float farPlaneDist = 3000f)
    {
        Matrix world = Matrix.CreateTranslation(-Main.screenPosition.X, -Main.screenPosition.Y, 0f);

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
        view *= zoomScaleMatrix;

        Matrix projection = Matrix.CreatePerspectiveFieldOfView(
            ToRadians(fov),
            (float) width / height, // Aspect ratio
            nearPlaneDist, farPlaneDist); // Near and far planes

        return world * view * projection;
    }

    /// <param name="center">World-space position of the model</param>
    /// <param name="rotation">Local rotation of the model</param>
    /// <param name="scale">Multiplier on the models scale</param>
    /// <param name="startRot">An additional angle change on the 2D plane applied to <paramref name="rotation"/></param>
    /// <param name="flip">Whether or not to reflect the model</param>
    /// <param name="flipRot">General rotation of the model plane when reflected</param>
    /// <param name="unflipRot">General rotation of the model plane</param>
    /// <returns></returns>
    public static Matrix Get3DTextureMatrix(Vector2 center, Quaternion rotation, float scale, float startRot = 0f,
        bool flip = false, float flipRot = 0f, float unflipRot = 0f)
    {
        Matrix model = Matrix.CreateScale(scale);
        model = Matrix.Multiply(model,
            Matrix.CreateFromQuaternion(Quaternion.Concatenate(rotation,
                Quaternion.CreateFromAxisAngle(Vector3.UnitZ, startRot))));
        model = Matrix.Multiply(model, Matrix.CreateTranslation(new Vector3(center, 0f)));
        Matrix view = Matrix.Multiply(Matrix.CreateTranslation(new Vector3(-Main.screenPosition, 0f)),
            Main.GameViewMatrix.ZoomMatrix);
        Matrix projection =
            Matrix.CreateOrthographicOffCenter(0f, Main.screenWidth, Main.screenHeight, 0f, -450f, 450f);
        Matrix mvp = model * view * projection;
        if (flip)
            mvp = Matrix.Multiply(
                Matrix.Multiply(Matrix.CreateReflection(new Plane(Vector3.UnitX, 1f)), Matrix.CreateRotationZ(flipRot)),
                mvp);
        mvp = Matrix.Multiply(Matrix.CreateRotationZ(unflipRot), mvp);

        return mvp;
    }

    #endregion

    #region Drawing

    public static void Draw3D(Texture2D texture, Vector2 pos, Quaternion rotation, float scale, float startRot,
        Color? color = null, Vector2? pivot = null, bool flip = false, float flipRot = 0f, float unflipRot = 0f)
    {
        VertexPositionColorTexture[] quad = GenerateQuadClockwise(texture.Size(), color, pivot);
        ManagedShader projectionShader = AssetRegistry.GennedShaders.PrimitiveProjection;
        projectionShader.TrySetParameter("vertexMatrix",
            Get3DTextureMatrix(pos, rotation, scale, startRot, flip, flipRot, unflipRot));
        projectionShader.Render();

        GraphicsDevice gd = Main.instance.GraphicsDevice;

        RasterizerState prevRast = gd.RasterizerState;
        SamplerState prevState = gd.SamplerStates[1];
        Texture prevTex = gd.Textures[1];

        gd.RasterizerState = RasterizerState.CullNone;
        gd.SamplerStates[1] = SamplerState.PointClamp;
        gd.Textures[1] = texture;

        gd.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, quad, 0, quad.Length, TextureQuadIndices, 0,
            TextureQuadIndices.Length / 3);

        gd.RasterizerState = prevRast;
        gd.SamplerStates[1] = prevState;
        gd.Textures[1] = prevTex;
    }

    #endregion

    #region Shaping

    public static readonly short[] TextureQuadIndices = [0, 1, 2, 2, 3, 0];

    public static VertexPositionColorTexture[] GenerateQuadClockwise(Vector2 quadArea, Color? color = null,
        Vector2? pivot = null)
    {
        Vector2 p = pivot ?? Vector2.Zero;

        float ox = p.X * quadArea.X;
        float oy = p.Y * quadArea.Y;

        Vector3 topLeftPos = new(0f - ox, -quadArea.Y + oy, 0f);
        Vector3 topRightPos = new(quadArea.X - ox, -quadArea.Y + oy, 0f);
        Vector3 bottomRightPos = new(quadArea.X - ox, 0f + oy, 0f);
        Vector3 bottomLeftPos = new(0f - ox, 0f + oy, 0f);

        Color col = color ?? Color.White;
        VertexPositionColorTexture topLeft = new(topLeftPos, col, new Vector2(0.01f, 0.01f));
        VertexPositionColorTexture topRight = new(topRightPos, col, new Vector2(0.99f, 0.01f));
        VertexPositionColorTexture bottomRight = new(bottomRightPos, col, new Vector2(0.99f, 0.99f));
        VertexPositionColorTexture bottomLeft = new(bottomLeftPos, col, new Vector2(0.01f, 0.99f));
        return [topLeft, topRight, bottomRight, bottomLeft];
    }

    public static VertexPositionColorTexture[] GenerateQuadClockwise(Vector2 quadArea, Vector3 position,
        Quaternion rotation, Color? color = null, Vector2? pivot = null)
    {
        Vector2 p = pivot ?? Vector2.Zero;

        float ox = p.X * quadArea.X;
        float oy = p.Y * quadArea.Y;

        Vector3 topLeftPos = new(0f - ox, -quadArea.Y + oy, 0f);
        Vector3 topRightPos = new(quadArea.X - ox, -quadArea.Y + oy, 0f);
        Vector3 bottomRightPos = new(quadArea.X - ox, 0f + oy, 0f);
        Vector3 bottomLeftPos = new(0f - ox, 0f + oy, 0f);

        Matrix rotationMatrix = Matrix.CreateFromQuaternion(rotation);
        Matrix translationMatrix = Matrix.CreateTranslation(position);

        topLeftPos = Vector3.Transform(topLeftPos, rotationMatrix * translationMatrix);
        topRightPos = Vector3.Transform(topRightPos, rotationMatrix * translationMatrix);
        bottomRightPos = Vector3.Transform(bottomRightPos, rotationMatrix * translationMatrix);
        bottomLeftPos = Vector3.Transform(bottomLeftPos, rotationMatrix * translationMatrix);

        Color col = color ?? Color.White;
        return
        [
            new VertexPositionColorTexture(topLeftPos, col, new Vector2(0.01f, 0.01f)),
            new VertexPositionColorTexture(topRightPos, col, new Vector2(0.99f, 0.01f)),
            new VertexPositionColorTexture(bottomRightPos, col, new Vector2(0.99f, 0.99f)),
            new VertexPositionColorTexture(bottomLeftPos, col, new Vector2(0.01f, 0.99f))
        ];
    }

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

                    indices[indexOffset + (i * widthSegments + j) * 6 + 0] = (short) upperLeft;
                    indices[indexOffset + (i * widthSegments + j) * 6 + 1] = (short) lowerRight;
                    indices[indexOffset + (i * widthSegments + j) * 6 + 2] = (short) lowerLeft;
                    indices[indexOffset + (i * widthSegments + j) * 6 + 3] = (short) upperLeft;
                    indices[indexOffset + (i * widthSegments + j) * 6 + 4] = (short) upperRight;
                    indices[indexOffset + (i * widthSegments + j) * 6 + 5] = (short) lowerRight;
                }
            }

            vertexOffset += (widthSegments + 1) * (heightSegments + 1);
            indexOffset += widthSegments * heightSegments * 6;
        }

        return indices;
    }

    public static short[] GenerateCylinderIndices(int widthSegments, int heightSegments)
    {
        int numIndices = widthSegments * heightSegments * 6;
        short[] indices = new short[numIndices];

        int idx = 0;
        for (int i = 0; i < heightSegments; i++)
        {
            for (int j = 0; j < widthSegments; j++)
            {
                int bottomLeft = i * (widthSegments + 1) + j;
                int bottomRight = bottomLeft + 1;
                int topLeft = bottomLeft + (widthSegments + 1);
                int topRight = topLeft + 1;

                // First triangle
                indices[idx++] = (short) bottomLeft;
                indices[idx++] = (short) topLeft;
                indices[idx++] = (short) bottomRight;

                // Second triangle
                indices[idx++] = (short) bottomRight;
                indices[idx++] = (short) topLeft;
                indices[idx++] = (short) topRight;
            }
        }

        return indices;
    }

    public static void FillTaperedCylinderVertices(float thickness, Matrix rotationMatrix, Color baseColor,
        Vector3 start,
        Vector3 end, Span<VertexPositionColorTexture> vertices, int widthSegments, int heightSegments,
        float widthFactor = 1f, int vertexStartOffset = 0, float percentageFromEnd = .2f)
    {
        float widthStep = 1f / widthSegments;
        float heightStep = 1f / heightSegments;

        int vertexOffset = vertexStartOffset;

        for (int side = 0; side < 2; side++)
        {
            float cylinderOffsetAngle = side == 0 ? Pi : 0f;

            for (int i = 0; i <= heightSegments; i++)
            {
                float heightInterpolant = i * heightStep;
                Vector3 cylinderPoint = Vector3.Lerp(start, end, heightInterpolant);

                float width;
                float transitionPoint = 1f - percentageFromEnd;

                if (heightInterpolant <= transitionPoint)
                    width = thickness;
                else
                {
                    float term = (heightInterpolant - 1f + percentageFromEnd) / percentageFromEnd;
                    width = thickness * (float) Math.Sqrt(1f - term * term * term);
                }

                width *= widthFactor;

                for (int j = 0; j <= widthSegments; j++)
                {
                    float angle = Pi * j * widthStep + cylinderOffsetAngle;
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

    public static Vertex3DTex[] FillOpenCylinderVertices(int widthSegments, int heightSegments,
        float radius, float length, Matrix rotation, Vector3 start, Color color)
    {
        int numVertices = (widthSegments + 1) * (heightSegments + 1);
        Vertex3DTex[] vertices = new Vertex3DTex[numVertices];

        for (int i = 0; i <= heightSegments; i++)
        {
            float v = (float) i / heightSegments;
            float y = v * length;

            for (int j = 0; j <= widthSegments; j++)
            {
                float u = (float) j / widthSegments;
                float angle = u * TwoPi;

                float x = MathF.Cos(angle) * radius;
                float z = MathF.Sin(angle) * radius;

                Vector3 localPosition = new Vector3(x, y, z);
                Vector3 transformedPosition = Vector3.Transform(localPosition, rotation) + start;

                int index = i * (widthSegments + 1) + j;
                float angleCosine = MathF.Cos(angle);
                vertices[index] = new Vertex3DTex(transformedPosition.ToNumerics(), color,
                    new SystemVector3(u, v, angleCosine));
            }
        }

        return vertices;
    }

    #endregion

    #region Debug

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
         $" Viewport: {g.Viewport}").Log();
    }

    public static void Debug3DLine(Vector3 position, Quaternion rotation, float rad = 10f, float len = 500f)
    {
        const int widthSegments = 50, heightSegments = 50;
        const int numVertices = (widthSegments + 1) * (heightSegments + 1);

        short[] indices = GenerateCylinderIndices(widthSegments, heightSegments);
        Vertex3DTex[] vertices = new Vertex3DTex[numVertices];

        for (int i = 0; i <= heightSegments; i++)
        {
            float v = (float) i / heightSegments;
            float y = v * len;

            for (int j = 0; j <= widthSegments; j++)
            {
                float u = (float) j / widthSegments;
                float angle = u * TwoPi;

                float x = MathF.Cos(angle) * rad;
                float z = MathF.Sin(angle) * rad;

                Vector3 localPosition = new Vector3(x, y, z);
                int index = i * (widthSegments + 1) + j;
                float angleCosine = MathF.Cos(angle);
                vertices[index] = new Vertex3DTex(localPosition.ToNumerics(), Color.White,
                    new SystemVector3(u, v, angleCosine));
            }
        }

        Matrix world = Matrix.CreateScale(1f);
        world = Matrix.Multiply(world, Matrix.CreateFromQuaternion(rotation));
        world = Matrix.Multiply(world, Matrix.CreateTranslation(position - new Vector3(Main.screenPosition, 0f)));
        int width = Main.screenWidth;
        int height = Main.screenHeight;
        Vector2 zoom = Main.GameViewMatrix.Zoom;
        Matrix zoomScaleMatrix = Matrix.CreateScale(zoom.X, zoom.Y, 1f);
        Matrix view = Matrix.CreateLookAt(
            new Vector3(width / 2f, -height / 2f, -1000f),
            new Vector3(width / 2f, -height / 2f, 0f),
            Vector3.Up);
        view *= Matrix.CreateTranslation(0f, -height, 0f);
        view *= Matrix.CreateRotationZ(Pi);
        view *= zoomScaleMatrix;
        Matrix projection = Matrix.CreatePerspectiveFieldOfView(
            ToRadians(45f),
            (float) width / height,
            .1f, 10_000f);

        ManagedShader shader = AssetRegistry.GennedShaders.GenericLighting;
        shader.TrySetParameter("model", world);
        shader.TrySetParameter("view", view);
        shader.TrySetParameter("projection", projection);
        shader.Render();

        GraphicsDevice gd = Main.instance.GraphicsDevice;
        BlendState prev = gd.BlendState;
        gd.RasterizerState = RasterizerState.CullNone;
        gd.BlendState = BlendState.AlphaBlend;
        gd.DepthStencilState = DepthStencilState.Default;

        gd.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length, indices, 0,
            vertices.Length / 3);
        gd.BlendState = prev;
    }

    #endregion
}
