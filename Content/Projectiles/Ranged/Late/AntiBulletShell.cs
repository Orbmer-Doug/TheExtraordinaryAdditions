using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;

public class AntiBulletShell : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.AntiBulletShell);
    private const int Lifetime = 400;
    public override void SetDefaults()
    {
        Projectile.width = 50;
        Projectile.height = 14;
        Projectile.friendly = true;
        Projectile.ignoreWater = false;
        Projectile.aiStyle = 14;
        Projectile.penetrate = -1;
        Projectile.timeLeft = Lifetime;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public int Time;
    public bool TouchedGrass;
    public override void AI()
    {
        Time++;
        Projectile.extraUpdates = 0;

        if (!TouchedGrass)
        {
            Projectile.VelocityBasedRotation(0f);
        }
        Projectile.velocity.Y -= 0.055f;
        Projectile.velocity.X *= 0.992f;

        if (Time < 240)
        {
            Vector2 pos = Projectile.Center + Vector2.UnitX.RotatedBy(Projectile.rotation) * Main.rand.NextFloat(-Projectile.width * .5f, Projectile.width * .5f);
            Vector2 vel = Vector2.UnitY * -Main.rand.NextFloat(3f, 6f);
            float size = Main.rand.NextFloat(.5f, .9f);
            int type = Main.rand.NextBool() ? DustID.SteampunkSteam : DustID.Smoke;
            Dust.NewDustPerfect(pos, type, vel, default, default, size);
        }
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (!TouchedGrass)
            TouchedGrass = true;

        if (Projectile.velocity.Length() > 5f)
            SoundEngine.PlaySound(SoundID.NPCHit4 with { Pitch = .4f, Volume = .5f }, Projectile.Center);

        if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > float.Epsilon)
            Projectile.velocity.X = -oldVelocity.X * .45f;
        if (Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > float.Epsilon)
            Projectile.velocity.Y = -oldVelocity.Y * .45f;
        Projectile.velocity *= 0.98f;

        return false;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        float interpolant = Utils.GetLerpValue(Lifetime - 240f, Lifetime, Projectile.timeLeft, true);
        Projectile.DrawBaseProjectile(lightColor);
        Projectile.DrawProjectileBackglow(Color.Lerp(Color.Chocolate, Color.Chocolate * 2f, interpolant) * interpolant, interpolant * 3f);
        return false;
    }
}