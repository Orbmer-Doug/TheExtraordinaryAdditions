using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;

public class BreakerBeam : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;

    public override void SetDefaults()
    {
        Projectile.width = 50;
        Projectile.height = 100;
        Projectile.tileCollide = Projectile.friendly = Projectile.noEnchantmentVisuals = Projectile.ignoreWater = true;
        Projectile.hostile = false;
        Projectile.stopsDealingDamageAfterPenetrateHits = true;
        Projectile.extraUpdates = 1;
        Projectile.timeLeft = 800;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
    }

    public ref float Time => ref Projectile.ai[0];
    public bool Grounded
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }
    public bool Fading
    {
        get => Projectile.ai[2] == 1f;
        set => Projectile.ai[2] = value.ToInt();
    }
    public bool Special
    {
        get => Projectile.Additions().ExtraAI[0] == 1f;
        set => Projectile.Additions().ExtraAI[0] = value.ToInt();
    }
    public float Direction => Projectile.rotation + MathHelper.PiOver2;
    public Color Color => Special ? new Color(163, 222, 250) : new Color(85, 237, 71);
    private Vector2 savedPos;
    private const float TotalDist = 1100f;
    public float CurrentDist => Projectile.Center.Distance(savedPos);

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.WriteVector2(savedPos);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        savedPos = reader.ReadVector2();
    }

    public override void AI()
    {
        if (Time == 0f)
        {
            savedPos = Projectile.Center;
            this.Sync();
        }

        if (Grounded)
            Projectile.tileCollide = false;

        if (trail == null || trail._disposed)
            trail = new(WidthFunct, ColorFunct, null, MaxPoints);

        Vector2 samplePos = Projectile.direction == -1 ? Projectile.RotHitbox().Right : Projectile.RotHitbox().Left;
        Vector2 start = samplePos - (Direction).ToRotationVector2() * Projectile.height * Projectile.scale * 0.5f;
        Vector2 end = samplePos + (Direction).ToRotationVector2() * Projectile.height * Projectile.scale * 0.5f;
        Vector2 middle = (start + end) * 0.5f + (Direction - MathHelper.PiOver2 + (.18f * Projectile.direction)).ToRotationVector2() * Projectile.width * 2 * Projectile.scale;
        for (int i = 0; i < MaxPoints; i++)
        {
            Vector2 point = QuadraticBezier(start, middle, end, InverseLerp(0f, MaxPoints, i));
            slashPoints.SetPoint(i, point);
        }

        Projectile.rotation = Projectile.velocity.ToRotation();
        if (CurrentDist > TotalDist)
            Fading = true;

        if (Time % 2 == 1)
        {
            ParticleRegistry.SpawnSparkParticle(slashPoints.Points[Main.rand.Next(slashPoints.Count)], -Projectile.velocity * Main.rand.NextFloat(.5f, .7f),
                Main.rand.Next(20, 40), Main.rand.NextFloat(.4f, .6f), Color.Lerp(Color.White, Main.rand.NextFloat(0f, .3f)));
        }
        if (Time % 4 == 3)
        {
            ParticleRegistry.SpawnGlowParticle(slashPoints.Points[Main.rand.Next(slashPoints.Count)], -Projectile.velocity.RotatedByRandom(.1f) * Main.rand.NextFloat(.2f, .4f),
                Main.rand.Next(30, 50), Main.rand.NextFloat(20f, 30f), Color.Lerp(Color.White, .4f));
        }
        if (Collision.SolidCollision(Projectile.Center + Projectile.velocity * 2, 2, 2))
        {
            Fading = true;
            this.Sync();
        }

        if (Fading)
        {
            if (Projectile.timeLeft > 10)
                Projectile.timeLeft = 10;
            if (Time > 16f)
                Time = 16f;
            Time--;
            Projectile.Opacity = InverseLerp(0f, 10f, Projectile.timeLeft);
        }
        else
        {
            Time++;
            Projectile.scale = Projectile.Opacity = InverseLerp(0f, 10f, Time);
        }
    }

    public override bool? CanHitNPC(NPC target) => Fading ? false : null;

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Fading = true;
        Projectile.tileCollide = false;
        return false;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.CollisionFromPoints(slashPoints.Points, WidthFunct);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Vector2 norm = Projectile.velocity.SafeNormalize(Vector2.Zero);
        for (int i = 0; i < 30; i++)
        {
            ParticleRegistry.SpawnSquishyLightParticle(slashPoints.Points[Main.rand.Next(slashPoints.Count)], Projectile.velocity * Main.rand.NextFloat(.3f, .8f), Main.rand.Next(20, 34), Main.rand.NextFloat(.6f, 1f), Color, 1f, 2f, 5f);
        }
        ParticleRegistry.SpawnTwinkleParticle(Projectile.Center + norm * 20f, Vector2.Zero, 20, new(Main.rand.NextFloat(.9f, 1.4f)), Color * 1.5f, 8, default, RandomRotation());

        if (Special)
            Projectile.NewProj(Projectile.Center + norm * 10f, Vector2.Zero, ModContent.ProjectileType<BreakerStorm>(), Projectile.damage, Projectile.knockBack * 2, Projectile.owner);

        AdditionsSound.BreakerStorm.Play(Projectile.Center, .5f, -.2f);

        Fading = true;
    }

    public const int MaxPoints = 20;
    public OptimizedPrimitiveTrail trail;
    public ManualTrailPoints slashPoints = new(MaxPoints);
    public float WidthFunct(float c) => Convert01To010(c) * Projectile.scale * Projectile.Opacity * Projectile.width;
    public Color ColorFunct(SystemVector2 c, Vector2 pos) => Color * Projectile.Opacity;
    public override bool PreDrawExtras()
    {
        void draw()
        {
            if (trail == null || slashPoints == null)
                return;

            ManagedShader shader = ShaderRegistry.SpecialLightningTrail;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.SuperWavyPerlin), 1, SamplerState.LinearWrap);
            trail.DrawTrail(shader, slashPoints.Points, -1, true);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        return false;
    }
}