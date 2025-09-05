using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Middle;

public class CopperWireWrappedRock : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CopperWireWrappedRock);
    public override void SetDefaults()
    {
        Item.damage = 145;
        Item.DamageType = DamageClass.Ranged;
        Item.width = 94;
        Item.height = 36;
        Item.scale = 1f;
        Item.maxStack = 1;
        Item.useTime = Item.useAnimation = 50;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.knockBack = 2f;
        Item.value = AdditionsGlobalItem.RarityLightPurpleBuyPrice;
        Item.rare = ItemRarityID.LightPurple;
        Item.UseSound = SoundID.Item1;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.shoot = ModContent.ProjectileType<FunnyRock>();
        Item.shootSpeed = 1f;
        Item.autoReuse = true;
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.CopperBar, 10);
        recipe.AddIngredient(ItemID.StoneBlock, 100);
        recipe.AddIngredient(ItemID.SoulofMight, 14);
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.Register();
    }
}
