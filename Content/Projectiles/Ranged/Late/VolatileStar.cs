using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;

public class VolatileStar : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.Invis;
    public ref float Time => ref Projectile.ai[0];
    public override void SetDefaults()
    {
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.penetrate = 1;
        Projectile.timeLeft = 120;
        Projectile.scale = 1f;
        Projectile.Size = new Vector2(40f);
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.MaxUpdates = 1;
    }

    public override void AI()
    {
        if (Time == 0f)
        {
            Projectile.scale = Main.rand.NextFloat(.9f, 1.2f);
            Projectile.ExpandHitboxBy((int)(Projectile.scale * Projectile.Size.X));
        }

        if (Main.rand.NextBool(19))
            ParticleRegistry.SpawnSparkleParticle(Projectile.RotHitbox().RandomPoint(), -Projectile.velocity * Main.rand.NextFloat(.2f, .5f),
                Main.rand.Next(20, 30), Main.rand.NextFloat(.7f, .8f), CurrentColor, Color.BlueViolet, Main.rand.NextFloat(.5f, 1.3f));

        after ??= new(6, () => Projectile.Center);
        after.UpdateFancyAfterimages(new(Projectile.Center, Projectile.Size, Projectile.Opacity, Projectile.rotation, 0, 130, 1, 0f, null, true));

        Projectile.rotation += (Math.Abs(Projectile.velocity.X) + Math.Abs(Projectile.velocity.Y)) * 0.01f * Projectile.direction;
        Projectile.Opacity = InverseLerp(0f, 4f, Time);
        Time++;
    }

    public Color CurrentColor => Color.Lerp(Color.White, MulticolorLerp(Cos01(Main.GlobalTimeWrappedHourly * 2f + Projectile.identity * .2f),
        Color.Violet, Color.DarkViolet, Color.BlueViolet, Color.SlateBlue, Color.DarkSlateBlue), .7f) * Projectile.Opacity * 1.1f;

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            Texture2D starTexture = AssetRegistry.GetTexture(AdditionsTexture.LensStar);
            Texture2D bloomTexture = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);

            float rotation = Main.GlobalTimeWrappedHourly * 7f;
            Vector2 center = Projectile.Center;

            float properBloomSize = bloomTexture.Height / (float)starTexture.Height;
            Color[] col = [CurrentColor * Projectile.Opacity * .8f];
            after.DrawFancyAfterimages(starTexture, col, Projectile.Opacity, Projectile.scale, 0f, true);
            after.DrawFancyAfterimages(bloomTexture, col, Projectile.Opacity, Projectile.scale, 0f, true);

            Vector2 origStar = starTexture.Size() / 2f;
            Vector2 origBloom = bloomTexture.Size() / 2f;

            Main.spriteBatch.Draw(starTexture, ToTarget(center, Projectile.Size * .5f), null, Color.White, -Projectile.rotation + MathHelper.PiOver4, origStar, 0, 0f);
            Main.spriteBatch.Draw(starTexture, ToTarget(center, Projectile.Size), null, CurrentColor, Projectile.rotation, origStar, 0, 0f);
            Main.spriteBatch.Draw(bloomTexture, ToTarget(center, Projectile.Size * 1.5f), null, CurrentColor * .7f, Projectile.rotation, origBloom, 0, 0f);
        }
        PixelationSystem.QueueTextureRenderAction(draw, PixelationLayer.UnderPlayers, BlendState.Additive);

        return false;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        for (int i = 0; i < 14; i++)
        {
            Vector2 vel = -Projectile.velocity.RotatedByRandom(.2f).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(2f, 7f);
            ParticleRegistry.SpawnGlowParticle(Projectile.Center, vel, Main.rand.Next(54, 90), Main.rand.NextFloat(.2f, .4f), CurrentColor);

            Dust.NewDustPerfect(Projectile.Center, DustID.AncientLight, vel.RotatedByRandom(.4f) * Main.rand.NextFloat(2.2f, 3f), Main.rand.Next(0, 200), CurrentColor, Main.rand.NextFloat(.8f, 1.1f)).noGravity = true;
        }
    }

    public override void OnKill(int timeLeft)
    {
        SoundID.Item4.Play(Projectile.Center, Main.rand.NextFloat(.3f, .4f), 0f, .2f, null, 30);

        ParticleRegistry.SpawnDetailedBlastParticle(Projectile.Center, Vector2.Zero, Projectile.Size * 1.8f, Vector2.Zero, 20, CurrentColor);
        Projectile.CreateFriendlyExplosion(Projectile.Center, Projectile.Size * 1.8f, Projectile.damage / 2, Projectile.knockBack / 2f, 20, 20);

        for (int i = 0; i < 12; i++)
        {
            ParticleRegistry.SpawnSparkleParticle(Projectile.Center, Main.rand.NextVector2Circular(10f, 10f), Main.rand.Next(20, 40), Main.rand.NextFloat(.4f, .8f), CurrentColor, Color.Violet, .8f);
        }
    }
}