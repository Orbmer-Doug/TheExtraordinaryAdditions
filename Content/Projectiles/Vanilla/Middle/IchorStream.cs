using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;

public class IchorStream : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width =
        Projectile.height = 32;
        Projectile.hostile = false;
        Projectile.friendly = true;
        Projectile.penetrate = 5;
        Projectile.extraUpdates = 2;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = true;
        Projectile.DamageType = DamageClass.Magic;
    }
    public ref float Time => ref Projectile.ai[0];
    public override void AI()
    {
        Projectile.scale -= 0.002f;
        if (Projectile.scale <= 0f)
        {
            Projectile.Kill();
        }
        if (Time > 1f)
        {
            Projectile.velocity.Y += 0.075f;
            if (Main.rand.NextBool(5))
            {
                float scale = Main.rand.NextFloat(.4f, .8f);
                ParticleRegistry.SpawnBloodParticle(Projectile.Center, Projectile.velocity.RotatedByRandom(.25f) * Main.rand.NextFloat(.3f, .6f),
                    Main.rand.Next(25, 40), scale, Color.Gold);
            }
            int offset = 16;
            Vector2 pos = new(Projectile.position.X + offset, Projectile.position.Y + offset);
            for (int i = 0; i < 3; i++)
            {
                float velX = Projectile.velocity.X / 3f * i;
                float velY = Projectile.velocity.Y / 3f * i;
                int num125 = 14;
                int main = Dust.NewDust(new Vector2(Projectile.position.X + num125, Projectile.position.Y + num125), Projectile.width - num125 * 2, Projectile.height - num125 * 2, DustID.Ichor, 0f, 0f, 100);
                Main.dust[main].noGravity = true;
                Dust dust2 = Main.dust[main];
                dust2.velocity *= 0.1f;
                dust2 = Main.dust[main];
                dust2.velocity += Projectile.velocity * 0.5f;
                dust2.position.X -= velX;
                dust2.position.Y -= velY;
            }
            if (Main.rand.NextBool(8))
            {
                int fall = Dust.NewDust(pos, Projectile.width - offset * 2, Projectile.height - offset * 2, DustID.Ichor, 0f, 0f, 100, default, 0.5f);
                Dust dust2 = Main.dust[fall];
                dust2.velocity *= 0.25f;
                dust2 = Main.dust[fall];
                dust2.velocity += Projectile.velocity * 0.5f;
            }
        }
        Time++;
    }
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        // Vanilla
        if (target.IsDestroyer() || target.type == NPCID.Probe)
        {
            modifiers.FinalDamage *= .75f;
        }
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        // convenient the ichor id is 69...
        target.AddBuff(BuffID.Ichor, 600);
    }
}
