using CalamityMod.Items.Materials;
using CalamityMod.Tiles.Furniture.CraftingStations;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Early;
using TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Middle;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Late;

[AutoloadEquip(EquipType.Body)]
public class AbsoluteCoreplate : ModItem, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.AbsoluteCoreplate);
    public static int BodySlotID
    {
        get;
        private set;
    }
    public override void SetStaticDefaults()
    {
        BodySlotID = Item.bodySlot;
    }
    public override void SetDefaults()
    {
        Item.width = 38;
        Item.height = 26;
        Item.defense = 50;
        Item.rare = ModContent.RarityType<UniqueRarity>();
        Item.value = AdditionsGlobalItem.UniqueRarityPrice;
    }

    public override void UpdateEquip(Player player)
    {
        Lighting.AddLight(player.Center, Color.AntiqueWhite.ToVector3() * 1.5f);

        player.statLifeMax2 += 110;
        player.lifeRegenTime += 2f;
        player.lifeRegen += 1;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.CrimsonScalemail, 1);
        recipe.AddIngredient(ModContent.ItemType<VoltChestplate>(), 1);
        recipe.AddIngredient(ModContent.ItemType<SpecteriteChestPiece>(), 1);
        recipe.AddIngredient(ModContent.ItemType<BlueTuxedo>(), 1);
        recipe.AddIngredient(ModContent.ItemType<TremorPlating>(), 1);
        recipe.AddIngredient(ItemID.SolarFlareBreastplate, 1);
        recipe.AddIngredient(ModContent.ItemType<CoreofCalamity>(), 4);
        recipe.AddIngredient(ModContent.ItemType<GalacticaSingularity>(), 7);
        recipe.AddIngredient(ModContent.ItemType<LifeAlloy>(), 5);
        recipe.AddIngredient(ModContent.ItemType<RuinousSoul>(), 5);
        recipe.AddIngredient(ModContent.ItemType<AscendantSpiritEssence>(), 5);
        recipe.AddIngredient(ModContent.ItemType<AuricBar>(), 14);
        recipe.AddTile(ModContent.TileType<CosmicAnvil>());
        recipe.Register();
    }
}