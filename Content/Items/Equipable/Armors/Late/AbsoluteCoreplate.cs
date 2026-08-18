using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Early;
using TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Middle;
using TheExtraordinaryAdditions.Content.Rarities;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Late;

[AutoloadEquip(EquipType.Body)]
public class AbsoluteCoreplate : ModItem
{
    public override string Texture => AssetRegistry.GennedTextures.AbsoluteCoreplate.Path;
    public static int BodySlotID { get; private set; }

    public override void SetStaticDefaults()
    {
        BodySlotID = Item.bodySlot;
    }

    public override void SetDefaults()
    {
        Item.width = 38;
        Item.height = 26;
        Item.rare = ModContent.RarityType<UniqueRarity>();
        Item.value = AdditionsGlobalItem.UniqueRarityPrice;
        Item.vanity = true;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<VoltChestplate>());
        recipe.AddIngredient(ModContent.ItemType<SpecteriteChestPiece>());
        recipe.AddIngredient(ModContent.ItemType<BlueTuxedo>());
        recipe.AddIngredient(ModContent.ItemType<TremorPlating>());
        recipe.AddTile(TileID.LunarCraftingStation);
        recipe.Register();
    }
}
