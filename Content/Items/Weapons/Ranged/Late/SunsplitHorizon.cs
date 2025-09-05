using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Materials.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Late;

public class SunsplitHorizon : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.SunsplitHorizon);
    public override void SetStaticDefaults()
    {
        Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(6, 4, true));
        ItemID.Sets.AnimatesAsSoul[Type] = true;
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }

    public override void SetDefaults()
    {
        Item.damage = 200;
        Item.DamageType = DamageClass.Ranged;
        Item.width = 114;
        Item.height = 50;
        Item.useTime = 4;
        Item.useAnimation = 4;
        Item.knockBack = 0;
        Item.value = AdditionsGlobalItem.LaserRarityPrice;
        Item.rare = ModContent.RarityType<LaserClassRarity>();
        Item.UseSound = null;
        Item.autoReuse = true;
        Item.shoot = ModContent.ProjectileType<SunsplitHoldout>();
        Item.shootSpeed = 11f;
        Item.noUseGraphic = true;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.useAmmo = AmmoID.None;
    }

    public override bool CanShoot(Player player) => false;

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        if (ModLoader.TryGetMod("calamityMod", out Mod calamityMod) && calamityMod.TryFind("UnholyCore", out ModItem UnholyCore) && calamityMod.TryFind("CoreOfHavoc", out ModItem CoreOfHavoc))
        {
            recipe.AddIngredient(ItemID.Flamethrower, 1);
            recipe.AddIngredient(ModContent.ItemType<PlasmaCore>(), 1);
            recipe.AddIngredient(UnholyCore.Type, 10);
            recipe.AddIngredient(CoreOfHavoc.Type, 12);
            recipe.AddTile(TileID.LunarCraftingStation);
        }
        else
        {
            recipe.AddIngredient(ItemID.Flamethrower, 1);
            recipe.AddIngredient(ModContent.ItemType<PlasmaCore>(), 1);
            recipe.AddIngredient(ItemID.LunarBar, 8);
            recipe.AddTile(TileID.LunarCraftingStation);
        }
        recipe.Register();
    }
}