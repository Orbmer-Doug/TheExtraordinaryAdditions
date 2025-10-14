using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Early;

public class WaterStream : ModProjectile
{
    public Player Owner => Main.player[Projectile.owner];

    public override string Texture => AssetRegistry.Invis;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 18;
        Projectile.aiStyle = 0;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.penetrate = 5;
        Projectile.timeLeft = SecondsToFrames(6);
        Projectile.ignoreWater = true;
        Projectile.tileCollide = true;
        Projectile.DamageType = DamageClass.Magic;

        Projectile.extraUpdates = 2;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 20;
        Projectile.aiStyle = 12;
    }

    public ref float Time => ref Projectile.ai[0];
    public override void AI()
    {
        Time++;

        if (Projectile.ai[1] == 1f)
        {
            if (NPCTargeting.TryGetClosestNPC(new(Projectile.Center, 240, true), out NPC target))
                Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Projectile.SafeDirectionTo(target.Center) * 10f, .09f);
        }

        Lighting.AddLight(Projectile.Center, Color.Aqua.ToVector3() * .5f);
        Color col1 = Color.Aquamarine;
        Color col2 = Color.DarkBlue;
        Color col3 = Color.CornflowerBlue;
        Vector2 pos = Projectile.Center;
        Vector2 altpos = Projectile.RandAreaInEntity();
        Vector2 vel = -Projectile.velocity * .5f;
        Vector2 altvel = -Projectile.velocity * Main.rand.NextFloat(.3f, 1.49f);

        for (int a = 0; a < 2; a++)
        {
            for (int b = 0; b < 3; b++)
            {
                float num133 = Projectile.velocity.X / 3f * b;
                float num134 = Projectile.velocity.Y / 3f * b;
                int num135 = 6;
                int num136 = Dust.NewDust(new Vector2(Projectile.position.X + num135, Projectile.position.Y + num135), Projectile.width - num135 * 2, Projectile.height - num135 * 2, DustID.DungeonWater, 0f, 0f, 100, default, 1.2f);
                Main.dust[num136].noGravity = true;
                Dust dust2 = Main.dust[num136];
                dust2.velocity *= 0.3f;
                dust2 = Main.dust[num136];
                dust2.velocity += Projectile.velocity * 0.5f;
                Main.dust[num136].position.X -= num133;
                Main.dust[num136].position.Y -= num134;
            }

            if (Main.rand.NextBool(8))
            {
                int num137 = 6;
                int num138 = Dust.NewDust(new Vector2(Projectile.position.X + num137, Projectile.position.Y + num137), Projectile.width - num137 * 2, Projectile.height - num137 * 2, DustID.DungeonWater, 0f, 0f, 100, default, 0.75f);
                Dust dust2 = Main.dust[num138];
                dust2.velocity *= 0.5f;
                dust2 = Main.dust[num138];
                dust2.velocity += Projectile.velocity * 0.5f;
            }

        }

        if (Time % 3f == 2f)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 2; i++)
                {
                    float rand = Main.rand.NextFloat(1f, 3f);
                    Vector2 veloc = Main.rand.NextVector2CircularEdge(rand, rand);

                    Gore bubble = Gore.NewGorePerfect(Projectile.GetSource_FromAI(), pos, veloc, 411);
                    bubble.timeLeft = Main.rand.Next(8, 14);
                    bubble.scale = Main.rand.NextFloat(0.6f, 1f) * 1.2f;
                    bubble.type = Main.rand.NextBool(3) ? 412 : 411;
                }
            }
        }
    }

    public override void OnKill(int timeLeft)
    {
        for (int a = 0; a < 30; a++)
        {
            Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.WaterCandle, 0f, 0f, 150, default, 1.1f);
        }

        if (this.RunLocal())
        {
            Projectile.penetrate = -1;
            Projectile.ExpandHitboxBy(24);
            Projectile.Damage();
        }
    }
}
