using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Summon;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Pets;

public class JellyfishVro : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.JellyfishBro);
    public Player Owner => Main.player[Projectile.owner];
    public ref float Time => ref Projectile.ai[0];

    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 12;
        Main.projPet[Projectile.type] = true;
        ProjectileID.Sets.CharacterPreviewAnimations[Projectile.type] = ProjectileID.Sets.SimpleLoop(0, Main.projFrames[Projectile.type], 6);
    }

    public override void SetDefaults()
    {
        Projectile.netImportant = true;
        Projectile.scale = .7f;
        Projectile.width = 80;
        Projectile.height = 116;
        Projectile.friendly = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft *= 5;
        Projectile.tileCollide = false;
    }

    public override void AI()
    {
        if (Owner.Available() && Owner.HasBuff(ModContent.BuffType<BubbleMan>()))
            Projectile.timeLeft = 2;

        float dist = Projectile.Center.Distance(Owner.Center);
        Vector2 idealPosition = Owner.MountedCenter - Vector2.UnitY * MathHelper.Lerp(30f, 50f, Sin01(Time * .06f)) + Vector2.UnitX * Owner.direction * 40;
        Vector2 goalVelocity = (idealPosition - Projectile.Center) * 0.03f;

        float approachAcceleration = 0.1f + MathF.Pow(InverseLerp(70, 0, dist), 2f) * 0.3f;
        Projectile.velocity = Vector2.Lerp(Projectile.velocity, goalVelocity, approachAcceleration);
        Projectile.velocity *= 0.98f;
        Projectile.rotation = Projectile.rotation.AngleLerp(Projectile.velocity.X * .05f, .2f);

        Projectile.SetAnimation(12, 6);

        Vector2 pos = Projectile.RandAreaInEntity();
        if (Time % 33f == 0f)
        {
            for (int i = 0; i < 2; i++)
            {
                float rand = Main.rand.NextFloat(1f, 6f);
                Vector2 vel = Main.rand.NextVector2CircularEdge(rand, rand);

                Gore bubble = Gore.NewGorePerfect(Projectile.GetSource_FromAI(), pos, vel, 411);
                bubble.timeLeft = Main.rand.Next(8, 14);
                bubble.scale = Main.rand.NextFloat(0.6f, 1f) * 1.2f;
                bubble.type = Main.rand.NextBool(3) ? 412 : 411;
            }
        }

        Lighting.AddLight(Projectile.Top, Color.Navy.ToVector3() * 2f);

        Time++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Projectile.DrawBaseProjectile(lightColor);
        return false;
    }
}
