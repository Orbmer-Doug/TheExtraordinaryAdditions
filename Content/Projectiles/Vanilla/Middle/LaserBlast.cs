using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;

public class LaserBlast : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.Invis;

    public const int Lifetime = 30;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 7;
        Projectile.friendly = true;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.timeLeft = Lifetime;
        Projectile.penetrate = 2;
        Projectile.stopsDealingDamageAfterPenetrateHits = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 1;
        Projectile.MaxUpdates = 2;
    }

    public ref float Time => ref Projectile.ai[0];
    public bool Hit
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }

    public Player Owner => Main.player[Projectile.owner];
    public Vector2 End;
    public override void SendExtraAI(BinaryWriter writer) => writer.WriteVector2(End);
    public override void ReceiveExtraAI(BinaryReader reader) => End = reader.ReadVector2();
    public override void AI()
    {
        if (trail == null || trail.Disposed)
            trail = new(WidthFunct, ColorFunct, null, 50);

        Vector2 expected = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * Animators.MakePoly(3f).OutFunction.Evaluate(Time, 0f, Lifetime, 0f, 1000f);
        End = LaserCollision(Projectile.Center, expected, CollisionTarget.Tiles | CollisionTarget.NPCs, out CollisionTarget hit, WidthFunct(.5f));
        if (End != expected && !Hit)
        {
            Hit = true;
        }

        points.SetPoints(Projectile.Center.GetLaserControlPoints(End, 50));

        Projectile.Opacity = InverseLerp(0f, 10f, Projectile.timeLeft);

        if (!Hit)
        {
            Color color = Main.rand.NextBool(4) ? new Color(106, 93, 255) * 1.6f : new Color(106, 93, 255);
            ParticleRegistry.SpawnSparkParticle(End + Main.rand.NextVector2Circular(Projectile.height, Projectile.height) * 2, Projectile.velocity, 30, .6f, color);
            Time++;
        }
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Projectile.knockBack *= .6f;
        Projectile.damage = (int)(Projectile.damage * .4f);
    }

    public override bool ShouldUpdatePosition() => false;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.LineCollision(Projectile.Center, End + Projectile.velocity.SafeNormalize(Vector2.Zero) * 10f, WidthFunct(.5f));
    }

    public float WidthFunct(float c)
    {
        return OptimizedPrimitiveTrail.HemisphereWidthFunct(c, Projectile.height * Projectile.Opacity);
    }

    public Color ColorFunct(SystemVector2 c, Vector2 pos)
    {
        return Color.Violet * InverseLerp(0f, .04f, c.X) * Projectile.Opacity;
    }

    public TrailPoints points = new(50);
    public OptimizedPrimitiveTrail trail;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || points == null)
                return;

            ManagedShader shader = ShaderRegistry.FlameTrail;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.FireNoise), 1, SamplerState.LinearWrap);
            trail.DrawTrail(shader, points.Points);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        return false;
    }
}