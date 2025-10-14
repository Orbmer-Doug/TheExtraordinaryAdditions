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

[AutoloadEquip(EquipType.Legs)]
public class AbsoluteGreaves : ModItem, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.AbsoluteGreaves);
    public static int LegsSlotID
    {
        get;
        private set;
    }
    public override void SetStaticDefaults()
    {
        LegsSlotID = Item.legSlot;
    }

    public override void SetDefaults()
    {
        Item.width = 22;
        Item.height = 18;
        Item.defense = 45;
        Item.rare = ModContent.RarityType<UniqueRarity>();
        Item.value = AdditionsGlobalItem.UniqueRarityPrice;
    }

    public override void UpdateEquip(Player player)
    {
        Lighting.AddLight(player.Center, Color.AntiqueWhite.ToVector3() * 1.5f);

        player.runSlowdown *= 2f;
        player.moveSpeed += 0.30f;
        player.runAcceleration *= 1.5f;
        player.noFallDmg = true;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.CrimsonGreaves, 1);
        recipe.AddIngredient(ModContent.ItemType<VoltGrieves>(), 1);
        recipe.AddIngredient(ModContent.ItemType<SpecteriteGreaves>(), 1);
        recipe.AddIngredient(ModContent.ItemType<BlueLeggings>(), 1);
        recipe.AddIngredient(ModContent.ItemType<TremorSheathe>(), 1);
        recipe.AddIngredient(ItemID.SolarFlareLeggings, 1);
        recipe.AddIngredient(ModContent.ItemType<CoreofCalamity>(), 3);
        recipe.AddIngredient(ModContent.ItemType<GalacticaSingularity>(), 6);
        recipe.AddIngredient(ModContent.ItemType<LifeAlloy>(), 5);
        recipe.AddIngredient(ModContent.ItemType<RuinousSoul>(), 5);
        recipe.AddIngredient(ModContent.ItemType<AscendantSpiritEssence>(), 4);
        recipe.AddIngredient(ModContent.ItemType<AuricBar>(), 12);
        recipe.AddTile(ModContent.TileType<CosmicAnvil>());
        recipe.Register();
    }
}
