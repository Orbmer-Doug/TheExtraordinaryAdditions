using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using static TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles.BarrageBeamManager;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

/* TODO: There seems to be a itty bitty problem with the bloom where if there is enough projectiles the renderer falls back to earlier beams
 * This causes triangular artifacts to arc between them and is more noticeable early on the more bloom layers there is
 * This ONLY happens with bloom despite it literally just being the main beam drawn extra times with a different shader
 * Whether it is uninitialized or out-of-bounds vertex data causing this i dunno
 * But it seems i caNT FUCKING FIX IIIITTTTTTTT
 * last resort is to make arrays of vertex buffers but that doesn't seem ideal
 */

/// <summary>
/// A system meant to conduct the drawing of all <see cref="BarrageBeam"/>'s <br></br>
/// Optimizes drawing by drawing all beams as one continuous mesh
/// </summary>
public class BarrageBeamManager : ModSystem
{
    public static IndexBuffer CylinderIndices;
    public static IndexBuffer BloomIndices;
    public static IndexBuffer PortalIndices;
    public static DynamicVertexBuffer BatchBeamVertexBuffer;
    public static DynamicVertexBuffer BatchPortalVertexBuffer;
    public static DynamicVertexBuffer BatchBloomVertexBuffer;
    public static ManagedRenderTarget BeamRenderTarget;
    public static readonly List<BarrageBeam> ActiveBeams = new();

    public const int CylinderWidthSegments = 16;
    public const int CylinderHeightSegments = 20;
    public const int BloomWidthSegments = 16;
    public const int BloomHeightSegments = 20;
    public const int NumVertices = (CylinderWidthSegments + 1) * (CylinderHeightSegments + 1) * 2;
    public const int NumIndices = CylinderWidthSegments * CylinderHeightSegments * 6 * 2;
    public const int BloomNumVertices = (BloomWidthSegments + 1) * (BloomHeightSegments + 1) * 2;
    public const int BloomNumIndices = BloomWidthSegments * BloomHeightSegments * 6 * 2;
    public const int NumPortalVertices = 4;
    public const int NumPortalIndices = 6;
    public const int NumBloomLayers = 3;
    private const int MaxProjectiles = 80;

    public static readonly Quaternion Rotation = Animators.EulerAnglesConversion(1, MathHelper.PiOver2, (3f * MathHelper.Pi) / 2f);
    public static readonly Quaternion PortalRotation = Animators.EulerAnglesConversion(1, MathHelper.PiOver2, 0f);
    public static readonly Matrix RotationMatrix = Matrix.CreateFromQuaternion(Rotation);

    public override void OnModLoad()
    {
        if (Main.netMode == NetmodeID.Server)
            return;

        Main.QueueMainThreadAction(() =>
        {
            // Index buffers for main beam cylinders
            CylinderIndices = new IndexBuffer(Main.instance.GraphicsDevice, IndexElementSize.SixteenBits, NumIndices * MaxProjectiles, BufferUsage.None);
            short[] cylinderIndices = new short[NumIndices * MaxProjectiles];
            for (int p = 0; p < MaxProjectiles; p++)
            {
                short[] projIndices = GenerateCylinderIndices(p * NumVertices, CylinderWidthSegments, CylinderHeightSegments);
                Array.Copy(projIndices, 0, cylinderIndices, p * NumIndices, NumIndices);
            }
            CylinderIndices.SetData(cylinderIndices);

            // Index buffers for bloom cylinders
            BloomIndices = new IndexBuffer(Main.instance.GraphicsDevice, IndexElementSize.SixteenBits, BloomNumIndices * MaxProjectiles * NumBloomLayers, BufferUsage.None);
            short[] bloomIndicesArray = new short[BloomNumIndices * MaxProjectiles * NumBloomLayers];
            for (int p = 0; p < MaxProjectiles; p++)
            {
                for (int b = 0; b < NumBloomLayers; b++)
                {
                    short[] bloomProjIndices = GenerateCylinderIndices(p * BloomNumVertices * NumBloomLayers + b * BloomNumVertices, BloomWidthSegments, BloomHeightSegments);
                    Array.Copy(bloomProjIndices, 0, bloomIndicesArray, (p * NumBloomLayers + b) * BloomNumIndices, BloomNumIndices);
                }
            }
            BloomIndices.SetData(bloomIndicesArray);

            // Index buffers for portals
            PortalIndices = new IndexBuffer(Main.instance.GraphicsDevice, IndexElementSize.SixteenBits, NumPortalIndices * MaxProjectiles, BufferUsage.None);
            short[] portalIndices = new short[NumPortalIndices * MaxProjectiles];
            for (int p = 0; p < MaxProjectiles; p++)
            {
                short[] quadIndices = new short[] {
                    (short)(p * NumPortalVertices + 0),
                    (short)(p * NumPortalVertices + 1),
                    (short)(p * NumPortalVertices + 2),
                    (short)(p * NumPortalVertices + 0),
                    (short)(p * NumPortalVertices + 2),
                    (short)(p * NumPortalVertices + 3)
                };
                Array.Copy(quadIndices, 0, portalIndices, p * NumPortalIndices, NumPortalIndices);
            }
            PortalIndices.SetData(portalIndices);

            // Vertex buffers
            BatchBeamVertexBuffer = new DynamicVertexBuffer(Main.instance.GraphicsDevice, typeof(VertexPositionColorTexture), NumVertices * MaxProjectiles, BufferUsage.WriteOnly);
            BatchBloomVertexBuffer = new DynamicVertexBuffer(Main.instance.GraphicsDevice, typeof(VertexPositionColorTexture), BloomNumVertices * MaxProjectiles * NumBloomLayers, BufferUsage.WriteOnly);
            BatchPortalVertexBuffer = new DynamicVertexBuffer(Main.instance.GraphicsDevice, typeof(VertexPositionColorTexture), NumPortalVertices * MaxProjectiles, BufferUsage.WriteOnly);

            // Render target
            BeamRenderTarget = new ManagedRenderTarget(true, (w, h) => new RenderTarget2D(Main.instance.GraphicsDevice, w / 2, h / 2), true);
            Main.instance.GraphicsDevice.SetRenderTarget(BeamRenderTarget);
            Main.instance.GraphicsDevice.Clear(Color.Transparent);
            Main.instance.GraphicsDevice.SetRenderTarget(null);
        });

        RenderTargetManager.RenderTargetUpdateLoopEvent += DrawToTarget;
        On_Main.DrawProjectiles += DrawTheTarget;
    }

    public override void OnModUnload()
    {
        if (Main.netMode == NetmodeID.Server)
            return;

        Main.QueueMainThreadAction(() =>
        {
            CylinderIndices?.Dispose();
            BloomIndices?.Dispose();
            PortalIndices?.Dispose();
            BatchBeamVertexBuffer?.Dispose();
            BatchPortalVertexBuffer?.Dispose();
            BatchBloomVertexBuffer?.Dispose();
            BeamRenderTarget?.Dispose();
            CylinderIndices = null;
            PortalIndices = null;
            BatchBeamVertexBuffer = null;
            BatchPortalVertexBuffer = null;
            BatchBloomVertexBuffer = null;
            BeamRenderTarget = null;
        });

        RenderTargetManager.RenderTargetUpdateLoopEvent -= DrawToTarget;
        On_Main.DrawProjectiles -= DrawTheTarget;
    }

    public override void PostUpdateProjectiles()
    {
        for (int i = ActiveBeams.Count - 1; i >= 0; i--)
        {
            BarrageBeam beam = ActiveBeams[i];
            if (beam == null || !beam.Projectile.active)
                ActiveBeams.RemoveAt(i);
        }
    }

    public static void RegisterBeam(BarrageBeam beam)
    {
        if (!ActiveBeams.Contains(beam) && ActiveBeams.Count < MaxProjectiles)
            ActiveBeams.Add(beam);
    }

    private static short[] GenerateCylinderIndices(int baseVertexOffset, int widthSegments, int heightSegments)
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

    public static VertexPositionColorTexture[] GeneratePortalQuad(Vector3 center, Quaternion rotation, float size, Color? color = null)
    {
        float halfSize = size / 2f;
        Vector3 topLeftPos = new(-halfSize, -halfSize, 0f);
        Vector3 topRightPos = new(halfSize, -halfSize, 0f);
        Vector3 bottomRightPos = new(halfSize, halfSize, 0f);
        Vector3 bottomLeftPos = new(-halfSize, halfSize, 0f);

        Matrix rotationMatrix = Matrix.CreateFromQuaternion(rotation);
        Matrix translationMatrix = Matrix.CreateTranslation(center);

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

    private void DrawToTarget()
    {
        if (!AssetRegistry.HasFinishedLoading || Main.gameMenu || Main.netMode == NetmodeID.Server || ActiveBeams.Count == 0)
            return;

        GraphicsDevice device = Main.instance.GraphicsDevice;
        device.SetRenderTarget(BeamRenderTarget);
        device.Clear(Color.Transparent);

        // Compute view/projection matrices
        int width = Main.screenWidth;
        int height = Main.screenHeight;
        Vector2 zoom = Main.GameViewMatrix.Zoom;
        Matrix zoomScaleMatrix = Matrix.CreateScale(zoom.X, zoom.Y, 1f);
        float cameraDistance = 2000f;
        Matrix effectView = Matrix.CreateLookAt(
            new Vector3(width / 2f, -height / 2f, -cameraDistance),
            new Vector3(width / 2f, -height / 2f, 0f),
            Vector3.Up);
        effectView *= Matrix.CreateTranslation(0f, -height, 0f);
        effectView *= Matrix.CreateRotationZ(MathHelper.Pi);
        if (Main.LocalPlayer.gravDir == -1f)
            effectView *= Matrix.CreateScale(1f, -1f, 1f) * Matrix.CreateTranslation(0f, height, 0f);
        effectView *= zoomScaleMatrix;
        Matrix effectProjection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(50f), (float)width / height, 1f, 3000f);
        Matrix effectWorld = Matrix.CreateTranslation(-Main.screenPosition.X, -Main.screenPosition.Y, 0f);
        Matrix overall = effectWorld * effectView * effectProjection;

        // Fill batched vertex buffers
        VertexPositionColorTexture[] beamVertices = new VertexPositionColorTexture[NumVertices * ActiveBeams.Count];
        VertexPositionColorTexture[] bloomVertices = new VertexPositionColorTexture[BloomNumVertices * ActiveBeams.Count * NumBloomLayers];
        VertexPositionColorTexture[] portalVertices = new VertexPositionColorTexture[NumPortalVertices * ActiveBeams.Count];

        // Initialize bloom vertices to zero to prevent garbage data
        Array.Clear(bloomVertices, 0, bloomVertices.Length);

        // Fill vertex buffers
        for (int i = 0; i < ActiveBeams.Count; i++)
        {
            BarrageBeam beam = ActiveBeams[i];
            Vector3 start = new Vector3(beam.Projectile.Center, beam.LaserZPos);
            Vector3 portalStart = new Vector3(beam.Projectile.Center, beam.PortalZPos);
            Vector3 end = start + Vector3.Transform(Vector3.UnitX, Rotation) * beam.LaserbeamLength;

            // Fill beam vertices
            beam.FillCylinderVertices(new Color(28, 225, 255) * beam.Projectile.Opacity, start, end, beamVertices.AsSpan(i * NumVertices, NumVertices), CylinderWidthSegments, CylinderHeightSegments);

            // Fill bloom vertices
            for (int b = 0; b < NumBloomLayers; b++)
            {
                float scaleFactor = 1f + (b + 1) * 0.2f;
                float alpha = 0.5f / (b + 1);
                beam.FillCylinderVertices(Color.DeepSkyBlue with { A = 0 } * alpha * beam.Projectile.Opacity, start, end, bloomVertices.AsSpan((i * NumBloomLayers + b) * BloomNumVertices, BloomNumVertices), BloomWidthSegments, BloomHeightSegments, scaleFactor);
            }

            // Fill portal vertices
            VertexPositionColorTexture[] portalVerts = GeneratePortalQuad(portalStart, PortalRotation, beam.Projectile.scale * 12f, Color.White);
            Array.Copy(portalVerts, 0, portalVertices, i * NumPortalVertices, NumPortalVertices);
        }

        // Upload to GPU
        BatchBeamVertexBuffer.SetData(beamVertices);
        BatchBloomVertexBuffer.SetData(bloomVertices);
        BatchPortalVertexBuffer.SetData(portalVertices);

        device.RasterizerState = RasterizerState.CullCounterClockwise;
        device.DepthStencilState = DepthStencilState.Default;
        device.BlendState = BlendState.AlphaBlend;

        // Draw portal
        ManagedShader portalShader = AssetRegistry.GetShader("3DPortalShaderAlt");
        portalShader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.Pixel), 0, SamplerState.PointClamp);
        portalShader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.DendriticNoiseDim), 1, SamplerState.LinearWrap);
        portalShader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.TurbulentNoise), 2, SamplerState.LinearWrap);
        portalShader.TrySetParameter("globalTime", Main.GlobalTimeWrappedHourly);
        portalShader.TrySetParameter("coolColor", Color.DarkCyan.ToVector3());
        portalShader.TrySetParameter("mediumColor", Color.Cyan.ToVector3());
        portalShader.TrySetParameter("hotColor", Color.Cyan.ToVector3());
        portalShader.TrySetParameter("vertexMatrix", overall);
        device.SetVertexBuffer(BatchPortalVertexBuffer);
        device.Indices = PortalIndices;
        for (int i = 0; i < ActiveBeams.Count; i++)
        {
            portalShader.TrySetParameter("scale", ActiveBeams[i].PortalInterpolant); // So its unique every projectile
            portalShader.Effect.CurrentTechnique.Passes[ManagedShader.DefaultPassName].Apply();
            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, i * NumPortalVertices, NumPortalVertices, i * NumPortalIndices, NumPortalIndices / 3);
        }

        // Draw bloom
        ManagedShader bloomShader = ShaderRegistry.StandardPrimitiveShader;
        bloomShader.TrySetParameter("transformMatrix", overall);
        bloomShader.Effect.CurrentTechnique.Passes[ManagedShader.DefaultPassName].Apply();
        device.SetVertexBuffer(BatchBloomVertexBuffer);
        device.Indices = BloomIndices;
        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, BloomNumVertices * ActiveBeams.Count * NumBloomLayers, 0, BloomNumIndices * ActiveBeams.Count * NumBloomLayers / 3);

        // Draw main beam
        ManagedShader shader = AssetRegistry.GetShader("AsterlinDeathrayShader");
        shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.ManifoldNoise), 1, SamplerState.LinearWrap);
        shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.TurbulentNoise), 2, SamplerState.LinearWrap);
        shader.TrySetParameter("transformMatrix", overall);
        shader.TrySetParameter("baseColor", new Color(28, 225, 255).ToVector3());
        shader.TrySetParameter("globalTime", Main.GlobalTimeWrappedHourly);
        shader.Effect.CurrentTechnique.Passes[ManagedShader.DefaultPassName].Apply();
        device.SetVertexBuffer(BatchBeamVertexBuffer);
        device.Indices = CylinderIndices;
        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, NumVertices * ActiveBeams.Count, 0, NumIndices * ActiveBeams.Count / 3);

        device.SetRenderTarget(null);
        device.SetVertexBuffer(null);
        device.Indices = null;
    }

    private void DrawTheTarget(On_Main.orig_DrawProjectiles orig, Main self)
    {
        if (AssetRegistry.HasFinishedLoading && !Main.gameMenu && Main.netMode != NetmodeID.Server && ActiveBeams.Count != 0)
        {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(BeamRenderTarget, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
        }

        orig(self);
    }
}
public class BarrageBeam : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public int MaxLaserLength => 1500;
    public ref float Time => ref Projectile.ai[0];
    public ref float LaserbeamLength => ref Projectile.ai[1];
    public ref float PortalInterpolant => ref Projectile.ai[2];
    public ref float PortalZPos => ref Projectile.Additions().ExtraAI[0];
    public ref float LaserZPos => ref Projectile.Additions().ExtraAI[1];
    public Vector2 TargetPosition;

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.WriteVector2(TargetPosition);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        TargetPosition = reader.ReadVector2();
    }

    public override void SetDefaults()
    {
        Projectile.Size = new(32f);
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.hostile = true;
        Projectile.friendly = false;
        Projectile.timeLeft = 9000000;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }

    public override void AI()
    {
        if (Time == 0)
        {
            const float clusteringDistance = 360f;
            const int maxTries = 150;

            int tries = 0;
            Vector2 pos;
            bool validPosition;

            do
            {
                validPosition = true;

                pos = Main.LocalPlayer.Center + Vector2.Zero.SafeNormalize(Main.rand.NextVector2Unit()).RotatedByRandom(RandomRotation()) * (new Vector2(700f, 420f) * Main.rand.NextFloat(.1f, 1f));
                pos = pos.ClampInWorld();

                foreach (Projectile r in AllProjectilesByID(Type))
                {
                    if (r.whoAmI == Projectile.whoAmI)
                        continue;

                    if (pos.Distance(r.Center) < clusteringDistance)
                    {
                        validPosition = false;
                        break;
                    }
                }

                tries++;
            }
            while (!validPosition && tries < maxTries);

            TargetPosition = pos;
            Projectile.netUpdate = true;
        }

        Projectile.scale = 30f;

        float hoverInterpol = InverseLerp(0f, Asterlin.Barrage_HoverTime, Time);
        Projectile.Center = Vector2.SmoothStep(Projectile.Center, TargetPosition, Animators.CubicBezier(.48f, .41f, 0f, 1f)(hoverInterpol));
        Projectile.Opacity = hoverInterpol;

        int timeUntilFade = Asterlin.Barrage_HoverTime + Asterlin.Barrage_BeamExpandTime + Asterlin.Barrage_BeamTime;
        if (Time < timeUntilFade)
        {
            PortalZPos = LaserZPos = -300f;
            PortalInterpolant = Animators.MakePoly(3f).OutFunction(hoverInterpol);
            LaserbeamLength = Animators.MakePoly(3.6f).OutFunction.Evaluate(Time, Asterlin.Barrage_HoverTime, Asterlin.Barrage_HoverTime + Asterlin.Barrage_BeamExpandTime, 0f, MaxLaserLength);
        }
        else
        {
            float fadeInterpolant = InverseLerp(timeUntilFade, timeUntilFade + Asterlin.Barrage_BeamFadeTime, Time);
            LaserZPos = Animators.MakePoly(2.8f).InFunction.Evaluate(-300f, -2000f, fadeInterpolant);
            PortalInterpolant = 1f - fadeInterpolant;
            LaserbeamLength = Animators.MakePoly(1.8f).InOutFunction.Evaluate(0f, MaxLaserLength, 1f - fadeInterpolant);
            if (fadeInterpolant >= 1f)
                Projectile.Kill();
        }
        Time++;
        BarrageBeamManager.RegisterBeam(this);
    }

    public override bool? CanDamage() => LaserbeamLength >= MathF.Abs(LaserZPos) ? null : false;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => CircularHitboxCollision(Projectile.Center, Projectile.scale, targetHitbox);

    public void FillCylinderVertices(in Color baseColor, in Vector3 start, in Vector3 end, Span<VertexPositionColorTexture> vertices, int widthSegments, int heightSegments, float widthFactor = 1f, int vertexStartOffset = 0)
    {
        float widthStep = 1f / widthSegments;
        float heightStep = 1f / heightSegments;

        Vector3 xnaDirection = Vector3.Normalize(end - start);
        Vector3 direction = new(xnaDirection.X, xnaDirection.Y, xnaDirection.Z);
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

                float thickness = Projectile.scale;

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
                    Vector3 orthogonalOffset = Vector3.Transform(baseOffset, RotationMatrix) * width;
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
    public override bool PreDraw(ref Color lightColor) => false;
}

/* Per-projectile drawing, kills frames
public class BarrageBeamRegistry : ModSystem
{
    public static IndexBuffer CylinderIndices;
    public static IndexBuffer BloomIndices;

    public override void OnModLoad()
    {
        if (Main.netMode == NetmodeID.Server)
            return;

        Main.QueueMainThreadAction(() =>
        {
            CylinderIndices = new IndexBuffer(Main.instance.GraphicsDevice, IndexElementSize.SixteenBits, NumIndices, BufferUsage.WriteOnly);
            CylinderIndices.SetData(GenerateCylinderIndices(0));

            BloomIndices = new IndexBuffer(Main.instance.GraphicsDevice, IndexElementSize.SixteenBits, NumIndices * BloomSubdivisions, BufferUsage.WriteOnly);
            short[] combinedBloomIndices = new short[NumIndices * BloomSubdivisions];
            for (int i = 0; i < BloomSubdivisions; i++)
            {
                short[] layerIndices = GenerateCylinderIndices(i * NumVertices);
                System.Array.Copy(layerIndices, 0, combinedBloomIndices, i * NumIndices, NumIndices);
            }
            BloomIndices.SetData(combinedBloomIndices);
        });
    }

    public override void OnModUnload()
    {
        if (Main.netMode == NetmodeID.Server)
            return;

        Main.QueueMainThreadAction(() =>
        {
            CylinderIndices?.Dispose();
            CylinderIndices = null;
            BloomIndices?.Dispose();
            BloomIndices = null;
        });
    }

    private short[] GenerateCylinderIndices(int baseVertexOffset)
    {
        short[] indices = new short[NumIndices];
        int vertexOffset = baseVertexOffset;
        int indexOffset = 0;

        for (int side = 0; side < 2; side++)
        {
            for (int i = 0; i < CylinderHeightSegments; i++)
            {
                for (int j = 0; j < CylinderWidthSegments; j++)
                {
                    int upperLeft = vertexOffset + i * (CylinderWidthSegments + 1) + j;
                    int upperRight = upperLeft + 1;
                    int lowerLeft = upperLeft + CylinderWidthSegments + 1;
                    int lowerRight = lowerLeft + 1;

                    indices[indexOffset + (i * CylinderWidthSegments + j) * 6 + 0] = (short)upperLeft;
                    indices[indexOffset + (i * CylinderWidthSegments + j) * 6 + 1] = (short)lowerRight;
                    indices[indexOffset + (i * CylinderWidthSegments + j) * 6 + 2] = (short)lowerLeft;
                    indices[indexOffset + (i * CylinderWidthSegments + j) * 6 + 3] = (short)upperLeft;
                    indices[indexOffset + (i * CylinderWidthSegments + j) * 6 + 4] = (short)upperRight;
                    indices[indexOffset + (i * CylinderWidthSegments + j) * 6 + 5] = (short)lowerRight;
                }
            }

            vertexOffset += (CylinderWidthSegments + 1) * (CylinderHeightSegments + 1);
            indexOffset += CylinderWidthSegments * CylinderHeightSegments * 6;
        }

        return indices;
    }
}

public class BarrageBeam : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;

    public ref float Time => ref Projectile.ai[0];
    public ref float LaserbeamLength => ref Projectile.ai[1];
    public static readonly Quaternion Rotation = Animators.EulerAnglesConversion(1, MathHelper.PiOver2, (3f * MathHelper.Pi) / 2f);
    public static readonly Quaternion PortalRotation = Animators.EulerAnglesConversion(1, MathHelper.PiOver2, 0f);
    public static readonly Color BeamColor = new Color(28, 225, 255);
    public static readonly Color BloomColor = Color.DeepSkyBlue with { A = 0 };

    public const short CylinderWidthSegments = 12;
    public const short CylinderHeightSegments = 30;
    public const short BloomSubdivisions = 20;
    public const int NumVertices = (CylinderWidthSegments + 1) * (CylinderHeightSegments + 1) * 2;
    public const int NumIndices = CylinderWidthSegments * CylinderHeightSegments * 6 * 2;

    private DynamicVertexBuffer beamVertexBuffer;
    private DynamicVertexBuffer bloomVertexBuffer;
    private VertexPositionColorTexture[] beamVertices;
    private VertexPositionColorTexture[] bloomVertices;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 50000;
    }

    public override void SetDefaults()
    {
        Projectile.Size = new(33f);
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.hostile = true;
        Projectile.friendly = false;
        Projectile.timeLeft = 9000000;
        CooldownSlot = ImmunityCooldownID.Bosses;
        InitializeBuffers();
    }
    private void InitializeBuffers()
    {
        Main.QueueMainThreadAction(() =>
        {
            GraphicsDevice gd = Main.instance.GraphicsDevice;

            // Create dynamic vertex buffers
            beamVertexBuffer = new DynamicVertexBuffer(gd, typeof(VertexPositionColorTexture), NumVertices, BufferUsage.WriteOnly);
            bloomVertexBuffer = new DynamicVertexBuffer(gd, typeof(VertexPositionColorTexture), NumVertices * BloomSubdivisions, BufferUsage.WriteOnly);
        });

        // Temporary CPU arrays for filling data
        beamVertices = new VertexPositionColorTexture[NumVertices];
        bloomVertices = new VertexPositionColorTexture[NumVertices * BloomSubdivisions];
    }

    public override void AI()
    {
        Projectile.scale = 30f;
        LaserbeamLength = Animators.MakePoly(3.6f).OutFunction.Evaluate(Time, 0f, 40f, 0f, 1500f);
        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => CircularHitboxCollision(Projectile.Center, Projectile.scale, targetHitbox);

    /*
    public void GetVerticesAndIndices(in Color baseColor, in Vector3 start, in Vector3 end, out VertexPositionColorTexture[] vertices, out int[] indices, float widthFactor = 1f)
    {
        int numVertices = (CylinderWidthSegments + 1) * (CylinderHeightSegments + 1) * 2; // Double for two sides
        int numIndices = CylinderWidthSegments * CylinderHeightSegments * 6 * 2; // Double for two sides

        vertices = new VertexPositionColorTexture[numVertices];
        indices = new int[numIndices];

        float widthStep = 1f / CylinderWidthSegments;
        float heightStep = 1f / CylinderHeightSegments;

        Vector3 direction = Vector3.Normalize(end - start);
        float length = Vector3.Distance(start, end);

        int vertexOffset = 0;
        int indexOffset = 0;

        for (int side = 0; side < 2; side++) // Generate for both sides (0 = right, 1 = left)
        {
            float cylinderOffsetAngle = side == 0 ? MathHelper.Pi : 0f;

            for (int i = 0; i <= CylinderHeightSegments; i++)
            {
                float heightInterpolant = i * heightStep;
                Vector3 cylinderPoint = start + direction * (length * heightInterpolant);

                float width;
                const float percentageFromEnd = 0.2f;
                const float transitionPoint = 1f - percentageFromEnd;

                float thickness = Projectile.scale;

                if (heightInterpolant <= transitionPoint)
                    width = thickness;
                else
                {
                    float term = (heightInterpolant - 1f + percentageFromEnd) / percentageFromEnd;
                    width = thickness * (float)Math.Sqrt(1f - term * term * term); // Hemispherical taper
                }
                width *= widthFactor;

                for (int j = 0; j <= CylinderWidthSegments; j++)
                {
                    float angle = MathHelper.Pi * j * widthStep + cylinderOffsetAngle;
                    Vector3 orthogonalOffset = Vector3.Transform(new Vector3(0f, MathF.Sin(angle), MathF.Cos(angle)), Rotation) * width;
                    Vector3 finalPoint = cylinderPoint + orthogonalOffset;
                    vertices[vertexOffset + i * (CylinderWidthSegments + 1) + j] = new(new(finalPoint.X, finalPoint.Y, finalPoint.Z), baseColor, new Vector2(heightInterpolant, j * widthStep));
                }
            }

            for (int i = 0; i < CylinderHeightSegments; i++)
            {
                for (int j = 0; j < CylinderWidthSegments; j++)
                {
                    int upperLeft = vertexOffset + i * (CylinderWidthSegments + 1) + j;
                    int upperRight = upperLeft + 1;
                    int lowerLeft = upperLeft + CylinderWidthSegments + 1;
                    int lowerRight = lowerLeft + 1;

                    indices[indexOffset + (i * CylinderWidthSegments + j) * 6 + 0] = upperLeft;
                    indices[indexOffset + (i * CylinderWidthSegments + j) * 6 + 1] = lowerRight;
                    indices[indexOffset + (i * CylinderWidthSegments + j) * 6 + 2] = lowerLeft;
                    indices[indexOffset + (i * CylinderWidthSegments + j) * 6 + 3] = upperLeft;
                    indices[indexOffset + (i * CylinderWidthSegments + j) * 6 + 4] = upperRight;
                    indices[indexOffset + (i * CylinderWidthSegments + j) * 6 + 5] = lowerRight;
                }
            }

            vertexOffset += (CylinderWidthSegments + 1) * (CylinderHeightSegments + 1);
            indexOffset += CylinderWidthSegments * CylinderHeightSegments * 6;
        }
    }

    public void GetBloomVerticesAndIndices(Color baseColor, Vector3 start, Vector3 end, out VertexPositionColorTexture[] rightVertices, out int[] indices)
    {
        int numVertices = (CylinderWidthSegments + 1) * (CylinderHeightSegments + 1) * 2;
        int numIndices = CylinderWidthSegments * CylinderHeightSegments * 6 * 2;

        rightVertices = new VertexPositionColorTexture[numVertices * BloomSubdivisions];
        indices = new int[numIndices * BloomSubdivisions];

        for (int i = 0; i < BloomSubdivisions; i++)
        {
            float subdivisionInterpolant = i / (float)(BloomSubdivisions - 1f);
            float bloomWidthFactor = subdivisionInterpolant * 1.3f + 1f;
            Color bloomColor = baseColor * MathHelper.SmoothStep(0.07f, 0.005f, MathF.Sqrt(subdivisionInterpolant));
            GetVerticesAndIndices(bloomColor, start, end, out VertexPositionColorTexture[] localRightVertices, out int[] localIndices, bloomWidthFactor);

            for (int j = 0; j < localRightVertices.Length; j++)
                rightVertices[j + i * numVertices] = localRightVertices[j];

            // Copy indices with offset
            for (int j = 0; j < localIndices.Length; j++)
                indices[j + i * numIndices] = localIndices[j] + i * numVertices;
        }
    }
    */
/*

    private void FillCylinderVertices(in Color baseColor, in Vector3 start, in Vector3 end, Span<VertexPositionColorTexture> vertices, float widthFactor = 1f, int vertexStartOffset = 0)
    {
        float widthStep = 1f / CylinderWidthSegments;
        float heightStep = 1f / CylinderHeightSegments;

        Vector3 direction = Vector3.Normalize(end - start);
        float length = Vector3.Distance(start, end);

        int vertexOffset = vertexStartOffset;

        for (int side = 0; side < 2; side++)
        {
            float cylinderOffsetAngle = side == 0 ? MathHelper.Pi : 0f;

            for (int i = 0; i <= CylinderHeightSegments; i++)
            {
                float heightInterpolant = i * heightStep;
                Vector3 cylinderPoint = start + direction * (length * heightInterpolant);

                float width;
                const float percentageFromEnd = 0.2f;
                const float transitionPoint = 1f - percentageFromEnd;

                float thickness = Projectile.scale;

                if (heightInterpolant <= transitionPoint)
                    width = thickness;
                else
                {
                    float term = (heightInterpolant - 1f + percentageFromEnd) / percentageFromEnd;
                    width = thickness * (float)Math.Sqrt(1f - term * term * term); // Hemispherical taper
                }
                width *= widthFactor;

                for (int j = 0; j <= CylinderWidthSegments; j++)
                {
                    float angle = MathHelper.Pi * j * widthStep + cylinderOffsetAngle;
                    Vector3 orthogonalOffset = Vector3.Transform(new Vector3(0f, MathF.Sin(angle), MathF.Cos(angle)), Rotation) * width;
                    Vector3 finalPoint = cylinderPoint + orthogonalOffset;
                    vertices[vertexOffset + i * (CylinderWidthSegments + 1) + j] = new(new(finalPoint.X, finalPoint.Y, finalPoint.Z), baseColor, new Vector2(heightInterpolant, j * widthStep));
                }
            }

            vertexOffset += (CylinderWidthSegments + 1) * (CylinderHeightSegments + 1);
        }
    }
    private void FillBloomVertices(Color baseColor, Vector3 start, Vector3 end)
    {
        int numVertices = (CylinderWidthSegments + 1) * (CylinderHeightSegments + 1) * 2;

        for (int i = 0; i < BloomSubdivisions; i++)
        {
            float subdivisionInterpolant = i / (float)(BloomSubdivisions - 1f);
            float bloomWidthFactor = subdivisionInterpolant * 1.3f + 1f;
            Color bloomColor = baseColor * MathHelper.SmoothStep(0.07f, 0.005f, MathF.Sqrt(subdivisionInterpolant));
            FillCylinderVertices(bloomColor, start, end, bloomVertices, bloomWidthFactor, i * numVertices);
        }
    }

    public static VertexPositionColorTexture[] GeneratePortalQuad(Vector3 center, Quaternion rotation, float size, Color? color = null)
    {
        // Define a quad centered at the origin, then transform it
        float halfSize = size / 2f;
        Vector3 topLeftPos = new(-halfSize, -halfSize, 0f);
        Vector3 topRightPos = new(halfSize, -halfSize, 0f);
        Vector3 bottomRightPos = new(halfSize, halfSize, 0f);
        Vector3 bottomLeftPos = new(-halfSize, halfSize, 0f);

        // Apply the quaternion rotation and translate to the center
        Matrix rotationMatrix = Matrix.CreateFromQuaternion(rotation);
        Matrix translationMatrix = Matrix.CreateTranslation(center);

        // Transform vertices
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

    public override bool PreDraw(ref Color lightColor)
    {
        int width = Main.instance.GraphicsDevice.Viewport.Width;
        int height = Main.instance.GraphicsDevice.Viewport.Height;

        Vector2 zoom = Main.GameViewMatrix.Zoom;
        Matrix zoomScaleMatrix = Matrix.CreateScale(zoom.X, zoom.Y, 1f);

        // Position camera back along Z to view the scene (adjust distance to match your desired scale/distortion)
        float cameraDistance = 2000f; // larger = less perspective (more ortho-like), smaller = more depth emphasis
        Matrix effectView = Matrix.CreateLookAt(
        new Vector3(width / 2f, -height / 2f, -cameraDistance),
        new Vector3(width / 2f, -height / 2f, 0f),
        Vector3.Up);

        // Existing adjustments (offset, flip, grav, zoom) – apply them after base view
        effectView *= Matrix.CreateTranslation(0f, -height, 0f);
        effectView *= Matrix.CreateRotationZ(MathHelper.Pi);
        if (Main.LocalPlayer.gravDir == -1f)
            effectView *= Matrix.CreateScale(1f, -1f, 1f) * Matrix.CreateTranslation(0f, height, 0f);
        effectView *= zoomScaleMatrix;
        Matrix effectProjection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(50f), (float)Main.screenWidth / Main.screenHeight, 1f, 3000f);
        Matrix effectWorld = Matrix.CreateTranslation(-Main.screenPosition.X, -Main.screenPosition.Y, 0f);

        Matrix overall = effectWorld * effectView * effectProjection;

        Vector3 start = new Vector3(Projectile.Center, 0f);
        Vector3 end = start + Vector3.Transform(Vector3.UnitX, Rotation) * LaserbeamLength;

        GraphicsDevice gd = Main.instance.GraphicsDevice;
        RasterizerState prevRast = gd.RasterizerState;
        DepthStencilState prevStencil = gd.DepthStencilState;
        gd.RasterizerState = RasterizerState.CullCounterClockwise;
        gd.DepthStencilState = DepthStencilState.Default;

        float portalSize = Projectile.scale * 12f; // Adjust size as needed
        VertexPositionColorTexture[] portalVertices = GeneratePortalQuad(start, PortalRotation, portalSize, Color.White);
        short[] portalIndices = Utility.TextureQuadIndices;
        ManagedShader portalShader = AssetRegistry.GetShader("3DPortalShaderAlt");
        portalShader.TrySetParameter("globalTime", Main.GlobalTimeWrappedHourly);
        portalShader.TrySetParameter("scale", Projectile.Opacity);
        portalShader.TrySetParameter("coolColor", Color.DarkCyan.ToVector3());
        portalShader.TrySetParameter("mediumColor", Color.Cyan.ToVector3());
        portalShader.TrySetParameter("hotColor", Color.Cyan.ToVector3());
        portalShader.TrySetParameter("vertexMatrix", overall);
        portalShader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.Pixel), 0, SamplerState.PointClamp);
        portalShader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.DendriticNoiseDim), 1, SamplerState.LinearWrap);
        portalShader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.TurbulentNoise), 2, SamplerState.LinearWrap);
        portalShader.Effect.CurrentTechnique.Passes[ManagedShader.DefaultPassName].Apply();
        gd.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, portalVertices, 0, portalVertices.Length, portalIndices, 0, portalIndices.Length / 3);

        ManagedShader bloomShader = ShaderRegistry.StandardPrimitiveShader;
        bloomShader.TrySetParameter("transformMatrix", overall);
        bloomShader.Effect.CurrentTechnique.Passes[ManagedShader.DefaultPassName].Apply();
        FillBloomVertices(BloomColor, start, end);
        bloomVertexBuffer.SetData(bloomVertices);
        gd.SetVertexBuffer(bloomVertexBuffer);
        gd.Indices = BarrageBeamRegistry.BloomIndices;
        gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, NumVertices * BloomSubdivisions, 0, NumIndices * BloomSubdivisions / 3);

        ManagedShader shader = AssetRegistry.GetShader("AsterlinDeathrayShader");
        shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.ManifoldNoise), 1, SamplerState.LinearWrap);
        shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.TurbulentNoise), 2, SamplerState.LinearWrap);
        shader.TrySetParameter("transformMatrix", overall);
        shader.TrySetParameter("baseColor", BeamColor.ToVector3());
        shader.TrySetParameter("globalTime", Main.GlobalTimeWrappedHourly);
        shader.Effect.CurrentTechnique.Passes[ManagedShader.DefaultPassName].Apply();
        FillCylinderVertices(BeamColor, start, end, beamVertices);
        beamVertexBuffer.SetData(beamVertices);
        gd.SetVertexBuffer(beamVertexBuffer);
        gd.Indices = BarrageBeamRegistry.CylinderIndices;
        gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, NumVertices, 0, NumIndices / 3);

        gd.RasterizerState = prevRast;
        gd.DepthStencilState = prevStencil;
        gd.Indices = null;
        return false;
    }
}
*/