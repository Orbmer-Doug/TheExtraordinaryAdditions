using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Misc;

public class Rebar : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Rebar);
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
        ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
    }

    public override void SetDefaults()
    {
        Projectile.width = 10;
        Projectile.height = 36;
        Projectile.hostile = false;
        Projectile.friendly = true;
        Projectile.tileCollide = true;
        Projectile.timeLeft = 255;
        Projectile.scale = 1f;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.penetrate = -1;
    }

    public bool Bounced;
    public override void AI()
    {
        after ??= new(8, () => Projectile.Center);
        after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0, 255));

        if (Bounced)
            Projectile.rotation += Projectile.direction * .2f;
        else
            Projectile.rotation = Projectile.velocity.ToRotation();

        Projectile.alpha++;

        if (Projectile.ai[0]++ % 3f == 0f)
        {
        }
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Bounced = true;
        Projectile.velocity = -Projectile.velocity.RotatedByRandom(.4f);
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        Bounced = true;
    }

    public override bool OnTileCollide(Vector2 lastVelocity)
    {
        Bounced = true;
        if (Projectile.velocity != lastVelocity && Math.Abs(lastVelocity.X) > 0f)
        {
            Projectile.velocity = lastVelocity.RotatedByRandom(.4f) * 1f;
        }
        return false;
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Vector2 origin = texture.Size() * 0.5f;
        Projectile.DrawProjectileBackglow(Color.DarkSlateGray, 6f, 72, 7);
        after?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [lightColor]);
        Main.spriteBatch.Draw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, 0, 0f);
        return false;
    }
}
