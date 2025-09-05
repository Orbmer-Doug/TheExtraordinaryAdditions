using MonoMod.Cil;
using System;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.ILEditing;
using TheExtraordinaryAdditions.Core.Interfaces;
using static TheExtraordinaryAdditions.Core.Systems.OverrideHooksManager;

namespace TheExtraordinaryAdditions.Core.Systems;

// Pretty much any method that isn't a boolean

public class OverrideHooksManager
{
    public static event ILContext.Manipulator ModifyPreAI
    {
        add => HookHelper.ModifyMethodWithIL(typeof(ProjectileLoader).GetMethod("PreAI", UniversalBindingFlags), value);
        remove => HookHelper.ILEventRemove();
    }
    internal static MethodInfo PreAIMethod => typeof(ProjectileLoader).GetMethod("PreAI", UniversalBindingFlags);
    public delegate bool Orig_PreAIDelegate(Projectile projectile);


    public static event ILContext.Manipulator ModifyPreDraw
    {
        add => HookHelper.ModifyMethodWithIL(typeof(ProjectileLoader).GetMethod("PreDraw", UniversalBindingFlags), value);
        remove => HookHelper.ILEventRemove();
    }
    internal static MethodInfo PreDrawMethod => typeof(ProjectileLoader).GetMethod("PreDraw", UniversalBindingFlags);
    public delegate bool Orig_PreDrawDelegate(Projectile projectile, ref Color lightColor);


    public static event ILContext.Manipulator ModifySetDefaults
    {
        add => HookHelper.ModifyMethodWithIL(typeof(ProjectileLoader).GetMethod("SetDefaults", UniversalBindingFlags), value);
        remove => HookHelper.ILEventRemove();
    }
    internal static MethodInfo SetDefaultsMethod => typeof(ProjectileLoader).GetMethod("SetDefaults", UniversalBindingFlags);
    public delegate void Orig_SetDefaultsDelegate(Projectile projectile, bool createModProjectile = true);


    public static event ILContext.Manipulator ModifyOnKill
    {
        add => HookHelper.ModifyMethodWithIL(typeof(ProjectileLoader).GetMethod("OnKill", UniversalBindingFlags), value);
        remove => HookHelper.ILEventRemove();
    }
    internal static MethodInfo OnKillMethod => typeof(ProjectileLoader).GetMethod("OnKill", UniversalBindingFlags);
    public delegate void Orig_OnKillDelegate(Projectile projectile, int timeLeft);


    public static event ILContext.Manipulator ModifyOnHitNPC
    {
        add => HookHelper.ModifyMethodWithIL(typeof(ProjectileLoader).GetMethod("OnHitNPC", UniversalBindingFlags), value);
        remove => HookHelper.ILEventRemove();
    }
    internal static MethodInfo OnHitNPCMethod => typeof(ProjectileLoader).GetMethod("OnHitNPC", UniversalBindingFlags);
    public delegate void Orig_OnHitNPCDelegate(Projectile projectile, NPC target, in NPC.HitInfo hit, int damageDone);


    public static event ILContext.Manipulator ChangeModifyHitNPC
    {
        add => HookHelper.ModifyMethodWithIL(typeof(ProjectileLoader).GetMethod("ModifyHitNPC", UniversalBindingFlags), value);
        remove => HookHelper.ILEventRemove();
    }
    internal static MethodInfo ModifyHitNPCMethod => typeof(ProjectileLoader).GetMethod("ModifyHitNPC", UniversalBindingFlags);
    public delegate void Orig_ModifyHitNPCDelegate(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers);


    public static event ILContext.Manipulator ModifyCanDamage
    {
        add => HookHelper.ModifyMethodWithIL(typeof(ProjectileLoader).GetMethod("CanDamage", UniversalBindingFlags), value);
        remove => HookHelper.ILEventRemove();
    }
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

public class OverrideSystemHooks : ICustomDetourProvider
{
    void ICustomDetourProvider.ModifyMethods()
    {
        HookHelper.ModifyMethodWithDetour(OverrideHooksManager.PreAIMethod, OverrideSystemHooks.PreAIDetourMethod);
        HookHelper.ModifyMethodWithDetour(OverrideHooksManager.PreDrawMethod, OverrideSystemHooks.PreDrawDetourMethod);
        HookHelper.ModifyMethodWithDetour(OverrideHooksManager.SetDefaultsMethod, OverrideSystemHooks.SetDefaultsDetourMethod);
        HookHelper.ModifyMethodWithDetour(OverrideHooksManager.OnKillMethod, OverrideSystemHooks.OnKillDetourMethod);
        HookHelper.ModifyMethodWithDetour(OverrideHooksManager.OnHitNPCMethod, OverrideSystemHooks.OnHitNPCDetourMethod);
        HookHelper.ModifyMethodWithDetour(OverrideHooksManager.ModifyHitNPCMethod, OverrideSystemHooks.ModifyHitNPCDetourMethod);
        HookHelper.ModifyMethodWithDetour(OverrideHooksManager.CanDamageMethod, OverrideSystemHooks.CanDamageDetourMethod);
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
