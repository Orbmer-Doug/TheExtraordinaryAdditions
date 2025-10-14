using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using TheExtraordinaryAdditions.Core;
using TheExtraordinaryAdditions.Core.Config;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Base;

public abstract class BaseIdleHoldoutProjectile : BaseHoldoutProjectile
{
    public override bool SetItemTime => false;
    private static Dictionary<int, int> itemProjectileRelationship = [];
    public abstract int AssociatedItemID { get; }
    public abstract int IntendedProjectileType { get; }
    public static Dictionary<int, int> ItemProjectileRelationship
    {
        get => itemProjectileRelationship;
        set => itemProjectileRelationship = value ?? new();
    }

    // Unsure if reflection is best practice but considering 50+ derived subclasses... convenience?
    public static void LoadAll()
    {
        ItemProjectileRelationship = new Dictionary<int, int>();
        Type[] types = AssemblyManager.GetLoadableTypes(AdditionsMain.Instance.Code);
        foreach (Type type in types)
        {
            if (type.IsAbstract)
                continue;

            if (type.IsSubclassOf(typeof(BaseIdleHoldoutProjectile)))
            {
                BaseIdleHoldoutProjectile instance = Activator.CreateInstance(type) as BaseIdleHoldoutProjectile;
                if (instance.AssociatedItemID < ItemID.Count && !AdditionsConfigServer.Instance.UseCustomAI)
                    continue;

                ItemProjectileRelationship[instance.AssociatedItemID] = instance.IntendedProjectileType;
            }
        }
    }

    public override bool ShouldDie()
    {
        return !Owner.Available() || Item.type != AssociatedItemID;
    }
}

public class GlobalIdleHoldoutItem : GlobalItem
{
    public override void SetDefaults(Item entity)
    {
        if (BaseIdleHoldoutProjectile.ItemProjectileRelationship.ContainsKey(entity.type))
        {
            entity.noMelee = true;
            entity.noUseGraphic = true;
            entity.UseSound = null;
        }
    }

    public override bool CanShoot(Item item, Player player)
    {
        if (BaseIdleHoldoutProjectile.ItemProjectileRelationship.ContainsKey(item.type))
            return false;

        return base.CanShoot(item, player);
    }
}

public class GlobalIdleHoldoutPlayer : ModPlayer
{
    public override void PostUpdateEquips()
    {
        foreach (int itemID in BaseIdleHoldoutProjectile.ItemProjectileRelationship.Keys)
        {
            Item heldItem = Player.HeldItem;
            if (heldItem.type != itemID)
                continue;

            int holdoutType = BaseIdleHoldoutProjectile.ItemProjectileRelationship[itemID];
            if (Main.myPlayer == Player.whoAmI && Player.CountOwnerProjectiles(holdoutType) <= 0)
            {
                int damage = Player.GetWeaponDamage(heldItem, false);
                float kb = Player.GetWeaponKnockback(heldItem, heldItem.knockBack);
                Projectile p = Main.projectile[Projectile.NewProjectile(Player.GetSource_ItemUse(heldItem, null), Player.Center,
                    Vector2.Zero, holdoutType, damage, kb, Player.whoAmI, 0f, 0f, 0f)];
            }
        }
    }
}