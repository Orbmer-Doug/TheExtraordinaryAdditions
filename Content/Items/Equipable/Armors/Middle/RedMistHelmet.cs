using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Content.Cooldowns;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Middle;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Middle;

[AutoloadEquip(EquipType.Head)]
public class RedMistHelmet : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.RedMistHelmet);
    public override void SetStaticDefaults()
    {
        ArmorIDs.Head.Sets.DrawHead[Item.headSlot] = false; // Don't draw the head at all. Used by Space Creature Mask
        ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = false; // Draw hair as if a hat was covering the top. Used by Wizards Hat
        ArmorIDs.Head.Sets.DrawFullHair[Item.headSlot] = false; // Draw all hair as normal. Used by Mime Mask, Sunglasses
        ArmorIDs.Head.Sets.DrawsBackHairWithoutHeadgear[Item.headSlot] = false;
    }
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(235, 64, 52));
    }

    public override void SetDefaults()
    {
        Item.width = 18;
        Item.height = 18;
        Item.value = AdditionsGlobalItem.RarityCyanBuyPrice;
        Item.rare = ModContent.RarityType<BloodWroughtRarity>();
        Item.defense = 23;
    }
    public const int AuraDamage = 300;
    public override void UpdateArmorSet(Player player)
    {
        string hotkey = AdditionsKeybinds.SetBonusHotKey.TooltipHotkeyString();
        player.setBonus = this.GetLocalization("SetBonus").Format(hotkey);

        int type = ModContent.ProjectileType<StygainAura>();
        if (AdditionsKeybinds.SetBonusHotKey.JustPressed && player.ownedProjectileCounts[type] <= 0 && !CalUtils.HasCooldown(player, RedMistCooldown.ID) && player.whoAmI == Main.myPlayer)
        {
            AdditionsSound.etherealThrow.Play(player.Center, 1f, 0f, .1f);
            player.NewPlayerProj(player.Center, Vector2.Zero, type, AuraDamage, 1f, player.whoAmI);
            CalUtils.AddCooldown(player, RedMistCooldown.ID, StygainAura.CooldownTime);
        }

        player.Additions().RedMist = true;
    }

    public override bool IsArmorSet(Item head, Item body, Item legs)
    {
        if (body.type == Mod.Find<ModItem>("MimicryChestplate").Type)
        {
            return legs.type == Mod.Find<ModItem>("MimicryLeggings").Type;
        }
        return false;
    }

    public override void UpdateEquip(Player player)
    {
        player.GetDamage(DamageClass.Melee) += 0.12f;
        player.GetDamage(DamageClass.Ranged) += 0.12f;
        player.GetAttackSpeed(DamageClass.Melee) += .08f;
        player.moveSpeed += 0.1f;
        player.statLifeMax2 += 20;
    }
}