using CalamityMod.Items.Materials;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Early;
using TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Magic.Late;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Late;

public class PyroclasticVeil : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.PyroclasticVeil);

    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(240, 20, 50));
    }

    public override void SetDefaults()
    {
        Item.damage = 744;
        Item.DamageType = DamageClass.Magic;
        Item.width = Item.height = 38;
        Item.useTime = 4;
        Item.useAnimation = 4;
        Item.knockBack = 0;
        Item.value = AdditionsGlobalItem.RarityRedBuyPrice;
        Item.rare = ItemRarityID.Purple;
        Item.autoReuse = true;
        Item.shoot = ModContent.ProjectileType<PyroclasticHover>();
        Item.crit = 0;
        Item.mana = 10;
        Item.shootSpeed = 11f;
        Item.channel = true;
        Item.noUseGraphic = true;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.useAmmo = AmmoID.None;
    }

    public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<UnholyCore>(), 10);
        recipe.AddIngredient(ModContent.ItemType<CoreofHavoc>(), 12);
        recipe.AddIngredient(ModContent.ItemType<TomeOfHellfire>(), 1);
        recipe.AddIngredient(ModContent.ItemType<Fireball>(), 1);
        recipe.AddIngredient(ItemID.LunarBar, 8);
        recipe.AddTile(TileID.LunarCraftingStation);
        recipe.Register();
    }
}