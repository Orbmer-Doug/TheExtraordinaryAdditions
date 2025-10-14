using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain.Projectiles;

public class TaintedStar : ProjOwnedByNPC<StygainHeart>
{
    public override string Texture => AssetRegistry.Invis;

    public override void SetDefaults()
    {
        Projectile.width = 30;
        Projectile.height = 30;
        Projectile.friendly = false;
        Projectile.hostile = true;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 120;
        Projectile.scale = 1f;
        Projectile.velocity *= 2f;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }

    public int Timer
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }
    public bool Telegraph
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }

    public override void SafeAI()
    {
        Projectile.velocity *= 1.05f;

        if (Main.rand.NextBool(3))
        {
            int dustType = Main.rand.Next(3);
            Dust.NewDust(Projectile.Center, 14, 14, dustType switch
            {
                1 => DustID.Blood,
                0 => DustID.BloodWater,
                _ => DustID.Rain_BloodMoon,
            }, Projectile.velocity.X * 0.1f, Projectile.velocity.Y * 0.1f, 150, default, 1.3f);
        }
        if (Projectile.soundDelay == 0)
        {
            Projectile.soundDelay = 20 + Main.rand.Next(40);
            if (Main.rand.NextBool(5))
            {
                SoundID.Item9.Play(Projectile.Center, .9f, -.1f);
            }
        }
        if (Main.rand.NextBool(48) && Main.netMode != NetmodeID.Server)
        {
            int starGore = Gore.NewGore(Projectile.GetSource_FromAI(null), Projectile.Center, new Vector2(Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f), Main.rand.NextBool() ? GoreID.BloodSquid3 : GoreID.BloodZombieChunk2, 1f);
            Gore obj = Main.gore[starGore];
            obj.velocity *= 0.66f;
            Gore obj2 = Main.gore[starGore];
            obj2.velocity += Projectile.velocity * 0.3f;
        }
        Projectile.rotation += (Math.Abs(Projectile.velocity.X) + Math.Abs(Projectile.velocity.Y)) * 0.01f * Projectile.direction;
        Timer++;
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        StygainHeart.ApplyLifesteal(this, target, info.Damage);
        for (int i = 0; i <= 10; i++)
        {
            ParticleRegistry.SpawnBloodParticle(target.Center, (-Projectile.velocity * Main.rand.NextFloat(.4f, 1f)).RotatedByRandom(.5),
                40, Main.rand.NextFloat(.5f, .7f), Main.rand.NextBool() ? Color.IndianRed : Color.Red);
        }
    }

    public override void OnKill(int timeLeft)
    {
        SoundID.DD2_WitherBeastHurt.Play(Projectile.Center, 1.1f, 0f, .2f);

        for (int i = 0; i < 30; i++)
        {
            float offsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            Vector2 shootVelocity = (MathHelper.TwoPi * i / 10f + offsetAngle).ToRotationVector2() * 9f;
            Dust dust = Dust.NewDustPerfect(Projectile.position, DustID.Blood, shootVelocity, default, default, 1.6f);
            dust.noGravity = true;
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
        void draw()
        {
            Texture2D starTexture = AssetRegistry.GetTexture(AdditionsTexture.Sparkle);
            Texture2D bloomTexture = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);
            float properBloomSize = starTexture.Height / (float)bloomTexture.Height;
            float rotation = Main.GlobalTimeWrappedHourly * 14f;
            Vector2 sparkCenter = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(bloomTexture, sparkCenter, null, Color.IndianRed, 0f, bloomTexture.Size() / 2f, properBloomSize, 0, 0f);
            Main.EntitySpriteDraw(starTexture, sparkCenter, null, Color.DarkRed, -rotation + MathHelper.PiOver4, starTexture.Size() / 2f, 1.2f, 0, 0f);
            Main.EntitySpriteDraw(starTexture, sparkCenter, null, Color.Red, rotation, starTexture.Size() / 2f, 1.7f, 0, 0f);

            if (Telegraph)
            {
                Texture2D horiz = AssetRegistry.GetTexture(AdditionsTexture.BloomLineHoriz);
                Vector2 start = Projectile.Center;
                Vector2 end = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * 2000f;
                Vector2 tangent = start.SafeDirectionTo(end) * start.Distance(end);
                const float ImageThickness = 8;
                float thicknessScale = 1f / ImageThickness;
                Vector2 middleOrigin = new(0, horiz.Height / 2f);
                float rot = start.AngleTo(end);
                float opacity = GetLerpBump(0f, 10f, 50f, 40f, Timer);
                Main.spriteBatch.DrawBetter(horiz, start, null, Color.Crimson * opacity, rot, middleOrigin,
                    new Vector2(start.Distance(end) / horiz.Width, thicknessScale * opacity), SpriteEffects.None);
                Main.spriteBatch.DrawBetter(horiz, start, null, Color.DarkRed * opacity * .6f, rot, middleOrigin,
                    new Vector2(start.Distance(end) / horiz.Width, thicknessScale * opacity * 2f), SpriteEffects.None);
            }
        }
        PixelationSystem.QueueTextureRenderAction(draw, PixelationLayer.UnderProjectiles, BlendState.Additive);
        return false;
    }

}
