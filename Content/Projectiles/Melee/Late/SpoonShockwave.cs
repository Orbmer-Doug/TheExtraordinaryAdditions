using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Late;

public class SpoonShockwave : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.GlowParticleSmall);

    public const int Life = 150;
    public override void SetDefaults()
    {
        Projectile.width = 36;
        Projectile.height = 36;
        Projectile.hostile = false;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = Life;
        Projectile.extraUpdates = 3;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 10;
    }

    public ref float Time => ref Projectile.ai[0];
    public Vector2 Size;
    public override void SendExtraAI(BinaryWriter writer) => writer.WriteVector2(Size);
    public override void ReceiveExtraAI(BinaryReader reader) => Size = reader.ReadVector2();
    public override void AI()
    {
        Projectile.Opacity = GetLerpBump(0f, 30f, Life, Life - 50f, Time);
        Size.X += Animators.MakePoly(4f).OutFunction.Evaluate(0f, 50f, InverseLerp(0f, Life, Time));
        Size.Y = 100f;

        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 size = new(Size.X / 2, 10);
        Rectangle rect = new((int)(Projectile.Center.X - size.X / 2), (int)(Projectile.Center.Y - size.Y / 2), (int)size.X, (int)size.Y);
        return rect.Intersects(targetHitbox);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            Texture2D tex = Projectile.ThisProjectileTexture();
            Vector2 orig = tex.Size() / 2;
            for (float i = .9f; i < 1.3f; i += .1f)
                Main.spriteBatch.DrawBetterRect(tex, ToTarget(Projectile.Center, Size * i), null, Color.White * Projectile.Opacity, 0f, orig);
        }
        PixelationSystem.QueueTextureRenderAction(draw, PixelationLayer.OverNPCs, BlendState.Additive);

        return false;
    }
}
