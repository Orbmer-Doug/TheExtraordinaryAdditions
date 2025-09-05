using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Materials.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Middle;

public class Hailfire : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Hailfire);
    public const int Damage = 270;
    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(255, 77, 23));
    }
    public override void SetDefaults()
    {
        Item.width = 120;
        Item.height = 42;
        Item.scale = 1f;
        Item.rare = ItemRarityID.Cyan;
        Item.value = AdditionsGlobalItem.RarityCyanBuyPrice;
        Item.useTime = Item.useAnimation = 16;
        Item.autoReuse = true;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.DamageType = DamageClass.Ranged;
        Item.damage = Damage;
        Item.knockBack = 2f;
        Item.noMelee = true;
        Item.shoot = ModContent.ProjectileType<HailfireHoldout>();
        Item.shootSpeed = 14f;
        Item.noUseGraphic = true;
        Item.channel = true;
    }

    public override bool CanShoot(Player player) => false;

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.GrenadeLauncher, 1);
        recipe.AddIngredient(ModContent.ItemType<TremorAlloy>(), 7);
        recipe.AddIngredient(ItemID.BeetleHusk, 12);
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.Register();
    }

}