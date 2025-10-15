using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Debuff;
using TheExtraordinaryAdditions.Content.Items.Materials.Early;
using TheExtraordinaryAdditions.Content.NPCs.Hostile.Lightning;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Middle;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Early;

[AutoloadEquip(EquipType.Head)]
public class VoltHelmet : ModItem, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.VoltHelmet);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(231, 191, 255));
    }

    public override void SetDefaults()
    {
        Item.width = 24;
        Item.height = 26;
        Item.value = AdditionsGlobalItem.RarityOrangeBuyPrice;
        Item.defense = 4;
        Item.rare = ItemRarityID.Orange;
    }

    public override bool IsArmorSet(Item head, Item body, Item legs)
    {
        if (body.type == ModContent.ItemType<VoltChestplate>())
        {
            return legs.type == ModContent.ItemType<VoltGrieves>();
        }
        return false;
    }

    public override void UpdateArmorSet(Player player)
    {
        player.GetAttackSpeed<MeleeDamageClass>() += 0.06f;
        string hotkey = AdditionsKeybinds.SetBonusHotKey.TooltipHotkeyString();
        player.setBonus = this.GetLocalization("SetBonus").Format(hotkey);

        if (player.whoAmI == Main.myPlayer && AdditionsKeybinds.SetBonusHotKey.JustPressed && !player.HasBuff(ModContent.BuffType<FulminationCooldown>()))
        {
            player.AddBuff(ModContent.BuffType<FulminationCooldown>(), SecondsToFrames(15));
            AdditionsSound.LightningStrike.Play(player.Center, 1f, 0f, .2f);
            Projectile bolt = Main.projectile[player.NewPlayerProj(player.Center, player.Center.SafeDirectionTo(player.Additions().mouseWorld) * 10f, ModContent.ProjectileType<LightningVolt>(), 100, 1f, player.whoAmI)];
            bolt.friendly = true;
            bolt.hostile = false;
            bolt.penetrate = 6;
            bolt.netUpdate = true;

            for (int i = 0; i < 20; i++)
                ParticleRegistry.SpawnSparkParticle(player.RandAreaInEntity(), bolt.velocity * Main.rand.NextFloat(.4f, 1.1f), Main.rand.Next(18, 22), Main.rand.NextFloat(.6f, .8f), Color.Purple);
        }
    }

    public override void UpdateEquip(Player player)
    {
        Lighting.AddLight(player.Center, new Color(206, 125, 255).ToVector3() * .34f);

        player.statManaMax2 += 20;
        player.manaCost *= .9f;
        ref StatModifier damage = ref player.GetDamage<MeleeDamageClass>();
        damage += 0.05f;
        player.GetCritChance<MeleeDamageClass>() += 5f;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<ShockCatalyst>(), 10);
        recipe.AddTile(TileID.Anvils);
        recipe.Register();
    }
}
