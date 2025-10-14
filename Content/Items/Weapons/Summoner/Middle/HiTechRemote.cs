using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Materials.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Summoner.Middle;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Summoner.Middle;

public class HiTechRemote : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.HiTechRemote);

    public override void SetStaticDefaults()
    {
        ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true;
        ItemID.Sets.StaffMinionSlotsRequired[Type] = 1f;
    }

    public override void SetDefaults()
    {
        Item.damage = 40;
        Item.knockBack = 1f;
        Item.width = 26;
        Item.height = 36;
        Item.useTime = Item.useAnimation = 30;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.value = AdditionsGlobalItem.RarityCyanBuyPrice;
        Item.rare = ItemRarityID.Cyan;
        Item.UseSound = null;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.DamageType = DamageClass.Summon;
        Item.shoot = ModContent.ProjectileType<RemoteHoldout>();
    }

    public override bool? UseItem(Player player)
    {
        return false;
    }

    public override bool CanShoot(Player player)
    {
        return false;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<CrumpledBlueprint>(), 1);
        recipe.AddIngredient(ItemID.MartianConduitPlating, 120);
        recipe.AddIngredient(ItemID.Wire, 180);
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.Register();
    }
}
