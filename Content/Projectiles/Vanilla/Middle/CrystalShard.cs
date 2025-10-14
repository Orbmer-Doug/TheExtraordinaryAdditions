using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;

public class CrystalShard : ModProjectile
{
    public override string Texture => ProjectileID.CrystalStorm.GetTerrariaProj();

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
    }

    private const int Lifetime = 600;
    public override void SetDefaults()
    {
        Projectile.ignoreWater = true;
        Projectile.width =
        Projectile.height = 8;
        Projectile.friendly = true;
        Projectile.alpha = 50;
        Projectile.scale = 1.2f;
        Projectile.timeLeft = Lifetime;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.tileCollide = true;
        Projectile.penetrate = 1;
    }

    public ref float Time => ref Projectile.ai[0];
    public FancyAfterimages fancy;
    public override void AI()
    {
        fancy ??= new(5, () => Projectile.Center);
        Time++;

        Projectile.Opacity = InverseLerp(0f, 10f, Time);
        float size = Projectile.scale * 0.5f;
        Lighting.AddLight(Projectile.Center, new Color(74, 128, 164).ToVector3() * size);
        Projectile.rotation += Projectile.velocity.X * 0.2f;
        if (Main.rand.NextBool(4))
        {
            ParticleRegistry.SpawnSparkleParticle(Projectile.RandAreaInEntity(), Vector2.Zero, Main.rand.Next(18, 22),
                Main.rand.NextFloat(.3f, .45f), new(155, 44, 111), new(136, 29, 94), Main.rand.NextFloat(.9f, 1.2f));
        }
        fancy?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One * Projectile.scale, Projectile.Opacity, Projectile.rotation, 0, 10, 4, 1f - Projectile.Opacity, null, false, -.1f));

        Projectile.velocity *= 0.985f;
        if (Time > 130f)
        {
            Projectile.scale -= 0.05f;
            if (Projectile.scale <= 0.2)
            {
                Projectile.scale = 0.2f;
                Projectile.Kill();
            }
        }
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (Projectile.velocity.X != oldVelocity.X)
            Projectile.velocity.X = -oldVelocity.X;
        if (Projectile.velocity.Y != oldVelocity.Y)
            Projectile.velocity.Y = -oldVelocity.Y;
        return false;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        for (int i = 0; i < 8; i++)
            ParticleRegistry.SpawnSparkParticle(Projectile.Center, Main.rand.NextVector2Circular(5f, 5f), Main.rand.Next(20, 30),
                Main.rand.NextFloat(.35f, .45f), Color.Lerp(new(74, 128, 164), new(155, 44, 111), Main.rand.NextFloat()), false, true);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            Projectile.DrawBaseProjectile(Lighting.GetColor(Projectile.Center.ToTileCoordinates()));
            fancy?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [new(0, 39, 65), new(0, 73, 121), new(74, 128, 164)],
                Lighting.Brightness((int)Projectile.Center.X / 16, (int)Projectile.Center.Y / 16));
        }
        PixelationSystem.QueueTextureRenderAction(draw, PixelationLayer.UnderProjectiles);
        return false;
    }
}
