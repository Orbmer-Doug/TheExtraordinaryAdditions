using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;

public class DivineArrow : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.DivineArrow);
    public override void SetDefaults()
    {
        Projectile.width = 48;
        Projectile.height = 14;
        Projectile.aiStyle = 0;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.penetrate = 1;
        Projectile.timeLeft = Projectile.ArrowLifeTime;
        Projectile.ignoreWater = false;
        Projectile.tileCollide = true;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.aiStyle = ProjAIStyleID.Arrow;
        Projectile.arrow = true;
    }

    public ref float Time => ref Projectile.ai[0];
    public override void AI()
    {
        Vector2 tip = Projectile.RotHitbox().Bottom;
        Lighting.AddLight(tip, Color.Gold.ToVector3() * .45f);

        if (Time % 4f == 3f)
        {
            ParticleRegistry.SpawnSquishyLightParticle(tip, Projectile.velocity.RotatedByRandom(.15f) * Main.rand.NextFloat(.1f, .3f), 30, .18f, Color.Gold, .75f);
        }

        Projectile.FacingRight();
        Time++;
    }

    public override void OnKill(int timeLeft)
    {
        Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
        SoundID.Dig.Play(Projectile.Center);

        Projectile.NewProj(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<DissipatingLight>(), (int)(Projectile.damage * .55f), 0f, Projectile.owner);

        float offsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
        int amount = 6;
        for (int i = 0; i < amount; i++)
        {
            Vector2 shootVelocity = (MathHelper.TwoPi * i / amount + offsetAngle).ToRotationVector2() * Main.rand.NextFloat(6.5f, 10f);
            ParticleRegistry.SpawnSparkParticle(Projectile.Center, shootVelocity, 20, Main.rand.NextFloat(.9f, 1.5f), Color.Goldenrod);
        }
    }
}