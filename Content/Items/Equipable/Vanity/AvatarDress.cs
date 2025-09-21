using CalamityMod.Items.Materials;
using CalamityMod.Tiles.Furniture.CraftingStations;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Vanity;

[AutoloadEquip(EquipType.Body)]
public class AvatarDress : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.AvatarDress);

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(156, 184, 253));
    }

    public override void SetDefaults()
    {
        Item.width = 18;
        Item.height = 16;
        Item.value = AdditionsGlobalItem.UniqueRarityPrice;
        Item.rare = ItemRarityID.Cyan;
        Item.vanity = true;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<DubiousPlating>(), 15);
        recipe.AddIngredient(ModContent.ItemType<MysteriousCircuitry>(), 15);
        recipe.AddIngredient(ItemID.SoulofFlight, 20);
        recipe.AddIngredient(ModContent.ItemType<CoreofCalamity>(), 6);
        recipe.AddIngredient(ModContent.ItemType<AscendantSpiritEssence>(), 5);
        recipe.AddIngredient(ModContent.ItemType<AuricBar>(), 9);
        recipe.AddTile(ModContent.TileType<CosmicAnvil>());
        recipe.Register();
    }
}