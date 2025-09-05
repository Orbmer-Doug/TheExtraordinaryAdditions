using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Early;

public class ProximityDart : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.ProximityDart);
    private const int ExplosionWidthHeight = 100;

    public override void SetDefaults()
    {
        Projectile.width = 34;
        Projectile.height = 14;
        Projectile.friendly = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 120;
        DrawOffsetX = -2;
        DrawOriginOffsetY = -5;
    }

    public ref float Time => ref Projectile.ai[0];
    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        after?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [lightColor]);
        return false;
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        // Vanilla explosions do less damage to Eater of Worlds in expert mode, so we will too
        if (Main.expertMode)
        {
            if (target.type >= NPCID.EaterofWorldsHead && target.type <= NPCID.EaterofWorldsTail)
            {
                modifiers.FinalDamage /= 5;
            }
        }
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Projectile.timeLeft = 4;
        return false;
    }

    public override void AI()
    {
        after ??= new(4, () => Projectile.Center);
        after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0, 255));

        // The projectile is in the midst of exploding during the last 3 updates.
        if (this.RunLocal() && Projectile.timeLeft <= 3)
        {
            Projectile.tileCollide = false;

            // Set to transparent
            Projectile.Opacity = 0f;

            // Change the hitbox size, centered about the original projectile center. This makes the projectile damage enemies during the explosion.
            Projectile.Resize(ExplosionWidthHeight, ExplosionWidthHeight);

            Projectile.damage = 25;
            Projectile.knockBack = 6f;
        }
        else
        {
            if (Time % 2f == 0f)
                Dust.NewDustPerfect(Projectile.RotHitbox().Left + Main.rand.NextVector2Circular(3, 3), DustID.OrangeTorch, -Projectile.velocity * .4f);
        }
        Projectile.FacingRight();

        Time++;
        if (Time > 10f)
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + 0.3f, -20f, 20f);
    }

    public override void OnKill(int timeLeft)
    {
        SoundID.Item14.Play(Projectile.Center);

        for (int i = 0; i < 40; i++)
        {
            ParticleRegistry.SpawnBloomPixelParticle(Projectile.Center, Main.rand.NextVector2Circular(7f, 7f), Main.rand.Next(20, 30), Main.rand.NextFloat(.4f, .7f), Color.OrangeRed, Color.White, null, 1.2f, 5);
            ParticleRegistry.SpawnHeavySmokeParticle(Projectile.Center, Main.rand.NextVector2Circular(3f, 3f), Main.rand.Next(20, 50), Main.rand.NextFloat(.4f, .6f), Color.OrangeRed, Main.rand.NextFloat(.6f, 1.2f));
        }

        // Reset size to normal width and height.
        Projectile.Resize(34, 14);

        // Finally, actually explode the tiles and walls
        // Run this code only for the owner
        if (this.RunLocal())
        {
            int explosionRadius = 3;
            int minTileX = (int)(Projectile.Center.X / 16f - explosionRadius);
            int maxTileX = (int)(Projectile.Center.X / 16f + explosionRadius);
            int minTileY = (int)(Projectile.Center.Y / 16f - explosionRadius);
            int maxTileY = (int)(Projectile.Center.Y / 16f + explosionRadius);

            // Ensure that all tile coordinates are within the world bounds
            Utils.ClampWithinWorld(ref minTileX, ref minTileY, ref maxTileX, ref maxTileY);

            // These 2 methods handle actually mining the tiles and walls while honoring tile explosion conditions
            bool explodeWalls = Projectile.ShouldWallExplode(Projectile.Center, explosionRadius, minTileX, maxTileX, minTileY, maxTileY);
            Projectile.ExplodeTiles(Projectile.Center, explosionRadius, minTileX, maxTileX, minTileY, maxTileY, explodeWalls);
        }
    }
}