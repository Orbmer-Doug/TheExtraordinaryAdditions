using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Magic.Middle;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Middle;

public class StarlessSea : ModItem
{
    public static readonly Texture2D Fracture = AssetRegistry.GetTexture(AdditionsTexture.BloodFracture);
    public static readonly Texture2D Starless = AssetRegistry.GetTexture(AdditionsTexture.StarlessSea);

    public override string Texture => Main.bloodMoon
        ? AssetRegistry.GetTexturePath(AdditionsTexture.BloodFracture)
        : AssetRegistry.GetTexturePath(AdditionsTexture.StarlessSea);

    public override void SetDefaults()
    {
        Item.width = 38;
        Item.height = 48;
        Item.rare = ItemRarityID.Cyan;
        Item.useTime = Item.useAnimation = 60;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.autoReuse = true;
        Item.noUseGraphic = true;
        Item.UseSound = null;
        Item.DamageType = DamageClass.Magic;
        Item.damage = 102;
        Item.knockBack = 2f;
        Item.noMelee = true;
        Item.value = AdditionsGlobalItem.RarityCyanBuyPrice;
        Item.shoot = ModContent.ProjectileType<StarlessHoldout>();
        Item.shootSpeed = 4f;
        Item.mana = 3;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        TooltipLine t1 = new(Mod, Name, this.GetLocalization("Tooltip").Value)
            { OverrideColor = new Color(64, 105, 247) };
        TooltipLine t2 = new(Mod, Name, this.GetLocalization("Tooltip2").Value)
            { OverrideColor = new Color(227, 57, 48) };

        tooltips.ModifyTooltip(Main.bloodMoon ? [t2] : [t1], true);
    }

    public override bool CanShoot(Player player)
    {
        return false;
    }

    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frameI,
        Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        spriteBatch.Draw(Main.bloodMoon ? Fracture : Starless, position, null, Color.White, 0f, origin, scale, 0, 0f);
        return false;
    }

    public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation,
        ref float scale, int whoAmI)
    {
        spriteBatch.Draw(Main.bloodMoon ? Fracture : Starless, Item.position - Main.screenPosition, null, lightColor,
            0f, Vector2.Zero, 1f, 0, 0f);
        return false;
    }

    public override void UpdateInventory(Player player)
    {
        Item.SetNameOverride(Main.bloodMoon ? "Blood Fracture" : "Starless Sea");
    }

    public override void AddRecipes()
    {
        var recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.WaterBolt);
        recipe.AddIngredient(ItemID.AquaScepter);
        recipe.AddIngredient(ItemID.Ectoplasm, 12);
        recipe.AddIngredient(ItemID.LihzahrdBrick, 25);
        recipe.AddIngredient(ItemID.RainCloud, 100);
        //TODO
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.Register();
    }
}
