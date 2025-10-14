using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;

public class HailfireExplosion : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.Invis;

    public int Radius
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }

    public int Time
    {
        get => (int)Projectile.ai[1];
        set => Projectile.ai[1] = value;
    }

    public override void SetDefaults()
    {
        Projectile.width = 1;
        Projectile.height = 1;
        Projectile.friendly = true;
        Projectile.ignoreWater = false;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 20;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 15;
        Projectile.netImportant = true;
    }

    public override void AI()
    {
        if (Time == 0f)
        {
            ParticleRegistry.SpawnDetailedBlastParticle(Projectile.Center, Vector2.Zero, Vector2.One * Radius, Vector2.Zero, 30, Color.OrangeRed, null, Color.Red, true);

            float completion = InverseLerp(0f, HailfireShell.MaxTime, Radius);
            for (int i = 0; i < (int)MathHelper.Lerp(30, 100, completion); i++)
            {
                Vector2 pos = Projectile.Center;
                Vector2 vel = (Main.rand.NextVector2Circular(2f, 2f) + Main.rand.NextVector2Circular(17f, 17f)) * completion;
                Color color = Color.OrangeRed.Lerp(Color.Red, Main.rand.NextFloat(0f, .4f));
                int life = Main.rand.Next(30, 40);
                float scale = Main.rand.NextFloat(.7f, 1.2f) * completion;
                ParticleRegistry.SpawnHeavySmokeParticle(pos, vel * .8f, life, scale, color, .9f);
                ParticleRegistry.SpawnGlowParticle(pos, vel * .2f, life / 2, scale * 135f, color.Lerp(Color.White, .7f), .9f);
            }
        }

        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return Utility.CircularHitboxCollision(Projectile.Center, Radius / 2, targetHitbox);
    }
}
