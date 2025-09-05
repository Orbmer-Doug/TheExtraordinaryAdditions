using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;

public class SolemButterflyLament : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.SolemButterflyLament);

    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 4;
    }
    public Player Owner => Main.player[Projectile.owner];

    public override void SetDefaults()
    {
        Projectile.width = 20;
        Projectile.height = 20;
        Projectile.friendly = true;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.MaxUpdates = 2;
        Projectile.timeLeft = 360;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.tileCollide = false;

        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public override void AI()
    {
        Projectile.SetAnimation(4, 10);
        Projectile.FacingRight();

        Projectile.alpha++;
        if (Projectile.alpha >= 255)
        {
            Projectile.Kill();
        }

        Projectile.velocity *= .95f;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        SpriteEffects direction = Projectile.direction == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        Main.EntitySpriteDraw(texture, drawPosition, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, direction, 0);
        return false;
    }
}