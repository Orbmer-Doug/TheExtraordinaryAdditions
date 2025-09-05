using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;

public class AdvancedOnyxBlast : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.AdvancedOnyxBlast);
    public override void SetDefaults()
    {
        Projectile.width = 20; Projectile.height = 42;
        Projectile.aiStyle = 0;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.penetrate = 1;
        Projectile.timeLeft = 40;
        Projectile.extraUpdates = 1;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = true;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 30;
    }

    public ref float Time => ref Projectile.ai[0];
    public override void AI()
    {
        Vector2 pos = Projectile.Center;
        Vector2 vel = -Projectile.velocity.RotatedByRandom(.15f) * Main.rand.NextFloat(.4f, .8f);

        ParticleRegistry.SpawnBloomPixelParticle(pos, vel, Main.rand.Next(20, 30), Main.rand.NextFloat(.2f, .3f), Color.Violet, Color.DarkViolet, null, 1.1f);
        Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
        Time++;
    }
    public override void OnKill(int timeLeft)
    {
        Vector2 mainPos = Projectile.Center;

        if (this.RunLocal())
        {
            Projectile.penetrate = -1;
            Projectile.Resize(220, 220);
            Projectile.Damage();
        }

        SoundEngine.PlaySound(SoundID.Item14, Projectile.position);

        Vector2 Pos = mainPos + Vector2.One * -20f;
        int width = 40;
        int heigth = width;
        float rotate = MathHelper.Pi;

        Vector2 actualPosX = mainPos + Vector2.UnitX.RotatedByRandom(rotate).RotatedBy(Projectile.velocity.ToRotation()) * width / 2f;
        Vector2 actualPosY = mainPos + Vector2.UnitY.RotatedByRandom(rotate) * (float)Main.rand.NextDouble() * width / 2f;

        //Particle.Spawn(new TileHeatParticle(mainPos, 2f, 4, Color.Violet, Color.DarkViolet, Color.Black));

        for (int a = 0; a < 4; a++)
        {
            Dust.NewDust(actualPosY, width, heigth, DustID.Granite, 0f, 0f, 100, default, 1.5f);
        }
        for (int b = 0; b < 20; b++)
        {
            Dust fire = Main.dust[Dust.NewDust(Pos, width, heigth, DustID.PurpleTorch, 0f, 0f, 200, default, 3.7f)];
            fire.position = actualPosY;
            fire.noGravity = true;
            fire.noLight = true;
            Dust dust2 = fire;
            dust2.velocity *= 3f;
            dust2 = fire;
            dust2.velocity += Projectile.DirectionTo(fire.position) * (2f + Main.rand.NextFloat() * 4f);
            fire = Main.dust[Dust.NewDust(Pos, width, heigth, DustID.PurpleTorch, 0f, 0f, 100, default, 1.5f)];
            fire.position = actualPosY;
            dust2 = fire;
            dust2.velocity *= 2f;
            fire.noGravity = true;
            fire.fadeIn = 1f;
            fire.color = Color.Crimson * 0.5f;
            fire.noLight = true;
            dust2 = fire;
            dust2.velocity += Projectile.DirectionTo(fire.position) * 8f;
        }
        for (int c = 0; c < 20; c++)
        {
            int fire = Dust.NewDust(Pos, width, heigth, DustID.PurpleTorch, 0f, 0f, 0, default, 2.7f);
            Main.dust[fire].position = actualPosX;
            Main.dust[fire].noGravity = true;
            Main.dust[fire].noLight = true;
            Dust dust2 = Main.dust[fire];
            dust2.velocity *= 3f;
            dust2 = Main.dust[fire];
            dust2.velocity += Projectile.DirectionTo(Main.dust[fire].position) * 2f;
        }
        for (int d = 0; d < 70; d++)
        {
            int num166 = Dust.NewDust(Pos, width, heigth, DustID.Granite, 0f, 0f, 0, default(Color), 1.5f);
            Main.dust[num166].position = actualPosX;
            Main.dust[num166].noGravity = true;
            Dust dust2 = Main.dust[num166];
            dust2.velocity *= 3f;
            dust2 = Main.dust[num166];
            dust2.velocity += Projectile.DirectionTo(Main.dust[num166].position) * 3f;
        }

        float offsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
        int count = 25;

        for (int i = 0; i < count; i++)
        {
            Vector2 shootVelocity = (MathHelper.TwoPi * i / count + offsetAngle).ToRotationVector2() * Main.rand.NextFloat(.5f, 16f);
            ParticleRegistry.SpawnMistParticle(mainPos, shootVelocity, Main.rand.NextFloat(.5f, 1.2f), Color.Lerp(Color.BlueViolet, Color.MediumPurple, Main.rand.NextFloat(.2f, .64f)), Color.DarkBlue, 180);
        }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Color col1 = Color.Violet;
        Vector2 offsets = new Vector2(0f, Projectile.gfxOffY) - Main.screenPosition;
        float auraRotation = Projectile.rotation;
        Vector2 drawStartOuter = offsets + Projectile.Center;
        Vector2 spinPoint = -Vector2.UnitY * 12f;
        float time = Main.player[Projectile.owner].miscCounter % 216000f / 60f;
        float rotation = MathHelper.TwoPi * time / 3f;
        Rectangle frame = Utils.Frame(tex);
        Vector2 origin = Utils.Size(frame) * 0.5f;
        int count = 6;

        for (int i = 0; i < count; i++)
        {
            Vector2 spinStart = drawStartOuter + Utils.RotatedBy(spinPoint, (double)(rotation - (float)Math.PI * i / (count / 2f)), default);
            SpriteBatch spriteBatch = Main.spriteBatch;
            Color glowAlpha = Projectile.GetAlpha(col1 * Projectile.Opacity);
            glowAlpha.A = (byte)Projectile.alpha;
            spriteBatch.Draw(tex, spinStart, frame, glowAlpha * 1.9f, auraRotation, origin, Projectile.scale * 1.1f, 0, 0f);
        }
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Main.EntitySpriteDraw(tex, drawPosition, frame, Projectile.GetAlpha(Color.White), Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, 0, 0);

        return false;
    }
}
