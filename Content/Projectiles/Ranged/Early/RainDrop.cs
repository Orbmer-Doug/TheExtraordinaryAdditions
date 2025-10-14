using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Early;

public class RainDrop : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.RainDrop);
    public ref float Time => ref Projectile.ai[0];

    public override void SetDefaults()
    {
        Projectile.width = 20;
        Projectile.height = 20;
        Projectile.timeLeft = 150;
        Projectile.penetrate = 1;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = true;
        Projectile.DamageType = DamageClass.Ranged;
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        modifiers.ScalingArmorPenetration += .75f;
    }

    public override void AI()
    {
        after ??= new(5, () => Projectile.Center);
        after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0, 255));

        Projectile.Opacity = InverseLerp(0f, 10f, Time);
        Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
        if (Time > 20f)
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + .2f, -20f, 15f);

        Time++;
    }

    public override void OnKill(int timeLeft)
    {
        for (int i = 0; i < 14; i++)
            Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Water, 0f, 0f, 0, default, 1f);
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Color color = Projectile.GetAlpha(lightColor);
        after?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [lightColor], Projectile.Opacity);
        Main.EntitySpriteDraw(texture, drawPosition, frame, color, Projectile.rotation, frame.Size() * 0.5f, 1f, 0, 0);
        return false;
    }
}