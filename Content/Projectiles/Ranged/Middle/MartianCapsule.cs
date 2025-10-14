using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;

public class MartianCapsule : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.MartianCapsule);

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.aiStyle = -1;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.penetrate = 1;
        Projectile.timeLeft = SecondsToFrames(6);
        Projectile.ignoreWater = true;
        Projectile.tileCollide = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 2;
    }

    public override void AI()
    {
        after ??= new(4, () => Projectile.Center);
        after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0, 30, 2, 1f, null, true));

        if (Projectile.ai[0]++ > 15f)
        {
            if (NPCTargeting.TryGetClosestNPC(new(Projectile.Center, 450, true), out NPC target))
                Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Projectile.SafeDirectionTo(target.Center) * 14f, .1f);
        }

        Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + .1f, -20f, 20f);
        Projectile.VelocityBasedRotation(.02f);
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);
        SoundEngine.PlaySound(SoundID.Item10 with { PitchVariance = .1f }, Projectile.position);

        if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > float.Epsilon)
            Projectile.velocity.X = -oldVelocity.X;
        if (Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > float.Epsilon)
            Projectile.velocity.Y = -oldVelocity.Y;

        return false;
    }

    public override void OnKill(int timeLeft)
    {
        Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
        SoundEngine.PlaySound(SoundID.Item91 with { PitchVariance = .1f, MaxInstances = 100 }, Projectile.Center);

        float offsetAngle = RandomRotation();
        for (int i = 0; i < 4; i++)
        {
            Vector2 shootVelocity = (MathHelper.TwoPi * i / 4f + offsetAngle).ToRotationVector2() * Main.rand.NextFloat(4f, 7f);
            int dmg = (int)(Projectile.damage * .25);
            int type = ModContent.ProjectileType<MartianLaser>();
            if (this.RunLocal())
                Projectile.NewProj(Projectile.Center, shootVelocity, type, dmg, 0f);

            for (int j = 0; j < 4; j++)
            {
                ParticleRegistry.SpawnSparkParticle(Projectile.Center, Main.rand.NextVector2CircularLimited(3f, 3f, .5f, 1f),
                    Main.rand.Next(30, 50), Main.rand.NextFloat(.4f, .7f), Color.SkyBlue);
            }
        }
        ParticleRegistry.SpawnSparkleParticle(Projectile.Center, Vector2.Zero, Main.rand.Next(19, 23), 2.5f, Color.DeepSkyBlue, Color.LightCyan, 1.4f);
        ParticleRegistry.SpawnDetailedBlastParticle(Projectile.Center, Vector2.Zero, Vector2.One * 80f, Vector2.Zero, Main.rand.Next(16, 20), Color.Cyan);
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        after?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [Color.LightCyan, Color.Cyan, Color.DarkCyan], Projectile.Opacity);
        Projectile.DrawBaseProjectile(Color.White);
        return false;
    }
}