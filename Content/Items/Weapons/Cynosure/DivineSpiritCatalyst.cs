using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Late;
using TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Late;
using TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Late;
using TheExtraordinaryAdditions.Content.Items.Weapons.Summoner.Late;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Cynosure;

public class DivineSpiritCatalyst : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.DivineSpiritCatalyst);
    public override void Update(ref float gravity, ref float maxFallSpeed)
    {
        float brightness = Main.essScale * Utils.NextFloat(Main.rand, 0.9f, 1.1f);
        Lighting.AddLight(Item.Center, 0.94f * brightness, 0.95f * brightness, 0.56f * brightness);
    }
    public void DrawBackAfterimage(SpriteBatch spriteBatch, Vector2 baseDrawPosition, Rectangle frame, float baseScale)
    {
        if (Item.velocity.X == 0f)
        {
            float pulse = (float)Math.Cos(1.618034f * Main.GlobalTimeWrappedHourly * 2f) + (float)Math.Cos(Math.E * Main.GlobalTimeWrappedHourly * 1.7000000476837158);
            pulse = pulse * 0.25f + 0.5f;
            pulse = (float)Math.Pow(pulse, 3.0);
            float num = MathHelper.Lerp(-0.3f, 1.2f, pulse);
            Color drawColor = Color.Lerp(Color.Violet, Color.DarkViolet, pulse);
            drawColor *= MathHelper.Lerp(0.35f, 0.67f, Convert01To010(pulse));
            float drawPositionOffset = num * baseScale * 8f;
            for (int i = 0; i < 3; i++)
            {
                Vector2 drawPosition = baseDrawPosition + Utils.ToRotationVector2(MathHelper.TwoPi * i / 8f) * drawPositionOffset;
                spriteBatch.Draw(TextureAssets.Item[this.Item.type].Value, drawPosition, (Rectangle?)frame, drawColor, 0f, Vector2.Zero, baseScale, 0, 0f);
            }
        }
    }

    public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        float brightness = Main.essScale * Utils.NextFloat(Main.rand, 0.9f, 1.1f);
        Lighting.AddLight(Item.Center, 1.2f * brightness, 0.4f * brightness, 0.8f);
        Rectangle frame = Utils.Frame(TextureAssets.Item[this.Item.type].Value, 1, 6, 0, 0, 0, 0);
        DrawBackAfterimage(spriteBatch, Item.position - Main.screenPosition, frame, scale);
        return true;
    }

    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        Item.velocity.X = 0f;
        DrawBackAfterimage(spriteBatch, position - Utils.Size(frame) * 0.5f, frame, scale);
        return true;
    }

    public override string LocalizationCategory => "Content.Items.Weapons.Cynosure";

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(Color.Lerp(Color.DarkSlateBlue * 1.15f, Color.DarkSlateGray * 1.1f, (float)Math.Sin(Main.GlobalTimeWrappedHourly)));
    }

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 1;
        Main.RegisterItemAnimation(this.Item.type, new DrawAnimationVertical(10, 6, false));
        ItemID.Sets.SortingPriorityMaterials[this.Type] = 122;
        ItemID.Sets.ItemNoGravity[this.Item.type] = true;

        ItemID.Sets.AnimatesAsSoul[Type] = true;
    }

    public override void SetDefaults()
    {
        Item.width = 31;
        Item.height = 35;
        Item.rare = ModContent.RarityType<PrimordialRarity>();

        Item.maxStack = Item.CommonMaxStack;
        Item.value = Item.buyPrice(platinum: 5);
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        if (ModLoader.TryGetMod("calamityMod", out Mod calamityMod) && calamityMod.TryFind("DraedonsForge", out ModTile DraedonsForge))
        {
            recipe.AddIngredient(ModContent.ItemType<FinalStrike>(), 1);
            recipe.AddIngredient(ModContent.ItemType<UnparalleledCoalescence>(), 1);
            recipe.AddIngredient(ModContent.ItemType<RealitySeamstressesGlove>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DeepestNadir>(), 1);
            recipe.AddTile(DraedonsForge.Type);
        }
        else
        {
            recipe.AddIngredient(ModContent.ItemType<FinalStrike>(), 1);
            recipe.AddIngredient(ModContent.ItemType<UnparalleledCoalescence>(), 1);
            recipe.AddIngredient(ModContent.ItemType<RealitySeamstressesGlove>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DeepestNadir>(), 1);
            recipe.AddTile(TileID.LunarCraftingStation);
        }
        recipe.Register();

    }


}
