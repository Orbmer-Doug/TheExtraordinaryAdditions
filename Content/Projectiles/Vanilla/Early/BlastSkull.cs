using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Early;

public class BlastSkull : ModProjectile
{
    public override string Texture => ProjectileID.Skull.GetTerrariaProj();

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
        Main.projFrames[Projectile.type] = 3;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 26;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.DamageType = DamageClass.Magic;
    }

    public override void AI()
    {
        after ??= new(5, () => Projectile.Center);
        Projectile.FacingRight();
        Projectile.SetAnimation(3, 2);
        after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, Projectile.spriteDirection.ToSpriteDirection(), 255, 0, 0, Projectile.ThisProjectileTexture().Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame), false, .3f));

        Vector2 vel = Projectile.velocity * .2f;
        int time = Main.rand.Next(10, 20);
        float size = Main.rand.NextFloat(.4f, .6f);
        Color col = Color.OrangeRed;
        ParticleRegistry.SpawnHeavySmokeParticle(Projectile.RotHitbox().Left, vel, time, size, col, .8f, true);
    }

    public override Color? GetAlpha(Color lightColor)
    {
        if (Projectile.alpha > 0)
            return Color.Transparent;
        return new Color(255, 255, 255, 200);
    }

    public override void OnKill(int timeLeft)
    {
        SoundID.Item14.Play(Projectile.Center, 1f, 0f, .1f);
        if (this.RunLocal())
        {
            Projectile.penetrate = -1;
            Projectile.ExpandHitboxBy(64);
            Projectile.Damage();
        }

        for (int i = 0; i < 20; i++)
        {
            Vector2 vel = Main.rand.NextVector2Circular(5, 5);
            Vector2 pos = Projectile.Center;
            int time = Main.rand.Next(20, 30);
            float size = Main.rand.NextFloat(.5f, .6f);
            Color col = Color.OrangeRed;
            ParticleRegistry.SpawnHeavySmokeParticle(pos, vel, time, size, col, .8f, true);
        }

        ParticleRegistry.SpawnDetailedBlastParticle(Projectile.Center, Vector2.Zero, Vector2.One * 64f, Vector2.Zero, 30, Color.Orange, null, Color.OrangeRed, true);
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        Projectile.DrawBaseProjectile(Projectile.GetAlpha(lightColor), Projectile.direction == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None);
        after?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [Projectile.GetAlpha(Color.Tan)], .2f);
        return false;
    }
}
