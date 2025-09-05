using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Late.Zenith;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Late;

public class FinalStrike : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.FinalStrike);
    public override void SetDefaults()
    {
        Item.width = 138;
        Item.height = 140;
        Item.damage = 720;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.channel = true;
        Item.useAnimation = 50;
        Item.useTime = 50;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.knockBack = 9f;
        Item.UseSound = SoundID.Item1;
        Item.autoReuse = true;
        Item.shoot = ModContent.ProjectileType<FinalStrikeHoldout>();
        Item.shootSpeed = 20f;
        Item.DamageType = DamageClass.Melee;

        Item.value = AdditionsGlobalItem.LegendaryRarityPrice;
        Item.rare = ModContent.RarityType<LegendaryRarity>();
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(235, 64, 52));
    }
    public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;
    public override bool AltFunctionUse(Player player) => true;
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.altFunctionUse != 2)
        {
            Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<FinalStrikeHoldout>(), damage, knockback, player.whoAmI);
            return false;
        }
        Projectile spear = Main.projectile[Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<FinalStrikeHoldout>(), damage * 2, knockback, player.whoAmI)];
        spear.As<FinalStrikeHoldout>().CurrentState = FinalStrikeHoldout.FinalStrikeState.Stab;

        return false;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        if (ModLoader.TryGetMod("calamityMod", out Mod calamityMod) && calamityMod.TryFind("AuricBar", out ModItem AuricBar) && calamityMod.TryFind("CosmicAnvil", out ModTile CosmicAnvil) && calamityMod.TryFind("AscendantSpiritEssence", out ModItem AscendantSpiritEssence) && calamityMod.TryFind("CosmiliteBar", out ModItem CosmiliteBar) && calamityMod.TryFind("DubiousPlating", out ModItem DubiousPlating))
        {
            recipe.AddIngredient(ItemID.Spear, 1);
            recipe.AddIngredient(ItemID.DarkLance, 1);
            recipe.AddIngredient(ModContent.ItemType<DecayingCutlery>(), 1);
            recipe.AddIngredient(ItemID.Gungnir, 1);
            recipe.AddIngredient(ItemID.NorthPole, 1);
            recipe.AddIngredient(ItemID.PiercingStarlight, 1);
            recipe.AddIngredient(ModContent.ItemType<Sangue>(), 1);
            recipe.AddIngredient(ItemID.DayBreak, 1);
            recipe.AddIngredient(ModContent.ItemType<AbyssalCurrents>(), 1);
            recipe.AddIngredient(ModContent.ItemType<CondereFulmina>(), 1);
            recipe.AddIngredient(AscendantSpiritEssence.Type, 10);
            recipe.AddIngredient(AuricBar.Type, 7);
            recipe.AddTile(CosmicAnvil.Type);
        }
        else
        {
            recipe.AddIngredient(ItemID.Spear, 1);
            recipe.AddIngredient(ItemID.DarkLance, 1);
            recipe.AddIngredient(ItemID.Gungnir, 1);
            recipe.AddIngredient(ItemID.NorthPole, 1);
            recipe.AddIngredient(ItemID.PiercingStarlight, 1);
            recipe.AddIngredient(ModContent.ItemType<Sangue>(), 1);
            recipe.AddIngredient(ItemID.DayBreak, 1);
            recipe.AddIngredient(ModContent.ItemType<AbyssalCurrents>(), 1);
            recipe.AddIngredient(ModContent.ItemType<CondereFulmina>(), 1);
            recipe.AddTile(TileID.LunarCraftingStation);
        }
        recipe.Register();
    }

}
