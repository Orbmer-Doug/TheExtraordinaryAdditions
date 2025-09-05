using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Content.Buffs.Debuff;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Late;

public class RendedFlares : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 400;
        Projectile.penetrate = 1;
        Projectile.scale = 0f;
        Projectile.DamageType = DamageClass.Melee;
    }
    public ref float Time => ref Projectile.ai[0];
    public override void AI()
    {
        Time++;
        Projectile.scale = InverseLerp(0f, 30f, Time);
        for (int i = 0; i < 5; i++)
        {
            float angularVelocity = Main.rand.NextFloat(0.045f, 0.09f);
            Vector2 vel = Projectile.velocity.RotatedByRandom(.1f) * Main.rand.NextFloat(.1f, 1f);
            Color fireColor = MulticolorLerp(Main.rand.NextFloat(0.2f, 0.8f), Color.Red, Color.OrangeRed, Color.IndianRed, Color.DarkRed, Color.Orange, Color.DarkOrange, Color.OrangeRed * 1.6f);
            ParticleRegistry.SpawnHeavySmokeParticle(Projectile.Center, vel, Main.rand.Next(14, 23), Main.rand.NextFloat(.7f, 1f), fireColor, 1f, true, angularVelocity);
        }

        if (Time < 30f)
        {
            Vector2 newScale = Vector2.One * Projectile.scale * 26f;
            if (Projectile.Size != newScale)
                Projectile.Size = newScale;
        }

        //if (Time > 40f)
          //  Projectile.HomeInNPCBetter(700f, 12f, .12f, true, true);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        target.AddBuff(ModContent.BuffType<PlasmaIncineration>(), SecondsToFrames(3));
    }
    public override void OnKill(int timeLeft)
    {
        if (this.RunLocal())
        {
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 1.4f, Pitch = -.2f }, Projectile.Center);
            Color fireColor = MulticolorLerp(Main.rand.NextFloat(0.2f, 0.8f), Color.Red, Color.OrangeRed, Color.IndianRed, Color.DarkRed, Color.Orange, Color.DarkOrange, Color.OrangeRed * 1.6f);
            ParticleRegistry.SpawnDetailedBlastParticle(Projectile.Center, Vector2.Zero, Vector2.One * 70f * Projectile.scale, Vector2.Zero, 40, Color.OrangeRed);
            for (int i = 0; i < 22; i++)
                ParticleRegistry.SpawnGlowParticle(Projectile.Center, Main.rand.NextVector2Circular(8f, 8f), Main.rand.Next(15, 20), Main.rand.NextFloat(.4f, .9f), fireColor);


            Projectile.penetrate = -1;
            Projectile.position.X += Projectile.width / 2;
            Projectile.position.Y += Projectile.height / 2;
            Projectile.width = 70;
            Projectile.height = 70;
            Projectile.position.X -= Projectile.width / 2;
            Projectile.position.Y -= Projectile.height / 2;
            Projectile.Damage();
        }
    }
}
