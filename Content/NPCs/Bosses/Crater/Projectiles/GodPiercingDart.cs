using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

public class GodPiercingDart : ProjOwnedByNPC<Asterlin>
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.GodPiercingDart);

    public int Time
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }
    public bool ExtendedTelegraph
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 1000;
    }

    public override void SetDefaults()
    {
        Projectile.width = 26;
        Projectile.height = 58;
        Projectile.friendly = false;
        Projectile.hostile = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 300;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }

    public const float Supersonic = 34f;
    public override void SafeAI()
    {
        if (trail == null || trail._disposed)
            trail = new(TrailWidthFunction, TrailColorFunction, null, 20);

        if (tele == null || tele._disposed)
            tele = new(TelegraphWidthFunction, TelegraphColorFunction, null, 30);

        Projectile.velocity *= 1.016f;
        Projectile.FacingUp();

        Vector2 start = Projectile.RotHitbox().Bottom;
        float dist = Animators.MakePoly(3f).InOutFunction.Evaluate(Time, 0f, Supersonic, 0f, ExtendedTelegraph ? 2200f : 400f);
        telePoints.SetPoints(start.GetLaserControlPoints(start + Projectile.velocity.SafeNormalize(Vector2.Zero) * dist, 30));

        if (Time < Supersonic)
        {
            Projectile.velocity *= .92f;
        }
        else if (Time == Supersonic)
        {
            ParticleRegistry.SpawnPulseRingParticle(start, Projectile.velocity * .01f, 20, Projectile.velocity.ToRotation(), new(.4f, 1f), 0f, Projectile.height, Color.Cyan);

            for (int i = 0; i < 12; i++)
            {
                Vector2 vel = NextVector2EllipseEdge(Projectile.height * .4f, Projectile.height, Projectile.velocity.ToRotation()) * .2f;
                ParticleRegistry.SpawnHeavySmokeParticle(start, vel, Main.rand.Next(20, 30), Main.rand.NextFloat(.4f, .6f), Color.Cyan.Lerp(Color.DarkCyan, Main.rand.NextFloat(0f, .4f)));
                ParticleRegistry.SpawnBloomLineParticle(start, vel, Main.rand.Next(40, 50), Main.rand.NextFloat(.2f, .4f), Color.Cyan);
            }
            //vec = Projectile.Center.SafeDirectionTo(Target.Center);
            AdditionsSound.explo04.Play(start, .7f, .2f);
            Projectile.timeLeft = 800;
        }
        else if (Time > Supersonic)
        {
            Projectile.extraUpdates = 8;

            /* if enchanted ones are staying in, give them the homing property
            float speed = 10f;
            float amt = .03f;
            if (Main.expertMode)
            {
                amt = .015f;
                speed = 7f;
            }
            if (Main.masterMode)
            {
                amt = .01f;
                speed = 4f;
            }
            amt /= Projectile.MaxUpdates;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, vec * speed, amt);
            */
            points.Update(Projectile.RotHitbox().Top);
        }
        Time++;

        // Done twice to ensure no slip-ups (particulary when spawning in)
        Projectile.FacingUp();
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.LineCollision(Projectile.BaseRotHitbox().Bottom, Projectile.BaseRotHitbox().Top, Projectile.width);
    }

    public float TrailWidthFunction(float completionRatio)
    {
        return MathHelper.SmoothStep(Projectile.height, 2f, completionRatio);
    }

    public Color TrailColorFunction(SystemVector2 completion, Vector2 pos)
    {
        return Color.Cyan * MathF.Sqrt(completion.X) * Projectile.Opacity;
    }

    public float FadeAway => Animators.MakePoly(2f).InFunction.Evaluate(Time, Supersonic, Supersonic + (18f * Projectile.MaxUpdates), 1f, 0f);
    public float TelegraphWidthFunction(float completionRatio)
    {
        float width = Projectile.width * .5f * FadeAway;
        float completion = InverseLerp(0.015f, 0.25f, completionRatio);
        float maxSize = width + completionRatio * width * 1.5f;
        return Animators.MakePoly(2).OutFunction.Evaluate(2f, maxSize, completion);
    }

    public Color TelegraphColorFunction(SystemVector2 completion, Vector2 pos)
    {
        float endFadeOpacity = GetLerpBump(0f, .2f, 1f, .8f, completion.X);

        float telegraphInterpolant = InverseLerp(0f, Supersonic - 4f, Time);
        Color telegraphColor = Color.Lerp(Color.LightCyan, Color.Cyan, MathF.Pow(telegraphInterpolant, 0.6f)) * telegraphInterpolant;

        return telegraphColor * endFadeOpacity * FadeAway * .3f;
    }

    public OptimizedPrimitiveTrail trail;
    public OptimizedPrimitiveTrail tele;
    public TrailPoints points = new(20);
    public ManualTrailPoints telePoints = new(30);

    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (tele != null && telePoints != null)
            {
                ManagedShader shader = ShaderRegistry.SideStreakTrail;
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.TechyNoise), 1, SamplerState.LinearWrap);
                tele.DrawTrail(shader, telePoints.Points);
            }

            if (trail != null && points != null)
            {
                ManagedShader shader = ShaderRegistry.BaseLaserShader;
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.TechyNoise), 1, SamplerState.LinearWrap);
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.WavyBlotchNoise), 2, SamplerState.LinearWrap);
                trail.DrawTrail(shader, points.Points);
            }
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        Texture2D texture = Projectile.ThisProjectileTexture();
        Projectile.DrawProjectileBackglow(Color.LightCyan, 3f, 100, 10);
        Main.spriteBatch.DrawBetter(texture, Projectile.Center, null, Projectile.GetAlpha(Color.White), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, 0);

        return false;
    }
}