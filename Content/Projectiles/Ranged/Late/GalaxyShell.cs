using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;

public class GalaxyShell : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.GalaxyShell);
    private const int Lifetime = 400;
    public override void SetDefaults()
    {
        Projectile.width = 7;
        Projectile.height = 11;
        Projectile.friendly = true;
        Projectile.ignoreWater = false;
        Projectile.aiStyle = 14;
        Projectile.penetrate = -1;
        Projectile.timeLeft = Lifetime;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }
    public int Time;

    public bool TouchedGrass;

    public override void AI()
    {
        Projectile.extraUpdates = 0;
        Time++;
        Timer++;
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

    public ref float Timer => ref Projectile.ai[0];
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        SpriteEffects direction = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

        Projectile.DrawProjectileBackglow(Color.MediumVioletRed, 3f);
        Vector2 drawOrigin = new(texture.Width * 0.5f, Projectile.height * 0.5f);
        for (int k = 0; k < Projectile.oldPos.Length; k++)
        {
            Vector2 drawPos = Projectile.oldPos[k] - Main.screenPosition + drawOrigin + new Vector2(0f, Projectile.gfxOffY);
            Color color = Projectile.GetAlpha(lightColor) * ((Projectile.oldPos.Length - k) / (float)Projectile.oldPos.Length);
            Main.EntitySpriteDraw(texture, drawPos, null, color, Projectile.rotation, drawOrigin, Projectile.scale, SpriteEffects.None, 0);
        }
        Main.EntitySpriteDraw(texture, drawPosition, texture.Frame(), Projectile.GetAlpha(lightColor), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, direction, 0);
        return false;
    }
}