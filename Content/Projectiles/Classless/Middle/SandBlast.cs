using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Middle;

public class SandBlast : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.SandBlast);
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
    }

    public override void SetDefaults()
    {
        Projectile.width = 10;
        Projectile.height = 10;
        Projectile.hostile = false;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 360;
        Projectile.alpha = 255;
        Projectile.scale = 1.5f;
    }

    public override void AI()
    {
        Projectile.tileCollide = Projectile.timeLeft < 240;
        Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.125f, 0f, 1f);
        if (Collision.SolidCollision(Projectile.position - Vector2.One * 5f, 10, 10))
        {
            Projectile.scale *= 0.9f;
            Projectile.velocity *= 0.25f;
            if (Projectile.scale < 0.5f)
            {
                Projectile.Kill();
            }
        }
        else
        {
            Projectile.velocity *= .999f;
        }
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
    }
    public override void OnKill(int timeLeft)
    {
        SoundEngine.PlaySound(SoundID.Item14, (Vector2?)Projectile.position, null);
        Projectile.position = Projectile.Center;
        Projectile.width = Projectile.height = 32;
        Projectile.position.X = Projectile.position.X - Projectile.width / 2;
        Projectile.position.Y = Projectile.position.Y - Projectile.height / 2;
        int amount = 36;
        for (int i = 0; i < amount; i++)
        {
            Vector2 val = (Vector2.Normalize(Projectile.velocity) * new Vector2(Projectile.width / 2f, Projectile.height) * 0.75f).RotatedBy((double)((i - (amount / 2 - 1)) * MathHelper.TwoPi / amount), default) + Projectile.Center;
            Vector2 vector7 = val - Projectile.Center;
            int num228 = Dust.NewDust(val + vector7, 0, 0, DustID.UnusedBrown, vector7.X * 1.5f, vector7.Y * 1.5f, 100, default, 1.2f);
            Main.dust[num228].noGravity = true;
            Main.dust[num228].noLight = true;
            Main.dust[num228].velocity = vector7;
        }
        Projectile.maxPenetrate = -1;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 10;
        Projectile.Damage();
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Vector2 origin = texture.Size() * 0.5f;
        Projectile.DrawProjectileBackglow(Color.Orange, 6f, 72, 7);
        Main.spriteBatch.Draw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, 0, 0f);
        return false;
    }
}
