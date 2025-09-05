using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.CrossCode;

public class ShockLightning : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.ShockLightning);

    public const int Lifetime = 20;
    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 5;
    }

    public override void SetDefaults()
    {
        Projectile.width = 32;
        Projectile.height = 96;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.DamageType = DamageClass.Generic;
        Projectile.timeLeft = Lifetime;
        Projectile.penetrate = -1;
    }

    public override bool ShouldUpdatePosition() => false;

    public ref float Time => ref Projectile.ai[0];
    public Vector2 End;
    public override void AI()
    {
        if (Time == 0f)
        {
            ParticleRegistry.SpawnFlash(Projectile.Center - Vector2.UnitY * Projectile.height / 2, 40, .8f, Projectile.height);
            ParticleRegistry.SpawnFlash(End, 30, .5f, Projectile.height);
            Projectile.velocity = Main.rand.NextBool() ? Vector2.UnitX : -Vector2.UnitX;
        }

        Projectile.frame = (int)MathHelper.Lerp(0, 5, InverseLerp(Lifetime, 0f, Projectile.timeLeft));
        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.LineCollision(Projectile.Center, End, Projectile.width);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D shock = Projectile.ThisProjectileTexture();
        Rectangle frame = shock.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame, 0, 0);

        Vector2 drawPos = Projectile.Center;
        Vector2 projDirToEnd = End.MoveTowards(drawPos, 24f) - drawPos;
        Vector2 normalized = projDirToEnd.SafeNormalize(Vector2.Zero);
        float segmentLength = frame.Height;
        float rot = normalized.ToRotation() + MathHelper.PiOver2;
        float lengthRemainingToDraw = projDirToEnd.Length() + segmentLength / 2f;

        while (lengthRemainingToDraw > 0f)
        {
            // Here, we draw the chain texture at the coordinates
            Main.spriteBatch.Draw(shock, drawPos - Main.screenPosition, frame, Color.White, rot, frame.Size() / 2, 1f, Projectile.direction.ToSpriteDirection(), 0f);

            // chainDrawPosition is advanced along the vector back to the player by the chainSegmentLength
            drawPos += normalized * segmentLength;
            lengthRemainingToDraw -= segmentLength;
        }

        return false;
    }
}
