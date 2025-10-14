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

    public ref float SavedVel => ref Projectile.ai[0];
    private bool Init
    {
        get => Projectile.ai[1] == 1;
        set => Projectile.ai[1] = value.ToInt();
    }

    public override void AI()
    {
        if (!Init)
        {
            SavedVel = Projectile.velocity.Length();
            Init = true;
        }
        if (Projectile.velocity.Length() < SavedVel)
            Projectile.velocity *= 2f;

        if (Collision.SolidCollision(Projectile.Center, Projectile.width, Projectile.height))
            Projectile.velocity *= .97f;

        Projectile.rotation += .3f;
    }
}
