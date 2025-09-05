using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Late;

public class Lanikea : ModItem, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Lanikea);
    public override void SetStaticDefaults()
    {
        Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(10, 9, false));
        ItemID.Sets.AnimatesAsSoul[Type] = true;
    }

    public override void SetDefaults()
    {
        Item.damage = 348;
        Item.DamageType = DamageClass.Ranged;
        Item.width = 104;
        Item.height = 32;
        Item.useTime = 26;
        Item.useAnimation = 26;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        Item.knockBack = 2f;
        Item.value = AdditionsGlobalItem.RarityPurpleBuyPrice;
        Item.rare = ModContent.RarityType<UniqueRarity>();
        Item.UseSound = null;
        Item.autoReuse = true;
        Item.shoot = ModContent.ProjectileType<LanikeaHoldout>();
        Item.shootSpeed = 16f;
        Item.crit = 8;
        Item.noUseGraphic = true;
    }
    public override bool CanShoot(Player player)
    {
        return false;
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<EbonyNovaBlaster>(), 1);
        recipe.AddIngredient(ItemID.LunarBar, 12);
        recipe.AddIngredient(ItemID.FallenStar, 30);
        recipe.AddCondition(Condition.TimeNight);
        recipe.AddCondition(Condition.MoonPhaseFull);
        recipe.AddTile(TileID.LunarCraftingStation);
        recipe.Register();
    }
}
