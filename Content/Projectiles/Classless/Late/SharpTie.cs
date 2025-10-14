using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Late;

public class SharpTie : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.SharpTie);

    public override void SetDefaults()
    {
        Projectile.width = 16;
        Projectile.height = 4;
        Projectile.timeLeft = 1000;
        Projectile.penetrate = 1;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.DamageType = DamageClass.Generic;
        Projectile.aiStyle = 0;
        Projectile.stopsDealingDamageAfterPenetrateHits = true;
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        modifiers.ScalingArmorPenetration += 1f;
    }

    public ref float Timer => ref Projectile.ai[0];
    public ref float FadeTimer => ref Projectile.ai[1];
    public override void AI()
    {
        after ??= new(10, () => Projectile.Center);
        after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.scale, Projectile.rotation, 0, 10));

        Projectile.rotation += Projectile.direction * 0.3f * Animators.MakePoly(3f).InFunction(Projectile.scale);
        Lighting.AddLight(Projectile.Center, Color.WhiteSmoke.ToVector3() * .6f);

        if (Projectile.Opacity == 0)
        {
            Projectile.velocity *= .44f;
            float interpol = InverseLerp(0, 20, FadeTimer);
            Projectile.scale = 1f - interpol;
            if (interpol >= 1)
                Projectile.Kill();
            FadeTimer++;
        }
        else
        {
            if (Timer % 2f == 0f)
            {
                Vector2 pos = Projectile.RotHitbox().RandomPoint();
                Vector2 vel = -Projectile.velocity.RotatedByRandom(.15f) * .8f;
                Color c1 = Color.White;
                float scale = Main.rand.NextFloat(.3f, .7f);
                ParticleRegistry.SpawnSparkParticle(pos, vel, 30, scale, c1);
            }

            NPC target = NPCTargeting.GetClosestNPC(new(Projectile.Center, 500, true, true));
            if (target != null && Timer > 20)
            {
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(target.Center) * 18f, 0.1f);
            }
            else
            {
                if (Projectile.velocity.Length() < 30f)
                    Projectile.velocity.Y -= .5f;
            }
        }

        Timer++;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Projectile.Opacity = 0f;
        for (int i = 0; i < 15; i++)
        {
            Vector2 pos = target.RotHitbox().GetClosestPoint(Projectile.Center) + Main.rand.NextVector2Circular(8, 8);
            Vector2 vel = -Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(.38f) * Main.rand.NextFloat(2f, 8f);
            int life = Main.rand.Next(20, 40);
            float scale = Main.rand.NextFloat(.4f, .9f);
            ParticleRegistry.SpawnSparkParticle(pos, vel, life, scale, Color.White);
        }
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        after?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [lightColor * Projectile.scale]);
        Projectile.DrawBaseProjectile(lightColor);
        return false;
    }
}