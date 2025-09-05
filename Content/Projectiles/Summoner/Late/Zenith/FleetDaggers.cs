using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Summoner.Late.Avia;

public class FleetDaggers : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.FleetDaggers);

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.MinionShot[Projectile.type] = true;
    }

    public override void SetDefaults()
    {
        Projectile.width = 14; Projectile.height = 46;
        Projectile.aiStyle = 0;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.penetrate = 1;
        Projectile.timeLeft = 200;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.DamageType = DamageClass.Summon;
    }

    public ref float Time => ref Projectile.ai[0];
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        for (int i = 0; i < 18; i++)
        {
            Vector2 pos = Projectile.RotHitbox().RandomPoint();
            Vector2 vel = Projectile.velocity * Main.rand.NextFloat(.2f, .4f);
            int life = Main.rand.Next(30, 40);
            float scale = Main.rand.NextFloat(.5f, .9f);
            Color col = Color.Violet.Lerp(Color.DarkViolet, Main.rand.NextFloat(.3f, .6f));
            ParticleRegistry.SpawnDustParticle(pos, vel, life, scale, col, .1f, false, true);
            ParticleRegistry.SpawnBloomPixelParticle(pos, vel, life, scale * 1.4f, col, col * 2f, null, 1.8f, 4);

            ParticleRegistry.SpawnMistParticle(pos, Main.rand.NextVector2Circular(5f, 5f), Main.rand.NextFloat(.7f, 1.4f), Color.Violet, Color.DarkViolet, Main.rand.NextFloat(90f, 140f));
        }
        AdditionsSound.DeepHit.Play(Projectile.Center, 1f, 0f, .1f, 20);
        if (Main.rand.NextBool(5))
        {
            AdditionsSound.MediumExplosion.Play(Projectile.Center, 1.2f);

            Projectile.NewProj(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<TenebrisBlast>(), (int)(Projectile.damage * 1.25), 0f);
        }
    }
    
    public override void AI()
    {
        after ??= new(6, () => Projectile.Center);
        after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0, 255));

        Time++;
        Projectile.Opacity = InverseLerp(0f, 10f, Time);
        if (Main.rand.NextBool())
            ParticleRegistry.SpawnHeavySmokeParticle(Projectile.RotHitbox().RandomPoint(), Projectile.velocity * Main.rand.NextFloat(.1f, .4f), Main.rand.Next(15, 24), Main.rand.NextFloat(.4f, .6f), Color.Purple.Lerp(Color.Violet, .4f), Main.rand.NextFloat(.4f, .8f));
        
        Lighting.AddLight(Projectile.Center, Color.BlueViolet.ToVector3() * 1f);
        Projectile.FacingUp();
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Color color = Color.Lerp(Color.Violet, Color.DeepPink, (float)MathF.Sin(Main.GlobalTimeWrappedHourly * 6f));
        float val = MathHelper.Lerp(0.93f, 1.07f, Sin01(MathHelper.TwoPi * Projectile.timeLeft / 14f));
        float opacity = MathHelper.Lerp(0.65f, val, Utils.GetLerpValue(0f, 15f, Time, true) * Utils.GetLerpValue(0f, 15f, Projectile.timeLeft, true));
        after?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [color], opacity);
        Projectile.DrawProjectileBackglow(color, 5f, 0, 9);
        return true;
    }
}