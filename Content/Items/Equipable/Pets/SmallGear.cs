using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using TheExtraordinaryAdditions.Content.Buffs.Summon;
using TheExtraordinaryAdditions.Content.Projectiles.Pets;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Pets;

public class SmallGear : ModItem, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.SmallGear);
    public override void SetDefaults()
    {
        Item.damage = 0;
        Item.useTime = Item.useAnimation = 20;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.noMelee = true;
        Item.width = Item.height = 10;
        Item.UseSound = SoundID.Item2;
        Item.shoot = ModContent.ProjectileType<Gearcat>();
        Item.buffType = ModContent.BuffType<GearcatBuff>();
        Item.value = Item.sellPrice(0, 0, 15, 0);
        Item.rare = ItemRarityID.Blue;
    }

    public override void UseStyle(Player player, Rectangle heldItemFrame)
    {
        if (player.whoAmI == Main.myPlayer && player.itemTime == 0)
        {
            player.AddBuff(Item.buffType, 3600, true, false);
        }
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddRecipeGroup(RecipeGroupID.IronBar, 6);
        recipe.AddIngredient(ItemID.WhiteString, 1);
        recipe.AddTile(TileID.WorkBenches);
        recipe.Register();
    }
}