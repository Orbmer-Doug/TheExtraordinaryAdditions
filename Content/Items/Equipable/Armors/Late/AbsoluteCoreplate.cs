using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Early;
using TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Middle;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals;

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
        player.GetModPlayer<GlobalPlayer>().AbsoluteArmor = true;
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
            recipe.AddIngredient(ItemID.CrimsonScalemail, 1);
            recipe.AddIngredient(ModContent.ItemType<VoltChestplate>(), 1);
            recipe.AddIngredient(ModContent.ItemType<SpecteriteChestPiece>(), 1);
            recipe.AddIngredient(ModContent.ItemType<BlueTuxedo>(), 1);
            recipe.AddIngredient(ModContent.ItemType<TremorPlating>(), 1);
            recipe.AddIngredient(ItemID.SolarFlareBreastplate, 1);
            recipe.AddIngredient(CoreofCalamity.Type, 4);
            recipe.AddIngredient(GalacticaSingularity.Type, 7);
            recipe.AddIngredient(LifeAlloy.Type, 5);
            recipe.AddIngredient(RuinousSoul.Type, 5);
            recipe.AddIngredient(AscendantSpiritEssence.Type, 5);
            recipe.AddIngredient(AuricBar.Type, 14);
            recipe.AddTile(CosmicAnvil.Type);
        }
        else
        {
            recipe.AddIngredient(ItemID.CrimsonScalemail, 1);
            recipe.AddIngredient(ModContent.ItemType<VoltChestplate>(), 1);
            recipe.AddIngredient(ModContent.ItemType<SpecteriteChestPiece>(), 1);
            recipe.AddIngredient(ModContent.ItemType<BlueTuxedo>(), 1);
            recipe.AddIngredient(ModContent.ItemType<TremorPlating>(), 1);
            recipe.AddIngredient(ItemID.SolarFlareBreastplate, 1);
            recipe.AddTile(TileID.LunarCraftingStation);
        }
        recipe.Register();
    }
}