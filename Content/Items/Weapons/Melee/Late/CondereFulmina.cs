using CalamityMod.Items.Materials;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Late.Fulmina;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Late;

public class CondereFulmina : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CondereFulmina);
    public override void SetStaticDefaults()
    {
        ItemID.Sets.ItemsThatAllowRepeatedRightClick[Item.type] = true;
    }

    public override void Update(ref float gravity, ref float maxFallSpeed)
    {
        if (TimeSystem.UpdateCount % 4 == 3)
            ParticleRegistry.SpawnLightningArcParticle(Vector2.Lerp(Item.BottomLeft, Item.TopRight, Main.rand.NextFloat()),
                Main.rand.NextVector2CircularLimited(100f, 100f, .9f, 1.4f), 10, Main.rand.NextFloat(.4f, .8f), Color.Cyan);
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(143, 212, 255));
    }

    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        DrawInventoryCustomScale(spriteBatch, TextureAssets.Item[Type].Value, position, frame, drawColor, itemColor, origin, scale, 0.2f, new Vector2(0f, 0f));
        return false;
    }

    public override void SetDefaults()
    {
        Item.rare = ItemRarityID.Purple;
        Item.value = AdditionsGlobalItem.RarityPurpleBuyPrice;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.useAnimation =
        Item.useTime = 30;
        Item.UseSound = SoundID.Item1;
        Item.autoReuse = true;
        Item.consumable = false;
        Item.damage = 1300;
        Item.knockBack = 0f;
        Item.noUseGraphic = true;
        Item.noMelee = true;
        Item.DamageType = DamageClass.Melee;
        Item.crit = 15;
        Item.width = Item.height = 184;
        Item.shootSpeed = 15f;
        Item.shoot = ModContent.ProjectileType<CondereFulminaHoldout>();
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.MagnetSphere, 1);
        recipe.AddIngredient(ItemID.FragmentVortex, 16);
        recipe.AddIngredient(ModContent.ItemType<StormlionMandible>(), 5);
        recipe.AddIngredient(ModContent.ItemType<ArmoredShell>(), 7);
        recipe.AddTile(TileID.LunarCraftingStation);
        recipe.Register();
    }
}