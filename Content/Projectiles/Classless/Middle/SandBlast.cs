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
        SoundID.Item14.Play(Projectile.Center, .9f, -.1f);
        Projectile.ExpandHitboxBy(32);

        int amount = 36;
        for (int i = 0; i < amount; i++)
        {
            Vector2 pos = (Vector2.Normalize(Projectile.velocity) * new Vector2(Projectile.width / 2f, Projectile.height) * 0.75f)
                .RotatedBy((double)((i - (amount / 2 - 1)) * MathHelper.TwoPi / amount), default) + Projectile.Center;

            Vector2 vel = pos - Projectile.Center;
            int dust = Dust.NewDust(pos + vel, 0, 0, DustID.UnusedBrown, vel.X * 1.5f, vel.Y * 1.5f, 100, default, 1.2f);
            Main.dust[dust].noGravity = true;
            Main.dust[dust].noLight = true;
            Main.dust[dust].velocity = vel;
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
