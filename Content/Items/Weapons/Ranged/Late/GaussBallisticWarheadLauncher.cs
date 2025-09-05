using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Materials.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Late;

public class GaussBallisticWarheadLauncher : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.GaussBallisticWarheadLauncher);
    public override void SetStaticDefaults()
    {
        ItemID.Sets.AnimatesAsSoul[Type] = true;
        Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(12, 4, false));
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }
    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        DrawInventoryCustomScale(spriteBatch, TextureAssets.Item[Type].Value, position, frame, drawColor, itemColor, origin, scale, .29f, new Vector2(0f, 0f));
        return false;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(179, 230, 122));
    }
    public override void SetDefaults()
    {
        Item.damage = 2100;
        Item.DamageType = DamageClass.Ranged;
        Item.shoot = ModContent.ProjectileType<GaussBallisticWarheadHoldout>();
        Item.useTime = Item.useAnimation = 2;
        Item.shootSpeed = 25f;
        Item.knockBack = 20f;
        Item.width = 162;
        Item.height = 42;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.useAmmo = AmmoID.Rocket;
        Item.rare = ModContent.RarityType<LaserClassRarity>();
        Item.value = AdditionsGlobalItem.LaserRarityPrice;
        Item.useStyle = ItemUseStyleID.Shoot;

    }
    public override bool CanShoot(Player player) => false;
    public override void HoldItem(Player player)
    {

    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        if (ModLoader.TryGetMod("calamityMod", out Mod calamityMod) && calamityMod.TryFind("ExoPrism", out ModItem ExoPrism) && calamityMod.TryFind("MysteriousCircuitry", out ModItem MysteriousCircuitry) && calamityMod.TryFind("DubiousPlating", out ModItem DubiousPlating))
        {
            recipe.AddIngredient(ExoPrism.Type, 14);
            recipe.AddIngredient(ModContent.ItemType<PlasmaCore>(), 1);
            recipe.AddIngredient(DubiousPlating.Type, 25);
            recipe.AddIngredient(MysteriousCircuitry.Type, 30);
            recipe.AddTile(TileID.LunarCraftingStation);
        }
        else
        {
            recipe.AddIngredient(ItemID.RocketLauncher, 1);
            recipe.AddIngredient(ModContent.ItemType<PlasmaCore>(), 1);
            recipe.AddIngredient(ItemID.FragmentVortex, 16);
            recipe.AddIngredient(ItemID.LunarBar, 20);
            recipe.AddTile(TileID.LunarCraftingStation);
        }
        recipe.Register();
    }
}