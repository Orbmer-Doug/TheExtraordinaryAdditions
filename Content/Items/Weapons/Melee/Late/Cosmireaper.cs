using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Late.Cosmireaper;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Late;

public class Cosmireaper : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Cosmireaper);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(ColorSwap(Color.MediumPurple * 1.1f, Color.PaleVioletRed, 4f));
    }

    public override void SetDefaults()
    {
        Item.damage = 4500;
        Item.DamageType = DamageClass.MeleeNoSpeed;
        Item.width = 92;
        Item.height = 118;
        Item.noMelee = true;
        Item.useTime = Item.useAnimation = 15;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.knockBack = 4f;
        Item.value = AdditionsGlobalItem.UniqueRarityPrice;
        Item.rare = ModContent.RarityType<UniqueRarity>();
        Item.autoReuse = false;
        Item.shoot = ModContent.ProjectileType<CosmireapHoldout>();
        Item.shootSpeed = 1f;
        Item.useTurn = false;
        Item.channel = true;
        Item.noUseGraphic = true;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        player.NewPlayerProj(position, Vector2.Zero, type, damage, knockback, player.whoAmI);
        return false;
    }

    public override bool CanUseItem(Player player)
    {
        return player.ownedProjectileCounts[Item.shoot] < 1;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        if (ModLoader.TryGetMod("calamityMod", out Mod calamityMod) && calamityMod.TryFind("CosmiliteBar", out ModItem CosmiliteBar)
            && calamityMod.TryFind("CosmicAnvil", out ModTile CosmicAnvil)
            && calamityMod.TryFind("AscendantSpiritEssence", out ModItem AscendantSpiritEssence))
        {
            recipe.AddIngredient(ItemID.Sickle, 1);
            recipe.AddIngredient(AscendantSpiritEssence.Type, 2);
            recipe.AddIngredient(CosmiliteBar.Type, 16);
            recipe.AddTile(CosmicAnvil.Type);
        }
        else
        {
            recipe.AddIngredient(ItemID.Sickle, 1);
            recipe.AddIngredient(ItemID.LunarBar, 16);
            recipe.AddTile(TileID.LunarCraftingStation);
        }
        recipe.Register();
    }
}