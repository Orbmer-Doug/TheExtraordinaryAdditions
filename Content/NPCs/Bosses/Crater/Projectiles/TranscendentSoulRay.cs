using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets.Audio;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

// An adaptation of the deathray made by Lucille Karma in Wrath of the Gods:
// https://github.com/LucilleKarma/WrathOfTheMachines/blob/main/Content/NPCs/ExoMechs/Projectiles/ExoOverloadbeam.cs
public class TranscendentSoulRay : ProjOwnedByNPC<Asterlin>
{
    /// <summary>
    /// The rotation of this beam
    /// </summary>
    public Quaternion Rotation
    {
        get;
        set;
    }

    public Projectile ProjOwner;

    /// <summary>
    /// How long this beam has existed
    /// </summary>
    public ref float Time => ref Projectile.ai[0];

    /// <summary>
    /// How long this laserbeam currently is
    /// </summary>
    public ref float LaserbeamLength => ref Projectile.ai[1];

    /// <summary>
    /// The rotation away from the screen
    /// </summary>
    public ref float SideAngle => ref Projectile.ai[2];

    public int SwayCounter
    {
        get => (int)Projectile.Additions().ExtraAI[0];
        set => Projectile.Additions().ExtraAI[0] = value;
    }

    /// <summary>
    /// The maximum length of this laserbeam
    /// </summary>
    public static float MaxLaserbeamLength => 6000f;

    /// <summary>
    /// How many segments should be generated for the cylinder when subdiving its radial part
    /// </summary>
    public const int CylinderWidthSegments = 12;

    /// <summary>
    /// How many segments should be generated for the cylinder when subdiving its height part
    /// </summary>
    public const int CylinderHeightSegments = 8;

    /// <summary>
    /// The amount of cylinders used when subdividing the vertices for use by the bloom
    /// </summary>
    public const int BloomSubdivisions = 20;

    public LoopedSound gamma;

    public override string Texture => AssetRegistry.Invis;
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 6000;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 180;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.hostile = true;
        Projectile.friendly = false;
        Projectile.timeLeft = 9000000;

        // This is done for more precision in the collision checks, due to the fact that the laser moves rather quickly
        // Wouldn't want it to skip over the player's hitbox in a single update and do nothing
        Projectile.MaxUpdates = 2;

        CooldownSlot = ImmunityCooldownID.Bosses;
        LaserbeamLength = 800f;
    }

    public override void SendAI(BinaryWriter writer)
    {
        writer.Write(Rotation.X);
        writer.Write(Rotation.Y);
        writer.Write(Rotation.Z);
        writer.Write(Rotation.W);
    }

    public override void ReceiveAI(BinaryReader reader)
    {
        float rotationX = reader.ReadSingle();
        float rotationY = reader.ReadSingle();
        float rotationZ = reader.ReadSingle();
        float rotationW = reader.ReadSingle();
        Rotation = new(rotationX, rotationY, rotationZ, rotationW);
    }

    public override void SafeAI()
    {
        // Update the menacing sound
        gamma ??= new(AssetRegistry.GetSound(AdditionsSound.sunAura) with { MaxInstances = 40 }, () => new ProjectileAudioTracker(Projectile).IsActiveAndInGame());
        gamma.Update(() => Projectile.Center, () => MathHelper.Clamp(Projectile.scale, 0f, 1f), () => -.05f);

        // Angle it up and down
        Projectile.rotation = MathHelper.PiOver2;

        float minAngle = .2f;
        float maxAngle = MathHelper.Pi - minAngle;
        if (Time < Asterlin.Hyperbeam_BeamBuildTime)
            SideAngle = Animators.MakePoly(2.2f).OutFunction.Evaluate(Time, 0f, Asterlin.Hyperbeam_BeamBuildTime, MathHelper.PiOver2, maxAngle);
        else
        {
            SideAngle = MathHelper.Lerp(minAngle, maxAngle, Cos01(SwayCounter * .01f));
            SwayCounter++;
        }

        Rotation = Animators.EulerAnglesConversion(1, Projectile.rotation, SideAngle);
        Projectile.Center = ProjOwner.Center;

        if (Boss.Hyperbeam_CurrentState == Asterlin.Hyperbeam_States.Fade)
        {
            LaserbeamLength = Animators.Sine.InOutFunction.Evaluate(Boss.AITimer, 0f, Asterlin.Hyperbeam_FadeTime, MaxLaserbeamLength, 0f);
            Projectile.Opacity = Animators.MakePoly(2.2f).InFunction.Evaluate(Boss.AITimer, 0f, Asterlin.Hyperbeam_FadeTime * .35f, 1f, 0f);
            Projectile.scale = Animators.MakePoly(3f).InOutFunction.Evaluate(Boss.AITimer, 0f, Asterlin.Hyperbeam_FadeTime, 2f, 0f);
            if (LaserbeamLength <= 0f)
                Projectile.Kill();
        }
        else if (Boss.Hyperbeam_CurrentState != Asterlin.Hyperbeam_States.Fade)
        {
            LaserbeamLength = Animators.CubicBezier(.12f, 1f, .61f, .98f).Evaluate(Time, 0f, Asterlin.Hyperbeam_BeamBuildTime, 0f, MaxLaserbeamLength);
            Projectile.Opacity = Animators.MakePoly(2.2f).OutFunction.Evaluate(Time, 0f, Asterlin.Hyperbeam_BeamBuildTime * .35f, 0f, 1f);
            Projectile.scale = Animators.MakePoly(3f).InOutFunction.Evaluate(Time, 0f, Asterlin.Hyperbeam_BeamBuildTime * .6f, 0f, 2f);
        }

        // Rack up time of existing
        Time++;
    }

    public override bool ShouldUpdatePosition() => false;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return false;
    }

    public void GetBloomVerticesAndIndices(Color baseColor, Vector3 start, Vector3 end, out Vertex2D[] leftVertices, out Vertex2D[] rightVertices, out int[] indices)
    {
        int numVertices = (CylinderWidthSegments + 1) * (CylinderHeightSegments + 1);
        int numIndices = CylinderWidthSegments * CylinderHeightSegments * 6;

        leftVertices = new Vertex2D[numVertices * BloomSubdivisions];
        rightVertices = new Vertex2D[leftVertices.Length];
        indices = new int[numIndices * BloomSubdivisions * 6];

        for (int i = 0; i < BloomSubdivisions; i++)
        {
            float subdivisionInterpolant = i / (float)(BloomSubdivisions - 1f);
            float bloomWidthFactor = subdivisionInterpolant * 1.3f + 1f;
            Color bloomColor = baseColor * MathHelper.SmoothStep(0.05f, 0.005f, MathF.Sqrt(subdivisionInterpolant));
            GetVerticesAndIndices(bloomWidthFactor, bloomColor, start, end, MathHelper.Pi, out Vertex2D[] localRightVertices, out int[] localIndices);
            GetVerticesAndIndices(bloomWidthFactor, bloomColor, start, end, 0f, out Vertex2D[] localLeftVertices, out _);

            for (int j = 0; j < localIndices.Length; j++)
                indices[j + i * numIndices] = localIndices[j] + i * numVertices;
            for (int j = 0; j < localLeftVertices.Length; j++)
            {
                leftVertices[j + i * numVertices] = localLeftVertices[j];
                rightVertices[j + i * numVertices] = localRightVertices[j];
            }
        }
    }

    /// <summary>
    /// Collects vertices and indices for rendering the laser cylinder.
    /// </summary>
    /// <param name="widthFactor">The width factor of the cylinder.</param>
    /// <param name="baseColor">The color of the cylinder.</param>
    /// <param name="start">The starting point of the beam, in 3D space.</param>
    /// <param name="end">The ending point of the beam, in 3D space.</param>
    /// <param name="cylinderOffsetAngle">The offset angle of the vertices on the cylinder.</param>
    /// <param name="vertices">The resulting vertices.</param>
    /// <param name="indices">The resulting indices.</param>
    public void GetVerticesAndIndices(float widthFactor, Color baseColor, Vector3 start, Vector3 end, float cylinderOffsetAngle, out Vertex2D[] vertices, out int[] indices)
    {
        int numVertices = (CylinderWidthSegments + 1) * (CylinderHeightSegments + 1);
        int numIndices = CylinderWidthSegments * CylinderHeightSegments * 6;

        vertices = new Vertex2D[numVertices];
        indices = new int[numIndices];

        float widthStep = 1f / CylinderWidthSegments;
        float heightStep = 1f / CylinderHeightSegments;

        Vector3 direction = Vector3.Normalize(end - start);
        float length = Vector3.Distance(start, end);

        for (int i = 0; i <= CylinderHeightSegments; i++)
        {
            float heightInterpolant = i * heightStep; // 0 to 1 along the height
            float curvedHeightFactor = heightInterpolant * heightInterpolant; // Quadratic curve for smoother taper
            Vector3 cylinderPoint = start + direction * (length * curvedHeightFactor);

            // Taper width with a smooth decrease, rounding at the top
            float baseWidth = Utils.Remap(heightInterpolant, 0f, 0.67f, 32f, Projectile.width * Projectile.scale) * widthFactor;
            float taperFactor = 1f - MathF.Pow(heightInterpolant, 3.4f);
            float width = baseWidth * taperFactor;

            for (int j = 0; j <= CylinderWidthSegments; j++)
            {
                float angle = MathHelper.Pi * j * widthStep + cylinderOffsetAngle;
                Vector3 orthogonalOffset = Vector3.Transform(new Vector3(0f, MathF.Sin(angle), MathF.Cos(angle)), Rotation) * width;
                Vector3 finalPoint = cylinderPoint + orthogonalOffset;
                vertices[i * (CylinderWidthSegments + 1) + j] = new(new(finalPoint.X, finalPoint.Y), baseColor, new SystemVector2(heightInterpolant, j * widthStep));
            }
        }

        for (int i = 0; i < CylinderHeightSegments; i++)
        {
            for (int j = 0; j < CylinderWidthSegments; j++)
            {
                int upperLeft = i * (CylinderWidthSegments + 1) + j;
                int upperRight = upperLeft + 1;
                int lowerLeft = upperLeft + CylinderWidthSegments + 1;
                int lowerRight = lowerLeft + 1;

                indices[(i * CylinderWidthSegments + j) * 6 + 0] = upperLeft;
                indices[(i * CylinderWidthSegments + j) * 6 + 1] = lowerRight;
                indices[(i * CylinderWidthSegments + j) * 6 + 2] = lowerLeft;

                indices[(i * CylinderWidthSegments + j) * 6 + 3] = upperLeft;
                indices[(i * CylinderWidthSegments + j) * 6 + 4] = upperRight;
                indices[(i * CylinderWidthSegments + j) * 6 + 5] = lowerRight;
            }
        }
    }

    /// <summary>
    /// Renders the beam.
    /// </summary>
    /// <param name="start">The starting point of the beam, in 3D space.</param>
    /// <param name="end">The ending point of the beam, in 3D space.</param>
    /// <param name="baseColor">The color of the cylinder.</param>
    /// <param name="widthFactor">The width factor of the cylinder.</param>
    public void RenderLaser(Vector3 start, Vector3 end, Color baseColor, float widthFactor)
    {
        GraphicsDevice gd = Main.instance.GraphicsDevice;
        GetVerticesAndIndices(widthFactor, baseColor, start, end, MathHelper.Pi, out Vertex2D[] rightVertices, out int[] indices);
        gd.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, rightVertices, 0, rightVertices.Length, indices, 0, indices.Length / 3);

        GetVerticesAndIndices(widthFactor, baseColor, start, end, 0f, out Vertex2D[] leftVertices, out _);
        gd.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, leftVertices, 0, leftVertices.Length, indices, 0, indices.Length / 3);
    }

    /// <summary>
    /// Renders bloom for the beam.
    /// </summary>
    /// <param name="start">The starting point of the beam, in 3D space.</param>
    /// <param name="end">The ending point of the beam, in 3D space.</param>
    /// <param name="projection">The matrix responsible for manipulating primitive vertices.</param>
    public void RenderBloom(Vector3 start, Vector3 end)
    {
        Color bloomColor = Color.Orange with { A = 0 };

        ManagedShader bloomShader = ShaderRegistry.StandardPrimitiveShader;
        bloomShader.Render();

        GetBloomVerticesAndIndices(bloomColor, start, end, out Vertex2D[] leftVertices, out Vertex2D[] rightVertices, out int[] indices);

        GraphicsDevice gd = Main.instance.GraphicsDevice;
        gd.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, rightVertices, 0, rightVertices.Length, indices, 0, indices.Length / 3);
        gd.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, leftVertices, 0, leftVertices.Length, indices, 0, indices.Length / 3);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        void drawLaser()
        {
            Vector3 start = new(Projectile.Center, 0f);
            Vector3 end = start + Vector3.Transform(Vector3.UnitX, Rotation) * LaserbeamLength;
            end.Z /= LaserbeamLength;

            GraphicsDevice gd = Main.instance.GraphicsDevice;
            gd.RasterizerState = RasterizerState.CullNone;

            int width = Main.screenWidth;
            int height = Main.screenHeight;

            RenderBloom(start, end);

            Color color = new Color((byte)(byte.MaxValue * .961f), (byte)(byte.MaxValue * .592f), (byte)(byte.MaxValue * .078f));
            ManagedShader shader = AssetRegistry.GetShader("AsterlinDeathrayShader");
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.DendriticNoiseDim), 1, SamplerState.LinearWrap);
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.SuperPerlin), 2, SamplerState.LinearWrap);
            shader.TrySetParameter("baseColor", color.ToVector3());
            shader.Render();

            RenderLaser(start, end, color, 1f);
        }
        PixelationSystem.QueuePrimitiveRenderAction(drawLaser, PixelationLayer.OverPlayers);
        return false;
    }
}