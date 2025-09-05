using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;
using TheExtraordinaryAdditions.UI.CrossUI;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Late;

public class AncientBoon : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.AncientBoon);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(252, 255, 99));
    }

    public override void SetDefaults()
    {
        Item.width = 26;
        Item.height = 26;
        Item.maxStack = 1;
        Item.defense = 2;
        Item.value = AdditionsGlobalItem.UniqueRarityPrice;
        Item.accessory = true;
        Item.rare = ModContent.RarityType<UniqueRarity>();
    }

    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        var modPlayer = player.GetModPlayer<ElementalBalance>();
        modPlayer.ElementalResourceRegenRate *= 1.111f;

        if (!player.slowFall && player.wingTime < player.wingTimeMax && !player.controlJump && player.miscCounter % 2 == 0)
        {
            player.wingTime += 1.8f;
        }

        player.statDefense *= 1.16f;
        player.GetModPlayer<GlobalPlayer>().ancientBoon = true;
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        if (ModLoader.TryGetMod("calamityMod", out Mod calamityMod) && calamityMod.TryFind("AuricBar", out ModItem AuricBar) && calamityMod.TryFind("CosmicAnvil", out ModTile CosmicAnvil) && calamityMod.TryFind("CoreofCalamity", out ModItem CoreofCalamity) && calamityMod.TryFind("AscendantSpiritEssence", out ModItem AscendantSpiritEssence) && calamityMod.TryFind("GalacticaSingularity", out ModItem GalacticaSingularity))
        {
            recipe.AddIngredient(ItemID.SoulofFlight, 40);
            recipe.AddIngredient(CoreofCalamity.Type, 5);
            recipe.AddIngredient(GalacticaSingularity, 12);
            recipe.AddIngredient(AscendantSpiritEssence, 5);
            recipe.AddIngredient(AuricBar.Type, 5);
            recipe.AddTile(CosmicAnvil.Type);
        }
        else
        {
            recipe.AddIngredient(ItemID.SoulofFlight, 120);
            recipe.AddIngredient(ItemID.SnowBlock, 100);
            recipe.AddIngredient(ItemID.RainCloud, 100);
            recipe.AddIngredient(ItemID.AshBlock, 100);
            recipe.AddIngredient(ItemID.MudBlock, 100);
            recipe.AddTile(TileID.VoidMonolith);
        }
        recipe.Register();
    }
}