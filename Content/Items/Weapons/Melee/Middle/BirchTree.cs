using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Early;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;

public class BirchTree : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.BirchTree);

    public override void SetDefaults()
    {
        Item.width = Item.height = 58;
        Item.value = AdditionsGlobalItem.RarityCyanBuyPrice;
        Item.rare = ItemRarityID.Cyan;
        Item.useTime = Item.useAnimation = 40;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.knockBack = 18;
        Item.damage = 600;
        Item.DamageType = DamageClass.Melee;
        Item.shoot = ModContent.ProjectileType<Birch>();
        Item.noUseGraphic = Item.noMelee = Item.autoReuse = true;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new(143, 94, 66));
    }

    public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        Main.projectile[Projectile.NewProjectile(source, position, velocity, type, damage, knockback, Main.myPlayer)].As<Birch>().SwingDir = Projectiles.Base.BaseSwordSwing.SwingDirection.Up;
        return false;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.Seedler, 1);
        recipe.AddIngredient(ModContent.ItemType<BirchStick>(), 1);
        recipe.AddIngredient(ItemID.Wood, 400);
        recipe.AddIngredient(ItemID.SoulofFright, 30);
        recipe.AddCondition(Condition.NearWater);
        recipe.Register();
    }
}