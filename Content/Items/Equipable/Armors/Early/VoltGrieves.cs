using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Materials.Early;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Early;

[AutoloadEquip(EquipType.Legs)]
public class VoltGrieves : ModItem, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.VoltGrieves);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(231, 191, 255));
    }

    public override void SetDefaults()
    {
        Item.width = 22;
        Item.height = 18;
        Item.value = AdditionsGlobalItem.RarityOrangeBuyPrice;
        Item.defense = 5;
        Item.rare = ItemRarityID.Orange;
    }

    public override void UpdateEquip(Player player)
    {
        Lighting.AddLight(player.Center, new Color(206, 125, 255).ToVector3() * .33f);

        player.moveSpeed += 0.12f;
        ref StatModifier damage = ref player.GetDamage<MagicDamageClass>();
        damage += 0.03f;
        player.GetAttackSpeed(DamageClass.Melee) += .05f;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<ShockCatalyst>(), 13);
        recipe.AddTile(TileID.Anvils);
        recipe.Register();
    }
}
