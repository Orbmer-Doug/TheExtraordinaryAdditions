using Terraria;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle.AZ;

public class GrubShrapnel : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.GrubShrapnel);
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
    }
    float SavedVel;
    private bool init;
    public override void AI()
    {
        if (!init)
        {
            SavedVel = Projectile.velocity.Length();
            init = true;
        }
        if (Projectile.velocity.Length() < SavedVel)
            Projectile.velocity *= 2f;

        if (Collision.SolidCollision(Projectile.Center, Projectile.width, Projectile.height))
            Projectile.velocity *= .97f;

        Projectile.rotation += .3f;
    }
}
