using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Core.Systems;

public class OverrideHooksManager
{
    internal static MethodInfo PreAIMethod => typeof(ProjectileLoader).GetMethod("PreAI", UniversalBindingFlags);
    public delegate bool Orig_PreAIDelegate(Projectile projectile);

    internal static MethodInfo PreDrawMethod => typeof(ProjectileLoader).GetMethod("PreDraw", UniversalBindingFlags);
    public delegate bool Orig_PreDrawDelegate(Projectile projectile, ref Color lightColor);

    internal static MethodInfo SetDefaultsMethod => typeof(ProjectileLoader).GetMethod("SetDefaults", UniversalBindingFlags);
    public delegate void Orig_SetDefaultsDelegate(Projectile projectile, bool createModProjectile = true);

    internal static MethodInfo OnKillMethod => typeof(ProjectileLoader).GetMethod("OnKill", UniversalBindingFlags);
    public delegate void Orig_OnKillDelegate(Projectile projectile, int timeLeft);

    internal static MethodInfo OnHitNPCMethod => typeof(ProjectileLoader).GetMethod("OnHitNPC", UniversalBindingFlags);
    public delegate void Orig_OnHitNPCDelegate(Projectile projectile, NPC target, in NPC.HitInfo hit, int damageDone);

    internal static MethodInfo ModifyHitNPCMethod => typeof(ProjectileLoader).GetMethod("ModifyHitNPC", UniversalBindingFlags);
    public delegate void Orig_ModifyHitNPCDelegate(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers);

    internal static MethodInfo CanDamageMethod => typeof(ProjectileLoader).GetMethod("CanDamage", UniversalBindingFlags);
    public delegate bool? Orig_CanDamageDelegate(Projectile projectile);
}

public class ClearOverridenProjectileMethods : ModSystem
{
    public override void Load()
    {
        On_Projectile.Kill += KillDetourMethod;
    }

    public override void Unload()
    {
        On_Projectile.Kill -= KillDetourMethod;
    }

    public static void KillDetourMethod(On_Projectile.orig_Kill orig, Projectile self)
    {
        if (self.ModProjectile == null && self.type < ProjectileID.Count)
        {
            if (!self.active)
            {
                return;
            }

            ProjectileOverride behaviorOverride = ProjectileOverride.BehaviorOverrideSet[self.type];
            if (behaviorOverride is not null)
            {
                behaviorOverride.OnKill(self);
                self.timeLeft = 0;
                self.active = false;
                return;
            }
        }

        orig(self);
    }
}

public class OverrideSystemHooks : ModSystem
{
    public override void Load()
    {
        MonoModHooks.Add(OverrideHooksManager.PreAIMethod, OverrideSystemHooks.PreAIDetourMethod);
        MonoModHooks.Add(OverrideHooksManager.PreDrawMethod, OverrideSystemHooks.PreDrawDetourMethod);
        MonoModHooks.Add(OverrideHooksManager.SetDefaultsMethod, OverrideSystemHooks.SetDefaultsDetourMethod);
        MonoModHooks.Add(OverrideHooksManager.OnKillMethod, OverrideSystemHooks.OnKillDetourMethod);
        MonoModHooks.Add(OverrideHooksManager.OnHitNPCMethod, OverrideSystemHooks.OnHitNPCDetourMethod);
        MonoModHooks.Add(OverrideHooksManager.ModifyHitNPCMethod, OverrideSystemHooks.ModifyHitNPCDetourMethod);
        MonoModHooks.Add(OverrideHooksManager.CanDamageMethod, OverrideSystemHooks.CanDamageDetourMethod);
    }

    internal static bool PreAIDetourMethod(OverrideHooksManager.Orig_PreAIDelegate orig, Projectile projectile)
    {
        if (projectile.ModProjectile == null && projectile.type < ProjectileID.Count)
        {
            ProjectileOverride container = ProjectileOverride.BehaviorOverrideSet[projectile.type] ?? null;

            if (container is not null)
            {
                return container.PreAI(projectile);
            }
        }

        return orig(projectile);
    }

    internal static bool PreDrawDetourMethod(OverrideHooksManager.Orig_PreDrawDelegate orig, Projectile projectile, ref Color lightColor)
    {
        if (projectile.ModProjectile == null && projectile.type < ProjectileID.Count)
        {
            ProjectileOverride container = ProjectileOverride.BehaviorOverrideSet[projectile.type] ?? null;

            if (container is not null)
            {
                return container.PreDraw(projectile, Main.spriteBatch, ref lightColor);
            }
        }

        return orig(projectile, ref lightColor);
    }

    internal static void SetDefaultsDetourMethod(OverrideHooksManager.Orig_SetDefaultsDelegate orig, Projectile projectile, bool createModProjectile = true)
    {
        if (projectile.ModProjectile == null && projectile.type < ProjectileID.Count && !Main.gameMenu)
        {
            ProjectileOverride container = ProjectileOverride.BehaviorOverrideSet[projectile.type] ?? null;

            if (container is not null)
            {
                container.SetDefaults(projectile);
                return;
            }
        }

        orig(projectile, createModProjectile);
    }

    internal static void OnKillDetourMethod(OverrideHooksManager.Orig_OnKillDelegate orig, Projectile projectile, int timeleft)
    {
        if (projectile.ModProjectile == null && projectile.type < ProjectileID.Count)
        {
            ProjectileOverride container = ProjectileOverride.BehaviorOverrideSet[projectile.type];

            if (container is not null)
            {
                return;
            }
        }

        orig(projectile, timeleft);
    }

    internal static void OnHitNPCDetourMethod(OverrideHooksManager.Orig_OnHitNPCDelegate orig, Projectile projectile, NPC target, in NPC.HitInfo hit, int damageDone)
    {
        if (projectile.ModProjectile == null && projectile.type < ProjectileID.Count)
        {
            ProjectileOverride container = ProjectileOverride.BehaviorOverrideSet[projectile.type];

            if (container is not null)
            {
                container.OnHitNPC(projectile, target, hit, damageDone);
                return;
            }
        }

        orig(projectile, target, hit, damageDone);
    }

    internal static void ModifyHitNPCDetourMethod(OverrideHooksManager.Orig_ModifyHitNPCDelegate orig, Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
    {
        if (projectile.ModProjectile == null && projectile.type < ProjectileID.Count)
        {
            ProjectileOverride container = ProjectileOverride.BehaviorOverrideSet[projectile.type];

            if (container is not null)
            {
                container.ModifyHitNPC(projectile, target, ref modifiers);
                return;
            }
        }

        orig(projectile, target, ref modifiers);
    }

    internal static bool? CanDamageDetourMethod(OverrideHooksManager.Orig_CanDamageDelegate orig, Projectile projectile)
    {
        if (projectile.ModProjectile == null && projectile.type < ProjectileID.Count)
        {
            ProjectileOverride container = ProjectileOverride.BehaviorOverrideSet[projectile.type];

            if (container is not null)
            {
                return container.CanDamage(projectile);
            }
        }

        return orig(projectile);
    }
}
