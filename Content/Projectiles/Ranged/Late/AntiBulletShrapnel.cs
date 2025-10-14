using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;

public class AntiBulletShrapnel : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.AntiBulletShrapnel);
    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 4;
    }

    public override void SetDefaults()
    {
        Projectile.width = 12;
        Projectile.height = 8;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.ignoreWater = false;
        Projectile.tileCollide = true;
        Projectile.extraUpdates = 2;
        Projectile.timeLeft = 140;
    }

    public ref float Time => ref Projectile.ai[0];
    public override bool? CanDamage() => Time > 5f;
    public override void AI()
    {
        float comp = InverseLerp(0f, 40, Time);
        float complete = 1f - comp;
        after ??= new(5, () => Projectile.Center);
        after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0, 0, 3, complete * 2f, null, false, -.1f));

        if (Time == 0)
        {
            Projectile.frame = Main.rand.Next(Main.projFrames[Projectile.type]);
        }
        if (Time > 40)
        {
            Projectile.extraUpdates = 0;

            if (Projectile.velocity.Y < 16f)
                Projectile.velocity.Y += 0.12f;
        }

        Projectile.Opacity = InverseLerp(0f, 20f, Projectile.timeLeft);
        Time++;
        Projectile.VelocityBasedRotation();
    }

    private bool HitGround;
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (!HitGround)
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.Center += oldVelocity * 2f;
            HitGround = true;
        }

        return false;
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        float comp = InverseLerp(0f, 40, Time);
        float complete = 1f - comp;
        Projectile.DrawBaseProjectile(Color.White.Lerp(lightColor, comp) * Projectile.Opacity);
        after?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [Color.OrangeRed * 1.2f, Color.Chocolate * 2f], complete);
        return false;
    }
}
