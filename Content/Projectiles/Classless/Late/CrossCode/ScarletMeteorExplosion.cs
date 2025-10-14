using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.CrossCode;

public class ScarletMeteorExplosion : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.ScarletMeteorExplosion);

    private const int horiz = 6;

    private const int vert = 2;

    public int FrameX
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }
    public int FrameY
    {
        get => (int)Projectile.ai[1];
        set => Projectile.ai[1] = value;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 100;
        Projectile.friendly = true;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.DamageType = DamageClass.Generic;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public override void AI()
    {
        Projectile.frameCounter++;
        if (Projectile.frameCounter % 3 == 2)
        {
            FrameX++;
            if (FrameX >= horiz)
            {
                FrameY++;
                FrameX = 0;
            }
            if (FrameY >= 2)
            {
                Projectile.Kill();
            }
        }
        Projectile.FacingUp();
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        // Prevent shredding of literally any enemy with more than one segment
        Projectile.damage = (int)(Projectile.damage * .9f);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return CircularHitboxCollision(Projectile.Center, 119f, targetHitbox);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Rectangle frame = tex.Frame(horiz, vert, FrameX, FrameY);
        Vector2 orig = frame.Size() / 2;
        Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, frame, Color.White, Projectile.rotation, orig, 1f, 0, 0f);
        return false;
    }
}
