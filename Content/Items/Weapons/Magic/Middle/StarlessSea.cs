using CalamityMod.Items.Materials;
using CalamityMod.Items.Placeables;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Magic.Middle;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Middle;

public class StarlessSea : ModItem
{
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        TooltipLine t1 = new(Mod, Name, this.GetLocalization("Tooltip").Value) { OverrideColor = new Color(64, 105, 247) };
        TooltipLine t2 = new(Mod, Name, this.GetLocalization("Tooltip2").Value) { OverrideColor = new Color(227, 57, 48) };

        tooltips.ModifyTooltip(Main.bloodMoon ? [t2] : [t1], true);
    }

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
        Item.damage = 115;
        Item.knockBack = 2f;
        Item.noMelee = true;
        Item.value = AdditionsGlobalItem.RarityCyanBuyPrice;
        Item.shoot = ModContent.ProjectileType<StarlessHoldout>();
        Item.shootSpeed = 4f;
        Item.mana = 20;
    }
    public override bool CanShoot(Player player) => false;

    public static readonly Texture2D Fracture = AssetRegistry.GetTexture(AdditionsTexture.BloodFracture);
    public static readonly Texture2D Starless = AssetRegistry.GetTexture(AdditionsTexture.StarlessSea);
    public override string Texture => Main.bloodMoon ? AssetRegistry.GetTexturePath(AdditionsTexture.BloodFracture) : AssetRegistry.GetTexturePath(AdditionsTexture.StarlessSea);
    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frameI, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        if (Main.bloodMoon)
            spriteBatch.Draw(Fracture, position, null, Color.White, 0f, origin, scale, 0, 0f);

        else
            spriteBatch.Draw(Starless, position, null, Color.White, 0f, origin, scale, 0, 0f);

        return false;
    }

    public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        if (Main.bloodMoon)
            spriteBatch.Draw(Fracture, Item.position - Main.screenPosition, null, lightColor, 0f, Vector2.Zero, 1f, 0, 0f);

        else
            spriteBatch.Draw(Starless, Item.position - Main.screenPosition, null, lightColor, 0f, Vector2.Zero, 1f, 0, 0f);

        return false;
    }
    public override void UpdateInventory(Player player)
    {
        if (Main.bloodMoon)
            Item.SetNameOverride("Blood Fracture");
        else
            Item.SetNameOverride("Starless Sea");
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.WaterBolt, 1);
        recipe.AddIngredient(ItemID.AquaScepter, 1);
        recipe.AddIngredient(ItemID.Ectoplasm, 12);
        recipe.AddIngredient(ItemID.LihzahrdBrick, 25);
        recipe.AddIngredient(ItemID.RainCloud, 100);
        recipe.AddIngredient(ModContent.ItemType<Lumenyl>(), 14);
        recipe.AddIngredient(ModContent.ItemType<AbyssGravel>(), 150);
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.Register();
    }
}