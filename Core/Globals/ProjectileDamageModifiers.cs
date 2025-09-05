using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Core.Globals;

public class ProjectileDamageModifiers : GlobalProjectile
{
    public override bool InstancePerEntity => true;

    public int flatDRTimer;
    public int flatDR;

    /// <summary>
    /// Assumes the projectile was hostile in the first place
    /// </summary>
    public int parriedTimer;

    public override void PostAI(Projectile projectile)
    {
        if (projectile.FinalExtraUpdate())
        {
            if (parriedTimer > 0)
            {
                projectile.damage = projectile.originalDamage * 2;
                projectile.friendly = true;
                projectile.hostile = false;
                parriedTimer--;
                if (parriedTimer <= 0)
                {
                    projectile.damage = projectile.originalDamage;
                    projectile.friendly = false;
                    projectile.hostile = true;
                }
            }

            if (flatDRTimer > 0)
            {
                flatDRTimer--;
                if (flatDRTimer <= 0)
                    flatDR = 0;
            }
        }
    }

    public override bool? CanDamage(Projectile projectile)
    {
        if (projectile.hostile && projectile.damage - flatDR <= 0)
        {
            return false;
        }
        return null;
    }

    public override void ModifyHitPlayer(Projectile projectile, Player target, ref Player.HurtModifiers modifiers)
    {
        modifiers.FinalDamage.Flat -= flatDR;
    }
}
