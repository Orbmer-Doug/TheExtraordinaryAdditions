using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Materials.Middle;

public class TremorAlloy : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TremorAlloy);

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 20;
    }

    public override void SetDefaults()
    {
        Item.width = 48;
        Item.height = 46;
        Item.rare = ItemRarityID.Cyan;

        Item.maxStack = Item.CommonMaxStack;
        Item.value = Item.buyPrice(gold: 1);
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(128, 86, 73));
    }

    public void DrawBackAfterimage(SpriteBatch spriteBatch, Vector2 baseDrawPosition, Rectangle frame, float baseScale)
    {
        if (Item.velocity.X == 0f)
        {
            float pulse = (float)Math.Cos(1.618034f * Main.GlobalTimeWrappedHourly * 2f) + (float)Math.Cos(Math.E * Main.GlobalTimeWrappedHourly * 1.7000000476837158);
            pulse = pulse * 0.25f + 0.5f;
            pulse = (float)Math.Pow(pulse, 3.0);
            Color drawColor = Color.Lerp(Color.Gray, Color.Tan, pulse);
            drawColor *= MathHelper.Lerp(0.35f, 0.67f, Convert01To010(pulse));

            Texture2D tex = TextureAssets.Item[Item.type].Value;

            float time = Main.GlobalTimeWrappedHourly * 5f % 10f / 10f;
            float scale = baseScale + time * 2f;
            spriteBatch.Draw(tex, baseDrawPosition, frame, drawColor * MathHelper.Lerp(0.7f, 0f, time), 0f, tex.Size() / 2f, scale, 0, 0f);
        }
    }

    public override void Update(ref float gravity, ref float maxFallSpeed)
    {
        maxFallSpeed = 20f;
    }

    public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        Rectangle frame = TextureAssets.Item[Item.type].Value.Frame(1, 1, 0, 0, 0, 0);
        DrawBackAfterimage(spriteBatch, Item.Center - Main.screenPosition, frame, scale);
        return true;
    }

    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        Item.velocity.X = 0f;
        DrawBackAfterimage(spriteBatch, position, frame, .2f);
        return true;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<MythicScrap>(), 1);
        recipe.AddIngredient(ModContent.ItemType<EmblazenedEmber>(), 1);
        recipe.AddTile(TileID.AdamantiteForge);
        recipe.Register();
    }
}
