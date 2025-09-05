using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;

public class GreatJesterArrow : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.GreatJesterArrow);
    public override void SetStaticDefaults()
    {
        // DisplayName.SetDefault("Lightning");
    }
    public override void SetDefaults()
    {
        DamageClass ranged = DamageClass.Ranged;
        Projectile.width = 32; Projectile.height = 16;
        Projectile.aiStyle = 0;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 100;
        Projectile.light = 1f;
        Projectile.ignoreWater = false;
        Projectile.tileCollide = true;
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Player owner = Main.player[Projectile.owner];
        int rand = Main.rand.Next(2); //Generates an integer from 0 to 1
        if (rand == 0)
        {
            target.AddBuff(80, 180);
        }
        Projectile.damage = (int)(Projectile.damage * 0.9f);
        int dust = Dust.NewDust(Projectile.Center, 2, 0, DustID.MagicMirror, 0f, 0f, 0, default, .5f);
        Main.dust[dust].noGravity = true;
        Main.dust[dust].velocity *= 10.9f;
        Main.dust[dust].scale = Main.rand.Next(100, 135) * 0.014f;

    }
    public override void AI()
    {
        int dust = Dust.NewDust(Projectile.Center, 2, 0, DustID.MagicMirror, 0f, 0f, 0, default, .5f);
        Main.dust[dust].noGravity = true;
        Main.dust[dust].velocity *= 0.9f;
        Main.dust[dust].scale = Main.rand.Next(100, 135) * 0.014f;

        int dust2 = Dust.NewDust(Projectile.Center, 2, 0, DustID.Enchanted_Gold, 0f, 0f, 0, default, .5f);
        Main.dust[dust2].noGravity = true;
        Main.dust[dust2].velocity *= 0.9f;
        Main.dust[dust2].scale = Main.rand.Next(100, 135) * 0.014f;

        int dust3 = Dust.NewDust(Projectile.Center, 2, 0, DustID.Enchanted_Pink, 0f, 0f, 0, default, .5f);
        Main.dust[dust3].noGravity = true;
        Main.dust[dust3].velocity *= 0.9f;
        Main.dust[dust3].scale = Main.rand.Next(100, 135) * 0.014f;

        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2 - MathHelper.PiOver4 * Projectile.spriteDirection - MathHelper.ToRadians(45f);
    }
    public override void OnKill(int timeLeft)
    {
        // This code and the similar code above in OnTileCollide spawn dust from the tiles collided with. SoundID.Item10 is the bounce sound you hear.
        Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
        SoundEngine.PlaySound(SoundID.Dig, Projectile.position);

        for (int i = 0; i < 10; i++)
        {
            Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.MagicMirror, 0f, 0f, 300, default, 15f);
            dust.noGravity = true;
            dust.velocity *= 10f;
            dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.MagicMirror, 0f, 0f, 100, default, 2f);
            dust.velocity *= 3f;
        }
    }

}