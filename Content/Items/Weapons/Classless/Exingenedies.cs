using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Late;
using TheExtraordinaryAdditions.Content.Items.Weapons.Melee;
using TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Late;
using TheExtraordinaryAdditions.Content.Items.Weapons.Summoner.Late;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Late;
using TheExtraordinaryAdditions.Content.Rarities;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics.Resources;
using TheExtraordinaryAdditions.Core.Graphics.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using TextSnippet = TheExtraordinaryAdditions.Core.Graphics.Systems.TextSnippet;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Classless;

public class Exingenedies : ModItem
{
    public override string Texture => AssetRegistry.GennedTextures.Invisible.Path;

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
        Item.knockBack = 0f;
        Item.shootSpeed = 0f;
        Item.shoot = ModContent.ProjectileType<TheExingendies>();
        Item.rare = ModContent.RarityType<FractallineRarity>();
        Item.value = 0;
    }

    public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
    {
        damage.Flat = Main.rand.Next(100, 50000);
    }

    public override void UpdateInventory(Player player)
    {
        Item.crit = Main.rand.Next(1, 99);
        Item.knockBack = 0f;
        Item.useTime = Item.useAnimation = Main.rand.Next(2, 25);
        Item.value = Main.rand.Next(0, int.MaxValue / 2);

        if (Keys.LeftShift.Current())
            TooltipCounter++;
        if (!Keys.LeftShift.Current())
            TooltipCounter = 0;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.FirstOrDefault(n => n.Name == "Knockback")?.Text = tooltips.FirstOrDefault(n => n.Name == "Knockback")
            ?.Text
            .Replace("No", "?");
        tooltips.DrawHeldShiftTooltip([
            new TooltipLine(Name, this.GetLocalization("Shift").Value),
        ]);
    }

    public static int TooltipCounter;
    public const int TotalTime = 120;

    public override bool PreDrawTooltipLine(DrawableTooltipLine line, ref int yOffset)
    {
        if (line.Name == "Exingenedies")
        {
            float completion = InverseLerp(0f, TotalTime, TooltipCounter);

            Vector2 drawOffset = Vector2.UnitY * yOffset;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.SamplerStateForCursor,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);

            string text = this.GetLocalization("Shift").Value;
            Vector2 textPosition = new Vector2(line.X, line.Y) + drawOffset;

            TextBlock block = new([
                new TextSnippet(text, 1f, Color.White, TextSnippet.AppearFadingFromRight,
                    TextSnippet.WaveDisplacement(2.2f))
            ])
            {
                AnimationCompletion = completion
            };
            const float width = 300f;
            block.ApplyWordWrap(width);

            Vector2 size = new Vector2(width, line.Font.MeasureString(text).Y);
            const int amt = 12;
            for (int i = 0; i < amt; i++)
            {
                Texture2D tex = AssetRegistry.GennedTextures.Pixel;
                Vector2 afterimageOffset = (MathHelper.TwoPi * i / amt).ToRotationVector2() * 10f;
                Main.spriteBatch.DrawBetterRect(tex,
                    ToTarget(new Vector2(line.X, line.Y) + drawOffset + afterimageOffset + Main.screenPosition, size),
                    null, Color.Black * .1f, 0f, Vector2.Zero);
            }

            ManagedShader displace = AssetRegistry.GennedShaders.NoiseDisplacement;
            displace.SetTexture(AssetRegistry.GennedTextures.Perlin, 1);
            displace.SetTexture(AssetRegistry.GennedTextures.noise, 2);
            displace.TrySetParameter("noiseIntensity", 6.67f);

            displace.TrySetParameter("color", Color.Transparent);
            displace.TrySetParameter("horizontalDisplacementFactor", 0.5104f + (1f - completion));
            displace.Render();
            block.Draw(textPosition + new Vector2(4f, 0f), MathF.Max(completion, .4f));
            displace.TrySetParameter("color", Color.White);
            displace.TrySetParameter("horizontalDisplacementFactor", 0.0104f + (1f - completion));
            displace.Render();
            block.Draw(textPosition, MathF.Max(completion, .4f));

            drawOffset.Y += size.X * line.BaseScale.X;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.SamplerStateForCursor,
                DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);

            return false;
        }

        return true;
    }

    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor,
        Color itemColor, Vector2 origin, float scale)
    {
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, null, null, null,
            Main.UIScaleMatrix);

        ManagedShader shader = AssetRegistry.GennedShaders.GenediesFlame;
        shader.TrySetParameter("Time", Main.GlobalTimeWrappedHourly);
        shader.SetTexture(AssetRegistry.GennedTextures.MeltNoise, 0, SamplerState.AnisotropicWrap);
        shader.Render();

        Texture2D tex = AssetRegistry.GennedTextures.Pixel;
        Main.spriteBatch.Draw(tex, position, null, Color.White, 0f, tex.Size() / 2f, 60f, 0, 0f);

        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, Main.UIScaleMatrix);
        return false;
    }

    public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation,
        ref float scale, int whoAmI)
    {
        ManagedShader shader = AssetRegistry.GennedShaders.GenediesFlame;
        shader.TrySetParameter("Time", Main.GlobalTimeWrappedHourly);
        shader.SetTexture(AssetRegistry.GennedTextures.MeltNoise, 0, SamplerState.AnisotropicWrap);

        ScreenShaderUpdates.QueueDrawAction(draw, BlendState.AlphaBlend, shader);
        return false;

        void draw()
        {
            Texture2D tex = AssetRegistry.GennedTextures.Pixel;
            Main.spriteBatch.Draw(tex, Item.position - Main.screenPosition, null, Color.White, 0f, tex.Size() / 2f,
                500f, 0, 0f);
        }
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
        CreateRecipe()
            .AddIngredient<GlareOfAlsafi>()
            .AddIngredient<UnparalleledCoalescence>()
            .AddIngredient<RealitySeamstressesGlove>()
            .AddIngredient<DeepestNadir>()
            .Register();
    }
}
