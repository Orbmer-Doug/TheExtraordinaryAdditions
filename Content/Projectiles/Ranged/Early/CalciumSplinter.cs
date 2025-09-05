using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Early;

public class CalciumSplinter : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CalciumSplinter);
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 14;
        Projectile.friendly = true;
        Projectile.penetrate = 3;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.extraUpdates = 0;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 20;
        Projectile.tileCollide = true;
    }

    public ref float State => ref Projectile.ai[0];
    public bool HitGround
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }

    public FancyAfterimages after;
    public override void AI()
    {
        after ??= new(4, () => Projectile.Center);
        after.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0, 240, 0, 0f, null, true, -.4f));

        if (State == 0f)
        {
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + .2f, -22f, 22f);
            Projectile.FacingRight();
        }

        Projectile.StickyProjAI(15);
        if (State == 2f)
        {
            Projectile.velocity *= 0f;
        }
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        Projectile.ModifyHitNPCSticky(20);
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (!HitGround)
        {
            Projectile.timeLeft = 300;
            Collision.HitTiles(Projectile.Center, -oldVelocity, Projectile.width, Projectile.height);
            HitGround = true;
        }
        State = 2f;
        return false;
    }

    public override void OnKill(int timeLeft)
    {
        for (int g = 0; g < 11; g++)
        {
            int dust = Dust.NewDust(Projectile.Center, 4, 4, DustID.Bone, 0f, 0f, 100, default);
            Main.dust[dust].noGravity = true;
            Main.dust[dust].velocity *= Main.rand.NextVector2CircularEdge(3f, 3f);
            Main.dust[dust].scale *= 1.21f;
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        after?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [lightColor]);
        Projectile.DrawBaseProjectile(lightColor);
        return false;
    }
}
