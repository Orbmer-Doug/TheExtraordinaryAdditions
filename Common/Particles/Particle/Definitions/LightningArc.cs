using Microsoft.Xna.Framework.Graphics;
using Terraria;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;

namespace TheExtraordinaryAdditions.Common.Particles;

public partial class ParticleRegistry
{
    private unsafe struct LightningArcData
    {
        public Vector2 Vel;
        public fixed float PointsX[30];
        public fixed float PointsY[30];
        public bool PointsGenerated;
    }

    private struct LightningArcContext(ParticleData particle)
    {
        public float TimeRatio = particle.TimeRatio;
        public float Scale = particle.Scale;
        public Color Color = particle.Color;
        public float Opacity = particle.Opacity;
        public OptimizedPrimitiveTrail trail;
    }

    private unsafe readonly struct LightningArcParticleDefinition
    {
        static LightningArcParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.LightningArc,
                texture: AssetRegistry.InvisTex,
                blendState: BlendState.AlphaBlend,
                update: static (ref ParticleData p) =>
                {
                    ref LightningArcData custom = ref p.GetCustomData<LightningArcData>();
                    if (!custom.PointsGenerated)
                    {
                        GenerateArcPoints(ref p, ref custom, initial: true);
                        custom.PointsGenerated = true;
                    }
                    else
                    {
                        UpdateArcPoints(ref p, ref custom);
                    }
                },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
                {
                    ref LightningArcData custom = ref p.GetCustomData<LightningArcData>();
                    if (!custom.PointsGenerated)
                        return;

                    Vector2[] points = new Vector2[30];
                    for (int i = 0; i < 30; i++)
                    {
                        points[i] = new Vector2(custom.PointsX[i], custom.PointsY[i]);
                    }

                    ManagedShader shader = ShaderRegistry.LightningArcShader;
                    shader.TrySetParameter("lifetimeRatio", p.TimeRatio);
                    shader.TrySetParameter("erasureThreshold", 0.75f);
                    shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.WavyNeurons), 1, SamplerState.LinearWrap);

                    LightningArcContext context = new(p);
                    if (context.trail == null)
                        context.trail = new(
                        widthFunction: completionRatio => ArcWidthFunction(context, completionRatio),
                        colorFunction: (factor, pos) => ArcColorFunction(context, factor),
                        offsetFunction: null,
                        maxTrailPoints: 30
                    );
                    context.trail?.DrawTrail(shader, points, 30);
                },
                drawType: DrawTypes.Pixelize,
                isPrimitive: true
                ));
        }
    }

    private static float ArcWidthFunction(LightningArcContext context, float completionRatio)
    {
        float lifetimeSquish = GetLerpBump(0.1f, 0.35f, 1f, 0.75f, context.TimeRatio);
        return MathHelper.Lerp(1f, 3f, Convert01To010(completionRatio)) * lifetimeSquish * context.Scale;
    }

    private static Color ArcColorFunction(LightningArcContext context, SystemVector2 factor)
    {
        return Color.Lerp(Color.White, context.Color, factor.X) * context.Opacity;
    }

    private unsafe static void GenerateArcPoints(ref ParticleData p, ref LightningArcData custom, bool initial)
    {
        Vector2 start = p.Position;
        Vector2 lengthForPerpendicular = custom.Vel.ClampLength(0f, 740f);
        Vector2 end = start + custom.Vel * Main.rand.NextFloat(0.67f, 1.2f) + Main.rand.NextVector2Circular(30f, 30f);
        Vector2 farFront = start - lengthForPerpendicular.RotatedByRandom(3.1f) * Main.rand.NextFloat(0.26f, 0.8f);
        Vector2 farEnd = end + lengthForPerpendicular.RotatedByRandom(3.1f) * 4f;

        for (int i = 0; i < 30; i++)
        {
            Vector2 point = Vector2.CatmullRom(farFront, start, end, farEnd, i / 29f);
            if (initial && Main.rand.NextBool(9))
                point += Main.rand.NextVector2Circular(10f, 10f);
            custom.PointsX[i] = point.X;
            custom.PointsY[i] = point.Y;
        }
    }

    private unsafe static void UpdateArcPoints(ref ParticleData p, ref LightningArcData custom)
    {
        for (int i = 0; i < 30; i += 2)
        {
            float trailCompletionRatio = i / 29f;
            float arcProtrudeAngleOffset = Main.rand.NextGaussian(0.63f) + MathHelper.PiOver2;
            float arcProtrudeDistance = Main.rand.NextGaussian(4.6f);
            if (Main.rand.NextBool(100))
                arcProtrudeDistance *= 3f;

            Vector2 arcOffset = custom.Vel.SafeNormalize(Vector2.Zero).RotatedBy(arcProtrudeAngleOffset) * arcProtrudeDistance;
            custom.PointsX[i] += arcOffset.X;
            custom.PointsY[i] += arcOffset.Y;
        }
    }

    public static void SpawnLightningArcParticle(Vector2 pos, Vector2 dist, int life, float scale, Color color)
    {
        ParticleData particle = new()
        {
            Position = pos,
            Velocity = Vector2.Zero,
            Lifetime = life,
            Scale = scale,
            Color = color,
            Opacity = 1f,
            Type = ParticleTypes.LightningArc,
            Width = 2,
            Height = 2
        };
        ref LightningArcData custom = ref particle.GetCustomData<LightningArcData>();
        custom.Vel = dist;
        custom.PointsGenerated = false;
        SafeSpawn(particle);
    }
}
