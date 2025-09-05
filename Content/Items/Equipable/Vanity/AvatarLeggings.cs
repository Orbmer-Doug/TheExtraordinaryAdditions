using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Vanity;

[AutoloadEquip(EquipType.Legs)]
public class AvatarLeggings : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.AvatarLeggings);

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(156, 184, 253));
    }

    public override void SetDefaults()
    {
        Item.width = 18;
        Item.height = 12;
        Item.value = AdditionsGlobalItem.UniqueRarityPrice;
        Item.rare = ItemRarityID.Cyan;
        Item.vanity = true;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        if (ModLoader.TryGetMod("calamityMod", out Mod calamityMod) && calamityMod.TryFind("AuricBar", out ModItem AuricBar) && calamityMod.TryFind("CosmicAnvil", out ModTile CosmicAnvil) && calamityMod.TryFind("CoreofCalamity", out ModItem CoreofCalamity) && calamityMod.TryFind("AscendantSpiritEssence", out ModItem AscendantSpiritEssence) && calamityMod.TryFind("MysteriousCircuitry", out ModItem MysteriousCircuitry) && calamityMod.TryFind("DubiousPlating", out ModItem DubiousPlating))
        {
            recipe.AddIngredient(DubiousPlating.Type, 12);
            recipe.AddIngredient(MysteriousCircuitry.Type, 12);
            recipe.AddIngredient(ItemID.SoulofFlight, 20);
            recipe.AddIngredient(CoreofCalamity.Type, 5);
            recipe.AddIngredient(AscendantSpiritEssence.Type, 5);
            recipe.AddIngredient(AuricBar.Type, 7);
            recipe.AddTile(CosmicAnvil.Type);
        }
        else
        {
            recipe.AddIngredient(ItemID.LunarBar, 18);
            recipe.AddIngredient(ItemID.RedDye, 4);
            recipe.AddIngredient(ItemID.SilverDye, 4);
            recipe.AddTile(TileID.LunarCraftingStation);
        }
        recipe.Register();
    }
}