using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;

public class TinyServant : ModProjectile
{
    public override string Texture => "Terraria/Images/NPC_" + NPCID.ServantofCthulhu;

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
        ProjectileID.Sets.TrailingMode[Projectile.type] = 2;

        Main.projFrames[Projectile.type] = 2;
    }

    public override void SetDefaults()
    {
        Projectile.width = 15;
        Projectile.height = 10;
        Projectile.scale = .5f;
        Projectile.aiStyle = 0;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.penetrate = 1;
        Projectile.timeLeft = 120;
        Projectile.ignoreWater = false;
        Projectile.tileCollide = false;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
    }

    public ref float Timer => ref Projectile.ai[0];
    public override void AI()
    {
        if (trail == null || trail._disposed)
            trail = new(WidthFunction, ColorFunction, null, 20);

        Projectile.FacingLeft();
        Projectile.SetAnimation(2, 10);
        if (Main.rand.NextBool(5))
        {
            Dust.NewDustPerfect(Projectile.RotHitbox().RandomPoint(), DustID.BloodWater, -Projectile.velocity * Main.rand.NextFloat(.2f, .6f), 0, default, Main.rand.NextFloat(.8f, 1.5f));
        }

        cache ??= new(20);
        cache.Update(Projectile.Center);

        if (Timer >= 45f)
        {
            if (NPCTargeting.TryGetClosestNPC(new(Projectile.Center, 500, true), out NPC target))
            {
                Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Projectile.SafeDirectionTo(target.Center) * 16f, .1f);
            }
        }
        Timer++;
    }

    public override bool? CanHitNPC(NPC target) => Timer > 45 ? null : false;
    public override void OnKill(int timeLeft)
    {
        SoundID.NPCDeath1.Play(Projectile.Center, 1f, 0f, .2f, null, 0);

        for (int a = 0; a < 20; a++)
        {
            Dust.NewDustPerfect(Projectile.RotHitbox().RandomPoint(), DustID.Blood,
                Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(.3f) * Main.rand.NextFloat(1f, 5f), 0, default, Main.rand.NextFloat(.5f, 1.1f));
        }

    }

    public float WidthFunction(float c) => Projectile.height * MathHelper.SmoothStep(1f, 0f, c);
    public Color ColorFunction(SystemVector2 c, Vector2 position)
    {
        return Color.Red * MathF.Sqrt(c.X) * Projectile.Opacity;
    }

    public TrailPoints cache;
    public OptimizedPrimitiveTrail trail;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || cache == null)
                return;

            ManagedShader shader = ShaderRegistry.FadedStreak;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.ShadowTrail), 1);
            trail.DrawTrail(shader, cache.Points, 80);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        Texture2D texture = Projectile.ThisProjectileTexture();
        Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Projectile.DrawProjectileBackglow(Color.DarkRed, 4f, 140, 4);

        Main.EntitySpriteDraw(texture, drawPosition, frame, lightColor, Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, 0, 0);

        return false;
    }
}
