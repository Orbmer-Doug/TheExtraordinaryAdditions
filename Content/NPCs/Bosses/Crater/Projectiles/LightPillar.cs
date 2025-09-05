using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles.Metaball;
using TheExtraordinaryAdditions.Common.Particles.Shader;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

public class LightPillar : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public ref float Time => ref Projectile.ai[0];
    public ref float CurrentLength => ref Projectile.ai[1];
    public ref float CurrentWidth => ref Projectile.ai[2];

    public const float MaxLength = 5500f;
    public const float MaxWidth = 300f;

    public float TelegraphCompletion => InverseLerp(0f, Asterlin.Judgement_PillarTelegraphTime, Time);
    public float LightCompletion => InverseLerp(Asterlin.Judgement_PillarTelegraphTime, Asterlin.Judgement_PillarTelegraphTime + Asterlin.Judgement_PillarFlameTime, Time);
    public float FadeCompletion => InverseLerp(Asterlin.Judgement_PillarTelegraphTime + Asterlin.Judgement_PillarFlameTime, Asterlin.Judgement_PillarTelegraphTime + Asterlin.Judgement_PillarFlameTime + Asterlin.Judgement_PillarFadeTime, Time);

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = (int)MaxLength + Main.screenWidth;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 4;
        Projectile.friendly = Projectile.tileCollide = false;
        Projectile.ignoreWater = Projectile.hostile = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 90000;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }

    public override void AI()
    {
        if (FadeCompletion >= 1f)
        {
            Projectile.Kill();
            return;
        }
        if (Time == 0)
            Projectile.Center = FindNearestSurface(Projectile.Center, true, 9000f, 1, true) ?? Projectile.Center;

        if (tele == null || tele._disposed)
            tele = new(TelegraphWidthFunction, TelegraphColorFunction, null, 80);
        if (flame == null || flame._disposed)
            flame = new(WidthFunction, ColorFunction, null, 80);

        if (LightCompletion < 1f)
        {
            int start = Asterlin.Judgement_PillarTelegraphTime;
            CurrentLength = Animators.MakePoly(1.8f).OutFunction.Evaluate(Time, start, start + 60, 0f, MaxLength);
            CurrentWidth = Animators.MakePoly(3f).InOutFunction.Evaluate(Time, start, start + 50, 0f, MaxWidth);
            Projectile.Opacity = InverseLerp(start, start + 20, Time);

            for (int i = 0; i < 4; i++)
            {
                float maxDist = MaxWidth / 1.2f;
                float dist = Main.rand.NextFloat(-maxDist, maxDist);
                float size = Main.rand.NextFloat(60f, 100f) * InverseLerp(maxDist, 0f, MathF.Abs(dist));
                MoltenBall.Spawn(Projectile.Center - Vector2.UnitX * dist + Vector2.UnitY, size);
            }
        }
        else if (FadeCompletion < 1f)
        {
            CurrentLength = Animators.Sine.InOutFunction.Evaluate(MaxLength, 0f, FadeCompletion);
            CurrentWidth = Animators.MakePoly(2f).InFunction.Evaluate(MaxWidth, 0f, FadeCompletion);
            Projectile.Opacity = 1f - FadeCompletion;
        }

        telePoints.SetPoints(Projectile.Center.GetLaserControlPoints(Projectile.Center - Vector2.UnitY * MaxLength, 80));
        points.SetPoints(Projectile.Center.GetLaserControlPoints(Projectile.Center - Vector2.UnitY * CurrentLength, 80));

        Time++;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => LightCompletion > 0f && FadeCompletion <= 0f;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.LineCollision(Projectile.Center, Projectile.Center - Vector2.UnitY * CurrentLength, CurrentWidth);
    }

    public ManualTrailPoints telePoints = new(80);
    public OptimizedPrimitiveTrail tele;
    public float TelegraphWidthFunction(float completionRatio) => MaxWidth * TelegraphCompletion;
    public Color TelegraphColorFunction(SystemVector2 c, Vector2 pos)
    {
        return Color.Lerp(Color.Lerp(Color.OrangeRed, Color.Gold, 0.5f), Color.Goldenrod, c.X) * QuadraticBump(TelegraphCompletion) * GetLerpBump(0f, .03f, 1f, .7f, c.X) * .5f;
    }

    public ManualTrailPoints points = new(80);
    public OptimizedPrimitiveTrail flame;
    public float WidthFunction(float completionRatio) => CurrentWidth;
    public Color ColorFunction(SystemVector2 c, Vector2 pos)
    {
        float interpolant = (1f + MathF.Sin(Main.GlobalTimeWrappedHourly * 2f)) / 2f;
        float colorInterpolant = MathHelper.Lerp(0.3f, 0.5f, interpolant);
        float trailOpacity = GetLerpBump(0f, 0.08f, 1f, 0.25f, c.X) * .9f;
        Color finalColor = Color.Lerp(Color.OrangeRed, Color.Gold, colorInterpolant) * 1.3f;
        finalColor.A = (byte)(trailOpacity * byte.MaxValue);

        return Color.Goldenrod * trailOpacity * Projectile.Opacity;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (telePoints != null && tele != null)
            {
                ManagedShader shader = ShaderRegistry.StandardPrimitiveShader;
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.Perlin), 1, SamplerState.LinearWrap);
                tele.DrawTrail(shader, telePoints.Points);
            }

            if (points != null && flame != null)
            {
                ManagedShader shader = AssetRegistry.GetShader("LightPillarShader");
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.HarshNoise), 1, SamplerState.AnisotropicWrap);
                flame.DrawTrail(shader, points.Points);
            }
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.OverPlayers);
        return false;
    }
}