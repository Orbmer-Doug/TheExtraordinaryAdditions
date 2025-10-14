using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles.Shader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

public class LightPillar : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public int Time
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }
    public int FadeTime
    {
        get => (int)Projectile.ai[1];
        set => Projectile.ai[1] = value;
    }
    public ref float FallSpeed => ref Projectile.ai[2];
    public bool HitGround
    {
        get => Projectile.AdditionsInfo().ExtraAI[0] == 1;
        set => Projectile.AdditionsInfo().ExtraAI[0] = value.ToInt();
    }

    public const int Width = 86;

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 1000;
    }

    public override void SetDefaults()
    {
        Projectile.Size = new(Width);
        Projectile.Opacity = 1f;
        Projectile.friendly = false;
        Projectile.tileCollide = Projectile.ignoreWater = Projectile.hostile = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 90000;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }

    public override void AI()
    {
        if (flame == null || flame.Disposed)
            flame = new(WidthFunction, ColorFunction, null, 20);

        Vector2 start = Projectile.Center + Vector2.UnitY * Projectile.width / 2;
        if (Time < Asterlin.Cleave_PillarWait)
            trail.SetPoints(Projectile.Center.GetLaserControlPoints(start - Vector2.UnitY * 500f, 20));
        else if (Time == Asterlin.Cleave_PillarWait)
            Projectile.velocity = Vector2.UnitY * FallSpeed;
        else if (Time > Asterlin.Cleave_PillarWait)
            trail.Update(start);

        if (HitGround)
        {
            Projectile.Opacity = Animators.MakePoly(3f).OutFunction.Evaluate(FadeTime, 0f, 30f, 1f, 0f);
            if (Projectile.Opacity <= 0)
                Projectile.Kill();
            FadeTime++;
        }
        else
            Projectile.Opacity = Animators.BezierEase(InverseLerp(0f, 20f, Time));

        Time++;
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (!HitGround)
        {
            AdditionsSound.RockBreak.Play(Projectile.Center, 1f, -.1f, .2f);
            for (int i = 0; i < 30; i++)
            {
                Vector2 pos = Projectile.Center + Vector2.UnitY * Projectile.width / 2 + Vector2.UnitX * Main.rand.NextFloat(-Projectile.width / 2, Projectile.width / 2);
                ParticleRegistry.SpawnBloomLineParticle(pos, -Vector2.UnitY * Main.rand.NextFloat(1f, 9f), Main.rand.Next(30, 50), Main.rand.NextFloat(.4f, .8f), Color.OrangeRed);
                ParticleRegistry.SpawnSquishyPixelParticle(pos, -Vector2.UnitY.RotatedByRandom(.1f) * Main.rand.NextFloat(3f, 14f), Main.rand.Next(30, 60), Main.rand.NextFloat(1.8f, 2.9f), Color.OrangeRed, Color.Gold, 5);
                ShaderParticleRegistry.SpawnMoltenParticle(pos, Main.rand.NextFloat(40f, 60f));
            }

            HitGround = true;
            Projectile.netUpdate = true;
        }
        return false;
    }

    public override bool? CanDamage() => Time > Asterlin.Cleave_PillarWait ? null : false;

    public TrailPoints trail = new(20);
    public OptimizedPrimitiveTrail flame;
    public float WidthFunction(float completionRatio) => Projectile.width;
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
            if (trail != null && flame != null)
            {
                ManagedShader shader = AssetRegistry.GetShader("LightPillarShader");
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.HarshNoise), 1, SamplerState.AnisotropicWrap);
                flame.DrawTrail(shader, trail.Points);
            }
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.OverPlayers);
        return false;
    }
}