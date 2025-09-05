using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;

public class IchorGlobule : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width =
        Projectile.height = 10;
        Projectile.friendly = true;
        Projectile.penetrate = 1;
        Projectile.ignoreWater = true;
        Projectile.MaxUpdates = 2;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 10;
    }

    public ref float Time => ref Projectile.ai[1];
    public override bool? CanHitNPC(NPC target) => Time >= 35f ? null : false;
    public override void AI()
    {
        Time++;

        Vector2 vel = Projectile.velocity.RotatedByRandom(.05f) * Main.rand.NextFloat(.1f, .2f);
        int life = Main.rand.Next(40, 50);
        float scale = Main.rand.NextFloat(1.4f, 1.7f);
        ParticleRegistry.SpawnSquishyPixelParticle(Projectile.Center, vel, life, scale, Color.Gold, Color.PaleGoldenrod, 3, true);
        Dust.NewDustPerfect(Projectile.RandAreaInEntity(), DustID.IchorTorch, vel * .1f, 0, default, Main.rand.NextFloat(.8f, 1.1f)).noGravity = true;

        if (NPCTargeting.TryGetClosestNPC(new(Projectile.Center, 650, true), out NPC target) && Time >= 35f)
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Projectile.SafeDirectionTo(target.Center) * 10f, .4f);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        target.AddBuff(BuffID.Ichor, 180 * Main.rand.Next(1, 3));
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        modifiers.ScalingArmorPenetration += 1f;
    }
}