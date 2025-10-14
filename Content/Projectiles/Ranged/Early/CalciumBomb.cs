using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Early;

public class CalciumBomb : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CalciumBomb);
    public override void SetDefaults()
    {
        Projectile.width = 26;
        Projectile.height = 30;
        Projectile.friendly = true;
        Projectile.penetrate = 1;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = false;
        Projectile.timeLeft = 500;
        Projectile.DamageType = DamageClass.Ranged;
    }
    public Player Owner => Main.player[Projectile.owner];
    public ref float Time => ref Projectile.ai[0];
    public FancyAfterimages after;
    public override void AI()
    {
        after ??= new(4, () => Projectile.Center);
        after.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity * .4f, Projectile.rotation, 0, 230, 0, 0f, null, false, -.2f));

        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (p != null && p.RotHitbox().Intersects(Projectile.RotHitbox()) && p.type == ModContent.ProjectileType<CalciumShot>())
            {
                for (int i = 0; i < 8; i++)
                {
                    Vector2 vel = (MathHelper.TwoPi * i / 8).ToRotationVector2().RotatedByRandom(.3f) * 12f;
                    Projectile.NewProj(Projectile.Center, vel, ModContent.ProjectileType<CalciumSplinter>(), Projectile.damage, Projectile.knockBack, Owner.whoAmI);
                }

                p.Kill();
                Projectile.Kill();
            }
        }

        if (Time > 8f)
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + .3f, -22f, 22f);
        Projectile.VelocityBasedRotation();
        Time++;
    }

    public override void OnKill(int timeLeft)
    {
        Vector2 from = Vector2.Zero;
        Vector2 to = Vector2.One * 90f;
        Projectile.CreateFriendlyExplosion(Projectile.Center, from, Projectile.damage / 2, Projectile.knockBack / 2f, 9, 8, to);
        ScreenShakeSystem.New(new(.1f, .1f), Projectile.Center);
        AdditionsSound.banditShot2B.Play(Projectile.Center, .6f, -.3f, .1f);

        for (int i = 0; i < 20; i++)
        {
            if (i % 4 == 3)
                ParticleRegistry.SpawnDetailedBlastParticle(Projectile.Center, from, to * Main.rand.NextFloat(.5f, 1f), Vector2.Zero, 40, Color.OrangeRed, RandomRotation(), null, true);

            Vector2 vel = Main.rand.NextVector2Circular(10f, 10f);
            int life = Main.rand.Next(200, 340);
            float scale = Main.rand.NextFloat(.9f, 1.9f);
            Color col = Color.OrangeRed.Lerp(Color.Chocolate, Main.rand.NextFloat(.2f, .5f));
            ParticleRegistry.SpawnSquishyPixelParticle(Projectile.Center, vel, life, scale, col, Color.Chocolate, 3, true);
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        after?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [lightColor]);
        Projectile.DrawBaseProjectile(lightColor);
        return false;
    }
}
