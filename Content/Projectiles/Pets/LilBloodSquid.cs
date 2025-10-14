using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Summon;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Pets;

public class LilBloodSquid : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.LilBloodSquid);
    public Player Owner => Main.player[Projectile.owner];
    public ref float Time => ref Projectile.ai[0];

    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 6;
        Main.projPet[Projectile.type] = true;
        ProjectileID.Sets.CharacterPreviewAnimations[Projectile.type] = ProjectileID.Sets.SimpleLoop(0, Main.projFrames[Projectile.type], 5);
    }

    public override void SetDefaults()
    {
        Projectile.netImportant = Projectile.friendly = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft *= 5;
        Projectile.tileCollide = false;
        Projectile.scale = .8f;
        Projectile.width = 44;
        Projectile.height = 70;
    }

    public override void AI()
    {
        if (Owner.Available() && Owner.HasBuff(ModContent.BuffType<HorrorsBeyondYourComprehension>()))
            Projectile.timeLeft = 2;

        float dist = Projectile.Center.Distance(Owner.Center);
        Vector2 idealPosition = Owner.MountedCenter - Vector2.UnitY * MathHelper.Lerp(20f, 40f, Sin01(Time * .06f)) - Vector2.UnitX * Owner.direction * 40;
        Vector2 goalVelocity = (idealPosition - Projectile.Center) * 0.03f;

        float approachAcceleration = 0.1f + MathF.Pow(InverseLerp(70, 0, dist), 2f) * 0.3f;
        Projectile.velocity = Vector2.Lerp(Projectile.velocity, goalVelocity, approachAcceleration);
        Projectile.velocity *= 0.98f;
        Projectile.rotation = Projectile.rotation.AngleLerp(Projectile.velocity.X * .05f, .2f);

        if (Utils.NextBool(Main.rand, 50))
        {
            int d1 = Dust.NewDust(Projectile.Center, Projectile.width, Projectile.height, DustID.Blood, 0f, 0f, 100, default(Color), 1.5f);
            int d2 = Dust.NewDust(Projectile.Center, Projectile.width, Projectile.height, DustID.CrimsonPlants, 0f, 0f, 170, default(Color), 0.5f);
            Main.dust[d2].noLight = true;
            Main.dust[d1].position = Projectile.Center;
            Main.dust[d2].position = Projectile.Center;
        }

        if (Main.bloodMoon)
            Lighting.AddLight(Projectile.Center, Color.Crimson.ToVector3() * .3f);

        Projectile.SetAnimation(6, 5);
        Time++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Projectile.DrawBaseProjectile(lightColor);
        if (Main.bloodMoon)
        {
            void draw()
            {
                Texture2D star = AssetRegistry.GetTexture(AdditionsTexture.LensStar);
                Vector2 orig = star.Size() / 2;
                Vector2 eye1 = Projectile.Center + PolarVector(-3f * Projectile.scale, Projectile.rotation) + PolarVector(23f * Projectile.scale, Projectile.rotation - MathHelper.PiOver2);
                Vector2 eye2 = Projectile.Center + PolarVector(14f * Projectile.scale, Projectile.rotation - MathHelper.PiOver2);
                Vector2 eye3 = Projectile.Center + PolarVector(9f * Projectile.scale, Projectile.rotation) + PolarVector(7f * Projectile.scale, Projectile.rotation - MathHelper.PiOver2);
                Vector2 eye4 = Projectile.Center + PolarVector(-7f * Projectile.scale, Projectile.rotation) + PolarVector(2f * Projectile.scale, Projectile.rotation - MathHelper.PiOver2);

                Main.spriteBatch.DrawBetterRect(star, ToTarget(eye1, Vector2.One * 25f), null, Color.Crimson, 0f, orig);
                Main.spriteBatch.DrawBetterRect(star, ToTarget(eye2, Vector2.One * 25f), null, Color.Crimson, 0f, orig);
                Main.spriteBatch.DrawBetterRect(star, ToTarget(eye3, Vector2.One * 25f), null, Color.Crimson, 0f, orig);
                Main.spriteBatch.DrawBetterRect(star, ToTarget(eye4, Vector2.One * 25f), null, Color.Crimson, 0f, orig);
            }
            PixelationSystem.QueueTextureRenderAction(draw, PixelationLayer.OverProjectiles, BlendState.Additive);
        }
        return false;
    }
}
