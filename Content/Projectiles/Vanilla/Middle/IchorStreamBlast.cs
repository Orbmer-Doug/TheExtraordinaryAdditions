using Terraria;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;

public class IchorStreamBlast : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width = 72;
        Projectile.height = 72;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 10;
        Projectile.MaxUpdates = 3;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 7;

        Projectile.hostile = false;
        Projectile.friendly = true;
    }
}