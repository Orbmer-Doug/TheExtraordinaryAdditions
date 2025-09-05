using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core;
using TheExtraordinaryAdditions.Core.Config;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Base;

public abstract class BaseIdleHoldoutProjectile : BaseHoldoutProjectile
{
    public override bool SetItemTime => false;
    private static Dictionary<int, int> itemProjectileRelationship = [];
    public abstract int AssociatedItemID { get; }
    public abstract int IntendedProjectileType { get; }
    public static Dictionary<int, int> ItemProjectileRelationship { get => itemProjectileRelationship; set => itemProjectileRelationship = value; }
    public static void LoadAll()
    {
        ItemProjectileRelationship = [];
        Type[] types = typeof(AdditionsMain).Assembly.GetTypes();
        foreach (Type type in types)
        {
            if (!type.IsAbstract && type.IsSubclassOf(typeof(BaseIdleHoldoutProjectile)))
            {
                if ((Activator.CreateInstance(type) as Item)?.ModItem == null && !AdditionsConfigServer.Instance.UseCustomAI)
                    continue;

                BaseIdleHoldoutProjectile instance = Activator.CreateInstance(type) as BaseIdleHoldoutProjectile;
                ItemProjectileRelationship[instance.AssociatedItemID] = instance.IntendedProjectileType;
            }
        }
    }

    public static void CheckForEveryHoldout(Player player)
    {
        foreach (int itemID in ItemProjectileRelationship.Keys)
        {
            Item heldItem = player.HeldItem;
            if (heldItem.type != itemID)
                continue;

            int holdoutType = ItemProjectileRelationship[itemID];
            if (Main.myPlayer == player.whoAmI && player.CountOwnerProjectiles(holdoutType) < 1)
            {
                int damage = player.GetWeaponDamage(heldItem, false);
                float kb = player.GetWeaponKnockback(heldItem, heldItem.knockBack);
                Projectile p = Main.projectile[Projectile.NewProjectile(player.GetSource_ItemUse(heldItem, null), player.Center,
                    Vector2.Zero, holdoutType, damage, kb, player.whoAmI, 0f, 0f, 0f)];
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