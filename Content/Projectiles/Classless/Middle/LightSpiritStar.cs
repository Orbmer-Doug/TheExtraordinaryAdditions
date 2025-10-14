using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Middle;

public class LightSpiritStar : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.Invis;
    public static Color CurrentColor => MulticolorLerp(MathF.Pow(Sin01(Main.GlobalTimeWrappedHourly * MathHelper.Pi), 3), Color.Gold, Color.Goldenrod,
        Color.LightGoldenrodYellow, Color.PaleGoldenrod);

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 30;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.penetrate = 1;
        Projectile.timeLeft = SecondsToFrames(3);
        Projectile.scale = 1f;
    }

    public override void AI()
    {
        if (Main.rand.NextBool(6))
        {
            int dustType = Main.rand.Next(3);
            Dust.NewDust(Projectile.Center, 14, 14, dustType switch
            {
                1 => 57,
                0 => 15,
                _ => 58,
            }, Projectile.velocity.X * 0.1f, Projectile.velocity.Y * 0.1f, 150, default, Main.rand.NextFloat(.9f, 1.2f));
        }

        if (Projectile.soundDelay == 0)
        {
            Projectile.soundDelay = 30 + Main.rand.Next(40);
            if (Main.rand.NextBool(5))
            {
                SoundID.Item9.Play(Projectile.Center, .7f, 0f, 0f, null);
            }
        }

        if (Main.rand.NextBool(48) && Main.netMode != NetmodeID.Server)
        {
            Gore starGore = Gore.NewGoreDirect(Projectile.GetSource_FromAI(null), Projectile.Center, new Vector2(Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f), 16, 1f);
            starGore.velocity *= 0.66f;
            starGore.velocity += Projectile.velocity * 0.3f;
        }

        if (NPCTargeting.TryGetClosestNPC(new(Projectile.Center, 300, true), out NPC target))
        {
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Projectile.SafeDirectionTo(target.Center) * 10f, .28f);
        }

        Projectile.VelocityBasedRotation(.01f);
    }


    public override void OnKill(int timeLeft)
    {
        SoundID.Item4.Play(Projectile.Center, .7f, .1f, .1f, null, 20);

        for (int i = 0; i < 12; i++)
        {
            Vector2 vel = -Projectile.velocity.RotatedByRandom(.14f) * Main.rand.NextFloat(.3f, .6f) + Main.rand.NextVector2CircularEdge(2f, 2f);
            int dustType = Main.rand.Next(3);
            Dust.NewDustPerfect(Projectile.RandAreaInEntity(), dustType switch
            {
                1 => 57,
                0 => 15,
                _ => 58,
            }, vel, 150, default, Main.rand.NextFloat(.7f, 1f));
        }

        if (Main.netMode != NetmodeID.Server)
        {
            for (int i = 0; i < 3; i++)
            {
                Gore.NewGore(Projectile.GetSource_Death(null), Projectile.position, new Vector2(Projectile.velocity.X * 0.05f, Projectile.velocity.Y * 0.05f), Main.rand.Next(16, 18), 1f);
            }
        }
    }
    
    public override bool PreDraw(ref Color lightColor)
    {
        void star()
        {
            Texture2D starTexture = AssetRegistry.GetTexture(AdditionsTexture.Sparkle);
            Texture2D bloomTexture = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);
            float rotation = Main.GlobalTimeWrappedHourly * 14f;

            Main.spriteBatch.DrawBetterRect(bloomTexture, ToTarget(Projectile.Center, Vector2.One * Projectile.width * 1.6f), null, CurrentColor * .6f, 0f, bloomTexture.Size() / 2);
            Main.spriteBatch.DrawBetterRect(starTexture, ToTarget(Projectile.Center, Vector2.One * Projectile.width * .5f), null, CurrentColor, rotation + MathHelper.PiOver4, starTexture.Size() / 2);
            Main.spriteBatch.DrawBetterRect(starTexture, ToTarget(Projectile.Center, Vector2.One * Projectile.width * 1.1f), null, Color.White, rotation, starTexture.Size() / 2);
        }
        LayeredDrawSystem.QueueDrawAction(star, PixelationLayer.UnderProjectiles, BlendState.Additive);

        return false;
    }
}
