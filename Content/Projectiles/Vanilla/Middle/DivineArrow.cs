using Terraria;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;

public class DivineArrow : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.DivineArrow.Path;

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
            ParticleRegistry.SpawnSquishyLightParticle(tip,
                Projectile.velocity.RotatedByRandom(.15f) * Main.rand.NextFloat(.1f, .3f), 30, .18f, Color.Gold, .75f);
        }

        Projectile.FacingRight();
        Time++;
    }

    public override void OnKill(int timeLeft)
    {
        Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width,
            Projectile.height);
        SoundID.Dig.Play(Projectile.Center);

        Projectile.CreateProj(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<DissipatingLight>(),
            (int) (Projectile.damage * .55f), 0f, Projectile.owner);

        float offsetAngle = RandomRotation();
        const int amount = 6;
        for (int i = 0; i < amount; i++)
        {
            Vector2 shootVelocity = (MathHelper.TwoPi * i / amount + offsetAngle).ToRotationVector2() *
                                    Main.rand.NextFloat(6.5f, 10f);
            ParticleRegistry.SpawnSparkParticle(Projectile.Center, shootVelocity, 20, Main.rand.NextFloat(.9f, 1.5f),
                Color.Goldenrod);
        }
    }
}

public class DissipatingLight : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GennedTextures.Invisible.Path;
    public Player Owner => Main.player[Projectile.owner];

    public override void SetDefaults()
    {
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.width =
            Projectile.height = 60;
        Projectile.alpha = 255;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 10;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 10;
        Projectile.penetrate = -1;
    }

    public override void AI()
    {
        if (Projectile.localAI[0] == 0f)
        {
            ParticleOrchestrator.RequestParticleSpawn(false,
                ParticleOrchestraType.Excalibur,
                new ParticleOrchestraSettings { PositionInWorld = Projectile.Center },
                Projectile.owner);
            Projectile.localAI[0] = 1f;
        }
    }
}

