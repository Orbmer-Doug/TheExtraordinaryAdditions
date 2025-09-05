using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;
using TheExtraordinaryAdditions.UI;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Late;

public class TesselesticMeltdown : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TesselesticMeltdown);
    public override void SetStaticDefaults()
    {
        ItemID.Sets.ItemsThatAllowRepeatedRightClick[Item.type] = true;
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(Color.Cyan);
    }
    public override void SetDefaults()
    {
        Item.damage = 475;
        Item.DamageType = DamageClass.Magic;
        Item.width = 176;
        Item.height = 166;
        Item.useTime = 4;
        Item.useAnimation = 4;
        Item.knockBack = 0;
        Item.value = AdditionsGlobalItem.LegendaryRarityPrice;
        Item.rare = ModContent.RarityType<CyberneticRarity>();
        Item.UseSound = null;
        Item.autoReuse = true;
        Item.crit = 0;
        Item.mana = 4;
        Item.shootSpeed = 11f;
        Item.noUseGraphic = true;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.useAmmo = AmmoID.None;
    }
    public override bool CanShoot(Player player) => false;
    public override void PostUpdate()
    {
        // Make some visuals at the rune part of the cube
        Vector2 pos = new(Item.Center.X + 48f, Item.Center.Y - 60f);
        if (Main.GameUpdateCount % 4f == 3f)
        {
            ParticleRegistry.SpawnLightningArcParticle(pos, Main.rand.NextVector2Circular(120f, 120f), Main.rand.Next(12, 20), .7f, Color.DeepSkyBlue);
        }
        Lighting.AddLight(pos, Color.Cyan.ToVector3() * 1.1f);
    }
    public override void HoldItem(Player player)
    {
        TesselesticHeatUI.CurrentlyViewing = true;
    }
}