using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Debuff;
using TheExtraordinaryAdditions.Content.NPCs.Misc;
using TheExtraordinaryAdditions.Core.Graphics;

namespace TheExtraordinaryAdditions.Content.Projectiles.Misc;

public class AshyWaterBalloonProjectile : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.AshyWaterBalloon);
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 24;
        Projectile.penetrate = -1;
        Projectile.DamageType = DamageClass.Generic;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.hostile = Projectile.friendly = true;
        Projectile.MaxUpdates = 4;
        Projectile.timeLeft = 60 * Projectile.MaxUpdates;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Main.rand.NextBool(10000))
        {
            int Snail = NPC.NewNPC(Projectile.GetSource_FromAI(), (int)target.Center.X, (int)target.Center.Y, ModContent.NPCType<TheGiantSnailFromAncientTimes>(), target.whoAmI);
            Main.npc[Snail].netUpdate = true;
        }

        target.AddBuff(ModContent.BuffType<AshyWater>(), 1200);
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        target.AddBuff(ModContent.BuffType<AshyWater>(), 1200);
    }

    public override void AI()
    {
        after ??= new(15, () => Projectile.Center);
        after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0, 200, 0, 0, null, true));

        Projectile.damage = 1;
        if (Projectile.localAI[0] == 0f)
        {
            SoundEngine.PlaySound(SoundID.Item20, Projectile.position, null);
            Projectile.localAI[0] += 1f;
        }

        Projectile.VelocityBasedRotation();
        Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + .2f, -20f, 20f);
        Projectile.velocity.X *= .98f;
    }

    public override void OnKill(int timeLeft)
    {
        for (int i = 0; i < 40; i++)
            Dust.NewDustPerfect(Projectile.Center, DustID.Ash, Main.rand.NextVector2Circular(4f, 4f), 0, default, Main.rand.NextFloat(.5f, 1.2f));

        foreach (Player player in Main.ActivePlayers)
        {
            if (!player.dead && Vector2.Distance(Projectile.Center, player.Center) < 220f)
                player.AddBuff(ModContent.BuffType<AshyWater>(), 1200);
        }
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (npc.life > 0 && Vector2.Distance(Projectile.Center, npc.Center) < 220f)
                npc.AddBuff(ModContent.BuffType<AshyWater>(), 1200);
        }
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        after?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [lightColor]);
        Projectile.DrawBaseProjectile(lightColor);
        return false;
    }
}