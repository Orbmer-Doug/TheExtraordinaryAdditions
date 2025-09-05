using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.NPCs.Hostile.Arid;

public class EmptyRound : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.EmptyRound);
    public int Time;

    public bool TouchedGrass;

    public override void SetDefaults()
    {
        Projectile.width = 7;
        Projectile.height = 10;
        Projectile.friendly = true;
        Projectile.ignoreWater = false;
        Projectile.aiStyle = 14;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 700;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public override void SetStaticDefaults()
    {
    }

    public override bool PreDraw(ref Color lightColor)
    {
        return true;
    }

    public override void AI()
    {
        Projectile.extraUpdates = 0;
        Time++;
        _ = Main.player[Projectile.owner];
        if (!TouchedGrass)
        {
            Projectile.rotation += 0.5f * Projectile.direction;
        }
        Projectile.velocity.Y -= 0.055f;
        Projectile.velocity.X *= 0.992f;
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Projectile.damage = 0;
        TouchedGrass = true;
        Projectile.velocity *= 0.98f;
        return false;
    }
}
