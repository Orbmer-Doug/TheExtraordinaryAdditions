using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;

public class TheAnvil : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TheAnvil);

    public override void SetDefaults()
    {
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.width = 90;
        Projectile.height = 42;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 600;
        Projectile.ignoreWater = false;
        Projectile.knockBack = .3f;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Projectile.damage = (int)(Projectile.damage * 0.7f);
    }

    public int Time
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }
    public bool Crashed
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }

    private const float CrashSpeed = 18f;
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        // Crash
        if (oldVelocity.Y >= CrashSpeed)
        {
            SoundEngine.PlaySound(SoundID.DD2_OgreGroundPound with { Pitch = -.2f, PitchVariance = .1f, Volume = Main.rand.NextFloat(1.2f, 1.6f), MaxInstances = 10 }, Projectile.Center);
            Projectile.CreateFriendlyExplosion(Projectile.Center.Lerp(Projectile.Bottom, .25f), new(Projectile.width * 4, Projectile.height), Projectile.damage / 2, Projectile.knockBack, 7, 8);

            for (int i = 0; i < 70; i++)
            {
                Vector2 pos = Projectile.BottomLeft.Lerp(Projectile.BottomRight, Main.rand.NextFloat());
                Vector2 vel = -Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(.45f) * Main.rand.NextFloat(8.4f, 15.8f);
                ParticleRegistry.SpawnSparkParticle(pos, vel, Main.rand.Next(30, 50), Main.rand.NextFloat(.6f, 1.7f), Color.Chocolate, true, true);
            }
            ScreenShakeSystem.New(new(.05f, .1f), Projectile.Center);

            Projectile.velocity = Vector2.Zero;
            Projectile.Center += oldVelocity / 2;
            Crashed = true;
            return false;
        }

        // Sliding sparks
        else if ((oldVelocity.X.BetweenNum(-14, -1f, true) || oldVelocity.X.BetweenNum(1f, 14f, true)) && Time % 2f == 0f)
        {
            for (int i = 0; i < 10; i++)
            {
                Vector2 pos = Projectile.BottomLeft.Lerp(Projectile.BottomRight, Main.rand.NextFloat());
                ParticleRegistry.SpawnSparkParticle(pos, -Projectile.velocity.RotatedByRandom(.45f) * Main.rand.NextFloat(.1f, .5f), 40, Main.rand.NextFloat(.4f, 1f), Color.Chocolate);
            }

            Projectile.velocity *= .99f;
        }

        else if (Projectile.velocity.Length() == 0f && !Crashed)
        {
            ParticleRegistry.SpawnPulseRingParticle(Projectile.Center, Vector2.Zero, 10, 0f, new(1f), 0f, 120f, Color.Gray);
            SoundStyle impactSound = SoundID.DD2_ExplosiveTrapExplode with { Pitch = -.1f, Volume = 1.1f, MaxInstances = 50 };
            SoundEngine.PlaySound(impactSound, Projectile.Center);

            Crashed = true;
        }

        return false;
    }
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (Projectile.velocity.Length() > CrashSpeed)
        {
            modifiers.ScalingArmorPenetration += 1f;
            modifiers.FinalDamage *= 1.25f;
            modifiers.SetCrit();
        }
    }
    public override void AI()
    {
        after ??= new(10, () => Projectile.Center);
        after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0, 0, 0, 0f, null, false, .2f));

        if (Time == 20f)
        {
            for (int i = 0; i < 3; i++)
            {
                ParticleRegistry.SpawnMenacingParticle(Projectile.RandAreaInEntity(), Vector2.UnitY * -Main.rand.NextFloat(3f, 6f), 30, .5f, Color.Fuchsia);
            }
        }

        // 0.1f for arrow gravity, 0.4f for knife gravity
        if (Time > 20f && !Crashed)
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + .9f, -CrashSpeed, CrashSpeed);

        Projectile.Opacity = InverseLerp(0f, 5f, Time);
        Time++;
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        Point p = Projectile.Center.ToTileCoordinates();
        after?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [Color.Chocolate], Lighting.Brightness(p.X, p.Y));
        Projectile.DrawBaseProjectile(lightColor);
        return false;
    }
}