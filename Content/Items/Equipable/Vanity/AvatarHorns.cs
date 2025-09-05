using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;

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
        if (ModLoader.TryGetMod("calamityMod", out Mod calamityMod) && calamityMod.TryFind("AuricBar", out ModItem AuricBar) && calamityMod.TryFind("CosmicAnvil", out ModTile CosmicAnvil) && calamityMod.TryFind("CoreofCalamity", out ModItem CoreofCalamity) && calamityMod.TryFind("AscendantSpiritEssence", out ModItem AscendantSpiritEssence) && calamityMod.TryFind("MysteriousCircuitry", out ModItem MysteriousCircuitry) && calamityMod.TryFind("DubiousPlating", out ModItem DubiousPlating))
        {
            recipe.AddIngredient(DubiousPlating.Type, 10);
            recipe.AddIngredient(MysteriousCircuitry.Type, 10);
            recipe.AddIngredient(ItemID.SoulofFlight, 20);
            recipe.AddIngredient(CoreofCalamity.Type, 4);
            recipe.AddIngredient(AscendantSpiritEssence.Type, 5);
            recipe.AddIngredient(AuricBar.Type, 5);
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