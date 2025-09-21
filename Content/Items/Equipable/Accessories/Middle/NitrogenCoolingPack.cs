using CalamityMod.Items.Materials;
using CalamityMod.Items.Placeables;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;
using TheExtraordinaryAdditions.UI.LaserUI;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Middle;

[AutoloadEquip(EquipType.Back)]
public class NitrogenCoolingPack : ModItem, ILocalizedModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.NitrogenCoolingPack);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(138, 204, 255));
    }

    public override void SetDefaults()
    {
        Item.width = 24;
        Item.height = 62;
        Item.maxStack = 1;
        Item.defense = 2;
        Item.value = AdditionsGlobalItem.RarityYellowBuyPrice;
        Item.accessory = true;
        Item.defense = 4;
        Item.rare = ItemRarityID.Yellow;
    }

    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        LaserResource modPlayer = player.GetModPlayer<LaserResource>();

        player.Additions().Nitrogen = true;
        player.buffImmune[BuffID.OnFire & BuffID.OnFire3 & BuffID.Burning & BuffID.Frostburn & BuffID.Frostburn2 & BuffID.Frozen & BuffID.Slow & BuffID.Chilled] = true;
        player.resistCold = true;
        modPlayer.HeatRegenRate *= 2f;
        player.GetArmorPenetration(DamageClass.Generic) += 10;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<Voidstone>(), 75);
        recipe.AddIngredient(ModContent.ItemType<CryonicBar>(), 10);
        recipe.AddIngredient(ModContent.ItemType<CoreofEleum>(), 8);
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.Register();
    }
}