using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Magic.Middle;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Middle;
public class LanceOfSanguineSteels : ModItem, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.LanceOfSanguineSteels);

    public override void SetStaticDefaults()
    {
        Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(15, 5, false));
        Item.staff[Item.type] = true;
    }

    public override void SetDefaults()
    {
        Item.damage = 171;
        Item.DamageType = DamageClass.Magic;
        Item.mana = 7;
        Item.width = Item.height = 70;
        Item.useTime = Item.useAnimation = 43;
        Item.noUseGraphic = true;
        Item.channel = true;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        Item.knockBack = 5.75f;
        Item.UseSound = null;
        Item.autoReuse = true;
        Item.shoot = ModContent.ProjectileType<SanguineSteelsHoldout>();
        Item.shootSpeed = 16.5f;
        Item.rare = ModContent.RarityType<BloodWroughtRarity>();
        Item.value = AdditionsGlobalItem.RarityCyanBuyPrice;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(235, 64, 52));
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        Projectile.NewProjectile((IEntitySource)(object)source, position, Vector2.Zero, type, damage, knockback, player.whoAmI, 0f, 0f, 0f);
        return false;
    }

    public override bool CanUseItem(Player player)
    {
        return player.ownedProjectileCounts[Item.shoot] <= 0;
    }
}
