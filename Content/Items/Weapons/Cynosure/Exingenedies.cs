using CalamityMod.Items.Materials;
using CalamityMod.Tiles;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.Cynosure;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Cynosure;

public class Exingenedies : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Invisible);
    public override string LocalizationCategory => "Content.Items.Weapons.Cynosure";

    public override void SetStaticDefaults()
    {
        ItemID.Sets.GamepadWholeScreenUseRange[Type] = true;
        ItemID.Sets.ItemNoGravity[Item.type] = true;
    }

    public override void SetDefaults()
    {
        Item.damage = 1250;
        Item.crit = 1000;
        Item.DamageType = DamageClass.Default;
        Item.noUseGraphic = Item.channel = Item.noMelee = true;
        Item.width = Item.height = 1;
        Item.useTime = Item.useAnimation = 25;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.knockBack = .1f;
        Item.shootSpeed = 9f;
        Item.shoot = ModContent.ProjectileType<TheExingendies>();
        Item.rare = ModContent.RarityType<PrimordialRarity>();
        Item.value = AdditionsGlobalItem.LegendaryRarityPrice;
    }

    public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
    {
        damage.Flat = Main.rand.Next(100, 50000);
    }

    public override void UpdateInventory(Player player)
    {
        Item.crit = Main.rand.Next(1, 99);
        Item.knockBack = Main.rand.NextFloat(2f, 3f);
        Item.useTime = Item.useAnimation = Main.rand.Next(2, 25);
        Item.value = Main.rand.Next(0, int.MaxValue / 2);
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.DrawHeldShiftTooltip([new(Name, this.GetLocalization("Shift").Format(AdditionsKeybinds.SetBonusHotKey.TooltipHotkeyString(), AdditionsKeybinds.MiscHotKey.TooltipHotkeyString()))]);
        tooltips.IntegrateHotkey(AdditionsKeybinds.MiscHotKey, "[KEY]");
        tooltips.IntegrateHotkey(AdditionsKeybinds.SetBonusHotKey, "[KEY2]");
    }

    public override bool PreDrawTooltipLine(DrawableTooltipLine line, ref int yOffset)
    {
        if (line.Name == "Tooltip1")
        {
            Vector2 drawOffset = Vector2.UnitY * yOffset;
            drawOffset.X += DrawLine(line, drawOffset, "");

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.SamplerStateForCursor, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);

            // Apply glitch effects to a sentence about similar themes of the garden
            ManagedShader displace = ShaderRegistry.NoiseDisplacement;
            displace.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.Perlin), 1);
            displace.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.noise), 2);
            displace.TrySetParameter("color", Color.SlateBlue);
            displace.TrySetParameter("noiseIntensity", 2.67f);
            displace.TrySetParameter("horizontalDisplacementFactor", 0.0204f);
            displace.Render();

            drawOffset.Y += DrawLine(line, drawOffset, this.GetLocalization("Line").Value);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.SamplerStateForCursor, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);

            return false;
        }
        return true;
    }

    public static float DrawLine(DrawableTooltipLine line, Vector2 drawOffset, string text)
    {
        Color textOuterColor = Color.Black;

        // Get the text of the tooltip line
        Vector2 textPosition = new Vector2(line.X, line.Y) + drawOffset;

        // Get an offset to the afterimageOffset based on a sine wave
        float sine = (float)((1f + Math.Sin(Main.GlobalTimeWrappedHourly * 2.5f)) * 0.5f);
        float sineOffset = MathHelper.Lerp(0.4f, 0.775f, sine);

        // Draw text backglow effects
        for (int i = 0; i < 12; i++)
        {
            Vector2 afterimageOffset = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * (2f * sineOffset);
            ChatManager.DrawColorCodedString(Main.spriteBatch, line.Font, text, (textPosition + afterimageOffset).RotatedBy(MathHelper.TwoPi * (i / 12)), textOuterColor * 0.9f, line.Rotation, line.Origin, line.BaseScale);
        }
        ChatManager.DrawColorCodedString(Main.spriteBatch, line.Font, text, textPosition, Color.DarkSlateBlue, line.Rotation, line.Origin, line.BaseScale);

        return line.Font.MeasureString(text).X * line.BaseScale.X;
    }

    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, null, null, null, Main.UIScaleMatrix);

        ManagedShader shader = AssetRegistry.GetShader("GenediesFlame");
        shader.TrySetParameter("Time", Main.GlobalTimeWrappedHourly);
        shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.MeltNoise), 0, SamplerState.AnisotropicWrap);
        shader.Render();

        Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.Pixel);
        Main.spriteBatch.Draw(tex, position, null, Color.White, 0f, tex.Size() / 2f, 60f, 0, 0f);

        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, Main.UIScaleMatrix);
        return false;
    }

    public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        void draw()
        {
            Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.Pixel);
            Main.spriteBatch.Draw(tex, Item.position - Main.screenPosition, null, Color.White, 0f, tex.Size() / 2f, 500f, 0, 0f);
        }
        ManagedShader shader = AssetRegistry.GetShader("GenediesFlame");
        shader.TrySetParameter("Time", Main.GlobalTimeWrappedHourly);
        shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.MeltNoise), 0, SamplerState.AnisotropicWrap);

        ScreenShaderUpdates.QueueDrawAction(draw, BlendState.AlphaBlend, shader);
        return false;
    }

    public override void PostUpdate()
    {
        ParticleRegistry.SpawnBlurParticle(Item.Center, 20, .6f, 400f);
    }

    public override bool AllowPrefix(int pre) => false;
    public override bool AltFunctionUse(Player player) => false;
    public override bool CanShoot(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<DivineSpiritCatalyst>(), 1);
        recipe.AddIngredient(ModContent.ItemType<ShadowspecBar>(), 10);
        recipe.AddTile(ModContent.TileType<PlacedRock>());
        recipe.Register();
    }
}