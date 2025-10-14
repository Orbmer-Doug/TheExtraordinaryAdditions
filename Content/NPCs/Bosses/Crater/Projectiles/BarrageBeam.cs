using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;

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
public sealed class BarrageBeamManager : ModSystem
{
    public static IndexBuffer CylinderIndices;
    public static IndexBuffer BloomIndices;
    public static IndexBuffer PortalIndices;
    public static DynamicVertexBuffer BatchBeamVertexBuffer;
    public static DynamicVertexBuffer BatchPortalVertexBuffer;
    public static DynamicVertexBuffer BatchBloomVertexBuffer;

    public static ManagedRenderTarget BeamRenderTarget;
    public static ManagedRenderTarget PortalRenderTarget;

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

    public static bool Golden;

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
            PortalRenderTarget = new ManagedRenderTarget(true, (w, h) => new RenderTarget2D(Main.instance.GraphicsDevice, w / 2, h / 2), true);
            Main.instance.GraphicsDevice.SetRenderTarget(PortalRenderTarget);
            Main.instance.GraphicsDevice.Clear(Color.Transparent);
            Main.instance.GraphicsDevice.SetRenderTarget(null);

            On_Main.DoDraw_WallsTilesNPCs += DrawPortalTarget;
            On_Main.DrawPlayers_AfterProjectiles += DrawBeamTarget;
        });

        RenderTargetManager.RenderTargetUpdateLoopEvent += DrawToTargets;
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
            PortalRenderTarget?.Dispose();
            CylinderIndices = null;
            PortalIndices = null;
            BatchBeamVertexBuffer = null;
            BatchPortalVertexBuffer = null;
            BatchBloomVertexBuffer = null;
            BeamRenderTarget = null;
            PortalRenderTarget = null;

            On_Main.DoDraw_WallsTilesNPCs -= DrawPortalTarget;
            On_Main.DrawPlayers_AfterProjectiles -= DrawBeamTarget;
        });

        RenderTargetManager.RenderTargetUpdateLoopEvent -= DrawToTargets;
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

    private static void DrawToTargets()
    {
        if (!AssetRegistry.HasFinishedLoading || Main.gameMenu || Main.netMode == NetmodeID.Server || ActiveBeams.Count == 0)
            return;

        // Compute primitive matrices
        Matrix overall = Get3DPerspectivePrimitiveMatrix();

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
            FillCylinderVertices(beam.Projectile.scale, RotationMatrix, (Golden ? Color.Gold : new Color(28, 225, 255)) * beam.Projectile.Opacity, start, end, beamVertices.AsSpan(i * NumVertices, NumVertices), CylinderWidthSegments, CylinderHeightSegments);

            // Fill bloom vertices
            for (int b = 0; b < NumBloomLayers; b++)
            {
                float scaleFactor = 1f + (b + 1) * 0.2f;
                float alpha = 0.5f / (b + 1);
                FillCylinderVertices(beam.Projectile.scale, RotationMatrix, (Golden ? Color.Goldenrod : Color.DeepSkyBlue) with { A = 0 } * alpha * beam.Projectile.Opacity, start, end, bloomVertices.AsSpan((i * NumBloomLayers + b) * BloomNumVertices, BloomNumVertices), BloomWidthSegments, BloomHeightSegments, scaleFactor);
            }

            // Fill portal vertices
            VertexPositionColorTexture[] portalVerts = GenerateQuadClockwise(new(beam.Projectile.scale * 12f), portalStart, PortalRotation, Color.White, true);
            Array.Copy(portalVerts, 0, portalVertices, i * NumPortalVertices, NumPortalVertices);
        }

        // Upload to GPU
        BatchBeamVertexBuffer.SetData(beamVertices);
        BatchBloomVertexBuffer.SetData(bloomVertices);
        BatchPortalVertexBuffer.SetData(portalVertices);

        GraphicsDevice device = Main.instance.GraphicsDevice;
        device.RasterizerState = RasterizerState.CullCounterClockwise;
        device.DepthStencilState = DepthStencilState.Default;
        device.BlendState = BlendState.AlphaBlend;

        device.SetRenderTarget(PortalRenderTarget);
        device.Clear(Color.Transparent);

        // Draw portal
        ManagedShader portalShader = AssetRegistry.GetShader("3DPortalShaderAlt");
        portalShader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.Pixel), 0, SamplerState.PointClamp);
        portalShader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.DendriticNoiseDim), 1, SamplerState.LinearWrap);
        portalShader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.TurbulentNoise), 2, SamplerState.LinearWrap);
        portalShader.TrySetParameter("globalTime", Main.GlobalTimeWrappedHourly);
        portalShader.TrySetParameter("coolColor", (Golden ? Color.DarkGoldenrod : Color.DarkCyan).ToVector3());
        portalShader.TrySetParameter("mediumColor", (Golden ? Color.Gold : Color.Cyan).ToVector3());
        portalShader.TrySetParameter("hotColor", (Golden ? Color.Gold : Color.Cyan).ToVector3());
        portalShader.TrySetParameter("vertexMatrix", overall);
        device.SetVertexBuffer(BatchPortalVertexBuffer);
        device.Indices = PortalIndices;
        for (int i = 0; i < ActiveBeams.Count; i++)
        {
            portalShader.TrySetParameter("scale", ActiveBeams[i].PortalInterpolant); // So its unique every projectile
            portalShader.Effect.CurrentTechnique.Passes[ManagedShader.DefaultPassName].Apply();
            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, i * NumPortalVertices, NumPortalVertices, i * NumPortalIndices, NumPortalIndices / 3);
        }

        device.SetRenderTarget(BeamRenderTarget);
        device.Clear(Color.Transparent);

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
        shader.TrySetParameter("baseColor", (Golden ? Color.Goldenrod : new Color(28, 225, 255)).ToVector3());
        shader.TrySetParameter("globalTime", Main.GlobalTimeWrappedHourly);
        shader.Effect.CurrentTechnique.Passes[ManagedShader.DefaultPassName].Apply();
        device.SetVertexBuffer(BatchBeamVertexBuffer);
        device.Indices = CylinderIndices;
        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, NumVertices * ActiveBeams.Count, 0, NumIndices * ActiveBeams.Count / 3);

        device.SetRenderTarget(null);
        device.SetVertexBuffer(null);
        device.Indices = null;

        if (!Main.gamePaused && Main.hasFocus)
            Golden = false;
    }

    private static void DrawPortalTarget(On_Main.orig_DoDraw_WallsTilesNPCs orig, Main self)
    {
        if (ActiveBeams.Count != 0 && AssetRegistry.HasFinishedLoading && !Main.gameMenu && Main.netMode != NetmodeID.Server)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(PortalRenderTarget, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
        }

        orig(self);
    }

    private static void DrawBeamTarget(On_Main.orig_DrawPlayers_AfterProjectiles orig, Main self)
    {
        orig(self);

        if (ActiveBeams.Count != 0 && AssetRegistry.HasFinishedLoading && !Main.gameMenu && Main.netMode != NetmodeID.Server)
        {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(BeamRenderTarget, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
        }
    }
}

public class BarrageBeam : ProjOwnedByNPC<Asterlin>
{
    public override string Texture => AssetRegistry.Invis;
    public int MaxLaserLength => 1500;
    public ref float Time => ref Projectile.ai[0];
    public ref float LaserbeamLength => ref Projectile.ai[1];
    public ref float PortalInterpolant => ref Projectile.ai[2];
    public ref float PortalZPos => ref Projectile.AdditionsInfo().ExtraAI[0];
    public ref float LaserZPos => ref Projectile.AdditionsInfo().ExtraAI[1];
    public Vector2 TargetPosition;

    public override void SendAI(BinaryWriter writer)
    {
        writer.Write((float)Projectile.scale);
        writer.WriteVector2((Vector2)TargetPosition);
    }
    public override void ReceiveAI(BinaryReader reader)
    {
        Projectile.scale = (float)reader.ReadSingle();
        TargetPosition = (Vector2)reader.ReadVector2();
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

    public override void SafeAI()
    {
        if (Time == 0 && this.RunServer())
        {
            const float clusteringDistance = 360f;
            const int maxTries = 150;

            int tries = 0;
            Vector2 pos;
            bool validPosition;

            do
            {
                validPosition = true;

                pos = Target.Center + Vector2.Zero.SafeNormalize(Main.rand.NextVector2Unit()).RotatedByRandom(RandomRotation()) * (new Vector2(700f, 420f) * Main.rand.NextFloat(.1f, 1f));
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

            Projectile.scale = BarrageBeamManager.Golden ? 40f : 30f;

            TargetPosition = pos;
            this.Sync();
        }

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
    public override bool PreDraw(ref Color lightColor) => false;
}