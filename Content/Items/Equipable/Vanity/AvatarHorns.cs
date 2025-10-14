using CalamityMod.Items.Materials;
using CalamityMod.Tiles.Furniture.CraftingStations;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Vanity;

[AutoloadEquip(EquipType.Head)]
public class AvatarHorns : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.AvatarHorns);

    public override void SetStaticDefaults()
    {
        ArmorIDs.Head.Sets.DrawHead[Item.headSlot] = false; // Don't draw the head at all. Used by Space Creature Mask
        ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = false; // Draw hair as if a hat was covering the top. Used by Wizards Hat
        ArmorIDs.Head.Sets.DrawFullHair[Item.headSlot] = false; // Draw all hair as normal. Used by Mime Mask, Sunglasses
        ArmorIDs.Head.Sets.DrawsBackHairWithoutHeadgear[Item.headSlot] = false;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(156, 184, 253));
    }

    public override void SetDefaults()
    {
        Item.width = 30;
        Item.height = 22;
        Item.value = AdditionsGlobalItem.UniqueRarityPrice;
        Item.rare = ItemRarityID.Cyan;
        Item.vanity = true;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<DubiousPlating>(), 10);
        recipe.AddIngredient(ModContent.ItemType<MysteriousCircuitry>(), 10);
        recipe.AddIngredient(ItemID.SoulofFlight, 20);
        recipe.AddIngredient(ModContent.ItemType<CoreofCalamity>(), 4);
        recipe.AddIngredient(ModContent.ItemType<AscendantSpiritEssence>(), 5);
        recipe.AddIngredient(ModContent.ItemType<AuricBar>(), 5);
        recipe.AddTile(ModContent.TileType<CosmicAnvil>());
        recipe.Register();
    }
}