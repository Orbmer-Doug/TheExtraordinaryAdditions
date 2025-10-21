using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Core.Systems;


public class OverrideHooksManager
{
    internal static MethodInfo SetDefaultsMethod => typeof(ProjectileLoader).GetMethod("SetDefaults", UniversalBindingFlags);
    public delegate void Orig_SetDefaultsDelegate(Projectile projectile, bool createModProjectile = true);

    internal static MethodInfo OnHitNPCMethod => typeof(ProjectileLoader).GetMethod("OnHitNPC", UniversalBindingFlags);
    public delegate void Orig_OnHitNPCDelegate(Projectile projectile, NPC target, in NPC.HitInfo hit, int damageDone);

    internal static MethodInfo ModifyHitNPCMethod => typeof(ProjectileLoader).GetMethod("ModifyHitNPC", UniversalBindingFlags);
    public delegate void Orig_ModifyHitNPCDelegate(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers);
}

public class OverrideSystemHooks : ModSystem
{
    public override void Load()
    {
        MonoModHooks.Add(OverrideHooksManager.SetDefaultsMethod, OverrideSystemHooks.SetDefaultsDetourMethod);
        MonoModHooks.Add(OverrideHooksManager.OnHitNPCMethod, OverrideSystemHooks.OnHitNPCDetourMethod);
        MonoModHooks.Add(OverrideHooksManager.ModifyHitNPCMethod, OverrideSystemHooks.ModifyHitNPCDetourMethod);
    }

    internal static void SetDefaultsDetourMethod(OverrideHooksManager.Orig_SetDefaultsDelegate orig, Projectile projectile, bool createModProjectile = true)
    {
        orig(projectile, createModProjectile);
        if (projectile.ModProjectile == null && ProjectileOverride.BehaviorOverrideSet.TryGetValue(projectile.type, out ProjectileOverride container))
            container.SetDefaults(projectile);
    }

    internal static void OnHitNPCDetourMethod(OverrideHooksManager.Orig_OnHitNPCDelegate orig, Projectile projectile, NPC target, in NPC.HitInfo hit, int damageDone)
    {
        if (ProjectileOverride.BehaviorOverrideSet.TryGetValue(projectile.type, out ProjectileOverride container))
        {
            container.OnHitNPC(projectile, target, hit, damageDone);
            projectile.netUpdate = true;
            return;
        }

        orig(projectile, target, hit, damageDone);
    }

    internal static void ModifyHitNPCDetourMethod(OverrideHooksManager.Orig_ModifyHitNPCDelegate orig, Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
    {
        if (ProjectileOverride.BehaviorOverrideSet.TryGetValue(projectile.type, out ProjectileOverride container))
        {
            container.ModifyHitNPC(projectile, target, ref modifiers);
            return;
        }

        orig(projectile, target, ref modifiers);
    }
}

public sealed class GlobalProjectileOverride : GlobalProjectile
{
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
    {
        return lateInstantiation && entity.type < ProjectileID.Count;
    }

    public override bool? CanDamage(Projectile projectile)
    {
        if (ProjectileOverride.BehaviorOverrideSet.TryGetValue(projectile.type, out ProjectileOverride container))
            return container.CanDamage(projectile);

        return base.CanDamage(projectile);
    }

    public override bool PreAI(Projectile projectile)
    {
        if (ProjectileOverride.BehaviorOverrideSet.TryGetValue(projectile.type, out ProjectileOverride container))
            return container.PreAI(projectile);

        return base.PreAI(projectile);
    }

    public override bool PreKill(Projectile projectile, int timeLeft)
    {
        if (ProjectileOverride.BehaviorOverrideSet.TryGetValue(projectile.type, out ProjectileOverride container))
        {
            container.OnKill(projectile);
            projectile.active = false;
            projectile.netUpdate = true;
            return false;
        }

        return base.PreKill(projectile, timeLeft);
    }

    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        if (ProjectileOverride.BehaviorOverrideSet.TryGetValue(projectile.type, out ProjectileOverride container))
            return container.PreDraw(projectile, Main.spriteBatch, ref lightColor);

        return base.PreDraw(projectile, ref lightColor);
    }
}