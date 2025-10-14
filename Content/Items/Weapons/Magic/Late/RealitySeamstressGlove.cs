using CalamityMod.Items.Materials;
using CalamityMod.Tiles.Furniture.CraftingStations;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Early;
using TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Magic.Late.Zenith;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Late;

public class RealitySeamstressesGlove : ModItem, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.RealitySeamstressesGlove);

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(Color.Lerp(new Color(255, 217, 236), Color.Violet * 1.1f, (float)Math.Sin(Main.GlobalTimeWrappedHourly)));
    }

    public override void SetDefaults()
    {
        Item.damage = 1500;
        Item.DamageType = DamageClass.Magic;
        Item.noUseGraphic = true;
        Item.channel = true;
        Item.mana = 10;
        Item.width = 60;
        Item.height = 63;
        Item.useTime = 25;
        Item.useAnimation = 25;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        Item.knockBack = 5f;
        Item.shootSpeed = 9f;
        Item.shoot = ModContent.ProjectileType<SeamstressMagic>();
        Item.value = AdditionsGlobalItem.LegendaryRarityPrice;
        Item.rare = ModContent.RarityType<LegendaryRarity>();
    }

    public override void PostUpdate()
    {
        float brightness = Main.essScale * Main.rand.NextFloat(0.2f, .6f);
        Lighting.AddLight(Item.Center, 0.94f * brightness, 0.95f * brightness, 0.56f * brightness);

        Vector2 pos = Item.Hitbox.RandomRectangle();
        if (Main.rand.NextBool(10))
        {
            Vector2 vel = Vector2.Lerp(Main.rand.NextVector2Unit(0f, MathHelper.TwoPi), -Vector2.UnitY, 0.5f) * Main.rand.NextFloat(1.8f, 2.6f);
            float scale = Main.rand.NextFloat(0.85f, 1.15f);
            ParticleRegistry.SpawnSparkleParticle(pos, vel, 20, scale, Color.Purple, Color.BlueViolet, 2.5f, .1f * Main.rand.NextBool().ToDirectionInt());
        }
    }

    public void DrawBackAfterimage(SpriteBatch spriteBatch, Vector2 baseDrawPosition, Rectangle frame, float baseScale)
    {
        if (Item.velocity.X == 0f)
        {
            float pulse = AperiodicSin(Main.GlobalTimeWrappedHourly) * .5f + .5f;
            float num = MathHelper.Lerp(-0.3f, 1.2f, pulse);
            Color drawColor = Color.Lerp(Color.Purple, Color.MediumPurple, pulse);
            drawColor *= MathHelper.Lerp(0.35f, 0.67f, Convert01To010(pulse));
            float drawPositionOffset = num * baseScale * 36f;
            int amount = 8;
            for (int i = 0; i < amount; i++)
            {
                Vector2 drawPosition = baseDrawPosition + (MathHelper.TwoPi * i / amount).ToRotationVector2() * drawPositionOffset;
                spriteBatch.Draw(TextureAssets.Item[Item.type].Value, drawPosition, frame, drawColor, 0f, Vector2.Zero, baseScale, 0, 0f);
            }
        }
    }

    public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        float brightness = Main.essScale * Main.rand.NextFloat(0.9f, 1.1f);
        Lighting.AddLight(Item.Center, Color.Violet.ToVector3() * brightness);
        Rectangle frame = TextureAssets.Item[Item.type].Value.Frame(1, 1, 0, 0, 0, 0);
        DrawBackAfterimage(spriteBatch, Item.position - Main.screenPosition, frame, scale);
        return true;
    }

    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        Item.velocity.X = 0f;
        DrawBackAfterimage(spriteBatch, position - frame.Size() * 0.3f, frame, .5f);
        return true;
    }

    public override bool CanUseItem(Player player)
    {
        return player.ownedProjectileCounts[Item.shoot] <= 0;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        Projectile.NewProjectile((IEntitySource)(object)source, position, velocity, Item.shoot, damage, knockback, player.whoAmI, 0f, 0f, 0f);
        return false;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<BrewingStorms>(), 1);
        recipe.AddIngredient(ItemID.MeteorStaff, 1);
        recipe.AddIngredient(ModContent.ItemType<Acheron>(), 1);
        recipe.AddIngredient(ItemID.FairyQueenMagicItem, 1);
        recipe.AddIngredient(ModContent.ItemType<StarlessSea>(), 1);
        recipe.AddIngredient(ItemID.LunarFlareBook, 1);
        recipe.AddIngredient(ModContent.ItemType<PyroclasticVeil>(), 1);
        recipe.AddIngredient(ModContent.ItemType<Epidemic>(), 1);
        recipe.AddIngredient(ModContent.ItemType<CometStorm>(), 1);
        recipe.AddIngredient(ItemID.Silk, 14);
        recipe.AddIngredient(ModContent.ItemType<CosmiliteBar>(), 5);
        recipe.AddIngredient(ModContent.ItemType<AscendantSpiritEssence>(), 10);
        recipe.AddIngredient(ModContent.ItemType<AuricBar>(), 5);
        recipe.AddTile(ModContent.TileType<CosmicAnvil>());
        recipe.Register();
    }
}