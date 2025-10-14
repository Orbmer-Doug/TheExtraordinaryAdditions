using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;

namespace TheExtraordinaryAdditions.Content.Projectiles;

public class qwer : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 500000;
    }
    public override void AI()
    {
        Player p = Main.LocalPlayer;
        Projectile.timeLeft = 200;
        if (p.Additions().MouseMiddle.Current)
            Projectile.Kill();
        points.Update(Projectile.Center);
    }
    public override void PostAI()
    {

    }

    public TrailPoints points = new(20);
    public override bool PreDraw(ref Color lightColor)
    {
        return false;
    }
}
