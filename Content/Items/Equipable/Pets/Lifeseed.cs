using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Summon;
using TheExtraordinaryAdditions.Content.Projectiles.Pets;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Pets;

public class Lifeseed : ModItem, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Lifeseed);
    public override void SetDefaults()
    {
        Item.damage = 0;
        Item.useTime = Item.useAnimation = 20;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.noMelee = true;
        Item.width = Item.height = 10;
        Item.UseSound = SoundID.Item2;
        Item.shoot = ModContent.ProjectileType<Antfly>();
        Item.buffType = ModContent.BuffType<AntflyBuff>();
        Item.value = Item.sellPrice(0, 0, 10, 0);
        Item.rare = ItemRarityID.Blue;
    }

    public override void UseStyle(Player player, Rectangle heldItemFrame)
    {
        if (player.whoAmI == Main.myPlayer && player.itemTime == 0)
        {
            player.AddBuff(Item.buffType, 3600, true, false);
        }
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.GlowingMushroom, 100);
        recipe.AddTile(TileID.WorkBenches);
        recipe.Register();
    }
}