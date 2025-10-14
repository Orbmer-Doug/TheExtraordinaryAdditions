using CalamityMod.Items.Materials;
using CalamityMod.Tiles.Furniture.CraftingStations;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Summoner.Early;
using TheExtraordinaryAdditions.Content.Items.Weapons.Summoner.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Summoner.Late;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Summoner.Late;

public class DeepestNadir : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.DeepestNadir);

    public override void SetDefaults()
    {
        Item.damage = 220;
        Item.width = Item.height = 4;
        Item.useTime = Item.useAnimation = 42;
        Item.UseSound = SoundID.Item152;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.rare = ModContent.RarityType<LegendaryRarity>();
        Item.value = AdditionsGlobalItem.LegendaryRarityPrice;
        Item.DamageType = DamageClass.SummonMeleeSpeed;
        Item.shoot = ModContent.ProjectileType<ThrashedVoid>();
        Item.shootSpeed = 1f;
        Item.knockBack = 5f;
        Item.noMelee = Item.noUseGraphic = true;
    }

    public override bool CanShoot(Player player)
    {
        return player.ownedProjectileCounts[Item.shoot] <= 0;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        for (int i = 0; i < 4; i++)
        {
            player.NewPlayerProj(position, velocity, type, damage, knockback, player.whoAmI);
        }
        return false;
    }

    public override bool MeleePrefix()
    {
        return true;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<TimberLash>(), 1);
        recipe.AddIngredient(ItemID.BlandWhip, 1);
        recipe.AddIngredient(ItemID.ThornWhip, 1);
        recipe.AddIngredient(ItemID.BoneWhip, 1);
        recipe.AddIngredient(ItemID.FireWhip, 1);
        recipe.AddIngredient(ModContent.ItemType<Atorcoppe>(), 1);
        recipe.AddIngredient(ItemID.CoolWhip, 1);
        recipe.AddIngredient(ModContent.ItemType<IchorWhip>(), 1);
        recipe.AddIngredient(ItemID.SwordWhip, 1);
        recipe.AddIngredient(ItemID.MaceWhip, 1);
        recipe.AddIngredient(ModContent.ItemType<EclipsedDuo>(), 1);
        recipe.AddIngredient(ItemID.ScytheWhip, 1);
        recipe.AddIngredient(ItemID.RainbowWhip, 1);
        recipe.AddIngredient(ModContent.ItemType<GalacticaSingularity>(), 12);
        recipe.AddIngredient(ModContent.ItemType<DarkPlasma>(), 8);
        recipe.AddIngredient(ModContent.ItemType<AuricBar>(), 5);
        recipe.AddTile(ModContent.TileType<CosmicAnvil>());
        recipe.Register();
    }
}