using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;

public class StickBoom : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 20;
        Projectile.friendly = true;
        Projectile.hostile = true;
        Projectile.penetrate = -1;
        Projectile.DamageType = DamageClass.Generic;
        Projectile.ignoreWater = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 0;
    }

    private Player Owner => Main.player[Projectile.owner];
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        modifiers.HitDirectionOverride = target.position.X > Owner.MountedCenter.X ? 1 : -1;
        if (Main.expertMode)
        {
            if (target.type >= NPCID.EaterofWorldsHead && target.type <= NPCID.EaterofWorldsTail)
                modifiers.FinalDamage /= 5;
        }
    }

    public override void AI()
    {
        Projectile.tileCollide = false;
        Projectile.alpha = 255;
        Projectile.Resize(300, 300);

        Projectile.damage = 130;
        Projectile.knockBack = 20f;
    }

    public override void OnKill(int timeLeft)
    {
        SoundEngine.PlaySound(SoundID.Item14, Projectile.position);
        for (int i = 0; i < 50; i++)
        {
            Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 100, default, 2f);
            dust.velocity *= 1.4f;
        }

        // Fire Dust spawn
        for (int i = 0; i < 80; i++)
        {
            Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 300, default, 1f);
            dust.noGravity = true;
            dust.velocity *= 10f;
            dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 100, default, 2f);
            dust.velocity *= 3f;
        }

        // Large Smoke Gore spawn
        for (int g = 0; g < 2; g++)
        {
            var goreSpawnPosition = new Vector2(Projectile.position.X + Projectile.width / 2 - 24f, Projectile.position.Y + Projectile.height / 2 - 24f);
            Gore gore = Gore.NewGoreDirect(Projectile.GetSource_FromThis(), goreSpawnPosition, default, Main.rand.Next(61, 64), 1f);
            gore.scale = 1.5f;
            gore.velocity.X += 1.5f;
            gore.velocity.Y += 1.5f;
            gore = Gore.NewGoreDirect(Projectile.GetSource_FromThis(), goreSpawnPosition, default, Main.rand.Next(61, 64), 1f);
            gore.scale = 1.5f;
            gore.velocity.X -= 1.5f;
            gore.velocity.Y += 1.5f;
            gore = Gore.NewGoreDirect(Projectile.GetSource_FromThis(), goreSpawnPosition, default, Main.rand.Next(61, 64), 1f);
            gore.scale = 1.5f;
            gore.velocity.X += 1.5f;
            gore.velocity.Y -= 1.5f;
            gore = Gore.NewGoreDirect(Projectile.GetSource_FromThis(), goreSpawnPosition, default, Main.rand.Next(61, 64), 1f);
            gore.scale = 1.5f;
            gore.velocity.X -= 1.5f;
            gore.velocity.Y -= 1.5f;
        }
        Projectile.Resize(100, 10);
    }
}