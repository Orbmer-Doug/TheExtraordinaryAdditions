using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Late.Zenith;

public class Streaks : ModProjectile
{
    public const int Life = 75;
    public ref float Time => ref Projectile.ai[0];
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.SeamStrike);

    public override void SetDefaults()
    {
        Projectile.width = 600;
        Projectile.height = 40;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.Opacity = 1f;
        Projectile.timeLeft = Life;
        Projectile.MaxUpdates = 4;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 10;
        Projectile.noEnchantmentVisuals = true;
    }

    public override void AI()
    {
        Projectile.rotation = Projectile.velocity.ToRotation();
        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return Projectile.RotHitbox().Intersects(targetHitbox);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            Texture2D bloomTexture = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);
            float ratio = InverseLerp(0f, Life, Time);
            float completion = Animators.MakePoly(2).OutFunction(ratio);
            float opacity = 1f - Animators.MakePoly(2.5f).InFunction(ratio);
            Color color = MulticolorLerp(InverseLerp(0f, 10f, Projectile.identity / 10f % 1),
                Color.LightSteelBlue, Color.White, Color.WhiteSmoke, Color.FloralWhite, Color.LightSkyBlue) * opacity;

            float x = Projectile.width * completion;
            float y = Projectile.height * opacity;
            Vector2 scale = new(x, y);
            Vector2 bloomOrigin = bloomTexture.Size() / 2;

            for (float i = .1f; i <= 2f; i += .1f)
            {
                Main.spriteBatch.Draw(bloomTexture, ToTarget(Projectile.Center, scale * i), null, color * (2f - i), Projectile.rotation, bloomOrigin, 0, 0f);
            }
        }
        PixelationSystem.QueueTextureRenderAction(draw, PixelationLayer.UnderProjectiles, BlendState.Additive);
        return false;
    }
}
