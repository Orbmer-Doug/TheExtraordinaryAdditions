using CalamityMod.Items.Materials;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Late.Everbladed;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Late;

public class EverbladedHeaven : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.EverbladedHeaven);

    public override void SetStaticDefaults()
    {
        ItemID.Sets.ItemsThatAllowRepeatedRightClick[Item.type] = true;
        ItemID.Sets.AnimatesAsSoul[Item.type] = true;
        Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(5, 6, false));
    }

    public override void SetDefaults()
    {
        Item.useStyle = ItemUseStyleID.Swing;
        Item.useTime = Item.useAnimation = 60;
        Item.damage = 3340;
        Item.knockBack = 4.5f;
        Item.width = 214;
        Item.height = 200;
        Item.useTurn = true;
        Item.UseSound = null;
        Item.rare = ModContent.RarityType<BloodWroughtRarity>();
        Item.value = AdditionsGlobalItem.UniqueRarityPrice;
        Item.DamageType = DamageClass.Melee;
        Item.shoot = ModContent.ProjectileType<EverbladedSwing>();
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.autoReuse = true;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(Color.Crimson);
    }

    public override bool CanShoot(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;

    public override bool AltFunctionUse(Player player) => true;

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        EverbladedSwing swing = Main.projectile[player.NewPlayerProj(position, velocity, type, damage, 0f, player.whoAmI)].As<EverbladedSwing>();
        if (player.Additions().SafeMouseRight.Current)
            swing.CurrentPhase = EverbladedSwing.Phase.VisceralSlice;

        return false;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<TripleKatanas>(), 1);
        recipe.AddIngredient(ModContent.ItemType<Mimicry>(), 1);
        recipe.AddIngredient(ModContent.ItemType<ShadowspecBar>(), 10);
        recipe.AddTile(TileID.LunarCraftingStation);
        recipe.Register();
    }
}