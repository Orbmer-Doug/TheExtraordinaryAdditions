using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles.Metaball;
using TheExtraordinaryAdditions.Content.Buffs.Debuff;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;

public class IonizedPlasma : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.Invis;

    public const int Lifetime = 120;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 50;
        Projectile.friendly = true;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.penetrate = 10;
        Projectile.timeLeft = Lifetime;
        Projectile.usesIDStaticNPCImmunity = true;
        Projectile.idStaticNPCHitCooldown = 3;
        Projectile.MaxUpdates = 3;
    }

    public ref float Timer => ref Projectile.ai[0];
    public override void AI()
    {
        Lighting.AddLight(Projectile.Center, Color.OrangeRed.ToVector3() * Projectile.scale * 2);

        int size = (int)Utils.Remap(Timer, 0f, Lifetime, 50f, 190f);
        if (Timer % 2 == 1)
            MetaballRegistry.SpawnPlasmaMetaball(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), Main.rand.NextVector2Circular(1f, 1f), 30, size, Projectile.scale);
        Projectile.ExpandHitboxBy(size);

        Projectile.scale = Utils.GetLerpValue(0f, Lifetime, Projectile.timeLeft, true);
        Timer++;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Projectile.damage = (int)(Projectile.damage * .85f);
        target.AddBuff(ModContent.BuffType<PlasmaIncineration>(), 300);
    }
}