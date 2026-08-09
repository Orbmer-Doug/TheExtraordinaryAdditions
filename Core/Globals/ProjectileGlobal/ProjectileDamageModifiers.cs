using Terraria;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Core.Globals.ProjectileGlobal;

public class ProjectileDamageModifiers : GlobalProjectile
{
    public override bool InstancePerEntity => true;

    public int FlatDRTimer;
    public int FlatDR;

    /// <summary>
    /// Assumes the projectile was hostile in the first place
    /// </summary>
    public int ParriedTimer;

    public override void PostAI(Projectile projectile)
    {
        if (projectile.FinalExtraUpdate())
        {
            if (ParriedTimer > 0)
            {
                projectile.damage = projectile.originalDamage * 2;
                projectile.friendly = true;
                projectile.hostile = false;
                ParriedTimer--;
                if (ParriedTimer <= 0)
                {
                    projectile.damage = projectile.originalDamage;
                    projectile.friendly = false;
                    projectile.hostile = true;
                }
            }

            if (FlatDRTimer > 0)
            {
                FlatDRTimer--;
                if (FlatDRTimer <= 0)
                    FlatDR = 0;
            }
        }
    }

    public override bool? CanDamage(Projectile projectile)
    {
        if (projectile.hostile && projectile.damage - FlatDR <= 0)
            return false;
        return null;
    }

    public override void ModifyHitPlayer(Projectile projectile, Player target, ref Player.HurtModifiers modifiers)
    {
        modifiers.FinalDamage.Flat -= FlatDR;
    }
}
