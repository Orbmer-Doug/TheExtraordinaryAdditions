using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Early;

public class SnareGas : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width = 34;
        Projectile.height = 36;
        Projectile.friendly = true;
        Projectile.ignoreWater = false;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 600;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 10;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Projectile.damage = (int)(Projectile.damage * 0.9);
    }

    public override void AI()
    {
        Projectile.velocity *= .975f;

        Projectile.rotation += Projectile.direction * .05f;

        Projectile.alpha++;
        if (Projectile.alpha >= 255)
        {
            Projectile.Kill();
        }
    }

    public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
    {
        Projectile.velocity *= .9f;
        return true;
    }

    public readonly string Gas1 = ProjectileID.SporeGas.GetTerrariaProj();
    public readonly string Gas2 = ProjectileID.SporeGas2.GetTerrariaProj();
    public readonly string Gas3 = ProjectileID.SporeGas3.GetTerrariaProj();

    public ref float Typ => ref Projectile.ai[0];
    public override void OnSpawn(IEntitySource source)
    {
        Typ = Main.rand.Next(0, 3);
        Projectile.netUpdate = true;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        string str = "";
        if (Typ == 0)
            str = Gas1;
        if (Typ == 1) 
            str = Gas2;
        if (Typ == 2) 
            str = Gas3;

        Texture2D tex = ModContent.Request<Texture2D>(str).Value;
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Main.EntitySpriteDraw(tex, drawPosition, null, Projectile.GetAlpha(Color.White), Projectile.rotation, tex.Size() * 0.5f, Projectile.scale, 0, 0);
        return false;
    }
}