using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Early;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Middle;

public class TorrentialStorms : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TorrentialStorms);

    public override void SetDefaults()
    {
        Item.damage = 134;
        Item.DamageType = DamageClass.Ranged;
        Item.width = 34;
        Item.height = 62;
        Item.scale = 1f;
        Item.maxStack = 1;
        Item.useTime = Item.useAnimation = 90;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.knockBack = 0;
        Item.value = AdditionsGlobalItem.RarityLimeBuyPrice;
        Item.rare = ItemRarityID.Lime;
        Item.UseSound = SoundID.Item21;
        Item.noMelee = true;
        Item.shoot = ProjectileID.RainFriendly;
        Item.useAmmo = AmmoID.Arrow;
        Item.shootSpeed = 20f;
        Item.autoReuse = true;
    }

    public override bool CanShoot(Player player) => false;

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<Downpour>(), 1);
        recipe.AddIngredient(ItemID.Cloud, 140);
        recipe.AddIngredient(ItemID.SoulofSight, 12);
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.Register();
    }
}
