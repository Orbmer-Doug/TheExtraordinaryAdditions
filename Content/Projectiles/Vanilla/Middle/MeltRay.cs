using System.IO;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles.Shader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;

public class MeltRay : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public const int Lifetime = 30;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = 1;
        Projectile.scale = 1f;
        Projectile.timeLeft = Lifetime;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.stopsDealingDamageAfterPenetrateHits = true;
        Projectile.localNPCHitCooldown = 15;
    }

    public bool HitSomething
    {
        get => Projectile.ai[0] == 1f;
        set => Projectile.ai[0] = value.ToInt();
    }
    public Vector2 End;
    public override void SendExtraAI(BinaryWriter writer) => writer.WriteVector2(End);
    public override void ReceiveExtraAI(BinaryReader reader) => End = reader.ReadVector2();
    public override void AI()
    {
        if (trail == null || trail.Disposed)
            trail = new(WidthFunct, ColorFunct, null, 100);

        Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.Zero);
        Vector2 start = Projectile.Center;
        if (!HitSomething)
        {
            Vector2 expected = start + Projectile.velocity * 1200;
            End = LaserCollision(start, expected, CollisionTarget.NPCs | CollisionTarget.Tiles, 12);
            if (End != expected)
            {
                for (int i = 0; i < 4; i++)
                    ShaderParticleRegistry.SpawnMoltenParticle(End + Main.rand.NextVector2Circular(10f, 10f), Main.rand.NextFloat(50f, 80f));

                if (this.RunLocal())
                {
                    for (int i = 0; i < 2; i++)
                        Projectile.NewProj(End, Main.rand.NextVector2CircularEdge(5f, 5f) + Vector2.UnitY * -6f, ModContent.ProjectileType<MeltGlobule>(),
                            (int)(Projectile.damage * .45f), Projectile.knockBack, Projectile.owner);
                }

                HitSomething = true;
            }
            points.SetPoints(start.GetLaserControlPoints(End, 100));
        }

        Projectile.Opacity = GetLerpBump(0f, .2f, 1f, .72f, InverseLerp(Lifetime, 0f, Projectile.timeLeft));
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.LineCollision(Projectile.Center, End + (Projectile.velocity * Projectile.width), Projectile.width);
    }

    public OptimizedPrimitiveTrail trail;
    public TrailPoints points = new(100);

    public float WidthFunct(float c) => OptimizedPrimitiveTrail.HemisphereWidthFunct(1f - c, Projectile.width * Projectile.Opacity);

    public Color ColorFunct(SystemVector2 c, Vector2 position)
    {
        return Color.Lerp(Color.OrangeRed.Lerp(Color.Chocolate, .32f), Color.Red, 0.5f) * InverseLerp(0f, .09f, c.X) * Projectile.Opacity;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || trail.Disposed || points == null)
                return;
            ManagedShader shader = ShaderRegistry.FlameTrail;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.FlameMap1), 1);
            trail.DrawTrail(shader, points.Points);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        return false;
    }
}