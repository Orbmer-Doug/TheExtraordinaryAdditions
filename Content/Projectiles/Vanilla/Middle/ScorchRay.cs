using System.IO;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;

public class ScorchRay : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public const int Lifetime = 20;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = -1;
        Projectile.scale = 1f;
        Projectile.timeLeft = Lifetime;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public ref float Time => ref Projectile.ai[0];
    public bool HitSomething
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }
    public ref float LaserLength => ref Projectile.ai[2];
    public int Hits
    {
        get => (int)Projectile.Additions().ExtraAI[0];
        set => Projectile.Additions().ExtraAI[0] = value;
    }

    public Player Owner => Main.player[Projectile.owner];

    public Vector2 End;
    public override void SendExtraAI(BinaryWriter writer) => writer.WriteVector2(End);
    public override void ReceiveExtraAI(BinaryReader reader) => End = reader.ReadVector2();
    public override void AI()
    {
        Vector2 start = Projectile.Center;
        Vector2 expected = start + Projectile.velocity * LaserLength;
        End = LaserCollision(start, expected, CollisionTarget.NPCs | CollisionTarget.Tiles, WidthFunct(.5f));
        if (End != expected && !HitSomething)
        {
            float offsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            int amount = 6;
            for (float a = .25f; a <= 2; a += .25f)
            {
                for (int i = 0; i < amount; i++)
                {
                    Vector2 velo = (MathHelper.TwoPi * i / amount + offsetAngle).ToRotationVector2() * Main.rand.NextFloat(4f, 7f) * a;
                    float scale = Utils.Remap(Time, 0f, 40f, 1.1f, 2f);
                    ParticleRegistry.SpawnGlowParticle(End, velo, Main.rand.Next(12, 18), 60f, Color.Chocolate);
                }
            }

            HitSomething = true;
        }

        if (trail == null || trail._disposed)
            trail = new(Tip, WidthFunct, ColorFunct, null, 100);

        points.SetPoints(start.GetLaserControlPoints(End, 100));
        LaserLength = Utils.Remap(Time, 0f, Lifetime * 3, 0f, 1200f);

        Projectile.Opacity = GetLerpBump(0f, .2f, 1f, .72f, InverseLerp(Lifetime, 0f, Projectile.timeLeft));

        if (!HitSomething)
            Time++;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Hits++;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => HitSomething ? false : null;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.LineCollision(Projectile.Center, End + Projectile.velocity * 30f, Projectile.height);
    }

    public OptimizedPrimitiveTrail trail;
    public ManualTrailPoints points = new(100);
    public static readonly ITrailTip Tip = new RoundedTip(30);

    public float WidthFunct(float c) => Projectile.width * Projectile.Opacity;

    public Color ColorFunct(SystemVector2 c, Vector2 position)
    {
        return Color.Orange * InverseLerp(0f, .09f, c.X) * Projectile.Opacity;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            ManagedShader shader = ShaderRegistry.CrunchyLaserShader;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.CrackedNoise2), 1);

            trail.DrawTippedTrail(shader, points.Points, Tip, false, -1);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        return false;
    }
}