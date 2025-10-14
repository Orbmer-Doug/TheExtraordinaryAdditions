using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Middle;

[AutoloadEquip(EquipType.Wings)]
public class WingsOfTwilight : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.WingsOfTwilight);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(158, 149, 25));
    }

    public override void SetStaticDefaults()
    {
        ArmorIDs.Wing.Sets.Stats[Item.wingSlot] = new WingStats(150, 6f, 1.7f);
    }

    public override void SetDefaults()
    {
        Item.width = 24;
        Item.height = 34;
        Item.value = AdditionsGlobalItem.RarityPinkBuyPrice;
        Item.rare = ItemRarityID.Pink;
        Item.accessory = true;
        Item.defense = 5;
    }

    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        DamageClass dmg = player.HeldItem.DamageType;

        if (dmg == DamageClass.Melee)
        {
            player.GetArmorPenetration<MeleeDamageClass>() += 8;
            player.GetAttackSpeed<MeleeDamageClass>() += .08f;
        }
        if (dmg == DamageClass.Ranged)
            player.GetCritChance<RangedDamageClass>() += 10f;
        if (dmg == DamageClass.Magic)
            player.statManaMax2 += 50;
        if (dmg == DamageClass.Summon)
            player.GetDamage<SummonDamageClass>() += .08f;

        if (player.controlJump && player.wingTime > 0f && player.jump == 0 && player.velocity.Y != 0f && !hideVisual)
        {
            int xOff = 4;
            if (player.direction == 1)
                xOff = -40;
            Dust dust = Main.dust[Dust.NewDust(new Vector2(player.position.X + player.width / 2 + xOff, player.position.Y + player.height / 2 - 15f),
                30, 30, DustID.AmberBolt, 0f, 0f, 100, default, 1f)];
            dust.noGravity = true;
            dust.velocity *= 0.3f;
            if (Main.rand.NextBool(10))
                dust.fadeIn = 2f;
            dust.shader = GameShaders.Armor.GetSecondaryShader(player.cWings, player);
        }
        player.noFallDmg = true;
    }

    public override void VerticalWingSpeeds(Player player, ref float ascentWhenFalling, ref float ascentWhenRising,
        ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend)
    {
        ascentWhenFalling = 0.75f; // Falling glide speed
        ascentWhenRising = 0.45f; // Rising speed
        maxCanAscendMultiplier = 1f;
        maxAscentMultiplier = 2.6f;
        constantAscend = 0.125f;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.SoulofFlight, 20);
        recipe.AddIngredient(ItemID.SoulofMight, 6);
        recipe.AddIngredient(ItemID.SoulofSight, 6);
        recipe.AddIngredient(ItemID.SoulofFright, 6);
        recipe.AddTile(TileID.BloodMoonMonolith);
        recipe.Register();
    }
}