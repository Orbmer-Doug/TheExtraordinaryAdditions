using CalamityMod.Items.Materials;
using CalamityMod.Tiles.Furniture.CraftingStations;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Cooldowns;
using TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Early;
using TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Late;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Late;

[AutoloadEquip(EquipType.Head)]
public class AbsoluteGreathelm : ModItem, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.AbsoluteGreathelm);
    public static int HeadSlotID
    {
        get;
        private set;
    }
    public override void SetStaticDefaults()
    {
        HeadSlotID = Item.headSlot;
    }

    public override void SetDefaults()
    {
        Item.width = 24;
        Item.height = 26;
        Item.defense = 40;
        Item.rare = ModContent.RarityType<UniqueRarity>();
        Item.value = AdditionsGlobalItem.UniqueRarityPrice;
    }

    public override bool IsArmorSet(Item head, Item body, Item legs)
    {
        if (body.type == ModContent.ItemType<AbsoluteCoreplate>())
        {
            return legs.type == ModContent.ItemType<AbsoluteGreaves>();
        }
        return false;
    }

    public override void UpdateArmorSet(Player player)
    {
        player.GetAttackSpeed<MeleeDamageClass>() += 0.15f;

        player.ignoreWater = true;
        player.aggro += 1500;
        player.manaCost *= .75f;
        player.statManaMax2 += 90;
        player.maxMinions += 4;
        player.maxTurrets += 2;

        string hotkey = AdditionsKeybinds.SetBonusHotKey.TooltipHotkeyString();
        player.setBonus = this.GetLocalization("SetBonus").Format(hotkey);

        if (AdditionsKeybinds.SetBonusHotKey.JustPressed && !CalUtils.HasCooldown(player, AbsoluteCooldown.ID))
        {
            player.NewPlayerProj(player.Center, Vector2.Zero, ModContent.ProjectileType<WhiteVoid>(), (int)player.GetTotalDamage<GenericDamageClass>().ApplyTo(7000), 0f, Main.myPlayer);
            CalUtils.AddCooldown(player, AbsoluteCooldown.ID, SecondsToFrames(15));
        }
    }

    public override void UpdateEquip(Player player)
    {
        Lighting.AddLight(player.Center, Color.AntiqueWhite.ToVector3() * 1.5f);

        ref StatModifier damage = ref player.GetDamage<GenericDamageClass>();
        damage += 0.35f;
        player.GetCritChance<GenericDamageClass>() += 25f;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.CrimsonHelmet, 1);
        recipe.AddIngredient(ModContent.ItemType<VoltHelmet>(), 1);
        recipe.AddIngredient(ModContent.ItemType<SpecteriteMask>(), 1);
        recipe.AddIngredient(ModContent.ItemType<BlueTopHat>(), 1);
        recipe.AddIngredient(ModContent.ItemType<TremorGreathelm>(), 1);
        recipe.AddIngredient(ItemID.SolarFlareHelmet, 1);
        recipe.AddIngredient(ModContent.ItemType<CoreofCalamity>(), 2);
        recipe.AddIngredient(ModContent.ItemType<GalacticaSingularity>(), 4);
        recipe.AddIngredient(ModContent.ItemType<LifeAlloy>(), 5);
        recipe.AddIngredient(ModContent.ItemType<RuinousSoul>(), 5);
        recipe.AddIngredient(ModContent.ItemType<AscendantSpiritEssence>(), 3);
        recipe.AddIngredient(ModContent.ItemType<AuricBar>(), 10);
        recipe.AddTile(ModContent.TileType<CosmicAnvil>());
        recipe.Register();
    }
}