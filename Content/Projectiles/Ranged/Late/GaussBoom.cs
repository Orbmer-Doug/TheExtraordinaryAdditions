using Terraria;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;

public class GaussBoom : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.Invis;
    public const int Size = 450;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = Size;
        Projectile.friendly = true;
        Projectile.ignoreWater = false;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 10;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 11;
    }

    public override void AI()
    {
        Lighting.AddLight(Projectile.Center, Color.Lime.ToVector3() * 7f);

        if (Projectile.ai[0] == 0f)
        {
            Projectile.Damage();
            Projectile.netUpdate = true;

            const int count = 60;
            for (int i = 0; i < count; i++)
            {
                Vector2 pos = Projectile.Center;
                Vector2 vel = (MathHelper.TwoPi * i / count + RandomRotation()).ToRotationVector2() * Main.rand.NextFloat(12f, 22f);
                Color color = Color.Lerp(Color.Yellow * 1.4f, Color.GreenYellow * 1.8f, Main.rand.NextFloat(.4f, .89f));
                int lifetime = Main.rand.Next(40, 90);
                float scale = Main.rand.NextFloat(.9f, 1.7f);

                ParticleRegistry.SpawnGlowParticle(pos, vel * Main.rand.NextFloat(.6f, 1f), lifetime, scale * 60f, color, Main.rand.NextFloat(.7f, 1.3f));
                ParticleRegistry.SpawnBloomLineParticle(pos, vel.RotatedByRandom(.4f) * 1.4f, lifetime / 2, scale * .9f, color);
            }

            for (int a = 0; a < 5; a++)
            {
                Color randomColor = Main.rand.Next(4) switch
                {
                    0 => Color.Yellow * 1.6f,
                    1 => Color.YellowGreen * 1.2f,
                    2 => Color.YellowGreen * 1.9f,
                    _ => Color.Yellow * 1.8f,
                };
                Color auraColor = Main.rand.Next(4) switch
                {
                    0 => Color.Yellow * 1.6f,
                    1 => Color.YellowGreen * 1.2f,
                    2 => Color.YellowGreen * 1.9f,
                    _ => Color.Yellow * 1.8f,
                };

                Vector2 pos = Projectile.Center;
                int life = 46 + a * 5;

                Vector2 end = Vector2.One * Size * Utils.MultiLerp(InverseLerp(0f, 5f, a), .2f, .4f, .6f, .8f, 1f);
                ParticleRegistry.SpawnDetailedBlastParticle(pos, Vector2.Zero, end, Vector2.Zero, life, randomColor, null, auraColor, true);
            }

            Projectile.ai[0] = 1f;
        }
    }
}