using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Early;
using TheExtraordinaryAdditions.Content.Projectiles.Magic.Middle;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Middle;

public class VirulentEntrapment : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.VirulentEntrapment);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(23, 244, 23));
    }
    public override void SetStaticDefaults()
    {
        Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(10, 9, false));
        ItemID.Sets.AnimatesAsSoul[Type] = true;
    }

    public override void SetDefaults()
    {
        Item.damage = 158;
        Item.DamageType = DamageClass.Magic;
        Item.width = 42;
        Item.height = 51;
        Item.useTime = 26;
        Item.useAnimation = 26;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        Item.channel = true;
        Item.knockBack = 2f;
        Item.value = AdditionsGlobalItem.RarityLimeBuyPrice;
        Item.rare = ItemRarityID.Lime;
        Item.UseSound = SoundID.Grass;
        Item.autoReuse = true;
        Item.shoot = ModContent.ProjectileType<VirulentHoldout>();
        Item.shootSpeed = 16f;
        Item.mana = 3;
        Item.noUseGraphic = true;
    }

    public override void HoldItem(Player player)
    {
        player.Additions().SyncMouse = true;
    }

    public override bool CanUseItem(Player player)
    {
        return !Main.projectile.Any((n) => n.active && n.owner == player.whoAmI && n.type == ModContent.ProjectileType<VirulentHoldout>());
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<NoxiousSnare>(), 1);
        recipe.AddIngredient(ItemID.ChlorophyteBar, 10);
        recipe.AddIngredient(ItemID.Stinger, 12);
        recipe.AddIngredient(ItemID.MudBlock, 50);
        recipe.AddTile(TileID.Bookcases);
        recipe.Register();
    }
}