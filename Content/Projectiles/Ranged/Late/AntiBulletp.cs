using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Content.Buffs.Debuff;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;

public class AntiBulletp : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.AntiBulletp);
    public override void SetStaticDefaults()
    {

    }
    public override void SetDefaults()
    {
        Projectile.width = 38;
        Projectile.height = 12;
        Projectile.timeLeft = 120;
        Projectile.penetrate = -1;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.extraUpdates = 4;
    }

    public ref float Time => ref Projectile.ai[0];
    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        after?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [Color.White, Color.OrangeRed, Color.Chocolate], Projectile.Opacity);
        Projectile.DrawBaseProjectile(Color.White);
        return false;
    }

    public override void AI()
    {
        after ??= new(15, () => Projectile.Center);
        after.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0, 0, 3, 3f));

        Projectile.FacingRight();
        Lighting.AddLight(Projectile.Center, Color.OrangeRed.ToVector3() * 2f * Projectile.Opacity);
        Projectile.Opacity = InverseLerp(0f, 20f, Time);
        Time++;
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        modifiers.ScalingArmorPenetration += 1f;
        modifiers.DefenseEffectiveness *= 0f;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        AdditionsSound.AsterlinHit.Play(Projectile.Center, .9f, -.2f);
        Vector2 splatterDirection = Projectile.velocity * .5f;
        for (int i = 0; i < 12; i++)
        {
            // Release sparks
            int life = Main.rand.Next(55, 70);
            float scale = Main.rand.NextFloat(0.7f, Main.rand.NextFloat(3.3f, 5.5f));
            Color col = Color.Lerp(Color.Chocolate, Color.OrangeRed * 1.2f, Main.rand.NextFloat(0.8f));
            Vector2 vel = splatterDirection.RotatedByRandom(Main.rand.NextFloat(.48f, .57f)) * Main.rand.NextFloat(1f, 1.2f);

            Vector2 pos = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * Projectile.width * .5f;
            ParticleRegistry.SpawnSparkParticle(pos, vel, life, scale, col);

            // Fly off shrapnel
            if (Main.rand.NextBool(2))
            {
                Projectile.NewProj(pos, vel, ModContent.ProjectileType<AntiBulletShrapnel>(), (int)(Projectile.damage * .33f), 0f, Projectile.owner);
            }
        }

        target.AddBuff(ModContent.BuffType<DentedBySpoon>(), 900);

        // be balanced
        if (Projectile.damage > 500)
            Projectile.damage = (int)(Projectile.damage * 0.8f);
    }
}