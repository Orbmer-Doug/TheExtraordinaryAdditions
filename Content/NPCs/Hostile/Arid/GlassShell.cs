using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.NPCs.Hostile.Arid;

public class GlassShell : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.GlassShell);
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
        ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
    }
    public override void SetDefaults()
    {
        Projectile.width = 136;
        Projectile.height = 2;
        Projectile.timeLeft = 500;
        Projectile.penetrate = 1;
        Projectile.friendly = false;
        Projectile.hostile = true;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = false;
        Projectile.aiStyle = 0;
        Projectile.extraUpdates = 5;
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        SpriteEffects direction = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

        Main.EntitySpriteDraw(texture, drawPosition, texture.Frame(), Projectile.GetAlpha(lightColor), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, direction, 0);
        return false;
    }
    public float count;
    public override void AI()
    {
        Lighting.AddLight(Projectile.Center, Color.OrangeRed.ToVector3() * 1f);

        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2 - MathHelper.PiOver4 * Projectile.spriteDirection - MathHelper.ToRadians(45f);
    }
    public override void OnKill(int timeLeft)
    {
        for (int i = 0; i < 10; i++)
        {
            int dust = Dust.NewDust(Projectile.Center, 1, 1, DustID.Smoke, 0f, 0f, 100, default, 1f);
            Main.dust[dust].noGravity = true;
            Main.dust[dust].velocity *= 2.0f;
            Main.dust[dust].scale *= 1.54f;

        }

    }
    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        for (int i = 0; i < 10; i++)
        {
            int dust = Dust.NewDust(Projectile.Center, 1, 1, DustID.Torch, 0f, 0f, 100, default, 1f);
            Main.dust[dust].noGravity = false;
            Main.dust[dust].velocity *= Main.rand.NextVector2CircularEdge(5f, 5f);
            Main.dust[dust].scale *= 1.84f;

        }
    }

}