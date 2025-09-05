using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Middle;

public class VirulentSeed : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.VirulentSeed);
    public override void SetDefaults()
    {
        Projectile.width = 14;
        Projectile.height = 22;
        Projectile.friendly = true;
        Projectile.ignoreWater = false;
        Projectile.tileCollide = false;
        Projectile.penetrate = 1;
        Projectile.timeLeft = 180;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }
    public ref float Time => ref Projectile.ai[0];
    public bool HasHitTarget
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }
    public Player Owner => Main.player[Projectile.owner];
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        target.AddBuff(BuffID.Venom, 60);

        if (!HasHitTarget)
        {
            Projectile.velocity *= 0.8f;
            HasHitTarget = true;
            Projectile.netUpdate = true;
        }
    }
    public override void AI()
    {
        Projectile.FacingUp();
        Time++;
        Projectile.Opacity = GetLerpBump(0f, 10f, 180f, 160f, Time);

        after ??= new(8, () => Projectile.Center);
        after.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One * Projectile.scale, Projectile.Opacity, Projectile.rotation, 0, 90, 3));

        if (Time.BetweenNum(30f, 120f))
        {
            NPC target = NPCTargeting.GetWeakestNPC(new(Projectile.Center, 400, true));
            if (target.CanHomeInto())
            {
                Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Projectile.Center.SafeDirectionTo(target.Center) * 10f, .3f);
                Projectile.velocity += Utility.VelEqualTrig(Projectile.velocity.SafeNormalize(Vector2.Zero), MathF.Sin, 20f, 1.5f, ref Projectile.Additions().ExtraAI[0], ref Projectile.Additions().ExtraAI[1]);
            }
        }
    }

    public override void OnKill(int timeLeft)
    {
        for (int i = 0; i < 10; i++)
        {
            Color col = Color.ForestGreen.Lerp(Color.DarkOliveGreen, Main.rand.NextFloat(.4f, .6f));
            ParticleRegistry.SpawnDustParticle(Projectile.RotHitbox().RandomPoint(), Projectile.velocity * Main.rand.NextFloat(.1f, .2f), Main.rand.Next(20, 30), Main.rand.NextFloat(.4f, .7f), col, .1f, false);
            ParticleRegistry.SpawnMistParticle(Projectile.RotHitbox().RandomPoint(), Projectile.velocity * .2f, Main.rand.NextFloat(0.45f, .9f), Color.ForestGreen, Color.DarkOliveGreen, Main.rand.NextFloat(100f, 140f));
        }
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        after?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [Color.LawnGreen, Color.Green, Color.Olive, Color.DarkOliveGreen]);
        Projectile.DrawBaseProjectile(lightColor);
        return false;
    }
}