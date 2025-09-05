using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Early;

public class HomingSkull : ModProjectile
{
    public override string Texture => ProjectileID.Skull.GetTerrariaProj();

    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 3;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 26;
        Projectile.alpha = 255;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = 3;
    }

    public override void AI()
    {
        if (Projectile.alpha > 0)
            Projectile.alpha -= 50;
        Projectile.FacingRight();
        Projectile.SetAnimation(3, 2);

        if (NPCTargeting.TryGetClosestNPC(new(Projectile.Center, 300, true), out NPC target))
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Projectile.SafeDirectionTo(target.Center) * 5f, .07f);

        Vector2 vel = Projectile.velocity * .2f;
        int time = Main.rand.Next(10, 20);
        float size = Main.rand.NextFloat(.4f, .6f);
        Color col = Color.OrangeRed;
        ParticleRegistry.SpawnHeavySmokeParticle(Projectile.RotHitbox().Left, vel, time, size, col, .8f, true);
    }

    public override Color? GetAlpha(Color lightColor)
    {
        if (Projectile.alpha > 0)
        {
            return Color.Transparent;
        }
        return new Color(255, 255, 255, 200);
    }

    public override void OnKill(int timeLeft)
    {
        SoundEngine.PlaySound(SoundID.Dig, Projectile.position);
        for (int i = 0; i < 20; i++)
        {
            Vector2 vel = Main.rand.NextVector2Circular(2f, 2f);
            Vector2 pos = Projectile.RandAreaInEntity();
            int time = Main.rand.Next(20, 34);
            float size = Main.rand.NextFloat(.4f, .5f);
            Color col = Color.OrangeRed;
            ParticleRegistry.SpawnHeavySmokeParticle(pos, vel, time, size, col, .8f);
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Projectile.DrawBaseProjectile(Projectile.GetAlpha(lightColor), Projectile.direction == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None);
        return false;
    }
}
