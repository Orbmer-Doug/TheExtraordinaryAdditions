using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Materials.Middle;

public class WrithingLight : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.WrithingLight);
    
    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 10;
        Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(12, 7, false));
        ItemID.Sets.SortingPriorityMaterials[Type] = 122;
        ItemID.Sets.ItemNoGravity[Item.type] = true;

        ItemID.Sets.AnimatesAsSoul[Type] = true;
    }

    public override void SetDefaults()
    {
        Item.width = 32;
        Item.height = 32;
        Item.rare = ItemRarityID.Red;

        Item.maxStack = Item.CommonMaxStack;
        Item.value = Item.buyPrice(gold: 5);
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(255, 223, 199));
    }

    public override void PostUpdate()
    {
        float brightness = Main.essScale * Main.rand.NextFloat(0.9f, 1.1f);
        Lighting.AddLight(Item.Center, 0.94f * brightness, 0.95f * brightness, 0.56f * brightness);
        if (Main.rand.NextBool(3))
        {
            Dust obj = Dust.NewDustPerfect(Item.Hitbox.RandomRectangle(), DustID.Torch, Vector2.UnitY.RotatedByRandom(.2f) * -Main.rand.NextFloat(2f, 6f), 0, default, 1f);
            obj.velocity = Vector2.Lerp(Main.rand.NextVector2Unit(0f, MathHelper.TwoPi), -Vector2.UnitY, 0.5f) * Main.rand.NextFloat(1.8f, 2.6f);
            obj.scale *= Main.rand.NextFloat(0.85f, 1.15f);
            obj.fadeIn = 0.9f;
            obj.noGravity = true;
        }
    }

    public void DrawBackAfterimage(SpriteBatch spriteBatch, Vector2 baseDrawPosition, Rectangle frame, float baseScale)
    {
        float pulse = (float)Math.Cos(MathHelper.PiOver2 * Main.GlobalTimeWrappedHourly * 2f) + (float)Math.Cos(Math.E * Main.GlobalTimeWrappedHourly * 1.7);
        pulse = pulse * 0.25f + 0.5f;
        pulse = (float)Math.Pow(pulse, 3.0);
        float num = MathHelper.Lerp(-0.3f, 1.2f, pulse);
        Color drawColor = Color.Lerp(Color.Orange, Color.OrangeRed, pulse);
        drawColor *= MathHelper.Lerp(0.35f, 0.67f, Convert01To010(pulse));

        float drawPositionOffset = num * baseScale * 8f;
        for (int i = 0; i < 8; i++)
        {
            Vector2 drawPosition = baseDrawPosition + (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 3f;
            spriteBatch.Draw(TextureAssets.Item[Item.type].Value, drawPosition, (Rectangle?)frame, drawColor, 0f, Vector2.Zero, baseScale, 0, 0f);
        }
    }

    public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        float brightness = Main.essScale * Main.rand.NextFloat(0.9f, 1.1f);
        Lighting.AddLight(Item.Center, 1.2f * brightness, 0.4f * brightness, 0.8f);
        Rectangle frame = Main.itemAnimations[Type].GetFrame(TextureAssets.Item[Type].Value);
        DrawBackAfterimage(spriteBatch, Item.position - Main.screenPosition, frame, scale);
        return true;
    }

    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        Item.velocity.X = 0f;
        DrawBackAfterimage(spriteBatch, position - frame.Size() * 0.5f, frame, scale);
        return true;
    }
}