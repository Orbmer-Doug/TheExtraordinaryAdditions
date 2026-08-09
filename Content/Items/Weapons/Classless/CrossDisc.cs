using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.CrossCode;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Classless;

public class CrossDisc : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CrossDisc);

    public override void SetStaticDefaults()
    {
        ItemID.Sets.ItemsThatAllowRepeatedRightClick[Item.type] = true;
    }

    public override void SetDefaults()
    {
        Item.useStyle = ItemUseStyleID.Swing;
        Item.useAnimation = 43;
        Item.useTime = 43;
        Item.damage = 3000;
        Item.knockBack = 1f;
        Item.width = 32;
        Item.height = 32;
        Item.UseSound = null;
        Item.rare = ModContent.RarityType<CrosscodeRarity>();
        Item.value = Item.buyPrice(platinum: 1);
        Item.DamageType = DamageClass.Generic;
        Item.shoot = ModContent.ProjectileType<CrossDiscHoldout>();
        Item.shootSpeed = 30;
        Item.noMelee = true;
        Item.shootsEveryUse = true;
        Item.autoReuse = true;
        Item.noUseGraphic = true;
        Item.crit = 0;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        TooltipLine feelingiceolated = new TooltipLine(Mod, "CrossDisc", this.GetLocalization("FeelingIceolated").Value)
        {
            OverrideColor = new Color(194, 255, 246)
        };
        TooltipLine lightningreturns = new TooltipLine(Mod, "CrossDisc", this.GetLocalization("LightningReturns").Value)
        {
            OverrideColor = new Color(142, 47, 237)
        };
        TooltipLine playingwithfire = new TooltipLine(Mod, "CrossDisc", this.GetLocalization("PlayingWithFire").Value)
        {
            OverrideColor = new Color(237, 119, 0)
        };
        TooltipLine whatdoyoumeannotearth =
            new TooltipLine(Mod, "CrossDisc", this.GetLocalization("WhatDoYouMeanNotEarth").Value)
            {
                OverrideColor = new Color(61, 227, 83)
            };
        tooltips.DrawHeldShiftTooltip([feelingiceolated, lightningreturns, playingwithfire, whatdoyoumeannotearth]);
        tooltips.ColorLocalization(new Color(132, 173, 217));
    }

    public override void PostUpdate()
    {
        float brightness = Main.essScale * Main.rand.NextFloat(0.002f, .006f);
        Lighting.AddLight(Item.Center, 122 * brightness, 253 * brightness, 255 * brightness);
        if (Main.rand.NextBool(4))
        {
            Dust dust = Dust.NewDustPerfect(Item.RandAreaInEntity(), DustID.AncientLight,
                -Vector2.UnitY.RotatedByRandom(.5f) * Main.rand.NextFloat(2.8f, 3.4f), 0, Color.LightBlue,
                Main.rand.NextFloat(.85f, 1.15f));
            dust.fadeIn = .9f;
            dust.noGravity = true;
        }
    }

    public override bool CanShoot(Player player) => false;
    public override bool? UseItem(Player player) => false;

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity,
        int type, int damage, float knockback) => false;

    public override bool AllowPrefix(int pre) => false;

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.LightDisc, 1);
        recipe.AddIngredient(ItemID.RedDye, 1);
        recipe.AddIngredient(ItemID.Silk, 3);
        //TODO
        recipe.Register();
    }
}
