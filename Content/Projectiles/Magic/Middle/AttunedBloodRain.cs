using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Middle;

public class AttunedBloodRain : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.BloodParticle2);
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Projectile.type] = 3;
        ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
    }

    public override void SetDefaults()
    {
        Projectile.width = 14;
        Projectile.height = 24;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.tileCollide = false;
        Projectile.extraUpdates = 0;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 250;
        Projectile.penetrate = 2;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 10;
    }

    private ref float Time => ref Projectile.ai[0];
    public override void AI()
    {
        Lighting.AddLight(Projectile.Center, Color.DarkRed.ToVector3() * .5f);
        Projectile.FacingUp();

        if (Time > 10f)
            Projectile.velocity.Y += .21f;

        Projectile.velocity *= 1.00125f;

        Projectile.Opacity = Projectile.scale = GetLerpBump(0f, 10f, 250f, 240f, Time);
        Time++;
    }


    public override void OnKill(int timeLeft)
    {
        for (int i = 0; i < 10; i++)
        {
            Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.Blood, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f, 100, new Color(53, Main.DiscoG, 255), 1f);
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Color color = Projectile.GetAlpha(Color.DarkRed) * Projectile.Opacity;
        float squish = MathHelper.Clamp(Projectile.velocity.Length() / 10f * 3f, 1f, 5f);

        Main.EntitySpriteDraw(texture, drawPosition, frame, color, Projectile.rotation, frame.Size() * 0.5f, new Vector2(1f, 1f * squish) * .18f, 0, 0);
        return false;
    }
}
