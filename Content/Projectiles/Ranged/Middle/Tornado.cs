using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;

public class Tornado : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TornadoProj);

    public override void SetDefaults()
    {
        Projectile.width = 10;
        Projectile.height = 10;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 600;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 15;
    }

    public override void AI()
    {
        Projectile.localAI[1] += 1f;
        if (Projectile.localAI[1] >= 10f)
        {
            Projectile.localAI[1] = 0f;
            int projCount = 0;
            int oldestTornado = 0;
            float tornadoAge = 0f;
            int projType = Projectile.type;
            for (int projIndex = 0; projIndex < Main.maxProjectiles; projIndex++)
            {
                Projectile proj = Main.projectile[projIndex];
                if (proj.active && proj.owner == Projectile.owner && proj.type == projType && proj.ai[0] < 900f)
                {
                    projCount++;
                    if (proj.ai[0] > tornadoAge)
                    {
                        oldestTornado = projIndex;
                        tornadoAge = proj.ai[0];
                    }
                }
            }
            if (projCount > 3)
            {
                Main.projectile[oldestTornado].netUpdate = true;
                Main.projectile[oldestTornado].ai[0] = 36000f;
                Main.projectile[oldestTornado].damage = 0;
                return;
            }
        }
        float num1125 = 900f;
        if (Projectile.soundDelay == 0)
        {
            Projectile.soundDelay = -1;
            SoundEngine.PlaySound(SoundID.Item122, (Vector2?)Projectile.Center, null);
        }
        Projectile.ai[0] += 1f;
        if (Projectile.ai[0] >= num1125)
        {
            Projectile.Kill();
        }
        if (Projectile.localAI[0] >= 30f)
        {
            Projectile.damage = 0;
            if (Projectile.ai[0] < num1125 - 120f)
            {
                float num1126 = Projectile.ai[0] % 60f;
                Projectile.ai[0] = num1125 - 120f + num1126;
                Projectile.netUpdate = true;
            }
        }
        float num1127 = 15f;
        float num1128 = 15f;
        Point point8 = Utils.ToTileCoordinates(Projectile.Center);
        int num1129 = default(int);
        int num1130 = default(int);
        Collision.ExpandVertically(point8.X, point8.Y, out num1129, out num1130, (int)num1127, (int)num1128);
        num1129++;
        num1130--;
        Vector2 value72 = new Vector2(point8.X, num1129) * 16f + new Vector2(8f);
        Vector2 value73 = new Vector2(point8.X, num1130) * 16f + new Vector2(8f);
        Vector2 vector146 = Vector2.Lerp(value72, value73, 0.5f);
        Vector2 value74 = default(Vector2);
        value74 = new Vector2(0f, value73.Y - value72.Y);
        value74.X = value74.Y * 0.2f;
        Projectile.width = (int)(value74.X * 0.65f);
        Projectile.height = (int)value74.Y;
        Projectile.Center = vector146;
        if (this.RunLocal())
        {
            bool flag74 = false;
            Vector2 center16 = Main.player[Projectile.owner].Center;
            Vector2 top = Main.player[Projectile.owner].Top;
            for (float num1131 = 0f; num1131 < 1f; num1131 += 0.05f)
            {
                Vector2 position2 = Vector2.Lerp(value72, value73, num1131);
                if (Collision.CanHitLine(position2, 0, 0, center16, 0, 0) || Collision.CanHitLine(position2, 0, 0, top, 0, 0))
                {
                    flag74 = true;
                    break;
                }
            }
            if (!flag74 && Projectile.ai[0] < num1125 - 120f)
            {
                float num1132 = Projectile.ai[0] % 60f;
                Projectile.ai[0] = num1125 - 120f + num1132;
                Projectile.netUpdate = true;
            }
        }
        _ = Projectile.ai[0];
        _ = num1125 - 120f;
        Projectile.velocity.Y += .4f;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        float num226 = 600f;
        float num227 = 15f;
        float num228 = 15f;
        float num229 = Projectile.ai[0];
        float scale5 = MathHelper.Clamp(num229 / 30f, 0f, 1f);
        if (num229 > num226 - 60f)
        {
            scale5 = MathHelper.Lerp(1f, 0f, (num229 - (num226 - 60f)) / 60f);
        }
        Point point5 = Utils.ToTileCoordinates(Projectile.Center);
        int num230 = default(int);
        int num231 = default(int);
        Collision.ExpandVertically(point5.X, point5.Y, out num230, out num231, (int)num227, (int)num228);
        num230++;
        num231--;
        float num232 = 0.2f;
        Vector2 value32 = new Vector2(point5.X, num230) * 16f + new Vector2(8f);
        Vector2 value33 = new Vector2(point5.X, num231) * 16f + new Vector2(8f);
        Vector2.Lerp(value32, value33, 0.5f);
        Vector2 vector33 = default(Vector2);
        vector33 = new Vector2(0f, value33.Y - value32.Y);
        vector33.X = vector33.Y * num232;
        new Vector2(value32.X - vector33.X / 2f, value32.Y);
        Texture2D texture2D23 = ModContent.Request<Texture2D>(Texture, AssetRequestMode.ImmediateLoad).Value;
        Rectangle rectangle9 = Utils.Frame(texture2D23, 1, 1, 0, 0, 0, 0);
        Vector2 origin3 = Utils.Size(rectangle9) / 2f;
        float num233 = -(float)Math.PI / 50f * num229;
        Vector2 spinningpoint2 = Utils.RotatedBy(Vector2.UnitY, (double)(num229 * 0.1f), default(Vector2));
        float num234 = 0f;
        float num235 = 5.1f;
        Color value34 = default(Color);
        value34 = new Color(225, 225, 225);
        Vector2 value35 = default(Vector2);
        for (float num236 = (int)value33.Y; num236 > (int)value32.Y; num236 -= num235)
        {
            num234 += num235;
            float num237 = num234 / vector33.Y;
            float num238 = num234 * (MathHelper.TwoPi) / -20f;
            float num239 = num237 - 0.15f;
            Vector2 vector34 = Utils.RotatedBy(spinningpoint2, (double)num238, default(Vector2));
            value35 = new Vector2(0f, num237 + 1f);
            value35.X = value35.Y * num232;
            Color color39 = Color.Lerp(Color.Transparent, value34, num237 * 2f);
            if (num237 > 0.5f)
            {
                color39 = Color.Lerp(Color.Transparent, value34, 2f - num237 * 2f);
            }
            color39.A = (byte)(color39.A * 0.5f);
            color39 *= scale5;
            vector34 *= value35 * 100f;
            vector34.Y = 0f;
            vector34.X = 0f;
            vector34 += new Vector2(value33.X, num236) - Main.screenPosition;
            Main.spriteBatch.Draw(texture2D23, vector34, (Rectangle?)rectangle9, color39, num233 + num238, origin3, 1f + num239, 0, 0f);
        }
        return false;
    }
}
