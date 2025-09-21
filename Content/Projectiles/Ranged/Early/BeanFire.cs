using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Early;

public class BeanFire : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.BeanFire);

    public override void SetDefaults()
    {
        Projectile.friendly = Projectile.tileCollide = true;
        Projectile.hostile = Projectile.ignoreWater = false;
        Projectile.width = 32;
        Projectile.height = 18;
        Projectile.timeLeft = 600;
        Projectile.penetrate = 3;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.aiStyle = 0;
    }

    public ref float Speed => ref Projectile.ai[0];
    public override void AI()
    {
        after ??= new(5, () => Projectile.Center);

        if (Projectile.FinalExtraUpdate())
        {
            after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0, 255));

            Lighting.AddLight(Projectile.Center, Color.OrangeRed.ToVector3() * 3f);
            Projectile.rotation += Projectile.direction * .19f;

            Dust.NewDustPerfect(Projectile.RotHitbox().RandomPoint(), DustID.WoodFurniture, -Projectile.velocity * .01f, 100, default, .7f).noGravity = true;
        }
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        for (int i = 0; i < 10; i++)
        {
            Dust dust = Dust.NewDustDirect(target.Center, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 400, default, 2f);
            dust.noGravity = false;
            dust.velocity *= 1f;
            dust = Dust.NewDustDirect(target.Center, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 300, default, 2f);
            dust.velocity *= 2f;
        }

        Projectile.damage = (int)(Projectile.damage * 0.9f);
    }

    public override void OnKill(int timeLeft)
    {
        for (int i = 0; i < 12; i++)
            Dust.NewDustPerfect(Projectile.RotHitbox().RandomPoint(), DustID.WoodFurniture, Main.rand.NextVector2Circular(2f, 2f), 0, default, .7f).noGravity = true;
    }

    public override bool OnTileCollide(Vector2 velocityChange)
    {
        if (Projectile.MaxUpdates < 30)
        {
            Projectile.penetrate++;
            Projectile.MaxUpdates += 1;
        }

        if (Projectile.velocity.X != velocityChange.X)
            Projectile.velocity.X = -velocityChange.X * 1.05f;
        if (Projectile.velocity.Y != velocityChange.Y)
            Projectile.velocity.Y = -velocityChange.Y * 1.05f;
        Projectile.velocity.X = MathHelper.Clamp(Projectile.velocity.X, -16f, 16f);
        Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y, -16f, 16f);

        return false;
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        if (after.afterimages != null)
            after?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [lightColor], Projectile.Opacity);
        Projectile.DrawProjectileBackglow(Color.SaddleBrown, 3f);
        return base.PreDraw(ref lightColor);
    }
}