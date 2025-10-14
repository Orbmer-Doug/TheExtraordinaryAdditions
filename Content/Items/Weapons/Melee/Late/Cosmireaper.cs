using CalamityMod.Items.Materials;
using CalamityMod.Tiles.Furniture.CraftingStations;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Late.Cosmireaper;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Late;

public class Cosmireaper : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Cosmireaper);

    public override void SetDefaults()
    {
        Item.damage = 4500;
        Item.DamageType = DamageClass.MeleeNoSpeed;
        Item.width = 92;
        Item.height = 118;
        Item.noMelee = true;
        Item.useTime = Item.useAnimation = 15;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.knockBack = 4f;
        Item.value = AdditionsGlobalItem.UniqueRarityPrice;
        Item.rare = ModContent.RarityType<UniqueRarity>();
        Item.autoReuse = false;
        Item.shoot = ModContent.ProjectileType<CosmireapHoldout>();
        Item.shootSpeed = 1f;
        Item.useTurn = false;
        Item.channel = true;
        Item.noUseGraphic = true;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(ColorSwap(Color.MediumPurple * 1.1f, Color.PaleVioletRed, 4f));
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        player.NewPlayerProj(position, Vector2.Zero, type, damage, knockback, player.whoAmI);
        return false;
    }

    public override bool CanUseItem(Player player)
    {
        if (Utility.FindProjectile(out Projectile scythe, Item.shoot, player.whoAmI))
        {
            if (!scythe.As<CosmireapHoldout>().Released || scythe.As<CosmireapHoldout>().State == CosmireapHoldout.States.Impact)
                return false;
        }
        return true;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.Sickle, 1);
        recipe.AddIngredient(ModContent.ItemType<AscendantSpiritEssence>(), 2);
        recipe.AddIngredient(ModContent.ItemType<CosmiliteBar>(), 16);
        recipe.AddTile(ModContent.TileType<CosmicAnvil>());
        recipe.Register();
    }
}