using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;
using static System.Net.Mime.MediaTypeNames;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Late;

public class CallerOfBirdsCall : BaseHoldoutProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CallerOfBirds);
    public override void Defaults()
    {
        Projectile.Size = new(52, 42);
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 30;
        Projectile.ownerHitCheck = true;
        Projectile.DamageType = DamageClass.Melee;
    }
    public ref float Time => ref Projectile.ai[0];
    public override void SafeAI()
    {
        if (this.RunLocal())
        {
            Projectile.velocity = Center.SafeDirectionTo(Mouse);
            if (Projectile.velocity != Projectile.oldVelocity)
                Projectile.netUpdate = true;
        }
        Projectile.rotation = -MathHelper.PiOver4;
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver4);
        Owner.SetBackHandBetter(0, Projectile.velocity.ToRotation());
        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
        Projectile.Center = Owner.GetFrontHandPositionImproved() + Vector2.UnitY * Animators.MakePoly(4f).InFunction.Evaluate(Time, 0f, 20f, 0f, -30f);

        if (this.RunLocal() && Time % 4 == 3)
        {
            Vector2 target = Owner.Additions().mouseWorld;
            for (int i = 0; i < 2; i++)
            {
                Vector2 position = Owner.Center - new Vector2(Main.rand.NextFloat(401) * Owner.direction, 800f);
                position.Y -= 200 * i;
                Vector2 vel = position.SafeDirectionTo(target);

                if (vel.Y < 0f)
                    vel.Y *= -1f;
                
                vel *= Item.shootSpeed;
                Projectile.NewProj(position, vel, ModContent.ProjectileType<Pigeon>(), Projectile.damage / 3, 0f, Owner.whoAmI);
            }
            
        }
        if (Time % 16 == 15)
            SoundID.Zombie11.Play(Projectile.Center + Main.rand.NextVector2Circular(40f, 40f), .5f, 0f, .4f, null, 50, Name);

        Time++;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Owner.wingTime += 30;
        Owner.wingRunAccelerationMult += 20;
        Owner.moveSpeed += 1;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        float comp = InverseLerp(0f, 20f, Time);
        Projectile.DrawProjectileBackglow(Color.Tan * comp, 3f * comp, 4, 3);
        Projectile.DrawBaseProjectile(Color.White);
        return false;
    }
}
