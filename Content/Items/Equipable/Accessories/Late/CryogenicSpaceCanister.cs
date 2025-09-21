using CalamityMod.Items.Materials;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;
using TheExtraordinaryAdditions.UI.LaserUI;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Late;

[AutoloadEquip(EquipType.Back)]
public class CryogenicSpaceCanister : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CryogenicSpaceCanister);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(Color.LightCyan);
    }

    public override void SetDefaults()
    {
        Item.width = 60;
        Item.height = 62;
        Item.maxStack = 1;
        Item.value = AdditionsGlobalItem.RarityRedBuyPrice;
        Item.accessory = true;
        Item.rare = ItemRarityID.Red;
    }

    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        LaserResource resource = player.GetModPlayer<LaserResource>();
        GlobalPlayer mod = player.GetModPlayer<GlobalPlayer>();

        player.buffImmune[BuffID.OnFire & BuffID.OnFire3 & BuffID.Burning & BuffID.Frostburn & BuffID.Frostburn2 & BuffID.Frozen & BuffID.Slow & BuffID.Chilled] = true;
        player.resistCold = true;
        mod.Nitrogen = true;

        if (resource.HeatCurrent == 0)
        {
            mod.Cryogenic = true;
            player.statDefense += 20;
        }
        if (resource.HeatCurrent > 0)
        {
            mod.Cryogenic = false;
            resource.HeatRegenRate *= 2.7f;
            player.GetArmorPenetration(DamageClass.Generic) += 15;
        }
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.LunarBar, 10);
        recipe.AddIngredient(ModContent.ItemType<CryonicBar>(), 12);
        recipe.AddIngredient(ModContent.ItemType<CoreofEleum>(), 15);
        recipe.AddTile(TileID.LunarCraftingStation);
        recipe.Register();
    }
}