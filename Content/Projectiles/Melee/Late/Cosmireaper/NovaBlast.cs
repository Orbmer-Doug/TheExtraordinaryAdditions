using Terraria;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Late.Cosmireaper;

public class NovaBlast : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.Invis;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 400;
        Projectile.friendly = true;
        Projectile.ignoreWater = false;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 14;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 14;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Projectile.damage = (int)(Projectile.damage * 0.985);
    }

    public override void AI()
    {
        Lighting.AddLight(Projectile.Center, Color.BlueViolet.ToVector3() * 2f);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 center = Projectile.Center;
        Vector2 size = Projectile.Size;
        return CircularHitboxCollision(center, ((Vector2)size).Length() * 0.5f, targetHitbox);
    }
}