using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;

public class CryingTear : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.BloodParticle2);
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 30;
        Projectile.hostile = false;
        Projectile.friendly = true;
        Projectile.tileCollide = true;
        Projectile.timeLeft = SecondsToFrames(3);
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.penetrate = 1;
    }

    public ref float Time => ref Projectile.ai[0];
    public override void AI()
    {
        Projectile.FacingUp();
        Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + .3f, -10f, 30f);

        if (Time > 20f)
        {
            if (NPCTargeting.TryGetClosestNPC(new(Projectile.Center, 400, true, true), out NPC target))
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(target.Center + target.velocity) * 9f, .1f);
        }

        Time++;
    }

    public override void OnKill(int timeLeft)
    {
        for (int i = 0; i < 12; i++)
        {
            ParticleRegistry.SpawnBloodParticle(Projectile.Center, -Projectile.velocity.RotatedByRandom(1.8f) * Main.rand.NextFloat(.2f, .6f),
                Main.rand.Next(20, 40), Main.rand.NextFloat(.5f, .8f), Color.DarkBlue.Lerp(Color.Aqua, .3f));
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        Color color = Color.DarkBlue;
        float squish = MathHelper.Clamp(Projectile.velocity.Length() / 10f * 3f, 1f, 5f);
        Main.spriteBatch.DrawBetter(texture, Projectile.Center, null, color, Projectile.rotation, texture.Size() * 0.5f, new Vector2(1f, 1f * squish) * .08f);
        return false;
    }
}
