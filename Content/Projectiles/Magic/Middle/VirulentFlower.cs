using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Middle;

public class VirulentFlower : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.VirulentFlower);
    private readonly int Timeleft = SecondsToFrames(5);
    public override void SetDefaults()
    {
        Projectile.width = 32;
        Projectile.height = 32;
        Projectile.friendly = true;
        Projectile.ignoreWater = false;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = Timeleft;
        Projectile.DamageType = DamageClass.Magic;
    }
    public override bool? CanDamage() => false;
    public override void AI()
    {
        Projectile.scale = Projectile.Opacity = GetLerpBump(0f, .3f, 1f, .7f, InverseLerp(0f, Timeleft, Projectile.timeLeft));

        // Sway left and right
        Projectile.rotation += ((float)MathF.Sin(Main.GlobalTimeWrappedHourly + Projectile.identity) * .1f) * (Projectile.identity % 2f == 1f).ToDirectionInt();

        // Emit light
        Lighting.AddLight(Projectile.Center, Color.LawnGreen.ToVector3() * Projectile.scale);

        // Enable "punching" the flower to go boom
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            Projectile proj = Main.projectile[i];

            int type = ModContent.ProjectileType<VirulentPunch>();
            if (proj.type == type && Projectile.Distance(proj.Center) <= Projectile.width && proj.active && proj.owner == Projectile.owner)
            {
                SoundID.Item43.Play(Projectile.Center, 1.2f, .1f, .1f);
                float offsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                for (int a = 0; a < 6; a++)
                {
                    Vector2 shootVelocity = (MathHelper.TwoPi * a / 6f + offsetAngle).ToRotationVector2() * Main.rand.NextFloat(4f, 6f);
                    if (this.RunLocal())
                        Projectile.NewProj(Projectile.Center, shootVelocity, ModContent.ProjectileType<VirulentSeed>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 1f);
                }

                Projectile.Kill();
            }
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Vector2 orig = tex.Size() * 0.5f;
        Main.EntitySpriteDraw(tex, drawPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, orig, Projectile.scale, 0, 0);
        return false;
    }

}