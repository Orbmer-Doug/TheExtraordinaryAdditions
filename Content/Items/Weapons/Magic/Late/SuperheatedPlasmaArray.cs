using CalamityMod.Items.Materials;
using CalamityMod.Tiles.Furniture.CraftingStations;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Materials.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Magic.Late;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Late;

public class SuperheatedPlasmaArray : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.SuperheatedPlasmaArray);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(242, 106, 0));
    }
    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        DrawInventoryCustomScale(spriteBatch, TextureAssets.Item[Type].Value, position, frame, drawColor, itemColor, origin, scale, 0.27f, new Vector2(0f, 0f));
        return false;
    }
    public override void SetDefaults()
    {
        Item.damage = 288;
        Item.knockBack = 0f;
        Item.DamageType = DamageClass.Magic;
        Item.autoReuse = true;
        Item.useTime = Item.useAnimation = 4;
        Item.mana = 5;
        Item.shootSpeed = 5.6f;
        Item.channel = true;
        Item.noUseGraphic = true;
        Item.shoot = ModContent.ProjectileType<SuperheatedPlasmaArrayHoldout>();

        Item.width = 198;
        Item.height = 42;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        Item.value = AdditionsGlobalItem.LaserRarityPrice;
        Item.rare = ModContent.RarityType<LaserClassRarity>();
    }

    public override Vector2? HoldoutOffset() => -Vector2.UnitX * 4f;

    public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.LastPrism, 1);
        recipe.AddIngredient(ModContent.ItemType<PlasmaCore>(), 1);
        recipe.AddIngredient(ItemID.MartianConduitPlating, 200);
        recipe.AddIngredient(ItemID.Glass, 120);
        recipe.AddIngredient(ModContent.ItemType<MysteriousCircuitry>(), 12);
        recipe.AddIngredient(ModContent.ItemType<DubiousPlating>(), 15);
        recipe.AddIngredient(ModContent.ItemType<AscendantSpiritEssence>(), 7);
        recipe.AddTile(ModContent.TileType<CosmicAnvil>());
        recipe.Register();
    }

    public override bool? UseItem(Player player)
    {
        if (player.whoAmI != Main.myPlayer)
        {
            return true;
        }
        return true;
    }
}