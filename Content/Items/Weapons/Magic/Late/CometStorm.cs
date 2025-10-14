using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Rarities;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Magic.Late;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Late;

public class CometStorm : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CometStorm);

    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(Color.AliceBlue);
    }

    public override void SetDefaults()
    {
        Item.damage = 220;
        Item.DamageType = DamageClass.Magic;
        Item.width = 116;
        Item.height = 184;
        Item.useTime =
        Item.useAnimation = 8;
        Item.knockBack = 0;
        Item.value = CalamityGlobalItem.RarityPureGreenBuyPrice;
        Item.rare = ModContent.RarityType<PureGreen>();
        Item.UseSound = null;
        Item.autoReuse = true;
        Item.shoot = ModContent.ProjectileType<CometStormHoldout>();
        Item.crit = 0;
        Item.mana = 5;
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
        recipe.AddIngredient(ItemID.MeteorStaff, 1);
        recipe.AddIngredient(ModContent.ItemType<Lumenyl>(), 10);
        recipe.AddIngredient(ModContent.ItemType<RuinousSoul>(), 5);
        recipe.AddIngredient(ModContent.ItemType<DarkPlasma>(), 6);
        recipe.AddTile(TileID.LunarCraftingStation);
        recipe.Register();
    }
}