using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Early;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Early;

public class LooseSawblade : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.LooseSawblade);

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 99;
    }

    public override void SetDefaults()
    {
        Item.damage = 20;
        Item.knockBack = 1.5f;
        Item.value = AdditionsGlobalItem.RarityWhiteBuyPrice;
        Item.rare = ItemRarityID.Orange;
        Item.useTime = 25;
        Item.useAnimation = 25;
        Item.width = 26;
        Item.height = 26;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.UseSound = SoundID.Item1;
        Item.shootSpeed = 11f;
        Item.shoot = ModContent.ProjectileType<LooseSawbladeProj>();
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.crit = 10;
        Item.maxStack = Item.CommonMaxStack;
        Item.consumable = true;
        Item.DamageType = DamageClass.Ranged;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(112, 117, 59));
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe(150);
        recipe.AddIngredient(ItemID.Bone, 4);
        recipe.AddRecipeGroup("AnySilverBar", 1);
        recipe.Register();
    }
}
