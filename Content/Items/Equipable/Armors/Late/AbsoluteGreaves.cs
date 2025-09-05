using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Early;
using TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Middle;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals;

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

        if (ModLoader.TryGetMod("calamityMod", out Mod calamityMod) && calamityMod.TryFind("AuricBar", out ModItem AuricBar)
            && calamityMod.TryFind("AscendantSpiritEssence", out ModItem AscendantSpiritEssence)
            && calamityMod.TryFind("CoreofCalamity", out ModItem CoreofCalamity)
            && calamityMod.TryFind("GalacticaSingularity", out ModItem GalacticaSingularity)
            && calamityMod.TryFind("LifeAlloy", out ModItem LifeAlloy)
            && calamityMod.TryFind("RuinousSoul", out ModItem RuinousSoul)
            && calamityMod.TryFind("CosmicAnvil", out ModTile CosmicAnvil))
        {
            recipe.AddIngredient(ItemID.CrimsonGreaves, 1);
            recipe.AddIngredient(ModContent.ItemType<VoltGrieves>(), 1);
            recipe.AddIngredient(ModContent.ItemType<SpecteriteGreaves>(), 1);
            recipe.AddIngredient(ModContent.ItemType<BlueLeggings>(), 1);
            recipe.AddIngredient(ModContent.ItemType<TremorSheathe>(), 1);
            recipe.AddIngredient(ItemID.SolarFlareLeggings, 1);
            recipe.AddIngredient(CoreofCalamity.Type, 3);
            recipe.AddIngredient(GalacticaSingularity.Type, 6);
            recipe.AddIngredient(LifeAlloy.Type, 5);
            recipe.AddIngredient(RuinousSoul.Type, 5);
            recipe.AddIngredient(AscendantSpiritEssence.Type, 4);
            recipe.AddIngredient(AuricBar.Type, 12);
            recipe.AddTile(CosmicAnvil.Type);
        }
        else
        {
            recipe.AddIngredient(ItemID.CrimsonGreaves, 1);
            recipe.AddIngredient(ModContent.ItemType<VoltGrieves>(), 1);
            recipe.AddIngredient(ModContent.ItemType<SpecteriteGreaves>(), 1);
            recipe.AddIngredient(ModContent.ItemType<BlueLeggings>(), 1);
            recipe.AddIngredient(ModContent.ItemType<TremorSheathe>(), 1);
            recipe.AddIngredient(ItemID.SolarFlareLeggings, 1);
            recipe.AddTile(TileID.LunarCraftingStation);
        }

        recipe.Register();
    }
}
