using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

public class LightrippingBeam : ProjOwnedByNPC<Asterlin>
{
    public override string Texture => AssetRegistry.Invis;

    public static readonly int TelegraphTime = SecondsToFrames(.8f);
    public static readonly int BeamTime = SecondsToFrames(1.5f);
    public static readonly int CollapseTime = SecondsToFrames(.25f);

    public static readonly int PortalAppearTime = SecondsToFrames(.2f);
    public static readonly int LaserExpandTime = SecondsToFrames(.3f);

    public static readonly int Lifetime = TelegraphTime + BeamTime + CollapseTime + PortalAppearTime + LaserExpandTime;

    public const int MaxLength = 3000;

    public int Time
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }
    public ref float LaserLength => ref Projectile.ai[1];

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = MaxLength;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 108;
        Projectile.timeLeft = Lifetime;
        Projectile.hostile = true;
        Projectile.alpha = 255;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }

    public override void SafeAI()
    {
        if (Time == 0f)
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.Zero);
        Projectile.rotation = Projectile.velocity.ToRotation();

        Time++;

        float lifeInterpolant = InverseLerp(0f, Lifetime, Time);

        float portalPlusTelegraphEnd = (float)(PortalAppearTime + TelegraphTime) / Lifetime; // ~0.3509
        float laserExpandEnd = (float)(PortalAppearTime + TelegraphTime + LaserExpandTime) / Lifetime; // ~0.3860
        float beamEnd = (float)(PortalAppearTime + TelegraphTime + LaserExpandTime + BeamTime) / Lifetime; // ~0.9123
        float collapseEnd = 1f;

        // LaserLength
        LaserLength = new PiecewiseCurve()
            .AddStall(0f, portalPlusTelegraphEnd)
            .Add(0f, MaxLength, laserExpandEnd, MakePoly(3f).OutFunction)
            .AddStall(MaxLength, beamEnd)
            .Add(MaxLength, 0f, collapseEnd, MakePoly(3f).InOutFunction)
            .Evaluate(lifeInterpolant);

        // Portal size
        Projectile.Opacity = new PiecewiseCurve()
            .Add(0f, 1f, (float)PortalAppearTime / Lifetime, MakePoly(3f).OutFunction)
            .AddStall(1f, beamEnd)
            .Add(1f, 0f, collapseEnd, MakePoly(4f).InOutFunction)
            .Evaluate(lifeInterpolant);

        // Laser width ratio and Distortion Intensity
        Projectile.scale = new PiecewiseCurve()
            .AddStall(0f, portalPlusTelegraphEnd)
            .Add(0f, 1f, laserExpandEnd, MakePoly(5f).OutFunction)
            .AddStall(1f, beamEnd)
            .Add(1f, 0f, collapseEnd, MakePoly(3f).InOutFunction)
            .Evaluate(lifeInterpolant);

        if (Time >= TelegraphTime && Projectile.velocity != Vector2.Zero)
        {
            trailPoints.SetPoints(Projectile.Center.GetLaserControlPoints(Projectile.Center + Projectile.velocity * LaserLength, 100));
        }
        else
            telePoints.SetPoints(Projectile.Center.GetLaserControlPoints(Projectile.Center + Projectile.velocity * MaxLength, 100));

        if (trail == null || trail._disposed)
            trail = new(LaserWidthFunction, LaserColorFunction, null, 100);
        if (telegraph == null || telegraph._disposed)
            telegraph = new(TelegraphWidthFunction, TelegraphColorFunction, null, 100);

        if (Time == TelegraphTime)
        {
            ParticleRegistry.SpawnChromaticAberration(Projectile.Center, 50, .6f, 600f);
            ParticleRegistry.SpawnBlurParticle(Projectile.Center, 50, 1f, 400f);
            AdditionsSound.VirtueAttack.Play(Projectile.Center, 1.3f, 0f, .1f);
        }

        if (Time.BetweenNum(TelegraphTime, Lifetime - 10))
        {
            Vector2 vel = Projectile.velocity.RotatedByRandom(Main.rand.NextFloat(.34f, .42f)) * Main.rand.NextFloat(150f, 980f);
            ParticleRegistry.SpawnLightningArcParticle(Projectile.Center, vel, Main.rand.Next(10, 20), 1f, Color.DeepSkyBlue);

        }
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.LineCollision(Projectile.Center, Projectile.Center + Projectile.velocity * LaserLength, Projectile.Size.Length());
    }

    public override bool ShouldUpdatePosition() => false;

    public override bool? CanDamage() => Time >= TelegraphTime ? null : false;

    public float LaserWidthFunction(float _) => Projectile.width * Projectile.scale;
    public Color LaserColorFunction(SystemVector2 completionRatio, Vector2 position)
    {
        float colorInterpolant = Sin01(Main.GlobalTimeWrappedHourly * -3.2f + completionRatio.X * 23f);
        return Color.Lerp(Color.Cyan * 1.2f, Color.DeepSkyBlue, colorInterpolant * 0.67f) * MathHelper.SmoothStep(1f, .8f, completionRatio.X);
    }

    public float TelegraphCompletion => MakePoly(4f).OutFunction(InverseLerp(PortalAppearTime, TelegraphTime, Time));
    public float TelegraphWidthFunction(float completionRatio) => Projectile.width * TelegraphCompletion;
    public Color TelegraphColorFunction(SystemVector2 completionRatio, Vector2 position)
    {
        return MulticolorLerp(completionRatio.X, Color.Cyan, Color.SkyBlue, Color.DeepSkyBlue) * .65f * MathHelper.Lerp(1f, 0f, TelegraphCompletion) * InverseLerp(0f, .08f, completionRatio.X);
    }

    public OptimizedPrimitiveTrail trail;
    public OptimizedPrimitiveTrail telegraph;
    public ManualTrailPoints trailPoints = new(100);
    public ManualTrailPoints telePoints = new(100);
    public override bool PreDraw(ref Color lightColor)
    {
        void drawPortal()
        {
            Texture2D noiseTexture = AssetRegistry.GetTexture(AdditionsTexture.Cosmos);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = noiseTexture.Size() * 0.5f;

            Color col1 = ColorSwap(Color.Cyan, Color.DeepSkyBlue * 1.2f, 1f);
            Color col2 = Color.Lerp(Color.White, Color.Cyan, .5f);

            Vector2 diskScale = Projectile.Opacity * new Vector2(.5f, 1f);
            ManagedShader portal = ShaderRegistry.PortalShader;

            portal.TrySetParameter("opacity", Projectile.Opacity);
            portal.TrySetParameter("color", col1);
            portal.TrySetParameter("secondColor", col2);
            portal.Render();

            Main.spriteBatch.Draw(noiseTexture, drawPosition, null, Color.White, Projectile.rotation, origin, diskScale, SpriteEffects.None, 0f);

            portal.TrySetParameter("secondColor", col2 * 2f);
            portal.Render();
            Main.spriteBatch.Draw(noiseTexture, drawPosition, null, Color.White, Projectile.rotation, origin, diskScale, SpriteEffects.None, 0f);
        }

        void drawTelegraph()
        {
            if (telegraph != null && !telegraph._disposed)
            {
                ManagedShader shader = ShaderRegistry.SideStreakTrail;
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.TechyNoise), 1);

                telegraph.DrawTrail(shader, telePoints.Points);
            }
        }

        void drawBeam()
        {
            if (trail != null && !trail._disposed)
            {
                ManagedShader beam = ShaderRegistry.BaseLaserShader;
                beam.TrySetParameter("heatInterpolant", 2f);
                beam.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.CrackedNoise), 0);
                beam.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.FlameMap2), 1);
                beam.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.SuperWavyPerlin), 2);
                trail.DrawTrail(beam, trailPoints.Points);
            }
        }

        PixelationSystem.QueueTextureRenderAction(drawPortal, PixelationLayer.OverPlayers, null, ShaderRegistry.PortalShader);
        if (Time < TelegraphTime)
            PixelationSystem.QueuePrimitiveRenderAction(drawTelegraph, PixelationLayer.OverPlayers);
        else
            PixelationSystem.QueuePrimitiveRenderAction(drawBeam, PixelationLayer.OverPlayers);

        return false;
    }
}