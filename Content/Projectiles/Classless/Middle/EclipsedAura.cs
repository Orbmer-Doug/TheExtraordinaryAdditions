using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Middle;

public class EclipsedAura : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 145;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.localNPCHitCooldown = 14;
        Projectile.usesLocalNPCImmunity = true;
    }

    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer ModdedOwner => Owner.Additions();
    public ref float Time => ref Projectile.ai[0];
    public override void AI()
    {
        if (Owner == null || Owner.dead || Owner.active == false || !ModdedOwner.EclipsedOne)
        {
            Projectile.Kill();
            return;
        }

        Projectile.DamageType = Owner.HeldItem.DamageType;
        Projectile.damage = (int)Owner.GetTotalDamage(Projectile.DamageType).ApplyTo(Owner.HeldItem.damage) / 7;

        Projectile.Center = Owner.MountedCenter;
        Projectile.timeLeft = 2;

        if (Time % 3 == 2)
        {
            ParticleRegistry.SpawnCloudParticle(Projectile.Center + Main.rand.NextVector2Circular(30, 30), Main.rand.NextVector2Circular(2f, 2f) + Owner.velocity,
                Color.SlateBlue, Color.DarkSlateGray, Main.rand.Next(50, 80), Main.rand.NextFloat(50f, 90f), Main.rand.NextFloat(.5f, 1f));
        }
        if (Main.rand.NextBool(14))
            ParticleRegistry.SpawnSquishyPixelParticle(Projectile.Center + Main.rand.NextVector2Circular(200f, 200f),
                Main.rand.NextVector2Circular(6f, 6f), Main.rand.Next(90, 120), Main.rand.NextFloat(.4f, .8f), Color.White, Color.SlateBlue);

        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (npc != null && npc.Hitbox.Intersects(Projectile.Hitbox) && !npc.townNPC && !npc.friendly)
            {
                npc.AddBuff(BuffID.Frostburn, 120);
                npc.AddBuff(BuffID.Frostburn2, 120);
                npc.AddBuff(BuffID.Chilled, 120);
                if (!npc.boss && npc.realLife < 0)
                    npc.velocity += Owner.Center.SafeDirectionTo(npc.Center) * .21f * npc.knockBackResist;
            }
        }

        foreach (Projectile proj in Main.ActiveProjectiles)
        {
            if (proj != null && proj.hostile == true && proj.friendly == false
                && proj.Hitbox.Intersects(Projectile.Hitbox)
                && proj.velocity != Vector2.Zero && proj.damage.BetweenNum(0, 300))
            {
                proj.velocity *= .991f;
            }
        }

        Time++;
    }
    public override bool ShouldUpdatePosition() => false;
}
