using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;

public class GlacialShards : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.GlacialShell);
    public override void SetDefaults()
    {
        Projectile.width = 6;
        Projectile.height = 30;
        Projectile.hostile = false;
        Projectile.friendly = true;
        Projectile.tileCollide = true;
        Projectile.timeLeft = 360;
    }

    public bool HasHitTile
    {
        get => Projectile.ai[0] == 1f;
        set => Projectile.ai[0] = value.ToInt();
    }

    public override void AI()
    {
        after ??= new(3, () => Projectile.Center);
        after.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0, 0, 2));
        Projectile.Opacity = Utils.GetLerpValue(0f, 30f, Projectile.timeLeft, true);

        if (HasHitTile)
        {
            Projectile.velocity *= 0f;
        }
        else
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (Projectile.ai[1]++ > 25f && Projectile.velocity.Y < 16f)
                Projectile.velocity.Y += .3f;
        }

        Lighting.AddLight(Projectile.Center, Color.BlueViolet.ToVector3() * Projectile.scale * .5f);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        for (int i = 0; i < 20; i++)
        {
            Vector2 pos = Projectile.RotHitbox().Top;
            Vector2 vel = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(.3f) * Main.rand.NextFloat(2f, 9f);
            int life = Main.rand.Next(30, 40);
            float scale = Main.rand.NextFloat(.4f, .5f);
            ParticleRegistry.SpawnBloomPixelParticle(pos, vel, life, scale, Color.SlateBlue, Color.Blue);
            Dust.NewDustPerfect(Projectile.RandAreaInEntity(), DustID.t_Frozen, RandomVelocity(.5f, 1f, 5f), 0, default, Main.rand.NextFloat(.8f, 1.2f)).noGravity = true;
        }
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (!HasHitTile)
        {
            for (int i = 0; i < 12; i++)
                Dust.NewDustPerfect(Projectile.BaseRotHitbox().Top, DustID.t_Frozen, -oldVelocity.RotatedByRandom(.4f) * Main.rand.NextFloat(.1f, .2f), 40, default, Main.rand.NextFloat(.5f, .8f)).noGravity = true;

            SoundEngine.PlaySound(SoundID.Item50 with { PitchVariance = .1f, Volume = .85f }, Projectile.Center);
            HasHitTile = true;
        }

        return false;
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        after?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [Color.DarkSlateBlue, Color.SlateBlue, Color.BlueViolet], Projectile.Opacity);
        Projectile.DrawBaseProjectile(lightColor);
        return false;
    }
}