using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Vanity;

[AutoloadEquip(EquipType.Legs)]
public class EclipsedOnesLeggings : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.EclipsedOnesLeggings);

    public override void SetDefaults()
    {
        Item.width = 29;
        Item.height = 21;
        Item.value = AdditionsGlobalItem.RarityPinkBuyPrice;
        Item.rare = ItemRarityID.Pink;
        Item.vanity = true;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.Silk, 15);
        recipe.AddIngredient(ItemID.FlinxFur, 5);
        recipe.AddIngredient(ItemID.IceBlock, 50);
        recipe.AddIngredient(ItemID.Bone, 12);

        recipe.AddTile(TileID.MythrilAnvil);
        recipe.Register();
    }
}