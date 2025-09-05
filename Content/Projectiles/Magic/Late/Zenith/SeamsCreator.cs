using System;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Late.Zenith;

public class SeamsCreator : ModProjectile, ILocalizedModType, IModType
{
    public NPC Target => Main.npc[(int)Projectile.ai[0]];

    public float SlashDirection
    {
        get
        {
            if (Projectile.ai[1] > (float)Math.PI)
            {
                return Main.rand.NextFloatDirection();
            }
            return Projectile.ai[1] + Main.rand.NextFloatDirection() * 0.2f;
        }
    }

    public override string Texture => AssetRegistry.Invis;

    public const int Amt = 4;
    public const int Wait = 9;
    public const int Life = Amt * Wait;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.ignoreWater = false;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = Life;
        Projectile.MaxUpdates = 2;
        Projectile.noEnchantmentVisuals = true;
    }

    public override void AI()
    {
        if (Projectile.timeLeft % Wait == (Wait - 1))
        {
            if (this.RunLocal())
            {
                Vector2 pos = Target.RandAreaInEntity();
                Vector2 spawnOffset = SlashDirection.ToRotationVector2() * Main.rand.NextFloatDirection();
                Vector2 sliceVelocity = spawnOffset.SafeNormalize(Vector2.UnitY) * 0.1f;
                AdditionsSound.MediumSwing.Play(pos, .8f, 0f, .2f);

                Projectile.NewProjectile(Projectile.GetSource_FromThis(null), pos, sliceVelocity, ModContent.ProjectileType<Seams>(), (int)(Projectile.damage * 0.4f), 0f, Projectile.owner, 0f, 0f, 0f);
            }
        }
    }

    public override bool? CanDamage() => false;
}
