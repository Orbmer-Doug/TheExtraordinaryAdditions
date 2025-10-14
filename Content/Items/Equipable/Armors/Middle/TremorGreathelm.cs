using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Cooldowns;
using TheExtraordinaryAdditions.Content.Items.Materials.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Middle;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Middle;

[AutoloadEquip(EquipType.Head)]
public class TremorGreathelm : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TremorGreathelm);

    public override void SetStaticDefaults()
    {
        ArmorIDs.Head.Sets.DrawHead[Item.headSlot] = false;
        ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = false;
        ArmorIDs.Head.Sets.DrawFullHair[Item.headSlot] = false;
        ArmorIDs.Head.Sets.DrawsBackHairWithoutHeadgear[Item.headSlot] = false;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(194, 194, 194));
    }

    public override void SetDefaults()
    {
        Item.width = 34;
        Item.height = 28;
        Item.value = AdditionsGlobalItem.RarityCyanBuyPrice;
        Item.rare = ItemRarityID.Cyan;
        Item.defense = 16;
    }

    public override void UpdateArmorSet(Player player)
    {
        string hotkey = AdditionsKeybinds.SetBonusHotKey.TooltipHotkeyString();
        player.setBonus = this.GetLocalization("SetBonus").Format(hotkey);

        if (AdditionsKeybinds.SetBonusHotKey.JustPressed && player.whoAmI == Main.myPlayer && !CalUtils.HasCooldown(player, TremorCooldown.ID))
        {
            for (int i = 0; i < 3; i++)
            {
                Vector2 vel = (MathHelper.TwoPi * i / Main.rand.Next(3, 5) + RandomRotation()).ToRotationVector2() * 16f;
                int p = player.NewPlayerProj(player.Center, vel, ModContent.ProjectileType<TremorSpike>(), 120, 1f, player.whoAmI);

                SoundID.NPCHit42.Play(player.Center, 1f, 0f, .1f);
            }
            CalUtils.AddCooldown(player, TremorCooldown.ID, SecondsToFrames(5));
        }

        player.aggro += 400;
    }

    public override bool IsArmorSet(Item head, Item body, Item legs)
    {
        if (body.type == ModContent.ItemType<TremorPlating>())
        {
            return legs.type == ModContent.ItemType<TremorSheathe>();
        }
        return false;
    }

    public override void UpdateEquip(Player player)
    {
        player.statManaMax2 += 65;
        player.manaCost *= .82f;
        player.GetCritChance(DamageClass.Magic) += 12f;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<TremorAlloy>(), 6);
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.AddTile(TileID.HeavyWorkBench);
        recipe.Register();
    }
}