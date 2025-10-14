using CalamityMod;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Multi.Middle;

public class SplinteredBone : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Bone;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.hostile = false;
        Projectile.friendly = true;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = false;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.penetrate = 2;
        Projectile.extraUpdates = 0;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 20;
        Projectile.timeLeft = 300;
    }

    private ref float Time => ref Projectile.ai[1];
    private ref float State => ref Projectile.ai[2];
    private const int Fall = 25;
    public override bool? CanHitNPC(NPC target)
    {
        if (Time > Fall)
            return null;

        return false;
    }

    public override void AI()
    {
        after ??= new(4, () => Projectile.Center);
        after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0, 255));

        if (State == 0f)
        {
            Projectile.VelocityBasedRotation();
            if (Time > Fall && Projectile.velocity.Y < 16f)
                Projectile.velocity.Y += .2f;
        }
        if (State == 1f)
        {
            Projectile.velocity = Vector2.Zero;
        }

        Projectile.StickyProjAI(15);
        Time++;
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        State = 1f;
        Projectile.timeLeft = 120;
        return false;
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        Projectile.ModifyHitNPCSticky(20);
    }

    public override void OnKill(int timeLeft)
    {
        for (int i = 0; i < 12; i++)
        {
            int dust = Dust.NewDust(Projectile.Center, 4, 4, DustID.Bone, 0f, 0f, 0, default);
            Main.dust[dust].noGravity = true;
            Main.dust[dust].velocity *= Main.rand.NextVector2CircularEdge(3f, 3f);
            Main.dust[dust].scale *= 1.21f;
        }
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        after?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [lightColor], Projectile.Opacity);
        Projectile.DrawBaseProjectile(lightColor);
        return false;
    }
}
