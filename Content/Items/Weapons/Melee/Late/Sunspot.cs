using CalamityMod.Items.Materials;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Materials.Middle;
using TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Late;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Late;

public class Sunspot : ModItem, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Sunspot);
    public const int Damage = 1480;
    public override void SetDefaults()
    {
        Item.damage = Damage;
        Item.DamageType = DamageClass.MeleeNoSpeed;
        Item.width = 30;
        Item.height = 242;
        Item.useTime = 24;
        Item.useAnimation = 24;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.useTurn = false;
        Item.knockBack = 7f;
        Item.noUseGraphic = true;
        Item.value = AdditionsGlobalItem.LaserRarityPrice;
        Item.rare = ModContent.RarityType<LaserClassRarity>();
        Item.UseSound = SoundID.Item8;
        Item.autoReuse = true;
        Item.shoot = ModContent.ProjectileType<SunspotSword>();
        Item.noMelee = true;
    }

    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        DrawInventoryCustomScale(spriteBatch, TextureAssets.Item[Type].Value, position, frame, drawColor, itemColor, origin, scale, .2f, new Vector2(0f, 0f));
        return false;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        return false;
    }

    public override void HoldItem(Player player)
    {
        if (player.ownedProjectileCounts[Item.shoot] == 0)
            Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, Vector2.Zero, Item.shoot, Item.damage, Item.knockBack, player.whoAmI);

        base.HoldItem(player);
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.FirstOrDefault(n => n.Name == "Damage").Text = tooltips.FirstOrDefault(n => n.Name == "Damage").Text.Replace("damage", "damage swung");
        tooltips.ColorLocalization(new(255, 72, 31));
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<SolarBrand>(), 1);
        recipe.AddIngredient(ModContent.ItemType<PlasmaCore>(), 1);
        recipe.AddIngredient(ModContent.ItemType<UnholyEssence>(), 20);
        recipe.AddIngredient(ModContent.ItemType<DivineGeode>(), 14);
        recipe.AddIngredient(ModContent.ItemType<MysteriousCircuitry>(), 15);
        recipe.AddIngredient(ModContent.ItemType<DubiousPlating>(), 15);
        recipe.AddTile(TileID.LunarCraftingStation);
        recipe.Register();
    }
}
