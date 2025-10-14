using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Early;

public class MiniValor : ModProjectile
{
    public override string Texture => ProjectileID.Valor.GetTerrariaProj();

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Projectile.type] = 12;
        ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
    }

    public override void SetDefaults()
    {
        Projectile.scale = .5f;
        Projectile.width = Projectile.height = 8;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.penetrate = 5;
        Projectile.timeLeft = 1200;
        Projectile.ignoreWater = false;
        Projectile.tileCollide = false;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
    }

    public Player Owner => Main.player[Projectile.owner];
    public Projectile Proj => Main.projectile[(int)Projectile.ai[1]];
    public ref float Timer => ref Projectile.ai[2];
    public ref float Rotation => ref Projectile.AdditionsInfo().ExtraAI[0];
    public ref float OffsetAngle => ref Projectile.AdditionsInfo().ExtraAI[1];
    public ref float Direction => ref Projectile.AdditionsInfo().ExtraAI[2];
    public override void AI()
    {
        if (Timer == 0)
            this.Sync();
        Timer++;
        Rotation++;

        float dist = 40f;
        Projectile.Center = Proj.Center + OffsetAngle.ToRotationVector2() * dist;
        Projectile.rotation += .3f * Direction;
        OffsetAngle += MathHelper.ToRadians(3f) * Direction;

        if (Projectile.Distance(Proj.Center) > 2000f)
        {
            Projectile.Center = Proj.Center;
        }

        if (!Proj.active)
        {
            Projectile.Kill();
        }
        else
            Projectile.timeLeft = 2;
    }

    public override void OnKill(int timeLeft)
    {
        if (Projectile.numHits > 0 && Projectile.penetrate == 0)
        {
            for (int a = 0; a < 10; a++)
            {
                Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(Projectile.width, Projectile.height),
                    DustID.DungeonWater, Main.rand.NextVector2Circular(2f, 2f), 0, default, Main.rand.NextFloat(.5f, .7f));
            }
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        // vanilla shenanigans, i really couldn't tell you
        Vector2 center = Proj.Center;

        float x = Projectile.Center.X - center.X;
        float y = Projectile.Center.Y - center.Y;
        float rotation = (float)Math.Atan2(y, x) - MathHelper.PiOver2;

        bool draw = true;
        bool flag2 = true;
        if (x == 0f && y == 0f)
        {
            draw = false;
        }
        else
        {
            float circ = (float)Math.Sqrt(x * x + y * y);
            circ = 12f / circ;
            x *= circ;
            y *= circ;
            center.X -= x * 0.1f;
            center.Y -= y * 0.1f;
            x = Projectile.position.X + (float)Projectile.width * 0.5f - center.X;
            y = Projectile.position.Y + (float)Projectile.height * 0.5f - center.Y;
        }

        while (draw)
        {
            float num6 = 12f;
            float circ = (float)Math.Sqrt(x * x + y * y);
            float num8 = circ;
            if (float.IsNaN(circ) || float.IsNaN(num8))
            {
                draw = false;
                continue;
            }

            if (circ < 20f)
            {
                num6 = circ - 8f;
                draw = false;
            }
            circ = 12f / circ;
            x *= circ;
            y *= circ;
            if (flag2)
            {
                flag2 = false;
            }
            else
            {
                center.X += x;
                center.Y += y;
            }
            x = Projectile.position.X + (float)Projectile.width * 0.5f - center.X;
            y = Projectile.position.Y + (float)Projectile.height * 0.1f - center.Y;
            if (num8 > 12f)
            {
                float num9 = 0.3f;
                float num10 = Math.Abs(Projectile.velocity.X) + Math.Abs(Projectile.velocity.Y);
                if (num10 > 16f)
                {
                    num10 = 16f;
                }
                num10 = 1f - num10 / 16f;
                num9 *= num10;
                num10 = num8 / 80f;
                if (num10 > 1f)
                {
                    num10 = 1f;
                }
                num9 *= num10;
                if (num9 < 0f)
                {
                    num9 = 0f;
                }
                num9 *= num10;
                num9 *= 0.5f;
                if (y > 0f)
                {
                    y *= 1f + num9;
                    x *= 1f - num9;
                }
                else
                {
                    num10 = Math.Abs(Projectile.velocity.X) / 3f;
                    if (num10 > 1f)
                    {
                        num10 = 1f;
                    }
                    num10 -= 0.5f;
                    num9 *= num10;
                    if (num9 > 0f)
                    {
                        num9 *= 2f;
                    }
                    y *= 1f + num9;
                    x *= 1f - num9;
                }
            }

            rotation = (float)Math.Atan2(y, x) - MathHelper.PiOver2;

            Color white = Color.White;
            white.A = (byte)(white.A * 0.4f);
            if (Owner.stringColor > 0)
            {
                white = WorldGen.paintColor(Owner.stringColor);
                if (white.R < 75)
                    white.R = 75;
                if (white.G < 75)
                    white.G = 75;
                if (white.B < 75)
                    white.B = 75;
                switch (Owner.stringColor)
                {
                    case 13:
                        white = new Color(20, 20, 20);
                        break;
                    case 0:
                    case 14:
                        white = new Color(200, 200, 200);
                        break;
                    case 28:
                        white = new Color(163, 116, 91);
                        break;
                    case 27:
                        white = new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB);
                        break;
                }
                white.A = (byte)(white.A * 0.4f);
            }

            float opacity = 0.5f;
            white = Lighting.GetColor((int)center.X / 16, (int)(center.Y / 16f), white);

            Texture2D texture = TextureAssets.FishingLine.Value;
            Vector2 pos = new Vector2(center.X + (float)texture.Width * 0.5f * Projectile.scale, center.Y + (float)texture.Height * 0.5f * Projectile.scale) - new Vector2(6f * Projectile.scale, 0f);
            Rectangle frame = new Rectangle(0, 0, texture.Width, (int)num6);
            Vector2 orig = new Vector2(texture.Width * 0.5f, 0f);
            Main.spriteBatch.DrawBetter(texture, pos, frame, white * opacity, rotation, orig, 1f, SpriteEffects.None);
        }

        Texture2D tex = Projectile.ThisProjectileTexture();
        float off = (float)(tex.Width - Projectile.width) * 0.5f + (float)Projectile.width * 0.5f;
        Vector2 origed = new(off, tex.Height / 2);
        Main.spriteBatch.DrawBetter(tex, Projectile.Center, null,
            lightColor * Projectile.Opacity, Projectile.rotation, origed, Projectile.scale);
        return false;
    }
}
