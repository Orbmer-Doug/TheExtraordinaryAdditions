using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Middle;

public class StarWaterBreak : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.StarWaterBreak);
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer ModdedOwner => Owner.Additions();

    public override void SetDefaults()
    {
        Projectile.width = 20;
        Projectile.height = 6;
        Projectile.timeLeft = 600;
        Projectile.penetrate = 1;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = true;
        Projectile.DamageType = DamageClass.Magic;
    }
    
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        modifiers.ScalingArmorPenetration += 1f;
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        after?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [Color.DarkSlateBlue, Color.SlateBlue, Color.Blue]);
        Projectile.DrawBaseProjectile(Color.White);
        return false;
    }

    public ref float Time => ref Projectile.ai[0];
    public override void AI()
    {
        after ??= new(10, () => Projectile.Center);
        after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0, 0, 1, 1f));

        Time++;
        if (Time <= 60f)
        {
            Projectile.rotation -= 1f;
            Projectile.velocity *= 0.985f;
            return;
        }
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(45f);

        if (Projectile.timeLeft > 60)
        {
            Projectile.timeLeft = 60;
        }

        Projectile.velocity *= 0.92f;
    }

    public override void OnKill(int timeLeft)
    {
        ParticleRegistry.SpawnSparkleParticle(Projectile.Center, Vector2.Zero, 20, Main.rand.NextFloat(1f, 2.4f), Color.BlueViolet, Color.DarkSlateBlue, .8f, .02f);
    }
}