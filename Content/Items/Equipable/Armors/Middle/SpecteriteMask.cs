using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Middle;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Middle;

[AutoloadEquip(EquipType.Head)]
public class SpecteriteMask : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.SpecteriteMask);

    public override void SetStaticDefaults()
    {
        ArmorIDs.Head.Sets.DrawHead[Item.headSlot] = false; // Don't draw the head at all. Used by Space Creature Mask
        ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = false; // Draw hair as if a hat was covering the top. Used by Wizards Hat
        ArmorIDs.Head.Sets.DrawFullHair[Item.headSlot] = false; // Draw all hair as normal. Used by Mime Mask, Sunglasses
        ArmorIDs.Head.Sets.DrawsBackHairWithoutHeadgear[Item.headSlot] = false;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(85, 77, 255));
    }

    public override void SetDefaults()
    {
        Item.width = 26;
        Item.height = 20;
        Item.value = AdditionsGlobalItem.RarityYellowBuyPrice;
        Item.rare = ItemRarityID.Yellow;
        Item.defense = 7;
    }

    public override void UpdateArmorSet(Player player)
    {
        string hotkey = AdditionsKeybinds.SetBonusHotKey.TooltipHotkeyString();
        player.setBonus = this.GetLocalization("SetBonus").Format(hotkey);

        int type = ModContent.ProjectileType<ShroomiteDash>();
        if (AdditionsKeybinds.SetBonusHotKey.Current && player.CountOwnerProjectiles(type) <= 0 && player.whoAmI == Main.myPlayer)
            player.NewPlayerProj(player.Center, Vector2.Zero, type, 500, 10f, player.whoAmI);

        player.GetModPlayer<SpecteritePlayer>().Equipped = true;
    }

    public override bool IsArmorSet(Item head, Item body, Item legs)
    {
        if (body.type == ModContent.ItemType<SpecteriteChestPiece>())
        {
            return legs.type == ModContent.ItemType<SpecteriteGreaves>();
        }
        return false;
    }

    public override void UpdateEquip(Player player)
    {
        player.GetCritChance(DamageClass.Ranged) += 18f;
        player.GetDamage(DamageClass.Ranged) += 0.18f;
        player.buffImmune[BuffID.Blackout] = true;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.ShroomiteMask, 1);
        recipe.AddIngredient(ItemID.Ectoplasm, 15);
        recipe.AddIngredient(ItemID.Lens, 5);
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.AddTile(TileID.AdamantiteForge);
        recipe.Register();

        Recipe recipe2 = CreateRecipe();
        recipe2.AddIngredient(ItemID.ShroomiteHelmet, 1);
        recipe2.AddIngredient(ItemID.Ectoplasm, 15);
        recipe2.AddIngredient(ItemID.Lens, 5);
        recipe2.AddTile(TileID.MythrilAnvil);
        recipe2.AddTile(TileID.AdamantiteForge);
        recipe2.Register();

        Recipe recipe3 = CreateRecipe();
        recipe3.AddIngredient(ItemID.ShroomiteHeadgear, 1);
        recipe3.AddIngredient(ItemID.Ectoplasm, 15);
        recipe3.AddIngredient(ItemID.Lens, 5);
        recipe3.AddTile(TileID.MythrilAnvil);
        recipe3.AddTile(TileID.AdamantiteForge);
        recipe3.Register();
    }
}

public sealed class SpecteritePlayer : ModPlayer
{
    public bool Equipped;
    public override void ResetEffects() => Equipped = false;

    public override void PostUpdate()
    {
        if (!Equipped)
            return;

        bool active = Player.active && !Player.DeadOrGhost;
        if (active && Player.velocity.Length() != 0 && !Player.mount.Active)
        {
            Vector2 randPos = Player.RotatedRelativePoint(Player.MountedCenter) + PolarVector(Player.height / 2 * Player.gravDir, Player.fullRotation + MathHelper.PiOver2) + PolarVector(Main.rand.NextFloat(-Player.width / 2, Player.width / 2), Player.fullRotation);
            Vector2 vel = -Player.velocity.RotatedByRandom(.18f) * Main.rand.NextFloat(.3f, .5f);
            ParticleRegistry.SpawnMistParticle(randPos, vel, Main.rand.NextFloat(.5f, .8f), new(85, 89, 225), new(8, 35, 97), Main.rand.NextByte(98, 182), .05f);
        }
    }
}