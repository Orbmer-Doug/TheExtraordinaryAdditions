using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Early;
using TheExtraordinaryAdditions.Content.Projectiles.Multi.Middle;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Multi.Middle;

public class BoneGunsword : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.BoneGunsword);
    public override void SetStaticDefaults()
    {
        ItemID.Sets.ItemsThatAllowRepeatedRightClick[Item.type] = true;
    }
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(166, 166, 166));
    }
    public override void SetDefaults()
    {
        Item.damage = 50;
        Item.DamageType = DamageClass.Generic;
        Item.width = 99;
        Item.height = 20;
        Item.useTime = Item.useAnimation = 25;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.knockBack = 4f;
        Item.value = AdditionsGlobalItem.RarityLightRedBuyPrice;
        Item.rare = ItemRarityID.LightRed;
        Item.UseSound = SoundID.Item1;

        Item.autoReuse = true;
        Item.noUseGraphic = true;
        Item.noMelee = true;

        Item.useAmmo = AmmoID.Bullet;
        Item.shoot = ModContent.ProjectileType<GunSwordSword>();
        Item.shootSpeed = 12f;
    }

    public override bool AltFunctionUse(Player player) => player.GetModPlayer<GunSwordPlayer>().Cooldown == 0;
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.altFunctionUse == 2)
            player.NewPlayerProj(position, velocity, ModContent.ProjectileType<GunGunSword>(), damage, knockback, player.whoAmI);
        else
            player.NewPlayerProj(position, velocity, ModContent.ProjectileType<GunSwordSword>(), damage, knockback, player.whoAmI);

        return false;
    }

    public override bool MeleePrefix() => true;
    public override bool RangedPrefix() => true;

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.SoulofNight, 8);
        recipe.AddIngredient(ItemID.IllegalGunParts, 1);
        recipe.AddIngredient(ItemID.BoneSword, 1);
        recipe.AddIngredient(ModContent.ItemType<BoneFlintlock>(), 1);
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.Register();
    }
}