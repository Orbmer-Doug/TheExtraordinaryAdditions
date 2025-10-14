using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Late.Zenith;

public class Seams : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.SeamStrike);

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 1;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.penetrate = 3;
        Projectile.Opacity = 1f;
        Projectile.timeLeft = MaxTime;
        Projectile.MaxUpdates = 2;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = Projectile.MaxUpdates * 12;
        Projectile.noEnchantmentVisuals = true;
        Projectile.stopsDealingDamageAfterPenetrateHits = true;
    }

    public ref float Time => ref Projectile.ai[0];
    public const int MaxTime = 35;
    public const int MaxWidth = 1400;
    public float Interpolant => InverseLerp(0f, MaxTime, Time);
    public Point Size;
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(Size.X);
        writer.Write(Size.Y);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Size.X = reader.ReadInt32();
        Size.Y = reader.ReadInt32();
    }

    public override void AI()
    {
        Projectile.rotation = Projectile.velocity.ToRotation();

        int width = (int)Animators.MakePoly(3f).InOutFunction.Evaluate(70f, MaxWidth, Interpolant);
        int height = (int)Animators.MakePoly(3f).OutFunction.Evaluate(100f, 10f, Interpolant);
        Size = new(width, height);
        Projectile.Opacity = Animators.MakePoly(2f).InFunction(InverseLerp(0f, 5f * Projectile.MaxUpdates, Projectile.timeLeft));

        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 size = new(Size.X / 2, 10);
        return new RotatedRectangle(Projectile.Center - size / 2, size, Projectile.rotation).Intersects(targetHitbox);
    }

    public override bool ShouldUpdatePosition() => false;

    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            float progress = InverseLerp(0f, Time, MaxTime);
            Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);

            Vector2 origin = tex.Size() * 0.5f;
            Color col = Color.Lerp(Color.Magenta, Color.LightCoral, Projectile.identity / 7f % 1f) * Projectile.Opacity;

            for (float i = .5f; i < 1f; i += .1f)
            {
                Main.spriteBatch.DrawBetterRect(tex, ToTarget(Projectile.Center, Size.ToVector2() * i * .4f * Projectile.Opacity), null, Color.White * Projectile.Opacity, Projectile.rotation, origin);
                Main.spriteBatch.DrawBetterRect(tex, ToTarget(Projectile.Center, Size.ToVector2() * i), null, col, Projectile.rotation, origin);
                Main.spriteBatch.DrawBetterRect(tex, ToTarget(Projectile.Center, Size.ToVector2() * i * 1.3f), null, new Color(77, 0, 110) * Projectile.Opacity * .4f, Projectile.rotation, origin);
            }
        }
        PixelationSystem.QueueTextureRenderAction(draw, PixelationLayer.Dusts, BlendState.Additive);

        return false;
    }
}
