using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Early;
using TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Middle;
using TheExtraordinaryAdditions.Content.Rarities;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Late;

[AutoloadEquip(EquipType.Legs)]
public class AbsoluteGreaves : ModItem
{
    public override string Texture => AssetRegistry.GennedTextures.AbsoluteGreaves.Path;
    public static int LegsSlotID { get; private set; }

    public override void SetStaticDefaults()
    {
        LegsSlotID = Item.legSlot;
    }

    public override void SetDefaults()
    {
        Item.width = 22;
        Item.height = 18;
        Item.rare = ModContent.RarityType<UniqueRarity>();
        Item.value = AdditionsGlobalItem.UniqueRarityPrice;
        Item.vanity = true;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<VoltGrieves>());
        recipe.AddIngredient(ModContent.ItemType<SpecteriteGreaves>());
        recipe.AddIngredient(ModContent.ItemType<BlueLeggings>());
        recipe.AddIngredient(ModContent.ItemType<TremorSheathe>());
        recipe.AddTile(TileID.LunarCraftingStation);
        recipe.Register();
    }
}
