using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.ILEditing;
using TheExtraordinaryAdditions.Core.Utilities;
using static MonoMod.Cil.ILContext;

namespace TheExtraordinaryAdditions.Core.Systems;

#nullable enable

// TODO: Unsure the need of this, compare with As<T> on multi/single player
public class ProjectileSpawnManagementSystem : ModSystem
{
    private static Action<Projectile>? preSyncAction;

    public static void PrepareProjectileForSpawning(Action<Projectile> a)
    {
        if (preSyncAction is null)
            preSyncAction = a;
        else
            preSyncAction += a;
    }

    public override void OnModLoad()
    {
        new ManagedILEdit("Inherent Custom Projectile Data Spawn Syncing", edit =>
        {
            IL_Projectile.NewProjectile_IEntitySource_float_float_float_float_int_int_float_int_float_float_float += edit.SubscriptionWrapper;
        }, edit =>
        {
            IL_Projectile.NewProjectile_IEntitySource_float_float_float_float_int_int_float_int_float_float_float -= edit.SubscriptionWrapper;
        }, PreSyncProjectileStuff).Apply();
    }

    private void PreSyncProjectileStuff(ILContext context, ManagedILEdit edit)
    {
        ILCursor cursor = new ILCursor(context);

        // Go after the projectile instantiation phase and find the local index of the spawned projectile.
        int projectileILIndex = 0;
        if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchStfld<Projectile>("stepSpeed")))
        {
            edit.LogFailure("The projectile.stepSpeed storage could not be found.");
            return;
        }

        int placeToSetAction = cursor.Index;
        if (!cursor.TryGotoPrev(i => i.MatchLdloc(out projectileILIndex)))
        {
            edit.LogFailure("The projectile's local IL index could not be found.");
            return;
        }

        cursor.Goto(placeToSetAction);
        cursor.Emit(OpCodes.Ldloc, projectileILIndex);
        cursor.EmitDelegate<Action<Projectile>>(projectile =>
        {
            // Invoke the pre-sync action and then destroy it, to ensure that the action doesn't bleed into successive, unrelated projectile spawn calls.
            preSyncAction?.Invoke(projectile);
            preSyncAction = null;
        });
    }
}