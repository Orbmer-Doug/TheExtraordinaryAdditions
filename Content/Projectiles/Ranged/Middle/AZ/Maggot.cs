
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle.AZ;

public class Maggot : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Maggot);
    public override void SetDefaults()
    {
        Projectile.height = 22;
        Projectile.width = 34;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 300;
    }
    public ref float Time => ref Projectile.ai[0];
    private bool Homing;
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(Homing);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Homing = reader.ReadBoolean();
    }
    public Player Owner => Main.player[Projectile.owner];
    public override void AI()
    {
        Time++;
        Color col = Color.Transparent;
        if (Owner.team == (int)Team.None)
        {
            col = Color.Green;
        }
        if (Owner.team == (int)Team.Red)
        {
            col = Color.Red;
        }
        if (Owner.team == (int)Team.Green || Main.netMode == NetmodeID.SinglePlayer)
        {
            col = Color.LimeGreen;
        }
        if (Owner.team == (int)Team.Blue)
        {
            col = Color.Blue;
        }
        if (Owner.team == (int)Team.Yellow)
        {
            col = Color.Gold;
        }
        if (Owner.team == (int)Team.Pink)
        {
            col = Color.Pink;
        }

        Color color = Homing ? col : Color.SlateGray;

        Projectile.rotation = Projectile.velocity.ToRotation();

        if (Time % 2f == 1f)
        {
            ParticleRegistry.SpawnMistParticle(Projectile.Center, -Projectile.velocity.RotatedByRandom(.25f) * Main.rand.NextFloat(.2f, .5f),
                Main.rand.NextFloat(.5f, .8f), color, col * .1f, 190);
        }
        if (Time > SecondsToFrames(2))
        {
            Player player = Main.player[Player.FindClosest(Projectile.Center, Projectile.width, Projectile.height)];
            if (CombinedHooks.CanHitPvpWithProj(Projectile, player) && player != Owner && player != null && PlayerLoader.CanHitPvpWithProj(Projectile, player))
            {
                if (player.active)
                {
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(player.Center) * 15f, .05f);
                }
                Homing = true;
            }
            else
            {
                if (NPCTargeting.TryGetClosestNPC(new(Projectile.Center, 1000, true, false), out NPC target))
                {
                    Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Projectile.SafeDirectionTo(target.Center) * 15f, .05f);
                }
            }
        }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
        Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        SpriteEffects effects = 0;

        Color col = Color.Transparent;
        if (Owner.team == (int)Team.None)
        {
            col = Color.Green;
        }
        if (Owner.team == (int)Team.Red)
        {
            col = Color.Red;
        }
        if (Owner.team == (int)Team.Green || Main.netMode == NetmodeID.SinglePlayer)
        {
            col = Color.LimeGreen;
        }
        if (Owner.team == (int)Team.Blue)
        {
            col = Color.Blue;
        }
        if (Owner.team == (int)Team.Yellow)
        {
            col = Color.Gold;
        }
        if (Owner.team == (int)Team.Pink)
        {
            col = Color.Pink;
        }

        Main.EntitySpriteDraw(texture, drawPosition, frame, Projectile.GetAlpha(col), Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, effects, 0);
        return false;
    }
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > float.Epsilon)
        {
            Projectile.velocity.X = -oldVelocity.X;
        }
        if (Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > float.Epsilon)
        {
            Projectile.velocity.Y = -oldVelocity.Y;
        }
        return false;
    }
}
